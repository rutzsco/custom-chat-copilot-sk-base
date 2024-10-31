// Copyright (c) Microsoft. All rights reserved.

using System.Xml.Xsl;
using ClientApp.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClientApp.Components;

public sealed partial class TextInputV4
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

    [Parameter] public ProfileSummary SelectedProfileSummary { get; set; } = null;

    [Parameter] public UserSelectionModel UserSelectionModel { get; set; } = null;

    private async Task OnKeyUpAsync(KeyboardEventArgs args)
    {
        Console.WriteLine($"OnKeyUpAsync - {UserQuestion}");
        if (args is { Key: "Enter", ShiftKey: false } && OnEnterKeyPressed.HasDelegate)
        {
            var question = UserQuestion;
            UserQuestion = string.Empty;
            question.TrimEnd('\n');

            var template = SelectedProfileSummary.PromptTemplates.First();
            var promptTemplate = Encoding.UTF8.GetString(Convert.FromBase64String(template.PromptTemplate));
            foreach (var variable in template.Variables)
            {
                Console.WriteLine($"${variable.Name}:{variable.Value}");
                promptTemplate = promptTemplate.Replace($"${variable.Name}", variable.Value);
            }

            var sb = new StringBuilder();
            sb.Append(promptTemplate);
            sb.AppendLine();
            sb.Append(question);
            Console.WriteLine($"OnKeyUpAsync - {sb.ToString()}");
            await OnEnterKeyPressed.InvokeAsync(sb.ToString());
        }
    }
    private async Task OnAskClickedAsync()
    {
        var question = UserQuestion;
        UserQuestion = string.Empty;

        var template = SelectedProfileSummary.PromptTemplates.First();
        var promptTemplate = Encoding.UTF8.GetString(Convert.FromBase64String(template.PromptTemplate));
        foreach (var variable in template.Variables)
        {
            Console.WriteLine($"${variable.Name}:{variable.Value}");
            promptTemplate = promptTemplate.Replace($"${variable.Name}", variable.Value);
        }

        var sb = new StringBuilder();
        sb.Append(promptTemplate);
        sb.AppendLine();
        sb.Append(question);
        Console.WriteLine($"OnKeyUpAsync - {sb.ToString()}");
        await OnEnterKeyPressed.InvokeAsync(sb.ToString());
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

    private async Task UploadFileAsync(IBrowserFile file)
    {
        Console.WriteLine("UploadFilesAsync");
        var buffer = new byte[file.Size];
        await file.OpenReadStream(8192000).ReadAsync(buffer);
        var imageContent = Convert.ToBase64String(buffer);

        var fileSummary = new FileSummary($"data:{file.ContentType};base64,{imageContent}", file.Name, file.ContentType);
        _files.Add(fileSummary);

        await OnFileUpload.InvokeAsync(fileSummary);
    }

    private async Task UploadFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            var buffer = new byte[file.Size];
            await file.OpenReadStream(8192000).ReadAsync(buffer);
            var imageContent = Convert.ToBase64String(buffer);

            var fileSummary = new FileSummary($"data:{file.ContentType};base64,{imageContent}", file.Name, file.ContentType);
            _files.Add(fileSummary);

            await OnFileUpload.InvokeAsync(fileSummary);
        }
    }

    public string Trim(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= 24)
        {
            return text;
        }

        return text.Substring(0, 24) + "...";
    }
}
