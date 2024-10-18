// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Examples
{
    [Parameter] public required ProfileSummary Profile { get; set; }
    [Parameter, EditorRequired] public required string Message { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }
    private async Task OnClickedAsync(string exampleText)
    {
        if (OnExampleClicked.HasDelegate)
        {
            await OnExampleClicked.InvokeAsync(exampleText);
        }
    }
}
