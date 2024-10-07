// Copyright (c) Microsoft. All rights reserved.

using System.Collections;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using Azure.AI.OpenAI;
using Azure.Core;
using ClientApp.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Profile.Prompts;
using Shared.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static MudBlazor.CategoryTypes;

namespace MinimalApi.Services;

internal sealed class EndpointChatServiceV2 : IChatService
{
    private readonly ILogger<EndpointChatService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string,string> _threadToChatSession;

    public EndpointChatServiceV2(ILogger<EndpointChatService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _threadToChatSession = new Dictionary<string, string>();    
    }


    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        
        var stateThread = await ResolveThreadIdAsync(profile, request.ChatId);

        var payload = JsonSerializer.Serialize(new { thread_id = stateThread, message = request.LastUserQuestion});
        var url = $"{_configuration[profile.AssistantEndpointSettings.APIEndpointSetting]}/run_assistant";
        var apiRequest = new HttpRequestMessage(HttpMethod.Post, url);
        //apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);
        apiRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var sb = new StringBuilder();
   


        var response = await _httpClient.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using (Stream stream = await response.Content.ReadAsStreamAsync())
        using (StreamReader reader = new StreamReader(stream))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Process each line or chunk as it streams in
                Console.WriteLine(line);
                sb.Append(line);
                yield return new ChatChunkResponse(line);
                await Task.Yield();
            }
        }

        var thoughts = new List<ThoughtRecord>();
        thoughts.Add(new ThoughtRecord("Assistant Response", sb.ToString()));

        yield return new ChatChunkResponse("", new ApproachResponse(sb.ToString(), null, new ResponseContext(profile.Name, null, thoughts.ToArray(), request.ChatTurnId, request.ChatId, null)));
    }
    private async Task<string> ResolveThreadIdAsync(ProfileDefinition profile, Guid chatId)
    {
        if (_threadToChatSession.ContainsKey(chatId.ToString()))
        {
            return _threadToChatSession[chatId.ToString()];
        }

        var payload = new { };
        var url = $"{_configuration[profile.AssistantEndpointSettings.APIEndpointSetting]}/create_thread";
        var apiRequest = new HttpRequestMessage(HttpMethod.Post, url);
        //apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);
        apiRequest.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(apiRequest);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent = responseContent.Trim('"');
        _threadToChatSession.Add(chatId.ToString(), responseContent);
        return responseContent;

    }
}
