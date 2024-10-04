// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Data;
using Blazor.Serialization.Extensions;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ClientApp.Pages;

public sealed partial class Chat
{
    //private const long MaxIndividualFileSize = 1_024L * 1_024;

    private MudForm _form = null!;

    // User input and selections
    private string _userQuestion = "";
    private List<FileSummary> _files = new();
    private List<DocumentSummary> _userDocuments = new();
    private string _selectedDocument = "";
    private UserQuestion _currentQuestion;

    private bool _filtersSelected = false;

    private string _selectedProfile = "";
    private List<ProfileSummary> _profiles = new();
    private ProfileSummary? _selectedProfileSummary = null;
    private ProfileSummary? _userUploadProfileSummary = null;

    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private bool _supportsFileUpload = false;

    private readonly Dictionary<UserQuestion, ApproachResponse?> _questionAndAnswerMap = [];

    private bool _gPT4ON = false;
    private Guid _chatId = Guid.NewGuid();

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    [Inject] public required HttpClient HttpClient { get; set; }
    [Inject] public required ApiClient ApiClient { get; set; }
    [Inject] public required IJSRuntime JSRuntime { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    [CascadingParameter(Name = nameof(Settings))] public required RequestSettingsOverrides Settings { get; set; }
    [CascadingParameter(Name = nameof(IsReversed))] public required bool IsReversed { get; set; }

    public bool _showProfiles { get; set; }
    public bool _showDocumentUpload { get; set; }
    public bool _showPictureUpload { get; set; }
    [SupplyParameterFromQuery(Name = "cid")] public string? ArchivedChatId { get; set; }

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

        StateHasChanged();

        if (AppConfiguration.ShowFileUploadSelection)
        {
            var userDocuments = await ApiClient.GetUserDocumentsAsync();
            _userDocuments = userDocuments.ToList();
        }

        if (!string.IsNullOrEmpty(ArchivedChatId))
        {
            await LoadArchivedChatAsync(_cancellationTokenSource.Token, ArchivedChatId);
        }
        EvaluateOptions();
    }


    private void OnProfileClick(string selection)
    {
        _selectedProfile = selection;
        _selectedProfileSummary = _profiles.FirstOrDefault(x => x.Name == selection);
        _supportsFileUpload = _selectedProfileSummary.Approach == ProfileApproach.Chat;
        OnClearChat();
    }
    private void OnFileUpload(FileSummary fileSummary)
    {
        _files.Add(fileSummary);
    }
    private void OnModelSelection(bool isPremium)
    {
        _gPT4ON = isPremium;
    }

    private Task OnAskQuestionAsync(string userInput)
    {
        _userQuestion = userInput;
        return OnAskClickedAsync();
    }

    private async Task OnRetryQuestionAsync()
    {
        _questionAndAnswerMap.Remove(_currentQuestion);
        await OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        Console.WriteLine($"OnAskClickedAsync: {_userQuestion}");

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
            var history = _questionAndAnswerMap.Where(x => x.Value is not null)
                .Select(x => new ChatTurn(x.Key.Question, x.Value.Answer))
                .ToList();
            history.Add(new ChatTurn(_userQuestion.Trim()));

            var options = new Dictionary<string, string>
            {
                ["GPT4ENABLED"] = _gPT4ON.ToString(),
                ["PROFILE"] = _selectedProfile
            };

            if (_userUploadProfileSummary != null && SelectedDocuments.Any())
            {
                options["PROFILE"] = _userUploadProfileSummary.Name;
            }

            var request = new ChatRequest(
                _chatId,
                Guid.NewGuid(),
                history.ToArray(),
                SelectedDocuments.Select(x => x.Name),
                _files,
                options,
                Approach.ReadRetrieveRead,
                null);

            //check access token expiration to see if access token refresh is needed
            string? accessTokenExpiration = await GetAuthMeFieldAsync("expires_on");

            var expiresOnDateTime = DateTimeOffset.Parse(accessTokenExpiration);
            if (expiresOnDateTime < DateTimeOffset.UtcNow.AddMinutes(5))
            {
                await HttpClient.GetAsync(".auth/refresh");
            }

            // get access token
            var accessToken = await GetAuthMeFieldAsync("access_token");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat/streaming")
            {
                Headers = {
                    {
                        "Accept", "application/json"
                    },
                    {
                        "X-MS-TOKEN-AAD-ACCESS-TOKEN", accessToken
                    }
                },
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };
            httpRequest.SetBrowserResponseStreamingEnabled(true);

            using HttpResponseMessage response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using Stream responseStream = await response.Content.ReadAsStreamAsync();
            var responseBuffer = new StringBuilder();

            await foreach (ChatChunkResponse chunk in JsonSerializer.DeserializeAsyncEnumerable<ChatChunkResponse>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultBufferSize = 32 }))
            {
                if (chunk == null)
                {
                    continue;
                }

                responseBuffer.Append(chunk.Text);
                var responseText = responseBuffer.ToString();

                if (chunk.FinalResult != null)
                {
                    _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(responseText, chunk.FinalResult.CitationBaseUrl, chunk.FinalResult.Context);
                    _isReceivingResponse = false;
                    _userQuestion = "";
                    _currentQuestion = default;
                }
                else
                {
                    _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(responseText, null, null);
                    _isReceivingResponse = true;
                }

                StateHasChanged();
            }
        }
        catch (HttpRequestException ex)
        {
            _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(string.Empty, null, null, "Error: Unable to get a response from the server.");
        }
        catch (JsonException ex)
        {
            _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(string.Empty, null, null, "Error: Failed to parse the server response.");
        }
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"OnAskClickedAsync: {ex}");
        //    _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(string.Empty, null, null, "An unexpected error occurred.");
        //}
        finally
        {
            _isReceivingResponse = false;
            StateHasChanged();
        }
    }

    private async Task<string?> GetAuthMeFieldAsync(string field)
    {
        var httpResponse = await HttpClient.GetAsync(".auth/me");
        httpResponse.EnsureSuccessStatusCode();

        var httpResponseContent = await httpResponse.Content.ReadAsStringAsync();
        var httpResponseContentJson = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(httpResponseContent);
        var httpResponseField = httpResponseContentJson?.FirstOrDefault()?[field]?.ToString();
        return httpResponseField;
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
        _files.Clear();

        EvaluateOptions();
    }

    private void EvaluateOptions()
    {
        _showProfiles = true;
        _showDocumentUpload = true;
        _showPictureUpload = true;
        if (_profiles.Count() < 1 || !string.IsNullOrEmpty(_selectedDocument))
        {
            _showProfiles = false;
        }

        if (!AppConfiguration.ShowFileUploadSelection)
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
            var ar = new ApproachResponse(chatMessage.Answer, chatMessage.ProfileId, new ResponseContext(chatMessage.Profile, chatMessage.DataPoints, Array.Empty<ThoughtRecord>(), Guid.Empty, Guid.Empty, null));
            _questionAndAnswerMap[new UserQuestion(chatMessage.Prompt, chatMessage.Timestamp.UtcDateTime)] = ar;
        }
        Navigation.NavigateTo(string.Empty, forceLoad: false);
    }
}
