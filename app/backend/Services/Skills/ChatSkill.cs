// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Services.Profile.Prompts;

namespace MinimalApi.Services.Skills;

public class ChatSkill
{
    [KernelFunction("Chat"), Description("Get a chat response for user question and sources")]
    public async Task<SKResult> ChatAsync([Description("chat History")] ChatTurn[] chatTurns,
                                          KernelArguments arguments,
                                          Kernel kernel)
    {
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var systemMessagePrompt = PromptService.GetPromptByName(PromptService.ChatSystemPrompt);
        arguments["SystemMessagePrompt"] = systemMessagePrompt;

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(systemMessagePrompt).AddChatHistory(chatTurns);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.ChatUserPrompt), arguments);
        arguments["UserMessage"] = userMessage;
        chatHistory.AddUserMessage(userMessage);

        var result = await chatGpt.GetChatCompletionsWithUsageAsync(chatHistory);
        arguments["ChatResult"] = result;

        return result;
    }
}
