// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using MinimalApi.Services.Documents;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Search;
using Shared.Json;

namespace MinimalApi.Services.ChatHistory;

public class DocumentServiceAzureNative : IDocumentService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _cosmosContainer;
    private readonly AzureBlobStorageService _blobStorageService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly SearchClientFactory _searchClientFactory;

    public DocumentServiceAzureNative(CosmosClient cosmosClient, AzureBlobStorageService blobStorageService, HttpClient httpClient, IConfiguration configuration, SearchClientFactory searchClientFactory)
    {
        _cosmosClient = cosmosClient;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
        _searchClientFactory = searchClientFactory;

        // Create database if it doesn't exist
        var db = _cosmosClient.CreateDatabaseIfNotExistsAsync(DefaultSettings.CosmosDBDatabaseName).GetAwaiter().GetResult();

        // Create get container if it doesn't exist
        _cosmosContainer = db.Database.CreateContainerIfNotExistsAsync(DefaultSettings.CosmosDBUserDocumentsCollectionName, "/userId").GetAwaiter().GetResult();
    }

    public async Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, string selectedProfile, Dictionary<string, string>? fileMetadata, CancellationToken cancellationToken)
    {
        var selectedProfileDefinition = ProfileDefinition.All.FirstOrDefault(p => p.Id == selectedProfile);
        var indexName = selectedProfileDefinition.RAGSettings.DocumentRetrievalIndexName;
        var metadata = string.Join(",", fileMetadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var response = await _blobStorageService.UploadFilesV2Async(userInfo, files, selectedProfile, fileMetadata, cancellationToken);

        var searchIndexerClient = _searchClientFactory.GetSearchIndexerClient();
        var task = searchIndexerClient.RunIndexerAsync(selectedProfileDefinition.RAGSettings.DocumentIndexerName);
        task.Wait(5000);

        foreach (var file in response.UploadedFiles)
        {
            await CreateDocumentUploadAsync(userInfo, file, indexName, selectedProfileDefinition.Id, metadata);
        }
        return response;
    }

    private async Task CreateDocumentUploadAsync(UserInformation user, UploadDocumentFileSummary fileSummary, string indexName, string profileId, string metadata, string contentType = "application/pdf")
    {
        var document = new DocumentUpload(Guid.NewGuid().ToString(), user.UserId, fileSummary.FileName, fileSummary.FileName, contentType, fileSummary.Size, indexName, profileId, DocumentProcessingStatus.Succeeded, metadata);
        await _cosmosContainer.CreateItemAsync(document, partitionKey: new PartitionKey(document.UserId));
    }

    public async Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user, string profileId)
    {
        var query = _cosmosContainer.GetItemQueryIterator<DocumentUpload>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE c.sessionId = @sessionId ORDER BY c.sourceName DESC")
            .WithParameter("@username", user.UserId)
            .WithParameter("@sessionId", profileId));

        var results = new List<DocumentUpload>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }
}
