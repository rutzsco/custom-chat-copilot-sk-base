// Copyright (c) Microsoft. All rights reserved.
using Shared.Json;

namespace MinimalApi.Services.Search;

public class IngestionService
{
    private readonly AzureBlobStorageService _blobStorageService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public IngestionService(AzureBlobStorageService blobStorageService, HttpClient httpClient, IConfiguration configuration)
    {
        _blobStorageService = blobStorageService;

        if (configuration[AppConfigurationSetting.IngestionPipelineAPI] != null)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(configuration[AppConfigurationSetting.IngestionPipelineAPI]);
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", configuration[AppConfigurationSetting.IngestionPipelineAPIKey]);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _configuration = configuration;
        }
    }

    public async Task TriggerIngestionPipelineAsync(IngestionRequest ingestionRequest)
    {
        var indexName = await CreateIndexAsync(ingestionRequest.IndexStemName);
        var request = new
        {
            source_container = ingestionRequest.SourceCountainer,
            extract_container = ingestionRequest.ExtractContainer,
            prefix_path = string.Empty,
            entra_id = "NA",
            session_id = "NA",
            index_stem_name = ingestionRequest.IndexStemName,
            index_name = indexName,
            cosmos_record_id = "INGESTION",
            automatically_delete = false,
            analyze_images = true,
            overlapping_chunks = false,
            chunk_size = 400,
            overlap = 200,
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request, SerializerOptions.Default);
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        var triggerResponse = await _httpClient.PostAsync("/api/orchestrators/pdf_orchestrator", body);
    }

    private async Task<string> CreateIndexAsync(string stemName)
    {
        var request = new
        {
            index_stem_name = stemName,
            fields = new
            {
                content = "string",
                pagenumber = "int",
                sourcefile = "string",
                sourcepage = "string",
                category = "string"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request, SerializerOptions.Default);
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        var triggerResponse = await _httpClient.PostAsync("/api/create_new_index", body);

        var indexName = await GetActiveIndexAsync(stemName);
        return indexName;
    }

    private async Task<string> GetActiveIndexAsync(string stemName)
    {
        var request = new
        {
            index_stem_name = stemName
        };
        var json = System.Text.Json.JsonSerializer.Serialize(request, SerializerOptions.Default);
        using var body = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_configuration[AppConfigurationSetting.IngestionPipelineAPI]}/api/get_active_index"),
            Content = body
        };

        // Send the request
        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);

        // Get the response content
        var responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }
}
