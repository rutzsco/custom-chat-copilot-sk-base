// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.ChatHistory;

public static class ChatHistoryExtension
{
    public static IEnumerable<ChatHistoryResponse> AsFeedbackResponse(this List<ChatMessageRecord> records)
    {
        foreach (var item in records)
        {
            if (item.Context != null && item.Context.Diagnostics != null)
            {
                if (item.Rating == null)
                {
                    yield return new ChatHistoryResponse(
                        item.ChatId,
                        item.Prompt,
                        item.Content,
                        -1,
                        string.Empty,
                        item.Context.Diagnostics.ModelDeploymentName,
                        item.Context.Diagnostics.WorkflowDurationMilliseconds,
                        item.Timestamp);
                }
                else
                {
                    yield return new ChatHistoryResponse(
                        item.ChatId,
                        item.Prompt,
                        item.Content,
                        item.Rating.Rating,
                        item.Rating.Feedback,
                        item.Context.Diagnostics.ModelDeploymentName,
                        item.Context.Diagnostics.WorkflowDurationMilliseconds,
                        item.Timestamp);
                }

            }
            else
            {
                if (item.Rating == null)
                {
                    yield return new ChatHistoryResponse(
                    item.ChatId,
                    item.Prompt,
                    item.Content,
                    0,
                    string.Empty,
                    "Unavialable",
                    0,
                    item.Timestamp);
                }
                else
                {
                    yield return new ChatHistoryResponse(
                        item.ChatId,
                        item.Prompt,
                        item.Content,
                        item.Rating.Rating,
                        item.Rating.Feedback,
                        "Unavialable",
                        0,
                        item.Timestamp);
                }
            }
        }
    }
}
