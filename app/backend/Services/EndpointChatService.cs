// Copyright (c) Microsoft. All rights reserved.

using System.Collections;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using Azure.AI.OpenAI;
using Azure.Core;
using ClientApp.Pages;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Profile.Prompts;
using Shared.Models;

namespace MinimalApi.Services;

internal sealed class EndpointChatService : IChatService
{
    private readonly ILogger<EndpointChatService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public EndpointChatService(ILogger<EndpointChatService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }


    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, CancellationToken cancellationToken = default)
    {
        var apiRequest = new HttpRequestMessage(HttpMethod.Post, _configuration[profile.AssistantEndpointSettings.APIEndpointSetting]);
        apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);


        var payload = JsonSerializer.Serialize(request.History);
        apiRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(apiRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<ChatChunkResponse>();
        yield return responseContent;
    }
}
