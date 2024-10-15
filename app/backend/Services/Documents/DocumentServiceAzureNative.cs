// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using MinimalApi.Services.Documents;
using Shared.Json;

namespace MinimalApi.Services.ChatHistory;

public class DocumentServiceAzureNative : IDocumentService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _cosmosContainer;
    private readonly AzureBlobStorageService _blobStorageService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DocumentServiceAzureNative(CosmosClient cosmosClient, AzureBlobStorageService blobStorageService, HttpClient httpClient, IConfiguration configuration)
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

    public async Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, Dictionary<string, string>? fileMetadata, CancellationToken cancellationToken)
    {
        var response = await _blobStorageService.UploadFilesAsync(userInfo, files, cancellationToken, fileMetadata);
        foreach (var file in response.UploadedFiles)
        {
            await CreateDocumentUploadAsync(userInfo, file);
        }

        // need to trigger the new index update here...

        return response;
    }

    private async Task CreateDocumentUploadAsync(UserInformation user, UploadDocumentFileSummary fileSummary, string contentType = "application/pdf")
    {
        var indexName = "TEST";
        var document = new DocumentUpload(Guid.NewGuid().ToString(), user.UserId, fileSummary.FileName, fileSummary.FileName, contentType, fileSummary.Size, indexName, user.SessionId, DocumentProcessingStatus.Succeeded, fileSummary.CompanyName, fileSummary.Industry);
        await _cosmosContainer.CreateItemAsync(document, partitionKey: new PartitionKey(document.UserId));
    }

    public async Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user)
    {
        var query = _cosmosContainer.GetItemQueryIterator<DocumentUpload>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE  c.userId = @username ORDER BY c.sourceName DESC")
            .WithParameter("@username", user.UserId));

        var results = new List<DocumentUpload>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }
}
