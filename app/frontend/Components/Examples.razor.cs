// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Examples
{
    [Parameter] public required ProfileSummary Profile { get; set; }
    [Parameter, EditorRequired] public required string Message { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnPromptTemplateClicked { get; set; }
    private async Task OnClickedAsync(string exampleText)
    {
        if (OnExampleClicked.HasDelegate)
        {
            await OnExampleClicked.InvokeAsync(exampleText);
        }
    }

    private async Task OnPromptTemplateClickedAsync(string templateName)
    {
        if (OnPromptTemplateClicked.HasDelegate)
        {
            var template = Profile.PromptTemplates.FirstOrDefault(t => t.Name == templateName);
            var promptTemplate = Encoding.UTF8.GetString(Convert.FromBase64String(template.PromptTemplate));
            foreach (var variable in template.Variables)
            {
                Console.WriteLine($"${variable.Name}:{variable.Value}");
                promptTemplate = promptTemplate.Replace($"${variable.Name}", variable.Value);
            }
            await OnPromptTemplateClicked.InvokeAsync(promptTemplate);
        }
    }
}
