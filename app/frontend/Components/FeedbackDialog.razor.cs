// Copyright (c) Microsoft. All rights reserved.

using static MudBlazor.CategoryTypes;

namespace ClientApp.Components;

public sealed partial class FeedbackDialog
{
    [Parameter] public required int Rating { get; set; }
    [Parameter] public required string MessageId { get; set; }
    [Parameter] public required string ChatId { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    [Inject] public required ApiClient ApiClient { get; set; }


    private string _feedback;
    private void OnCloseClick() => Dialog.Close(DialogResult.Ok(true));

    private async Task OnSubmitClickAsync()
    {
        if(!string.IsNullOrWhiteSpace(_feedback))
        {
            var request = new ChatRatingRequest(Guid.Parse(ChatId), Guid.Parse(MessageId), Rating, _feedback, Approach.ReadRetrieveRead);
            await ApiClient.ChatRatingAsync(request);
        }
        Dialog.Close(DialogResult.Ok(true));
    }
}
