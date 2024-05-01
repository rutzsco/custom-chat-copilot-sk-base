// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class TextInput
{
    private string? _value;

    [Parameter] public EventCallback OnEnterKeyPressed { get; set; }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public required string? Value
#pragma warning restore BL0007 // This is required for proper event propagation
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            ValueChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Placeholder { get; set; } = "";
    [Parameter] public string HelperText { get; set; } = "Use Shift + Enter for new lines.";
    [Parameter] public string Icon { get; set; } = Icons.Material.Filled.QuestionMark;


    private async Task OnKeyUpAsync(KeyboardEventArgs args)
    {
        if (args is { Key: "Enter", ShiftKey: false } &&
            OnEnterKeyPressed.HasDelegate)
        {
            await OnEnterKeyPressed.InvokeAsync();
        }
    }

    private void OnDisclaimerClicked()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true };
        DialogService.Show<DisclaimerDialog>("Disclaimer", options);
    }
    [Inject] public required IDialogService DialogService { get; set; }
}
