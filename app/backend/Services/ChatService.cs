// Copyright (c) Microsoft. All rights reserved.

using System.Collections;
using Azure.AI.OpenAI;
using Azure.Core;
using ClientApp.Pages;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Profile.Prompts;
using Shared.Models;

namespace MinimalApi.Services;

internal sealed class ChatService : IChatService
{
    private readonly ILogger<ReadRetrieveReadStreamingChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClientFacade _openAIClientFacade;
    private readonly AzureBlobStorageService _blobStorageService;

    public ChatService(OpenAIClientFacade openAIClientFacade, AzureBlobStorageService blobStorageService, ILogger<ReadRetrieveReadStreamingChatService> logger, IConfiguration configuration)
    {
        _openAIClientFacade = openAIClientFacade;
        _blobStorageService = blobStorageService;
        _logger = logger;
        _configuration = configuration;
    }


    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, CancellationToken cancellationToken = default)
    {

        var sw = Stopwatch.StartNew();

        var kernel = _openAIClientFacade.GetKernel(request.OptionFlags.IsChatGpt4Enabled());

        var generateSearchQueryFunction = kernel.Plugins.GetFunction(DefaultSettings.GenerateSearchQueryPluginName, DefaultSettings.GenerateSearchQueryPluginQueryFunctionName);

        var context = new KernelArguments().AddUserParameters(request.History, profile, user);

        // Search Query
        await kernel.InvokeAsync(generateSearchQueryFunction, context);

        // Chat Step
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var systemMessagePrompt = PromptService.GetPromptByName(profile.ChatSystemMessageFile);
        context["SystemMessagePrompt"] = systemMessagePrompt;

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(systemMessagePrompt).AddChatHistory(request.History);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.ChatSimpleUserPrompt), context);
        context["UserMessage"] = userMessage;

        if (request.OptionFlags.ImageContentExists())
        {
            var imageString = request.OptionFlags.GetImageContent();
            DataUriParser parser = new DataUriParser(imageString);
            if (parser.MediaType == "image/jpeg" || parser.MediaType == "image/png")
            {
                chatHistory.AddUserMessage(
                [
                   new TextContent(userMessage),
                   new ImageContent(parser.Data) { MimeType = parser.MediaType }
                ]);
            }
            else if(parser.MediaType == "text/csv" || parser.MediaType == "text/plain")
            {
                string csvData = System.Text.Encoding.UTF8.GetString(parser.Data);
                chatHistory.AddUserMessage(
                [
                   new TextContent(csvData),
                   new TextContent(userMessage)
                ]);
            }
            else if (parser.MediaType == "application/pdf")
            {
                string pdfData = PDFTextExtractor.ExtractTextFromPdf(parser.Data);
                chatHistory.AddUserMessage(
                [
                   new TextContent(pdfData),
                   new TextContent(userMessage)
                ]);
            }
        }
        else
        {
            chatHistory.AddUserMessage(userMessage);
        }
          
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
