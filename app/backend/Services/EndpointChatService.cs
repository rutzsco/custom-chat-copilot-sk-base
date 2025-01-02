// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.Profile;

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


    public async IAsyncEnumerable<ChatChunkResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(request.History);

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, _configuration[profile.AssistantEndpointSettings.APIEndpointSetting]);
        apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);
        apiRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using (var responseStream = await response.Content.ReadAsStreamAsync())
        {
            await foreach (ChatChunkResponse chunk in JsonSerializer.DeserializeAsyncEnumerable<ChatChunkResponse>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultBufferSize = 32 }))
            {
                if (chunk == null)
                    continue;

                yield return chunk;
                await Task.Yield();
            }
        }
 
    }
}
