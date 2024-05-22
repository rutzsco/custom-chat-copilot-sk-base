// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Antiforgery;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Security;
using Shared.Models;

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

        // Get source file
        api.MapGet("documents/{fileName}", OnGetSourceFileAsync);

        // Get enable logout
        api.MapGet("user", OnGetUser);

        // User document
        api.MapPost("documents", OnPostDocumentAsync);
        api.MapGet("user/documents", OnGetUserDocumentsAsync);

        api.MapGet("token/csrf", OnGetAntiforgeryTokenAsync);

        api.MapGet("status", OnGetStatus);
        return app;
    }

    private static IResult OnGetStatus()
    {
        return Results.Ok("OK");
    }

    private static async Task<IResult> OnGetAntiforgeryTokenAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return TypedResults.Ok(tokens?.RequestToken ?? string.Empty);
    }
        
    private static async Task<IResult> OnGetSourceFileAsync(HttpContext context, string fileName, BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        try
        {
            int underscoreIndex = fileName.IndexOf('|');  // Find the first underscore

            if (underscoreIndex != -1)
            {
                string profileName = fileName.Substring(0, underscoreIndex); // Get the substring before the underscore
                string blobName = fileName.Substring(underscoreIndex + 1); // Get the substring after the underscore

                Console.WriteLine($"Filename:{fileName} Container: {profileName} BlobName: {blobName}");


                // Get user information
                var userInfo = context.GetUserInfo();
                var profile = ProfileDefinition.All.FirstOrDefault(x => x.Id == profileName);
                if (profile == null || !userInfo.HasAccess(profile))
                {
                    throw new UnauthorizedAccessException("User does not have access to this profile");
                }

                var blobContainerClient = blobServiceClient.GetBlobContainerClient(profile.RAGSettings.StorageContianer);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

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
            else
            {
                throw new ArgumentOutOfRangeException("Invalid file name format");
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
        [FromServices] DocumentService documentService,
        [FromServices] ILogger<AzureBlobStorageService> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Upload documents");
        var userInfo = context.GetUserInfo();
        var response = await documentService.CreateDocumentUploadAsync(userInfo, files, cancellationToken);
        logger.LogInformation("Upload documents: {x}", response);

        return TypedResults.Ok(response);
    }
    
    private static IResult OnGetUser(HttpContext context)
    {
        var userInfo = context.GetUserInfo();
        return TypedResults.Ok(userInfo);
    }
    private static async Task<IResult> OnGetUserDocumentsAsync(HttpContext context, DocumentService documentService)
    {
        var userInfo = context.GetUserInfo();
        var documents = await documentService.GetDocumentUploadsAsync(userInfo);
        return TypedResults.Ok(documents.Select(d => new DocumentSummary(d.Id, d.SourceName, d.ContentType, d.Size, d.Status, d.Timestamp)));
    }
    private static async Task<IResult> OnPostChatRatingAsync(HttpContext context, ChatRatingRequest request, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        var userInfo = context.GetUserInfo();
        await chatHistoryService.RecordRatingAsync(userInfo, request);
        return Results.Ok();
    }

    private static async Task<IResult> OnPostChatAsync(HttpContext context, ChatRequest request, ReadRetrieveReadChatService chatService, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        // Get user information
        var userInfo = context.GetUserInfo();
        var profile = request.OptionFlags.GetChatProfile();
        if (!userInfo.HasAccess(profile))
        {
            throw new UnauthorizedAccessException("User does not have access to this profile");
        }

        if (request is { History.Length: > 0 })
        {
            var response = await chatService.ReplyAsync(userInfo, profile, request, cancellationToken);
            await chatHistoryService.RecordChatMessageAsync(userInfo, request, response);
            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

    private static async IAsyncEnumerable<ChatChunkResponse> OnPostChatStreamingAsync(HttpContext context, ChatRequest request, ChatService chatService, ReadRetrieveReadStreamingChatService ragChatService, ChatHistoryService chatHistoryService, DocumentService documentService, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userInfo = context.GetUserInfo();
        var profile = request.OptionFlags.GetChatProfile();
        if (!userInfo.HasAccess(profile))
        {
            throw new UnauthorizedAccessException("User does not have access to this profile");
        }

        if (profile.Approach == ProfileApproach.UserDocumentChat.ToString())
        {
            var selectedDocument = request.OptionFlags.GetSelectedDocument();
            var documents = await documentService.GetDocumentUploadsAsync(userInfo);
            var document = documents.FirstOrDefault(d => d.SourceName == selectedDocument);
            profile.RAGSettings.DocumentRetrievalIndexName = document.RetrivalIndexName;
        }

        var chat = ResolveChatService(request, chatService, ragChatService);
        await foreach (var chunk in chat.ReplyAsync(userInfo, profile, request).WithCancellation(cancellationToken))
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

    private static async Task<IEnumerable<ChatHistoryResponse>> OnGetHistoryAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var userInfo = context.GetUserInfo();
        var response = await chatHistoryService.GetMostRecentChatItemsAsync(userInfo);
        return response.AsFeedbackResponse();
    }

    private static async Task<IEnumerable<ChatHistoryResponse>> OnGetFeedbackAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var userInfo = context.GetUserInfo();
        var response = await chatHistoryService.GetMostRecentRatingsItemsAsync(userInfo);
        return response.AsFeedbackResponse();
    }
}
