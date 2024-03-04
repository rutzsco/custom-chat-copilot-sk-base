// Copyright (c) Microsoft. All rights reserved.

using System;
using MinimalApi.Services.ChatHistory;

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        // Process chat turn history
        api.MapGet("chat/history", OnGetHistoryAsync);

        // Process chat turn rating 
        api.MapPost("chat/rating", OnPostChatRatingAsync);

        // Process chat turn
        api.MapPost("chat", OnPostChatAsync);

        // Get all documents
        api.MapGet("documents", OnGetDocumentsAsync);

        // Get recent feedback
        api.MapGet("feedback", OnGetFeedbackAsync);

        // Get source file
        api.MapGet("documents/{fileName}", OnGetSourceFileAsync);

        // Get enable logout
        api.MapGet("user", OnGetUser);

        return app;
    }

    private static IResult OnGetUser(HttpContext context)
    {
        var userInfo = GetUserInfo(context);
        return TypedResults.Ok(userInfo);
    }

    private static async Task<IResult> OnGetSourceFileAsync(string fileName, BlobServiceClient blobServiceClient)
    {
        try
        {
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("content");
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
            yield return new FeedbackResponse(item.Prompt, item.Content, 0, string.Empty, item.Timestamp);
        }
    }

    private static async Task<IResult> OnPostChatAsync(HttpContext context, ChatRequest request, ReadRetrieveReadChatService chatService, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        var userInfo = GetUserInfo(context);
        if (request is { History.Length: > 0 })
        {
            var response = await chatService.ReplyAsync(request, cancellationToken);
            await chatHistoryService.RecordChatMessageAsync(userInfo, request, response);
            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }


    private static async IAsyncEnumerable<DocumentResponse> OnGetDocumentsAsync(BlobContainerClient client, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in client.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (blob is not null and { Deleted: false })
            {
                var props = blob.Properties;
                var baseUri = client.Uri;
                var builder = new UriBuilder(baseUri);
                builder.Path += $"/{blob.Name}";

                var metadata = blob.Metadata;
                var documentProcessingStatus = GetMetadataEnumOrDefault<DocumentProcessingStatus>(
                    metadata, nameof(DocumentProcessingStatus), DocumentProcessingStatus.NotProcessed);
                var embeddingType = GetMetadataEnumOrDefault<EmbeddingType>(
                    metadata, nameof(EmbeddingType), EmbeddingType.AzureSearch);

                yield return new(
                    blob.Name,
                    props.ContentType,
                    props.ContentLength ?? 0,
                    props.LastModified,
                    builder.Uri,
                    documentProcessingStatus,
                    embeddingType);

                static TEnum GetMetadataEnumOrDefault<TEnum>(
                    IDictionary<string, string> metadata,
                    string key,
                    TEnum @default) where TEnum : struct => metadata.TryGetValue(key, out var value)
                        && Enum.TryParse<TEnum>(value, out var status)
                            ? status
                            : @default;
            }
        }
    }

    private static async IAsyncEnumerable<FeedbackResponse> OnGetFeedbackAsync(HttpContext context, ChatHistoryService chatHistoryService)
    {
        var userInfo = GetUserInfo(context);
        var response = await chatHistoryService.GetMostRecentRatingsItemsAsync(userInfo);
        foreach (var item in response)
        {
            yield return new FeedbackResponse(item.Prompt, item.Content, item.Rating.Rating, item.Rating.Feedback, item.Rating.Timestamp);
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
        var u = new UserInformation(enableLogout, name, id);

        return u;
    }
}
