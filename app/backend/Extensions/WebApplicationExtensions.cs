// Copyright (c) Microsoft. All rights reserved.

using System;

using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Profile;

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        // Process chat turn
        api.MapPost("chat/streaming", OnPostChatStreamingAsync);
        api.MapPost("chat", OnPostChatAsync);

        // Process chat turn history
        api.MapGet("chat/history", OnGetHistoryAsync);

        // Process chat turn rating 
        api.MapPost("chat/rating", OnPostChatRatingAsync);

        // Get recent feedback
        api.MapGet("feedback", OnGetFeedbackAsync);

        // Upload a document
        api.MapPost("documents", OnPostDocumentAsync);

        // Get source file
        api.MapGet("documents/{fileName}", OnGetSourceFileAsync);

        // Get enable logout
        api.MapGet("user", OnGetUser);

        return app;
    }
    private static async Task<IResult> OnGetSourceFileAsync(string fileName, BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        try
        {
            var sourceContainer = configuration["AzureStorageContainer"];
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(sourceContainer);
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                var stream = new MemoryStream();
                await blobClient.DownloadToAsync(stream);
                stream.Position = 0; // Reset stream position to the beginning

                return Results.File(stream, "application/pdf");
            }
            else
            {
                return Results.NotFound("File not found");
            }
        }
        catch (Exception)
        {
            // Log the exception details
            return Results.Problem("Internal server error");
        }
    }
    private static async Task<IResult> OnPostDocumentAsync(HttpContext context, [FromForm] IFormFileCollection files,
        [FromServices] AzureBlobStorageService service,
        [FromServices] ILogger<AzureBlobStorageService> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Upload documents");

        var userInfo = GetUserInfo(context);
        var response = await service.UploadFilesAsync(userInfo, files, cancellationToken);

        logger.LogInformation("Upload documents: {x}", response);

        return TypedResults.Ok(response);
    }

    private static IResult OnGetUser(HttpContext context)
    {
        var userInfo = GetUserInfo(context);
        return TypedResults.Ok(userInfo);
    }

    private static async Task<IResult> OnPostChatRatingAsync(HttpContext context, ChatRatingRequest request, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        var userInfo = GetUserInfo(context);
        await chatHistoryService.RecordRatingAsync(userInfo, request);
        return Results.Ok();
    }

    private static async IAsyncEnumerable<FeedbackResponse> OnGetHistoryAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var user = GetUserInfo(context);
        var response = await chatHistoryService.GetMostRecentChatItemsAsync(user);
        foreach (var item in response)
        {
            if (item.Diagnostics != null)
            {
                yield return new FeedbackResponse(
                    item.Prompt,
                    item.Content,
                    0,
                    string.Empty,
                    item.Diagnostics.ModelDeploymentName,
                    item.Diagnostics.WorkflowDurationMilliseconds,
                    item.Timestamp);
            }
            else
            {
                yield return new FeedbackResponse(
                    item.Prompt,
                    item.Content,
                    0,
                    string.Empty,
                    "Unavialable",
                    0,
                    item.Timestamp);
            }
        }
    }

    private static async Task<IResult> OnPostChatAsync(HttpContext context, ChatRequest request, ReadRetrieveReadChatService chatService, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        var userInfo = GetUserInfo(context);
        var profile = request.OptionFlags.GetChatProfile();
        if (request is { History.Length: > 0 })
        {
            var response = await chatService.ReplyAsync(profile, request, cancellationToken);
            await chatHistoryService.RecordChatMessageAsync(userInfo, request, response);
            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

    private static async IAsyncEnumerable<ChatChunkResponse> OnPostChatStreamingAsync(HttpContext context, ChatRequest request, ChatService chatService, ReadRetrieveReadStreamingChatService ragChatService, ChatHistoryService chatHistoryService, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userInfo = GetUserInfo(context);
        var chat = ResolveChatService(request, chatService, ragChatService);
        var resultChunks = chat.ReplyAsync(request.OptionFlags.GetChatProfile(),request);
        await foreach (var chunk in resultChunks)
        {
            yield return chunk;
            if (chunk.FinalResult != null)
            {
                await chatHistoryService.RecordChatMessageAsync(userInfo, request, chunk.FinalResult);
            }
        }
    }


    private static IChatService ResolveChatService(ChatRequest request, ChatService chatService, ReadRetrieveReadStreamingChatService ragChatService)
    {  
        if (request.OptionFlags.IsChatProfile())
            return chatService;
        
        return ragChatService;
    }

    private static async IAsyncEnumerable<FeedbackResponse> OnGetFeedbackAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var userInfo = GetUserInfo(context);
        var response = await chatHistoryService.GetMostRecentRatingsItemsAsync(userInfo);
        foreach (var item in response)
        {
            if (item.Diagnostics == null)
            {
                yield return new FeedbackResponse(
                    item.Prompt,
                    item.Content,
                    item.Rating.Rating,
                    item.Rating.Feedback,
                    string.Empty,
                    0,
                    item.Rating.Timestamp);
            }
            else
            {
                yield return new FeedbackResponse(
                    item.Prompt,
                    item.Content,
                    item.Rating.Rating,
                    item.Rating.Feedback,
                    item.Diagnostics.ModelDeploymentName,
                    item.Diagnostics.WorkflowDurationMilliseconds,
                    item.Rating.Timestamp);
            }
        }
    }

    private static UserInformation GetUserInfo(HttpContext context)
    {
        var id = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
        var name = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
        if (string.IsNullOrEmpty(id))
        {
            id = "LocalDevUser";
            name = "LocalDevUser";
        }
        
        var enableLogout = !string.IsNullOrEmpty(id);
        var profiles = ProfileDefinition.All.Select(x => new ProfileSummary(x.Name,string.Empty, x.SampleQuestions));
        var user = new UserInformation(enableLogout, name, id, profiles);

        return user;
    }
}
