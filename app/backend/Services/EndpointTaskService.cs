// Copyright (c) Microsoft. All rights reserved.

using System.Collections;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Core;
using ClientApp.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Profile;
using static MinimalApi.Services.EndpointChatServiceV2;

namespace MinimalApi.Services;

internal sealed class EndpointTaskService : IChatService
{
    private readonly ILogger<EndpointChatService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public EndpointTaskService(ILogger<EndpointChatService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
 


        //var requestPayload = new
        //{
        //    taskId = request.ChatTurnId,
        //    files = new[]
        //        {
        //            new
        //            {
        //                Name = "Label",
        //                DataUrl = request.FileUploads.FirstOrDefault().DataUrl,
        //            }
        //        }
        //};
        var sb = new StringBuilder();
        var apiRequest = new HttpRequestMessage(HttpMethod.Post, _configuration[profile.AssistantEndpointSettings.APIEndpointSetting]);
        apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);
        apiRequest.Content = BuildTaskRequest(request);

        var response = await _httpClient.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();


        TaskResponse taskResponse = JsonSerializer.Deserialize<TaskResponse>(payload);
        var thoughts = new List<ThoughtRecord>();
        thoughts.Add(new ThoughtRecord("Assistant Response", sb.ToString()));

        yield return new ChatChunkResponse("", new ApproachResponse(taskResponse.answer, null, new ResponseContext(profile.Name, null, thoughts.ToArray(), request.ChatTurnId, request.ChatId, null)));
    }

    private StringContent BuildChatRequest(ChatRequest request)
    {
        var payload = JsonSerializer.Serialize(request.History);
        var content = new StringContent(payload, Encoding.UTF8, "application/json"); 
        return content;
    }


    public StringContent BuildTaskRequest(ChatRequest request)
    {
        var file = request.FileUploads.FirstOrDefault();

        var requestModel = new
        {
            task = request.ChatTurnId,
            requestMessage = "",
            files = new[]
            {
                new
                {
                    name = "Label",
                    dataUrl = file.DataUrl
                }
            }
        };
        var payload = JsonSerializer.Serialize(requestModel);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return content;
    }
}

public record TaskResponse(string answer, string? error = null);
