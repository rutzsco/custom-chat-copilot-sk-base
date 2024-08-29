// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class TextInputV3
{
    private string? _value;

    [Parameter] public EventCallback OnEnterKeyPressed { get; set; }
    [Parameter] public EventCallback OnResetPressed { get; set; }

    [Parameter]
    public required string? Value
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

    [Parameter] public string ImageUrl { get; set; } = "";
    
    private async Task OnKeyUpAsync(KeyboardEventArgs args)
    {
        if (args is { Key: "Enter", ShiftKey: false } &&
            OnEnterKeyPressed.HasDelegate)
        {
            await OnEnterKeyPressed.InvokeAsync();
        }
    }
    private async Task OnAskClickedAsync()
    {
        await OnEnterKeyPressed.InvokeAsync();
    }
    private async Task OnClearChatAsync()
    {
        await OnResetPressed.InvokeAsync();
    }
}
