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

    public async Task<DocumentIndexResponse> MergeDocumentsIntoIndexAsync(UploadDocumentsResponse uploadList) // DocumentIndexRequest indexRequest)
    {
        var searchServiceUrl = _configuration[AppConfigurationSetting.AzureSearchServiceEndpoint];
        var indexName = _configuration[AppConfigurationSetting.AzureSearchServiceIndexName];

        var indexEndpoint = $"{searchServiceUrl}/indexes('{indexName}')/docs/search.index?api-version=2024-07-01";
        var structuredResponse = new DocumentIndexResponse
        {
            EndPointUrl = indexEndpoint
        };
        var documents = new List<DocumentIndexMerge>();
        var i = 1;

        try
        {
            // should I be using the SearchService SDK interface here...???
            //var searchClient = new SearchClientFactory(_configuration, null, new AzureKeyCredential(_configuration[AppConfigurationSetting.AzureSearchServiceKey]));
            //var searchIndexClient = searchClient.GetOrCreateClient(indexName);
            // See https://learn.microsoft.com/en-us/azure/search/search-howto-dotnet-sdk
            //   Not sure how to creat this batch record properly...
            //var batch = new IndexDocumentsBatch<DocumentIndexMerge>();
            //foreach (var file in indexRequest.Documents.UploadedFiles)
            //{
            //    var action = IndexDocumentsAction("Upload", new DocumentIndexMerge(i++, file.FileName));
            //    batch.Actions.Add(new DocumentIndexMerge(i++, file.FileName));
            //}

            //IndexDocumentsBatch<DocumentIndexMerge> batch = IndexDocumentsBatch.Create(
            //  IndexDocumentsAction.Upload(  iterate documents here ???...)
            //);

            //IndexDocumentsResult indexResults = searchIndexClient.IndexDocuments(batch);
            //IndexDocumentsResult indexResults = await searchIndexClient.IndexDocumentsAsync<IndexDocumentsBatch<DocumentIndexMerge>>(documents);

            //foreach (var file in indexRequest.Documents.UploadedFiles)
            foreach (var file in uploadList.UploadedFiles)
            {
                documents.Add(new DocumentIndexMerge(i++, file.FileName));
            }
            var requestPayloadJson = documents.Count != 0 ? System.Text.Json.JsonSerializer.Serialize(documents, SerializerOptions.Default) : "{}";
            // FYI - this json field name needs to be "@search.action"...  you should be able to specify that in the JSON properties (and I did...), but it's not coming through here...
            requestPayloadJson = requestPayloadJson.Replace("\"searchAction\"", "\"@search.action\"");
            var requestPayload = new StringContent(requestPayloadJson, Encoding.UTF8, "application/json");
            //_httpClient.DefaultRequestHeaders.Add("X-MS-TOKEN-AAD-ACCESS-TOKEN", indexRequest.AccessToken);

            Console.WriteLine($"\nDEBUG: Calling {indexEndpoint}\n");
            var response = await _httpClient.PostAsync(indexEndpoint, requestPayload);
            response.EnsureSuccessStatusCode();
            requestPayload.Dispose();

            structuredResponse = new DocumentIndexResponse(await response.Content.ReadAsStringAsync());
            Console.WriteLine($"\nDEBUG: EOF MergeDocumentsIntoIndexAsync: {System.Text.Json.JsonSerializer.Serialize(structuredResponse)}\n");
            return structuredResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            structuredResponse.ErrorMessage = ex.Message;
            structuredResponse.DocumentCount = documents.Count;
            structuredResponse.IndexedCount = 0;
            structuredResponse.AllFilesIndexed = false;
            Console.WriteLine($"\nDEBUG: Catch MergeDocumentsIntoIndexAsync: {System.Text.Json.JsonSerializer.Serialize(structuredResponse)}\n");
            return structuredResponse;
        }
    }

    private async Task CreateDocumentUploadAsync(UserInformation user, UploadDocumentFileSummary fileSummary, string indexName, string profileId, string metadata, string contentType = "application/pdf")
    {
        var document = new DocumentUpload(Guid.NewGuid().ToString(), user.UserId, fileSummary.FileName, fileSummary.FileName, contentType, fileSummary.Size, indexName, profileId, DocumentProcessingStatus.Succeeded, metadata);
        await _cosmosContainer.CreateItemAsync(document, partitionKey: new PartitionKey(document.UserId));
    }

    public async Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user, string profileId)
    {
        var query = _cosmosContainer.GetItemQueryIterator<DocumentUpload>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE  c.userId = @username AND  c.sessionId = @sessionId ORDER BY c.sourceName DESC")
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
