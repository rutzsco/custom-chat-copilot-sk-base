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

    public async Task CreateDocumentUploadAsync(UserInformation user, string blobName, string fileName, string contentType = "application/pdf")
    {
        var document = new DocumentUpload(Guid.NewGuid().ToString(), user.UserId, blobName, fileName, contentType, 0, DocumentProcessingStatus.New);   
        await _cosmosContainer.CreateItemAsync(document, partitionKey: new PartitionKey(document.UserId));
    }


    public async Task<List<DocumentUpload>> GetDocumentUploadsAsync(string userId)
    {
        var query = _cosmosContainer.GetItemQueryIterator<DocumentUpload>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE  c.userId = @username ORDER BY c.sourceName DESC")
            .WithParameter("@username", userId));

        var results = new List<DocumentUpload>();
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
