// Copyright (c) Microsoft. All rights reserved.

using ClientApp.Models;

namespace ClientApp.Components;

public sealed partial class TextInputV3
{
    private List<FileSummary> _files = new List<FileSummary>();


    [Parameter] public EventCallback<FileSummary> OnFileUpload { get; set; }
    [Parameter] public EventCallback<string> OnEnterKeyPressed { get; set; }
    [Parameter] public EventCallback OnResetPressed { get; set; }

    [Parameter] public EventCallback<bool> OnModelSelection { get; set; }

    [Parameter] public required string UserQuestion { get; set; }

    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool SupportsFileUpload { get; set; }

    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Placeholder { get; set; } = "";

    [Parameter] public string ImageUrl { get; set; } = "";
    
    private async Task OnKeyUpAsync(KeyboardEventArgs args)
    {
        Console.WriteLine($"OnKeyUpAsync - {UserQuestion}");
        if (args is { Key: "Enter", ShiftKey: false } && OnEnterKeyPressed.HasDelegate)
        {
            UserQuestion.TrimEnd('\n');
            Console.WriteLine($"OnKeyUpAsync - {UserQuestion}");
            await OnEnterKeyPressed.InvokeAsync(UserQuestion);
        }
    }
    private async Task OnAskClickedAsync()
    {
        await OnEnterKeyPressed.InvokeAsync(UserQuestion);
        UserQuestion = string.Empty;
    }
    private async Task OnClearChatAsync()
    {
        UserQuestion = "";
        _files.Clear();
        await OnResetPressed.InvokeAsync();
    }
    private async Task OnModelSelectionAsync(bool toggle)
    {
        await OnModelSelection.InvokeAsync(toggle);
    }

    private async Task UploadFilesAsync(IBrowserFile file)
    {
        Console.WriteLine("UploadFilesAsync");
        var buffer = new byte[file.Size];
        await file.OpenReadStream(8192000).ReadAsync(buffer);
        var imageContent = Convert.ToBase64String(buffer);

        var fileSummary = new FileSummary($"data:{file.ContentType};base64,{imageContent}", file.Name);
        _files.Add(fileSummary);

        await OnFileUpload.InvokeAsync(fileSummary);
    }
}
