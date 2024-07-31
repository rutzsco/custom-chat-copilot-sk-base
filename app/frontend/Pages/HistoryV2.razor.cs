// Copyright (c) Microsoft. All rights reserved.

using static MudBlazor.Colors;
using System;
namespace ClientApp.Pages;

public sealed partial class HistoryV2 : IDisposable
{
    private Task _getFeedbackTask = null!;
    private bool _isLoadingDocuments = false;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<ChatSessionModel> _chatSessions = [];

    [Inject]
    public required ApiClient Client { get; set; }


    [Inject]
    public required ILogger<History> Logger { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }


    protected override void OnInitialized()
    {
        // Instead of awaiting this async enumerable here, let's capture it in a task
        // and start it in the background. This way, we can await it in the UI.
        _getFeedbackTask = GetChatSessionsAsync();
    }
    private async Task OnChangeFeedbackFilerAsync()
    {
        await GetChatSessionsAsync();
    }

    private async Task GetChatSessionsAsync()
    {
        _isLoadingDocuments = true;
        _chatSessions.Clear();

        try
        {
            var history = await Client.GetHistoryV2Async(_cancellationTokenSource.Token).ToListAsync();
            foreach (var item in history)
            {
                _chatSessions.Add(item);
            }
        }
        finally
        {
            _isLoadingDocuments = false;
            StateHasChanged();
        }
    }

    private void ViewChat(string chatId)
    {
        Navigation.NavigateTo($"?cid={chatId}");
    }

    public void Dispose() => _cancellationTokenSource.Cancel();
}
