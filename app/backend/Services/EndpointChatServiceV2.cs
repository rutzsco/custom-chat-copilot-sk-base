// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.Profile;

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
        
        var stateThread = await ResolveThreadIdAsync(profile, request);

        var payload = JsonSerializer.Serialize(new { thread_id = stateThread, message = request.LastUserQuestion});
        var url = $"{_configuration[profile.AssistantEndpointSettings.APIEndpointSetting]}/run_assistant";
        var apiRequest = new HttpRequestMessage(HttpMethod.Post, url);
        //apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);
        apiRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var sb = new StringBuilder();
   


        var response = await _httpClient.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using (Stream stream = await response.Content.ReadAsStreamAsync())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Process each line or chunk as it streams in
                sb.Append(line + "\n");
                yield return new ChatChunkResponse(line + "\n");
                await Task.Yield();
            }
        }

        var thoughts = new List<ThoughtRecord>();
        thoughts.Add(new ThoughtRecord("Assistant Response", sb.ToString()));

        yield return new ChatChunkResponse("", new ApproachResponse(sb.ToString(), null, new ResponseContext(profile.Name, null, thoughts.ToArray(), request.ChatTurnId, request.ChatId, null)));
    }
    private async Task<string> ResolveThreadIdAsync(ProfileDefinition profile, ChatRequest request)
    {
        if (_threadToChatSession.ContainsKey(request.ChatId.ToString()))
        {
            return _threadToChatSession[request.ChatId.ToString()];
        }


        if (request.FileUploads.Any())
        {
            var file = request.FileUploads.First();
            var payload = new { file_name = file.FileName, file_data = file.DataUrl.Replace("data:text/csv;base64,","").Replace("data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,", "") };
            var url = $"{_configuration[profile.AssistantEndpointSettings.APIEndpointSetting]}/upload_file_and_create_thread";
            var apiRequest = new HttpRequestMessage(HttpMethod.Post, url);
            apiRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(apiRequest);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent = responseContent.Trim('"');
            _threadToChatSession.Add(request.ChatId.ToString(), responseContent);
            return responseContent;
        }
        else
        {
            var payload = new { };
            var url = $"{_configuration[profile.AssistantEndpointSettings.APIEndpointSetting]}/create_thread";
            var apiRequest = new HttpRequestMessage(HttpMethod.Post, url);
            //apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);
            apiRequest.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(apiRequest);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent = responseContent.Trim('"');
            _threadToChatSession.Add(request.ChatId.ToString(), responseContent);
            return responseContent;
        }



    }
}
