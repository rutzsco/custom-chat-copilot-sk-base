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

internal sealed class ImageChatService : IChatService
{
    private readonly ILogger<ReadRetrieveReadStreamingChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClientFacade _openAIClientFacade;

    public ImageChatService(OpenAIClientFacade openAIClientFacade,
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
        var kernel = _openAIClientFacade.GetKernel(request.OptionFlags.IsChatGpt4Enabled());

        const string ImageUri = "https://upload.wikimedia.org/wikipedia/commons/d/d5/Half-timbered_mansion%2C_Zirkel%2C_East_view.jpg";


        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var context = new KernelArguments().AddUserParameters(request.History, profile, user, request.OptionFlags.GetSelectedDocument());
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory("You are a friendly assistant.");

        chatHistory.AddUserMessage(
        [
            new TextContent("What’s in this image?"),
            new ImageContent(new Uri(ImageUri))
        ]);

        var reply = await chatCompletionService.GetChatMessageContentAsync(chatHistory);


        var requestTokenCount = chatHistory.GetTokenCount();
        var result = context.BuildStreamingResoponse(profile, request, requestTokenCount, reply.Content, _configuration, _openAIClientFacade.GetKernelDeploymentName(request.OptionFlags.IsChatGpt4Enabled()), sw.ElapsedMilliseconds);
        yield return new ChatChunkResponse(string.Empty, result);
    }
}
