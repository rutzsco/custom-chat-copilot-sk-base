// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using System.Threading;
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
    private readonly AzureBlobStorageService _blobStorageService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DocumentService(CosmosClient cosmosClient, AzureBlobStorageService blobStorageService, HttpClient httpClient, IConfiguration configuration)
    {
        _cosmosClient = cosmosClient;
        _blobStorageService = blobStorageService;
        
        if (configuration[AppConfigurationSetting.IngestionPipelineAPI] != null)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(configuration[AppConfigurationSetting.IngestionPipelineAPI]);
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", configuration[AppConfigurationSetting.IngestionPipelineAPIKey]);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _configuration = configuration;
        }

        // Create database if it doesn't exist
        var db = _cosmosClient.CreateDatabaseIfNotExistsAsync(DefaultSettings.CosmosDBDatabaseName).GetAwaiter().GetResult();

        // Create get container if it doenst exist
        _cosmosContainer = db.Database.CreateContainerIfNotExistsAsync(DefaultSettings.CosmosDBUserDocumentsCollectionName, "/userId").GetAwaiter().GetResult();
    }

    public async Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, CancellationToken cancellationToken)
    {
        var response = await _blobStorageService.UploadFilesAsync(userInfo, files, cancellationToken);
        foreach (var file in response.UploadedFiles)
        {
            await CreateDocumentUploadAsync(userInfo, file);
        }
        return response;
    }


    private async Task CreateDocumentUploadAsync(UserInformation user, UploadDocumentFileSummary fileSummary, string contentType = "application/pdf")
    {
        // Get Ingestion Index Name
        var indexRequest = new GetIndexRequest() { index_stem_name = "rag-index" };
        var indexRequestJson = System.Text.Json.JsonSerializer.Serialize(indexRequest, SerializerOptions.Default);
        using var indexRequestPayload = new StringContent(indexRequestJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/get_active_index", indexRequestPayload);
        response.EnsureSuccessStatusCode();

        var indexName = await response.Content.ReadAsStringAsync();

        var document = new DocumentUpload(Guid.NewGuid().ToString(), user.UserId, fileSummary.FileName, fileSummary.FileName, contentType, fileSummary.Size, indexName, user.SessionId, DocumentProcessingStatus.New);   
        await _cosmosContainer.CreateItemAsync(document, partitionKey: new PartitionKey(document.UserId));

        var request = new ProcessingData()
        {
            source_container = "content",
            extract_container = "content-extract",
            prefix_path = fileSummary.FileName,
            entra_id = user.UserName,
            session_id = user.SessionId,
            index_name = indexName,
            index_stem_name = "rag-index",
            cosmos_record_id = document.Id,
            automatically_delete = false
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request, SerializerOptions.Default);
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        var triggerResponse = await _httpClient.PostAsync("/api/orchestrators/pdf_orchestrator", body);
    }


    public async Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user)
    {
        var query = _cosmosContainer.GetItemQueryIterator<DocumentUpload>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE  c.userId = @username AND c.sessionId = @sessionId ORDER BY c.sourceName DESC")
            .WithParameter("@username", user.UserId)
            .WithParameter("@sessionId", user.SessionId));

        var results = new List<DocumentUpload>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }
}
