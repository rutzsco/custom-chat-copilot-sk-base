// Copyright (c) Microsoft. All rights reserved.

using System;

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
        return app;
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
                var userInfo = GetUserInfo(context);
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
        var userInfo = GetUserInfo(context);
        var response = await documentService.CreateDocumentUploadAsync(userInfo, files, cancellationToken);
        logger.LogInformation("Upload documents: {x}", response);

        return TypedResults.Ok(response);
    }
    
    private static IResult OnGetUser(HttpContext context)
    {
        var userInfo = GetUserInfo(context);
        return TypedResults.Ok(userInfo);
    }
    private static async Task<IResult> OnGetUserDocumentsAsync(HttpContext context, DocumentService documentService)
    {
        var userInfo = GetUserInfo(context);
        var documents = await documentService.GetDocumentUploadsAsync(userInfo);
        return TypedResults.Ok(documents.Select(d => new DocumentSummary(d.Id, d.SourceName, d.ContentType, d.Size, d.Status, d.Timestamp)));
    }
    private static async Task<IResult> OnPostChatRatingAsync(HttpContext context, ChatRatingRequest request, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        var userInfo = GetUserInfo(context);
        await chatHistoryService.RecordRatingAsync(userInfo, request);
        return Results.Ok();
    }

    private static async Task<IResult> OnPostChatAsync(HttpContext context, ChatRequest request, ReadRetrieveReadChatService chatService, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        // Get user information
        var userInfo = GetUserInfo(context);
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
        // Get user information
        var userInfo = GetUserInfo(context);
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
        var resultChunks = chat.ReplyAsync(userInfo, profile, request);
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

    private static async Task<IEnumerable<ChatHistoryResponse>> OnGetHistoryAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var user = GetUserInfo(context);
        var response = await chatHistoryService.GetMostRecentChatItemsAsync(user);
        return response.AsFeedbackResponse();
    }

    private static async Task<IEnumerable<ChatHistoryResponse>> OnGetFeedbackAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var userInfo = GetUserInfo(context);
        var response = await chatHistoryService.GetMostRecentRatingsItemsAsync(userInfo);
        return response.AsFeedbackResponse();
    }

    private static UserInformation GetUserInfo(HttpContext context)
    {
        var id = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
        var name = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
        var claimsPrincipal = ClaimsPrincipalParser.Parse(context.Request);
        var userGroups = claimsPrincipal.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
        var session = claimsPrincipal.Claims.Where(c => c.Type == "nonce").Select(c => c.Value).FirstOrDefault();

        if (string.IsNullOrEmpty(id))
        {
            id = "LocalDevUser";
            name = "LocalDevUser";
            userGroups = new List<string> { "LocalDevUser" };
            session = "test-session";
        }
        
        var enableLogout = !string.IsNullOrEmpty(id);

        var profiles = ProfileDefinition.All.GetAuthorizedProfiles(userGroups).Select(x => new ProfileSummary(x.Name, string.Empty, (ProfileApproach)Enum.Parse(typeof(ProfileApproach), x.Approach, true), x.SampleQuestions));
        var user = new UserInformation(enableLogout, name, id, session, profiles, userGroups);

        return user;
    }
}
