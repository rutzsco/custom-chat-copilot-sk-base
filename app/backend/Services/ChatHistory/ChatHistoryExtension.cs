// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.ChatHistory;

public static class ChatHistoryExtension
{
    public static IEnumerable<FeedbackResponse> AsFeedbackResponse(this List<ChatMessageRecord> records)
    {
        foreach (var item in records)
        {
            if (item.Context != null && item.Context.Diagnostics != null)
            {
                yield return new FeedbackResponse(
                    item.Prompt,
                    item.Content,
                    0,
                    string.Empty,
                    item.Context.Diagnostics.ModelDeploymentName,
                    item.Context.Diagnostics.WorkflowDurationMilliseconds,
                    item.Timestamp);
            }
            else
            {
                yield return new FeedbackResponse(
                    item.Prompt,
                    item.Content,
                    0,
                    string.Empty,
                    "Unavialable",
                    0,
                    item.Timestamp);
            }
        }
    }
}
