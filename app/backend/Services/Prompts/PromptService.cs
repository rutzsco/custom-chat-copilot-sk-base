// Copyright (c) Microsoft. All rights reserved.
using System.Reflection;

namespace MinimalApi.Services.Prompts;

public static class PromptService
{
    public static string ChatSystemPrompt = "ChatSystemPrompt";
    public static string ChatUserPrompt = "ChatUserPrompt";
    public static string SearchSystemPrompt = "SearchSystemPrompt";
    public static string SearchUserPrompt = "SearchUserPrompt";

    public static string GetPromptByName(string prompt)
    {
        var resourceName = $"MinimalApi.Services.Prompts.{prompt}.txt";
        var assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new ArgumentException($"The resource {resourceName} was not found.");
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static async Task<string> RenderPromptAsync(Kernel kernel, string prompt, KernelArguments arguments)
    {
        var ptf = new KernelPromptTemplateFactory();
        var pt = ptf.Create(new PromptTemplateConfig(prompt));
        string intentUserMessage = await pt.RenderAsync(kernel, arguments);
        return intentUserMessage;
    }
}
