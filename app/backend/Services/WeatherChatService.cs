// Copyright (c) Microsoft. All rights reserved.

using System.Data;
using Azure.AI.OpenAI;
using Azure.Core;
using ClientApp.Pages;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Profile.Prompts;
using Shared.Models;

namespace MinimalApi.Services;

internal sealed class WeatherChatService : IChatService
{
    private readonly ILogger<ReadRetrieveReadStreamingChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClientFacade _openAIClientFacade;

    public WeatherChatService(OpenAIClientFacade openAIClientFacade,
                                                ILogger<ReadRetrieveReadStreamingChatService> logger,
                                                IConfiguration configuration)
    {
        _openAIClientFacade = openAIClientFacade;
        _logger = logger;
        _configuration = configuration;
    }

    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // Kernel setup
        var kernel = _openAIClientFacade.GetKernel(false);

        var s0 = kernel.Plugins.GetFunction("WeatherSkill", "GetLocation");
        var s1 = kernel.Plugins.GetFunction("WeatherSkill", "GetLocationLatLong");
        var s2 = kernel.Plugins.GetFunction("WeatherSkill", "GetForcast");
        var context = new KernelArguments().AddUserParameters(request.History, profile, user, request.OptionFlags.GetSelectedDocument());

        // RAG Steps
        await kernel.InvokeAsync(s0, context);
        await kernel.InvokeAsync(s1, context);
        await kernel.InvokeAsync(s2, context);

        // Chat Step
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var systemMessagePrompt = PromptService.GetPromptByName("WeatherChatSystemPrompt");
        context[ContextVariableOptions.SystemMessagePrompt] = systemMessagePrompt;

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(systemMessagePrompt).AddChatHistory(request.History);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.ChatUserPrompt), context);
        context[ContextVariableOptions.UserMessage] = userMessage;
        chatHistory.AddUserMessage(userMessage);

        var requestProperties = GenerateRequestProperties(chatHistory, DefaultSettings.AIChatRequestSettings);
        var sb = new StringBuilder();
        await foreach (StreamingChatMessageContent chatUpdate in chatGpt.GetStreamingChatMessageContentsAsync(chatHistory, DefaultSettings.AIChatRequestSettings, null, cancellationToken))
        {
            if (chatUpdate.Content != null)
            {
                await Task.Delay(1);
                sb.Append(chatUpdate.Content);
                yield return new ChatChunkResponse( chatUpdate.Content);
            }
        }
        sw.Stop();


        var requestTokenCount = chatHistory.GetTokenCount();
        var result = context.BuildStreamingResoponseV2(profile, request, requestTokenCount, sb.ToString(), _configuration, _openAIClientFacade.GetKernelDeploymentName(request.OptionFlags.IsChatGpt4Enabled()), sw.ElapsedMilliseconds, requestProperties);
        yield return new ChatChunkResponse(string.Empty, result);
    }

    private List<KeyValuePair<string, string>> GenerateRequestProperties(Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory, PromptExecutionSettings settings)
    {
        var results = new List<KeyValuePair<string,string>>();
        foreach (var item in chatHistory)
        {
            if (item is ChatMessageContent chatMessageContent)
            {
                var content = chatMessageContent.Content;
                var role = chatMessageContent.Role;
                results.Add(new KeyValuePair<string,string>($"PROMPTMESSAGE:{role}", content));
            }
        }

        foreach (var item in settings.ExtensionData)
        {
            results.Add(new KeyValuePair<string, string>($"PROMPTKEY:{item.Key}", item.Value.ToString()));
        }

        return results;
    }
}
