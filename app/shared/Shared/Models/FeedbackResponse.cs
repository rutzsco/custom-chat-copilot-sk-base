// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;


public class ChatSessionModel
{
    public ChatSessionModel(string chatId, List<ChatHistoryResponse> chatMessages)
    {
        ChatId = chatId;
        ChatMessages = chatMessages;
        Description = chatMessages.First().Prompt;
        Timestamp = chatMessages.Last().Timestamp;
    }

    public string ChatId { get; set; }

    public string Description { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public List<ChatHistoryResponse> ChatMessages { get; set; }
}

public record class ChatHistoryResponse(
    string ChatId,
    string Prompt,
    string Answer,
    int Rating,
    string Feedback,
    string Model,
    long ElapsedMilliseconds,
    DateTimeOffset Timestamp);
