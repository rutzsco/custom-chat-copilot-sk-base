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
using UglyToad.PdfPig.Graphics;

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


    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        var sw = Stopwatch.StartNew();

        var kernel = _openAIClientFacade.GetKernel(request.OptionFlags.IsChatGpt4Enabled());
        var context = new KernelArguments().AddUserParameters(request.History, profile, user);

        // Chat Step
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var systemMessagePrompt = PromptService.GetPromptByName(profile.ChatSystemMessageFile);
        context["SystemMessagePrompt"] = systemMessagePrompt;

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(systemMessagePrompt).AddChatHistory(request.History);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.ChatSimpleUserPrompt), context);
        context["UserMessage"] = userMessage;


    
        if (request.FileUploads.Any())
        {
            var chatMessageContentItemCollection = new ChatMessageContentItemCollection();
            chatMessageContentItemCollection.Add(new TextContent(userMessage));

            foreach (var file in request.FileUploads)
            {
                DataUriParser parser = new DataUriParser(file.DataUrl);
                if (parser.MediaType == "image/jpeg" || parser.MediaType == "image/png")
                {
                    chatMessageContentItemCollection.Add(new ImageContent(parser.Data, parser.MediaType));
                }
                else if (parser.MediaType == "application/pdf")
                {
                    string pdfData = PDFTextExtractor.ExtractTextFromPdf(parser.Data);
                    chatMessageContentItemCollection.Add(new TextContent(pdfData));
                }
                else
                {
                    string csvData = System.Text.Encoding.UTF8.GetString(parser.Data);
                    chatMessageContentItemCollection.Add(new TextContent(csvData));

                }
            }

            chatHistory.AddUserMessage(chatMessageContentItemCollection);
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
                sb.Append(chatUpdate.Content);
                yield return new ChatChunkResponse(chatUpdate.Content);
                await Task.Yield();
            }
        }
        sw.Stop();


        var requestTokenCount = chatHistory.GetTokenCount();
        var result = context.BuildChatSimpleResoponse(profile, request, requestTokenCount, sb.ToString(), _configuration, _openAIClientFacade.GetKernelDeploymentName(request.OptionFlags.IsChatGpt4Enabled()), sw.ElapsedMilliseconds);
        yield return new ChatChunkResponse(string.Empty, result);
    }
}
