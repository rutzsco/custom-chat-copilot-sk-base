﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Data;
using ClientApp.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static MudBlazor.CategoryTypes;

namespace ClientApp.Pages;

public sealed partial class Chat
{
    private const long MaxIndividualFileSize = 1_024L * 1_024;
    private IList<IBrowserFile> _files = new List<IBrowserFile>();
    private MudForm _form = null!;
    private MudFileUpload<IReadOnlyList<IBrowserFile>> _fileUpload = null!;
    private bool _showFileUpload = false;

    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private bool _filtersSelected = false;

    private string _selectedProfile = "";
    private List<ProfileSummary> _profiles = new();
    private ProfileSummary? _selectedProfileSummary = null;
    private ProfileSummary? _userUploadProfileSummary = null;

    private List<DocumentSummary> _userDocuments = new();
    private string _selectedDocument = "";

    private readonly Dictionary<UserQuestion, ApproachResponse?> _questionAndAnswerMap = [];

    private bool _gPT4ON = false;
    private Guid _chatId = Guid.NewGuid();

    private string _imageUrl = "";
    private string _imageFileName = "";

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    [Inject] public required HttpClient HttpClient { get; set; }

    [Inject] public required ApiClient ApiClient { get; set; }

    [Inject]
    public required IJSRuntime JSRuntime { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; }

    [CascadingParameter(Name = nameof(IsReversed))]
    public required bool IsReversed { get; set; }


    public bool _showProfiles { get; set; }
    public bool _showDocumentUpload { get; set; }
    public bool _showPictureUpload { get; set; }

    [SupplyParameterFromQuery(Name = "cid")]
    public string? ArchivedChatId { get; set; }

    private HashSet<DocumentSummary> _selectedDocuments = new HashSet<DocumentSummary>();

