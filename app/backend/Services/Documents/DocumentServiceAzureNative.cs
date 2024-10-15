// Copyright (c) Microsoft. All rights reserved.

using System.Drawing.Printing;
using Microsoft.Azure.Cosmos;
using MinimalApi.Services.Documents;
using MinimalApi.Services.Search.IndexDefinitions;
using MinimalApi.Services.Search;
using Shared.Json;
using ClientApp.Pages;
using ClientApp;
using Microsoft.Extensions.Hosting;
using System.Drawing;
using System.Reflection.Metadata;
using System;
using OpenAI;

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

        // Create get container if it doesn't exist
        _cosmosContainer = db.Database.CreateContainerIfNotExistsAsync(DefaultSettings.CosmosDBUserDocumentsCollectionName, "/userId").GetAwaiter().GetResult();
    }

    public async Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, Dictionary<string, string>? fileMetadata, CancellationToken cancellationToken)
    {
        var response = await _blobStorageService.UploadFilesAsync(userInfo, files, cancellationToken, fileMetadata);
        foreach (var file in response.UploadedFiles)
        {
            await CreateDocumentUploadAsync(userInfo, file);
        }

        // trigger the index update...
        var documentsToMerge = new List<DocumentIndexMerge>();
        var i = 1;
        foreach (var file in response.UploadedFiles)
        {
            documentsToMerge.Add(new DocumentIndexMerge(i++.ToString(), file.FileName));
        }

        var documentIndexMergeResponse = await MergeDocumentsIntoIndexAsync(documentsToMerge);

        // this return value is not right yet, but it's a place holder to show I can pass results back to calling page...
        response.FilesIndexed = documentIndexMergeResponse.IndexedCount;
        response.AllFilesIndexed = documentIndexMergeResponse.AllFilesIndexed;
        response.IndexErrorMessage = documentIndexMergeResponse.ErrorMessage;

        return response;
    }
    private async Task<DocumentIndexResponse> MergeDocumentsIntoIndexAsync(List<DocumentIndexMerge> documents)
    {
        var structuredResponse = new DocumentIndexResponse();
        try
        {
            // should I be using the SearchService SDK interface here...???

            // these are hard-coded for testing...  need to be dynamic when this is working
            var searchServiceName = "srch-fuwyp7kyt7kmy";
            var indexName = "vector-1729021028480";
            var indexEndpoint = $"https://{searchServiceName}.search.windows.net/indexes('{indexName}')/docs/search.index?api-version=2024-07-01";

            var requestPayloadJson = documents != null ? System.Text.Json.JsonSerializer.Serialize(documents, SerializerOptions.Default) : "{}";
            // this action descriptor needs to be "@search.action"...  you should be able to specify that in the JSON properties (and I did...), but it's not coming through here...
            requestPayloadJson = requestPayloadJson.Replace("\"searchAction\"", "\"@search.action\"");
            var requestPayload = new StringContent(requestPayloadJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(indexEndpoint, requestPayload);
            response.EnsureSuccessStatusCode();
            requestPayload.Dispose();

            structuredResponse = new DocumentIndexResponse(await response.Content.ReadAsStringAsync());
            return structuredResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            structuredResponse.ErrorMessage = ex.Message;
            structuredResponse.DocumentCount = documents.Count;
            structuredResponse.IndexedCount = 0;
            structuredResponse.AllFilesIndexed = false;
            return structuredResponse;
        }
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
