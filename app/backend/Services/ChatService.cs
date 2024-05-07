// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;
using Azure.Core;
using ClientApp.Pages;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Profile.Prompts;
using Shared.Models;

namespace MinimalApi.Services;

internal sealed class ChatService : IChatService
{
    private readonly ILogger<ReadRetrieveReadStreamingChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClientFacade _openAIClientFacade;

    public ChatService(OpenAIClientFacade openAIClientFacade, ILogger<ReadRetrieveReadStreamingChatService> logger, IConfiguration configuration)
    {
        _openAIClientFacade = openAIClientFacade;
        _logger = logger;
        _configuration = configuration;
    }


    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, CancellationToken cancellationToken = default)
    {

        var sw = Stopwatch.StartNew();

        var kernel = _openAIClientFacade.GetKernel(false);

        var generateSearchQueryFunction = kernel.Plugins.GetFunction(DefaultSettings.GenerateSearchQueryPluginName, DefaultSettings.GenerateSearchQueryPluginQueryFunctionName);

        var context = new KernelArguments().AddUserParameters(request.History, profile, user);

        // Search Query
        await kernel.InvokeAsync(generateSearchQueryFunction, context);


        // Chat Step
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var systemMessagePrompt = PromptService.GetPromptByName(PromptService.ChatSimpleSystemPrompt);
        context["SystemMessagePrompt"] = systemMessagePrompt;

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(systemMessagePrompt).AddChatHistory(request.History);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.ChatSimpleUserPrompt), context);
        context["UserMessage"] = userMessage;
        chatHistory.AddUserMessage(userMessage);
       

        var sb = new StringBuilder();
        await foreach (StreamingChatMessageContent chatUpdate in chatGpt.GetStreamingChatMessageContentsAsync(chatHistory, DefaultSettings.AIChatRequestSettings))
        {
            if (chatUpdate.Content != null)
            {
                await Task.Delay(1);
                sb.Append(chatUpdate.Content);
                yield return new ChatChunkResponse(chatUpdate.Content);
            }
        }
        sw.Stop();


        var requestTokenCount = chatHistory.GetTokenCount();
        var result = context.BuildChatSimpleResoponse(profile, request, requestTokenCount, sb.ToString(), _configuration, _openAIClientFacade.GetKernelDeploymentName(request.OptionFlags.IsChatGpt4Enabled()), sw.ElapsedMilliseconds);
        yield return new ChatChunkResponse(string.Empty, result);
    }
}
