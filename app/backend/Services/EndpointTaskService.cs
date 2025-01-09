// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using MinimalApi.Services.Profile;

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
        var sb = new StringBuilder();
        var apiRequest = new HttpRequestMessage(HttpMethod.Post, _configuration[profile.AssistantEndpointSettings.APIEndpointSetting]);
        apiRequest.Headers.Add("X-Api-Key", _configuration[profile.AssistantEndpointSettings.APIEndpointKeySetting]);
        apiRequest.Content = BuildTaskRequest(request);

        var response = await _httpClient.SendAsync(apiRequest);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();


        TaskResponse taskResponse = JsonSerializer.Deserialize<TaskResponse>(payload);
        var thoughts = new List<ThoughtRecord>();
        foreach (var thought in taskResponse.thoughtProcess)
        {
            thoughts.Add(new ThoughtRecord(FormatLogStep(thought), thought.content));
        }

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

    private string FormatLogStep(WorkflowLogEntry logEntry)
    {
        if (logEntry.diagnostics == null)
            return $"{logEntry.agentName}-{logEntry.step}";

        return $"{logEntry.agentName}-{logEntry.step} ({logEntry.diagnostics.elapsedMilliseconds} milliseconds)";
    }
}

public record TaskResponse(string answer, IEnumerable<WorkflowLogEntry> thoughtProcess, string? error = null);

public record WorkflowLogEntry(string agentName, string step, string? content, WorkflowStepDiagnostics? diagnostics);

public record WorkflowStepDiagnostics(long elapsedMilliseconds);


