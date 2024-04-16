// Copyright (c) Microsoft. All rights reserved.

using ClientApp.Models;

namespace ClientApp.Pages;

public sealed partial class SimpleChat
{
    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private bool _filtersSelected = false;

    private readonly Dictionary<UserQuestion, ApproachResponse?> _questionAndAnswerMap = [];

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
    .ConfigureNewLine("\n")
    .UseAdvancedExtensions()
    .UseEmojiAndSmiley()
    .UseSoftlineBreakAsHardlineBreak()
    .Build();

    private bool _gPT4ON = false;
    private Guid _chatId = Guid.NewGuid();

    [Inject] public required OpenAIPromptQueue OpenAIPrompts { get; set; }

    [Inject] public required ApiClient ApiClient { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; }

    [CascadingParameter(Name = nameof(IsReversed))]
    public required bool IsReversed { get; set; }

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
            var history = _questionAndAnswerMap.Where(x => x.Value is not null)
                .Select(x => new ChatTurn(x.Key.Question, x.Value.Answer))
                .ToList();

            history.Add(new ChatTurn(_userQuestion));

            var options = new Dictionary<string, string>();
            options["GPT4ENABLED"] = _gPT4ON.ToString();

            var request = new ChatRequest(_chatId,Guid.NewGuid(),[.. history], options, Settings.Approach, Settings.Overrides);
            OpenAIPrompts.EnqueueSimple(request,
                async (PromptResponse response) => await InvokeAsync(() =>
                {
                    (string prompt, string responseText, bool isComplete, ApproachResponse? result) = response;
                    var html = Markdown.ToHtml(responseText, _pipeline);

                    if (response.Result != null)
                    {
                        var ar = new ApproachResponse(html, response.Result.Thoughts, response.Result.DataPoints, response.Result.CitationBaseUrl, response.Result.MessageId, response.Result.ChatId, response.Result.Diagnostics, response.Result.Context);
                        _questionAndAnswerMap[_currentQuestion] = ar;
                    }
                    else
                    {
                        _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(html, null, null, null, Guid.Empty, Guid.Empty, null, null);
                    }
      
                    _isReceivingResponse = isComplete is false;
                    if (isComplete)
                    {
                        _userQuestion = "";
                        _currentQuestion = default;
                    }

                    StateHasChanged();
                })
            );
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("scrollToBottom", "answerSection");
    }

    private void OnClearChat()
    {
        _userQuestion = _lastReferenceQuestion = "";
        _currentQuestion = default;
        _questionAndAnswerMap.Clear();
        _chatId = Guid.NewGuid();
    }
}