    private HashSet<DocumentSummary> SelectedDocuments
    {
        get => _selectedDocuments;
        set
        {
            _selectedDocuments = value;
            OnSelectedDocumentsChanged();    
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var user = await ApiClient.GetUserAsync();
        _profiles = user.Profiles.Where(x => x.Approach != ProfileApproach.UserDocumentChat).ToList();
        _userUploadProfileSummary = user.Profiles.FirstOrDefault(x => x.Approach == ProfileApproach.UserDocumentChat);
        _selectedProfile = _profiles.First().Name;
        _selectedProfileSummary = _profiles.First();

        //StateHasChanged();

        if (AppConfiguration.ShowFileUploadSelection)
        {
            var userDocuments = await ApiClient.GetUserDocumentsAsync();
            _userDocuments = userDocuments.ToList();
        }

        if (!string.IsNullOrEmpty(ArchivedChatId))
        {
            await LoadArchivedChatAsync(_cancellationTokenSource.Token,ArchivedChatId);
        }
        EvaluateOptions();
    }

    private async Task UploadFilesAsync(IBrowserFile file)
    {       
        _files.Add(file);
        var buffer = new byte[file.Size];
        await file.OpenReadStream(2048000).ReadAsync(buffer);
        var imageContent = Convert.ToBase64String(buffer);
        _imageUrl = $"data:{file.ContentType};base64,{imageContent}";
        _imageFileName = file.Name;
        EvaluateOptions();
    }

    private void OnProfileClick(string selection)
    {
        _selectedProfile = selection;
        _selectedProfileSummary = _profiles.FirstOrDefault(x => x.Name == selection);
        OnClearChat();
    }

    private Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        return OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        if (string.IsNullOrWhiteSpace(_userQuestion))
        {
            return;
        }
        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;
        _currentQuestion = new(_userQuestion, DateTime.Now);
        _questionAndAnswerMap[_currentQuestion] = null;

        try
        {
            var history = _questionAndAnswerMap.Where(x => x.Value is not null).Select(x => new ChatTurn(x.Key.Question, x.Value.Answer)).ToList();
            history.Add(new ChatTurn(_userQuestion.Trim()));

            var options = new Dictionary<string, string>();
            options["GPT4ENABLED"] = _gPT4ON.ToString();

            // Set profile, override if user selected uploaded document
            options["PROFILE"] = _selectedProfile;
            if (_userUploadProfileSummary != null && SelectedDocuments.Any())
            {
                options["PROFILE"] = _userUploadProfileSummary.Name;
            }

            if (!string.IsNullOrEmpty(_imageUrl))
            {
                options["IMAGECONTENT"] = _imageUrl;
            }

            var request = new ChatRequest(_chatId, Guid.NewGuid(), [.. history], SelectedDocuments.Select(x => x.Name), options, Settings.Approach, Settings.Overrides);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat/streaming");
            httpRequest.Headers.Add("Accept", "application/json");
            httpRequest.SetBrowserResponseStreamingEnabled(true);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            using Stream responseStream = await response.Content.ReadAsStreamAsync();
            var responseBuffer = new StringBuilder();
            await foreach (ChatChunkResponse chunk in JsonSerializer.DeserializeAsyncEnumerable<ChatChunkResponse>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultBufferSize = 128 }))
            {
                if (chunk == null)
                {
                    continue;
                }

                responseBuffer.Append(chunk.Text);
                var restponseText = responseBuffer.ToString();

                var isComplete = chunk.FinalResult != null;            
                if (chunk.FinalResult != null)
                {
                    var ar = new ApproachResponse(restponseText, chunk.FinalResult.CitationBaseUrl, chunk.FinalResult.Context);
                    _questionAndAnswerMap[_currentQuestion] = ar;
                }
                else
                {
                    _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(restponseText, null, null);
                }

                _isReceivingResponse = isComplete is false;
                if (isComplete)
                {
                    _userQuestion = "";
                    _currentQuestion = default;
                }

                StateHasChanged();
            }
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }
    private void OnSelectedDocumentsChanged()
    {
        Console.WriteLine($"SelectedDocuments: {SelectedDocuments.Count()}");
        if (SelectedDocuments.Any())
        {
            if (SelectedDocuments.Count() == 1)
            {
                _selectedDocument = $"{SelectedDocuments.First().Name}";
            }
            else
            {
                _selectedDocument = $"{SelectedDocuments.Count()} - Documents selected";
            }
        }
        else
        {
            _selectedDocument = string.Empty;
        }

        OnClearChatDocuumentSelection();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Console.WriteLine($"OnAfterRenderAsync: _isReceivingResponse - {_isReceivingResponse}");
        await JS.InvokeVoidAsync("scrollToBottom", "answerSection");
        await JS.InvokeVoidAsync("highlight");
        if (!_isReceivingResponse)
        {
            await JS.InvokeVoidAsync("renderMathJax");
        }
    }

    private void OnClearChatDocuumentSelection()
    {
        _userQuestion = _lastReferenceQuestion = "";
        _currentQuestion = default;
        _questionAndAnswerMap.Clear();
        _chatId = Guid.NewGuid();
        EvaluateOptions();
    }

    private void OnClearChat()
    {
        _userQuestion = _lastReferenceQuestion = "";
        _currentQuestion = default;
        _questionAndAnswerMap.Clear();
        _selectedDocument = "";
        SelectedDocuments.Clear();
        _chatId = Guid.NewGuid();
        _imageUrl = string.Empty;
        EvaluateOptions();
    }

    private void EvaluateOptions()
    {
        _showProfiles = true;
        _showDocumentUpload = true;
        _showPictureUpload = true;
        if (_profiles.Count() < 1 || !string.IsNullOrEmpty(_selectedDocument) || !string.IsNullOrEmpty(_imageUrl))
        {
            _showProfiles = false;
        }

        if (!AppConfiguration.ShowFileUploadSelection || !string.IsNullOrEmpty(_imageUrl))
            _showDocumentUpload = false;

        if (_selectedProfileSummary.Approach != ProfileApproach.Chat || !string.IsNullOrEmpty(_selectedDocument))
        {
            _showPictureUpload = false;
        }
    }

    private async Task LoadArchivedChatAsync(CancellationToken cancellationToken, string chatId)
    {
        var chatMessages = await ApiClient.GetChatHistorySessionAsync(cancellationToken, chatId).ToListAsync();
        var profile = chatMessages.First().Profile;
        _selectedProfile = profile;
        _selectedProfileSummary = _profiles.FirstOrDefault(x => x.Name == profile);
        _chatId = Guid.Parse(chatId);

        foreach (var chatMessage in chatMessages.OrderBy(x => x.Timestamp))
        {
            var ar = new ApproachResponse(chatMessage.Answer, chatMessage.ProfileId, new ResponseContext(chatMessage.Profile,chatMessage.DataPoints, Array.Empty<ThoughtRecord>(), Guid.Empty, Guid.Empty, null));
            _questionAndAnswerMap[new UserQuestion(chatMessage.Prompt, chatMessage.Timestamp.UtcDateTime)] = ar;
        }
        Navigation.NavigateTo(string.Empty, forceLoad: false);
    }
}
