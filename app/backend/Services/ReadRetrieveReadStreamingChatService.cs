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

internal sealed class ReadRetrieveReadStreamingChatService : IChatService
{
    private readonly ILogger<ReadRetrieveReadStreamingChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClientFacade _openAIClientFacade;

    public ReadRetrieveReadStreamingChatService(OpenAIClientFacade openAIClientFacade,
                                                ILogger<ReadRetrieveReadStreamingChatService> logger,
                                                IConfiguration configuration)
    {
        _openAIClientFacade = openAIClientFacade;
        _logger = logger;
        _configuration = configuration;
    }

    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // Kernel setup
        var kernel = _openAIClientFacade.GetKernel(request.OptionFlags.IsChatGpt4Enabled());

        var generateSearchQueryFunction = kernel.Plugins.GetFunction(profile.RAGSettings.GenerateSearchQueryPluginName, profile.RAGSettings.GenerateSearchQueryPluginQueryFunctionName);
        var documentLookupFunction = kernel.Plugins.GetFunction(profile.RAGSettings.DocumentRetrievalPluginName, "Retrieval");
        var context = new KernelArguments().AddUserParameters(request.History, profile, user, request.SelectedUserCollectionFiles);

        // RAG Steps
        await kernel.InvokeAsync(generateSearchQueryFunction, context);
        if (context.TryGetValue(ContextVariableOptions.ResponsibleAIPolicyViolation, out var policyViolationObj) && policyViolationObj is bool policyViolation && policyViolation)
        {
            yield return new ChatChunkResponse("The response was filtered due to the prompt triggering Azure OpenAI's content management policy.", new ApproachResponse("The response was filtered due to the prompt triggering Azure OpenAI's content management policy.",string.Empty, null));
            yield break;
        }
        await kernel.InvokeAsync(documentLookupFunction, context);

        // Chat Step
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var systemMessagePrompt = PromptService.GetPromptByName(profile.RAGSettings.ChatSystemMessageFile);
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
                sb.Append(chatUpdate.Content);
                yield return new ChatChunkResponse( chatUpdate.Content);
                await Task.Yield();
            }
        }
        sw.Stop();


        var requestTokenCount = chatHistory.GetTokenCount();
        var result = context.BuildStreamingResoponse(profile, request, requestTokenCount, sb.ToString(), _configuration, _openAIClientFacade.GetKernelDeploymentName(request.OptionFlags.IsChatGpt4Enabled()), sw.ElapsedMilliseconds, requestProperties);

        _logger.LogInformation($"Chat Complete - Profile: {result.Context.Profile}, ChatId: {result.Context.ChatId}, ChatMessageId: {result.Context.MessageId}, ModelDeploymentName: {result.Context.Diagnostics.ModelDeploymentName}, TotalTokens: {result.Context.Diagnostics.AnswerDiagnostics.TotalTokens}");

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
