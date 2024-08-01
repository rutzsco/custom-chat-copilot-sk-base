// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.Profile;

namespace MinimalApi.Services.ChatHistory;

public static class ChatHistoryExtension
{
    public static List<ChatHistoryResponse> AsFeedbackResponse(this List<ChatMessageRecord> records)
    {
        var chatHistoryResponses = new List<ChatHistoryResponse>();

        foreach (var item in records)
        {
            var profileName = item.Context?.Profile ?? "Unavailable";
            var profileId = ProfileDefinition.All.SingleOrDefault(x => x.Name == profileName)?.Id ?? "Unavailable";

            var rating = item.Rating?.Rating ?? 0;
            var feedback = item.Rating?.Feedback ?? string.Empty;

            var modelDeploymentName = item.Context?.Diagnostics?.ModelDeploymentName ?? "Unavailable";
            var workflowDurationMilliseconds = item.Context?.Diagnostics?.WorkflowDurationMilliseconds ?? 0;

            var dataPoints = item.Context?.DataPoints ?? Array.Empty<SupportingContentRecord>();

            var chatHistoryResponse = new ChatHistoryResponse(
                item.ChatId,
                item.Prompt,
                item.Content,
                rating,
                feedback,
                modelDeploymentName,
                profileName,
                profileId,
                dataPoints,
                workflowDurationMilliseconds,
                item.Timestamp);

            chatHistoryResponses.Add(chatHistoryResponse);
        }

        return chatHistoryResponses;
    }
}
