// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client;
using MinimalApi.Services.Documents;
using Newtonsoft.Json;
using Shared.Json;
using Shared.Models;

namespace MinimalApi.Services.ChatHistory;


public class DocumentService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _cosmosContainer;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    public DocumentService(CosmosClient cosmosClient, HttpClient httpClient, IConfiguration configuration)
    {
        _cosmosClient = cosmosClient;
        _httpClient = httpClient;

        _httpClient.BaseAddress = new Uri(configuration["IngestionPipelineAPI"]);
        _httpClient.DefaultRequestHeaders.Add("x-functions-key", configuration["IngestionPipelineAPIKey"]);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _configuration = configuration;

        // Create database if it doesn't exist
        var db = _cosmosClient.CreateDatabaseIfNotExistsAsync(DefaultSettings.CosmosDBDatabaseName).GetAwaiter().GetResult();

        // Create get container if it doenst exist
        _cosmosContainer = db.Database.CreateContainerIfNotExistsAsync(DefaultSettings.CosmosDBUserDocumentsCollectionName, "/userId").GetAwaiter().GetResult();
    }

    public async Task CreateDocumentUploadAsync(UserInformation user, string blobName, string fileName, string contentType = "application/pdf")
    {
        var document = new DocumentUpload(Guid.NewGuid().ToString(), user.UserId, blobName, fileName, contentType, 0, DocumentProcessingStatus.New);   
        await _cosmosContainer.CreateItemAsync(document, partitionKey: new PartitionKey(document.UserId));

        var response = await _httpClient.GetAsync("api/get_active_index");
        var content = response.Content.ReadAsStringAsync().Result; 
        var indexData = JsonConvert.DeserializeObject<GetIndexResponse>(content);


        var request = new ProcessingData()
        {
            SourceContainer = "source",
            ExtractContainer = "extract",
            PrefixPath = "prefix",
            EntraId = user.UserName,
            SessionId = "session",
            IndexName = indexData.IndexStemName,
            CosmosRecordId = document.Id,
            AutomaticallyDelete = true
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request, SerializerOptions.Default);
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        var triggerResponse = await _httpClient.PostAsync("/api/orchestrators/pdf_orchestration", body);
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
