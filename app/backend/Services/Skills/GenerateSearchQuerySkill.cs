// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.Profile.Prompts;

namespace MinimalApi.Services.Skills;


public sealed class GenerateSearchQuerySkill
{
    [KernelFunction("GenerateSearchQuery"), Description("Generate a search query for user question.")]
    public async Task<string> GenerateSearchQueryAsync([Description("chat History")] ChatTurn[] chatTurns,
                                                       KernelArguments arguments,
                                                       Kernel kernel)
    {
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();

        // Build chat history
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(PromptService.GetPromptByName(PromptService.RAGSearchSystemPrompt)).AddChatHistory(chatTurns);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.RAGSearchUserPrompt), arguments);
        chatHistory.AddUserMessage(userMessage);

        // Get search query from LLM
        var searchAnswer = await chatGpt.GetChatMessageContentAsync(chatHistory, DefaultSettings.AISearchRequestSettings, kernel);
        arguments[ContextVariableOptions.SearchQuery] = searchAnswer.Content;

        // Build diagnostics for search request
        chatHistory.AddAssistantMessage(searchAnswer.Content);
        var requestProperties = chatHistory.GenerateRequestProperties(DefaultSettings.AIChatRequestSettings);
        arguments[ContextVariableOptions.SearchDiagnostics] = requestProperties;

        return searchAnswer.Content;
    }
}
