// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Services.Prompts;

namespace MinimalApi.Services.Skills;

public class ChatSkill
{
    [KernelFunction("Chat"), Description("Get a chat response for user question and sources")]
    public async Task<SKResult> ChatAsync([Description("chat History")] ChatTurn[] chatTurns,
                                          KernelArguments arguments,
                                          Kernel kernel)
    {
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var systemMessagePrompt = ResolveSystemPrompt((string)arguments["questionIntent"]);
        arguments["SystemMessagePrompt"] = systemMessagePrompt;

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(systemMessagePrompt).AddChatHistory(chatTurns);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.ChatUserPrompt), arguments);
        arguments["UserMessage"] = userMessage;
        chatHistory.AddUserMessage(userMessage);

        var result = await chatGpt.GetChatCompletionsWithUsageAsync(chatHistory);
        arguments["ChatResult"] = result;

        return result;
    }

    private string ResolveSystemPrompt(string intent)
    {
        return PromptService.GetPromptByName(PromptService.ChatSystemPrompt);
    }
}
