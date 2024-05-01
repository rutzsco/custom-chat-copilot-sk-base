// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client;
using MinimalApi.Services.Documents;
using Shared.Models;

namespace MinimalApi.Services.ChatHistory;


public class DocumentService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _cosmosContainer;

    public DocumentService(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;

        // Create database if it doesn't exist
        var db = _cosmosClient.CreateDatabaseIfNotExistsAsync(DefaultSettings.CosmosDBDatabaseName).GetAwaiter().GetResult();

        // Create get container if it doenst exist
        _cosmosContainer = db.Database.CreateContainerIfNotExistsAsync(DefaultSettings.CosmosDBUserDocumentsCollectionName, "/userId").GetAwaiter().GetResult();
    }

    public async Task CreateDocumentUploadAsync(UserInformation user, string blobName, string fileName)
    {
        var document = new DocumentUpload(Guid.NewGuid().ToString(), user.UserId, string.Empty, fileName, "New");   
        await _cosmosContainer.CreateItemAsync(document, partitionKey: new PartitionKey(document.User));
    }

    public async Task RecordRatingAsync(UserInformation user, ChatRatingRequest chatRatingRequest)
    {
        var chatRatingId = chatRatingRequest.MessageId.ToString();
        var partitionKey = new PartitionKey(chatRatingRequest.ChatId.ToString());
        var response = await _cosmosContainer.ReadItemAsync<ChatMessageRecord>(chatRatingId,partitionKey);
        var existingChatRating = response.Resource;

        var rating = new ChatRating(chatRatingRequest.Feedback, chatRatingRequest.Rating);
        existingChatRating.Rating = rating;
        await _cosmosContainer.UpsertItemAsync(existingChatRating, partitionKey: partitionKey);
    }


    public async Task<List<ChatMessageRecord>> GetMostRecentRatingsItemsAsync(UserInformation user)
    {

        var query = _cosmosContainer.GetItemQueryIterator<ChatMessageRecord>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE c.rating != null AND c.userId = @username ORDER BY c.rating.timestamp DESC")
            .WithParameter("@username", user.UserId));

        var results = new List<ChatMessageRecord>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    public async Task<List<ChatMessageRecord>> GetMostRecentChatItemsAsync(UserInformation user)
    {
        var query = _cosmosContainer.GetItemQueryIterator<ChatMessageRecord>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE c.userId = @username ORDER BY c.timestamp DESC")
            .WithParameter("@username", user.UserId));

        var results = new List<ChatMessageRecord>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }
}
