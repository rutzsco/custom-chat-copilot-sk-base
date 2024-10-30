// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using Azure.Core;
using ClientApp.Pages;
using Microsoft.AspNetCore.Antiforgery;
using MinimalApi.Services;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Documents;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Search;
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
        api.MapGet("chat/history-v2", OnGetHistoryV2Async);
        api.MapGet("chat/history/{chatId}", OnGetChatHistorySessionAsync);

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
        api.MapGet("collection/documents/{profileId}", OnGetCollectionDocumentsAsync);

        // Azure Search Native Index documents
        //api.MapPost("native/index/documents", OnPostNativeIndexDocumentsAsync);

        // Profile Selections
        api.MapGet("profile/selections", OnGetProfileUserSelectionOptionsAsync);

        api.MapGet("token/csrf", OnGetAntiforgeryTokenAsync);

        api.MapGet("status", OnGetStatus);

        api.MapPost("ingestion/trigger", OnPostTriggerIngestionPipelineAsync);
        api.MapGet("headers", OnGetHeadersAsync);
        return app;
    }

    private static IResult OnGetHeadersAsync(HttpContext context)
    {
        var headers = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            if (headers.Keys.Contains(header.Key))
            {
                headers[header.Key] = header.Value.FirstOrDefault() ?? "";
            }
            else
            {
                headers.Add(header.Key, header.Value.FirstOrDefault() ?? "");
            }
        }
        return Results.Ok(headers);
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

    private static async Task<IResult> OnGetProfileUserSelectionOptionsAsync(HttpContext context, string profileId, SearchClientFactory searchClientFactory, ILogger<WebApplication> logger)
    {
        var profileDefinition = ProfileDefinition.All.FirstOrDefault(x => x.Id == profileId);
        if (profileDefinition == null)
            return Results.BadRequest("Profile does not found.");

        if (profileDefinition.RAGSettings == null)
            return Results.BadRequest("Profile does not support user selection");

        var searchClient = searchClientFactory.GetOrCreateClient(profileDefinition.RAGSettings.DocumentRetrievalIndexName);
        var selectionOptions = new List<UserSelectionOption>();
        foreach (var selectionOption in profileDefinition.RAGSettings.ProfileUserSelectionOptions)
        {
            try
            {

                var searchOptions = new SearchOptions { Size = 0, Facets = { $"{selectionOption.IndexFieldName},count:{100}" },  };
                SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>("*", searchOptions);
                if (results.Facets != null && results.Facets.ContainsKey(selectionOption.IndexFieldName))
                {
                    var selectionValues = new List<string>();
                    foreach (FacetResult facet in results.Facets[selectionOption.IndexFieldName])
                        selectionValues.Add(facet.Value.ToString());

                    selectionOptions.Add(new UserSelectionOption(selectionOption.DisplayName, selectionValues.OrderBy(x => x)));
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"Error getting user selection options for profile {profileId}");
                selectionOptions.Add(new UserSelectionOption(selectionOption.DisplayName, new string[] { } ));
            }
        }

        // Set headers to prevent caching
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        var result = new UserSelectionModel(selectionOptions);
        return Results.Ok(result);
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

                ArgumentNullException.ThrowIfNull(profile.RAGSettings, "Profile RAGSettings is null");
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(profile.RAGSettings.StorageContianer);
                var blobClient = blobContainerClient.GetBlobClient(blobName);
                Console.WriteLine($"Blob Uri:{blobClient.Uri}");
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
        [FromServices] IDocumentService documentService,
        [FromServices] ILogger<AzureBlobStorageService> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Upload documents");
        var userInfo = context.GetUserInfo();
        var fileMetadataContent = context.Request.Headers["X-FILE-METADATA"];
        var selectedProfile = context.Request.Headers["X-PROFILE-METADATA"];
        Dictionary<string, string>? fileMetadata = null;
        if (!string.IsNullOrEmpty(fileMetadataContent))
            fileMetadata = JsonSerializer.Deserialize<Dictionary<string,string>>(fileMetadataContent);


        var response = await documentService.CreateDocumentUploadAsync(userInfo, files, selectedProfile, fileMetadata, cancellationToken);
        logger.LogInformation("Upload documents: {x}", response);

        return TypedResults.Ok(response);
    }

    //private static async Task<IResult> OnPostNativeIndexDocumentsAsync(HttpContext context,
    //    [FromBody] UploadDocumentsResponse documentList,
    //    //[FromBody] DocumentIndexRequest indexRequest,
    //    [FromServices] IDocumentService documentService,
    //    [FromServices] ILogger<AzureBlobStorageService> logger,
    //    CancellationToken cancellationToken)
    //{
    //    logger.LogInformation("Call Azure Search Native index to index the uploaded documents...");
    //    var userInfo = context.GetUserInfo();
    //    //var response = await documentService.MergeDocumentsIntoIndexAsync(documentList);
    //    logger.LogInformation("Azure Search Native index response: {x}", response);
    //    return TypedResults.Ok(response);
    //}

    private static IResult OnGetUser(HttpContext context)
    {
        var userInfo = context.GetUserInfo();
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        return TypedResults.Ok(userInfo);
    }
    private static async Task<IResult> OnGetUserDocumentsAsync(HttpContext context, IDocumentService documentService)
    {
        var userInfo = context.GetUserInfo();
        var documents = await documentService.GetDocumentUploadsAsync(userInfo, null);
        return TypedResults.Ok(documents.Select(d => new DocumentSummary(d.Id, d.SourceName, d.ContentType, d.Size, d.Status, d.StatusMessage,d.ProcessingProgress, d.Timestamp, d.Metadata)));
    }

    private static async Task<IResult> OnGetCollectionDocumentsAsync(HttpContext context, IDocumentService documentService, string profileId)
    {
        var userInfo = context.GetUserInfo();
        var documents = await documentService.GetDocumentUploadsAsync(userInfo, profileId);
        return TypedResults.Ok(documents.Select(d => new DocumentSummary(d.Id, d.SourceName, d.ContentType, d.Size, d.Status, d.StatusMessage, d.ProcessingProgress, d.Timestamp, d.Metadata)));
    }

    private static async Task<IResult> OnPostChatRatingAsync(HttpContext context, ChatRatingRequest request, IChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        var userInfo = context.GetUserInfo();
        await chatHistoryService.RecordRatingAsync(userInfo, request);
        return Results.Ok();
    }

    private static async Task<IResult> OnPostChatAsync(HttpContext context, ChatRequest request, ReadRetrieveReadChatService chatService, IChatHistoryService chatHistoryService, CancellationToken cancellationToken)
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

    private static async IAsyncEnumerable<ChatChunkResponse> OnPostChatStreamingAsync(HttpContext context, ChatRequest request, ChatService chatService, ReadRetrieveReadStreamingChatService ragChatService, IChatHistoryService chatHistoryService, EndpointChatService endpointChatService, EndpointChatServiceV2 endpointChatServiceV2, IDocumentService documentService, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userInfo = context.GetUserInfo();
        var profile = request.OptionFlags.GetChatProfile();
        if (!userInfo.HasAccess(profile))
        {
            throw new UnauthorizedAccessException("User does not have access to this profile");
        }

        if (profile.Approach == ProfileApproach.UserDocumentChat.ToString())
        {
            ArgumentNullException.ThrowIfNull(profile.RAGSettings, "Profile RAGSettings is null");

            var selectedDocument = request.SelectedUserCollectionFiles.FirstOrDefault();
            var documents = await documentService.GetDocumentUploadsAsync(userInfo,null);
            var document = documents.FirstOrDefault(d => d.SourceName == selectedDocument);

            ArgumentNullException.ThrowIfNull(document, "Document is null");
            profile.RAGSettings.DocumentRetrievalIndexName = document.RetrivalIndexName;
        }

        var chat = ResolveChatService(request, chatService, ragChatService, endpointChatService, endpointChatServiceV2);
        await foreach (var chunk in chat.ReplyAsync(userInfo, profile, request).WithCancellation(cancellationToken))
        {
            yield return chunk;
            if (chunk.FinalResult != null)
            {
                await chatHistoryService.RecordChatMessageAsync(userInfo, request, chunk.FinalResult);
            }
        }
    }
    private static IChatService ResolveChatService(ChatRequest request, ChatService chatService, ReadRetrieveReadStreamingChatService ragChatService, EndpointChatService endpointChatService, EndpointChatServiceV2 endpointChatServiceV2)
    {
        if (request.OptionFlags.IsChatProfile())
            return chatService;

        if (request.OptionFlags.IsEndpointAssistantProfile())
            return endpointChatService;

        if (request.OptionFlags.IsEndpointAssistantV2Profile())
            return endpointChatServiceV2;

        return ragChatService;
    }

    private static async Task<IEnumerable<ChatHistoryResponse>> OnGetHistoryAsync(HttpContext context, IChatHistoryService chatHistoryService)
    {
        var userInfo = context.GetUserInfo();
        var response = await chatHistoryService.GetMostRecentChatItemsAsync(userInfo);
        return response.AsFeedbackResponse();
    }

    private static async Task<IEnumerable<ChatSessionModel>> OnGetHistoryV2Async(HttpContext context, IChatHistoryService chatHistoryService)
    {
        var userInfo = context.GetUserInfo();
        var response = await chatHistoryService.GetMostRecentChatItemsAsync(userInfo);
        var apiResponseModel = response.AsFeedbackResponse();

        var first = apiResponseModel.First();
        List<ChatSessionModel> sessions = apiResponseModel
            .GroupBy(msg => msg.ChatId)
            .Select(g => new ChatSessionModel(g.Key, g.ToList()))
            .ToList();

        return sessions;
    }


    private static async Task<IEnumerable<ChatHistoryResponse>> OnGetChatHistorySessionAsync(string chatId, HttpContext context, IChatHistoryService chatHistoryService)
    {
        var userInfo = context.GetUserInfo();
        var response = await chatHistoryService.GetChatHistoryMessagesAsync(userInfo, chatId);
        var apiResponseModel = response.AsFeedbackResponse();
        return apiResponseModel;
    }

    private static async Task<IEnumerable<ChatHistoryResponse>> OnGetFeedbackAsync(HttpContext context, IChatHistoryService chatHistoryService)
    {
        var userInfo = context.GetUserInfo();
        var response = await chatHistoryService.GetMostRecentRatingsItemsAsync(userInfo);
        return response.AsFeedbackResponse();
    }

    private static async Task<IResult> OnPostTriggerIngestionPipelineAsync([FromServices] IngestionService ingestionService, IngestionRequest ingestionRequest)
    {
        await ingestionService.TriggerIngestionPipelineAsync(ingestionRequest);
        return Results.Ok();
    }
}
