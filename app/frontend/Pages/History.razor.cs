// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class History : IDisposable
{
    private Task _getFeedbackTask = null!;
    private bool _isLoadingDocuments = false;
    public bool _showFeeback { get; set; } = false;

    // Store a cancelation token that will be used to cancel if the user disposes of this component.
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<ChatHistoryResponse> _feedback = [];

    [Inject]
    public required ApiClient Client { get; set; }


    [Inject]
    public required ILogger<History> Logger { get; set; }


    protected override void OnInitialized()
    {
        // Instead of awaiting this async enumerable here, let's capture it in a task
        // and start it in the background. This way, we can await it in the UI.
        _getFeedbackTask = GetFeedbackAsync();
    }
    private async Task OnChangeFeedbackFilerAsync()
    {
        await GetFeedbackAsync();
    }

    private async Task GetFeedbackAsync()
    {
        _isLoadingDocuments = true;
        _feedback.Clear();

        try
        {
            if (_showFeeback)
            {
                var feedback = await Client.GetFeedbackAsync(_cancellationTokenSource.Token).ToListAsync();
                foreach (var item in feedback)
                {
                    _feedback.Add(item);
                }
            }
            else
            {
                var feedback = await Client.GetHistoryAsync(_cancellationTokenSource.Token).ToListAsync();
                foreach (var item in feedback)
                {
                    _feedback.Add(item);
                }
            }

        }
        finally
        {
            _isLoadingDocuments = false;
            StateHasChanged();
        }
    }

    public void Dispose() => _cancellationTokenSource.Cancel();
}
