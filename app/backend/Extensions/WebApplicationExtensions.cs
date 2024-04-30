// Copyright (c) Microsoft. All rights reserved.

using System;

using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Security;

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
            var response = await chatService.ReplyAsync(profile, request, cancellationToken);
            await chatHistoryService.RecordChatMessageAsync(userInfo, request, response);
            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

    private static async IAsyncEnumerable<ChatChunkResponse> OnPostChatStreamingAsync(HttpContext context, ChatRequest request, ChatService chatService, ReadRetrieveReadStreamingChatService ragChatService, ChatHistoryService chatHistoryService, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Get user information
        var userInfo = GetUserInfo(context);
        var profile = request.OptionFlags.GetChatProfile();
        if (!userInfo.HasAccess(profile))
        {
            throw new UnauthorizedAccessException("User does not have access to this profile");
        }

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

    private static async Task<IEnumerable<FeedbackResponse>> OnGetHistoryAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var user = GetUserInfo(context);
        var response = await chatHistoryService.GetMostRecentChatItemsAsync(user);
        return response.AsFeedbackResponse();
    }

    private static async Task<IEnumerable<FeedbackResponse>> OnGetFeedbackAsync(HttpContext context, ChatHistoryService chatHistoryService)
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


        if (string.IsNullOrEmpty(id))
        {
            id = "LocalDevUser";
            name = "LocalDevUser";
            userGroups = new List<string> { "LocalDevUser" };
        }
        
        var enableLogout = !string.IsNullOrEmpty(id);

        //get all group claims
        
        var p = ProfileDefinition.All.Where(p => p.SecurityModelGroupMembership.Any(userGroups.Contains)).Select(x => new ProfileSummary(x.Name, string.Empty, x.SampleQuestions));
        var user = new UserInformation(enableLogout, name, id, p, userGroups);

        return user;
    }
}
