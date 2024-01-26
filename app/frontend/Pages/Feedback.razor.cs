// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Feedback : IDisposable
{
    private Task _getFeedbackTask = null!;
    private bool _isLoadingDocuments = false;

    // Store a cancelation token that will be used to cancel if the user disposes of this component.
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<FeedbackResponse> _feedback = [];

    [Inject]
    public required ApiClient Client { get; set; }


    [Inject]
    public required ILogger<Docs> Logger { get; set; }


    protected override void OnInitialized()
    {
        // Instead of awaiting this async enumerable here, let's capture it in a task
        // and start it in the background. This way, we can await it in the UI.
        _getFeedbackTask = GetFeedbackAsync();
    }

    private async Task GetFeedbackAsync()
    {
        _isLoadingDocuments = true;

        try
        {
            var feedback = await Client.GetFeedbackAsync(_cancellationTokenSource.Token).ToListAsync();
            foreach (var item in feedback)
            {
                _feedback.Add(item);
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
