// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Chat
{
    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private bool _filtersSelected = false;
    private readonly Dictionary<UserQuestion, ApproachResponse?> _questionAndAnswerMap = [];

    private bool _gPT4ON = false;
    private Guid _chatId = Guid.NewGuid();

    [Inject] public required ISessionStorageService SessionStorage { get; set; }

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
            var history = _questionAndAnswerMap
                .Where(x => x.Value is not null)
                .Select(x => new ChatTurn(x.Key.Question, x.Value!.Answer))
                .ToList();

            history.Add(new ChatTurn(_userQuestion));

            var options = new Dictionary<string, string>();
            options["GPT4ENABLED"] = _gPT4ON.ToString();

            var request = new ChatRequest(_chatId,Guid.NewGuid(),[.. history], options, Settings.Approach, Settings.Overrides);
            var result = await ApiClient.ChatConversationAsync(request);

            _questionAndAnswerMap[_currentQuestion] = result.Response;
            if (result.IsSuccessful)
            {
                _userQuestion = "";
                _currentQuestion = default;
            }
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

    //private string _selectedModel = "";
    //private List<string> _models = Filters.GetModels().ToList();

    //private string _selectedYear = "";
    //private List<string> _years = Filters.GetYears().ToList();

    //private void OnSelectedFilterChange()
    //{
    //    //var c = new FilterCriteria(_selectedModel, _selectedPowerSystem, ResolveYear(_selectedYear));
    //    //_models = VehicleDefinitionFilter.GetAvailableModels(c).ToList();
    //    //_years = VehicleDefinitionFilter.GetAvailableYears(c).Select(x => x.ToString()).ToList();
    //    EvaluateFilters();
    //}

    //private void OnClearFilters()
    //{
    //    _selectedModel = "";
    //    _models = Filters.GetModels().ToList();

    //    _selectedYear = "";
    //    _years = Filters.GetYears().ToList();

    //    EvaluateFilters();
    //}

    //private void EvaluateFilters()
    //{
    //    if(_selectedModel != "")
    //    {
    //        _filtersSelected = true;
    //    }
    //    _filtersSelected = false;
    //}

    private static int? ResolveYear(string year)
    {
        if(string.IsNullOrWhiteSpace(year) || year == "Any")
        {
            return null;
        }
        return Convert.ToInt32(year);
    }
}
