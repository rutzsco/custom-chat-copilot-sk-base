// Copyright (c) Microsoft. All rights reserved.
using System.Reflection;

namespace MinimalApi.Services.Profile.Prompts;

public static class PromptService
{
    public static string ChatSystemPrompt = "RAGChatSystemPrompt";
    public static string ChatUserPrompt = "RAGChatUserPrompt";
    public static string RAGSearchSystemPrompt = "RAGSearchQuerySystemPrompt";
    public static string RAGSearchUserPrompt = "RAGSearchUserPrompt";

    public static string ChatSimpleSystemPrompt = "ChatSimpleSystemPrompt";
    public static string ChatSimpleUserPrompt = "ChatSimpleUserPrompt";

    public static string GetPromptByName(string prompt)
    {
        var resourceName = $"MinimalApi.Services.Profile.Prompts.{prompt}.txt";
        var assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new ArgumentException($"The resource {resourceName} was not found.");

            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
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
