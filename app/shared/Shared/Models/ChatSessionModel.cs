// Copyright (c) Microsoft. All rights reserved.

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shared.Models;


public class ChatSessionModel
{
    public ChatSessionModel(string chatId,  List<ChatHistoryResponse> chatMessages)
    {
        ChatId = chatId;
        ChatMessages = chatMessages;

        var first = chatMessages.First();
        Profile = first.Profile;
        Description = first.Prompt;

        Timestamp = chatMessages.Last().Timestamp;
    }

    public string ChatId { get; set; }

    public string Description { get; set; }

    public string Profile { get; set; }

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
    string Profile,
    string ProfileId,
    SupportingContentRecord[] DataPoints,
    long ElapsedMilliseconds,
    DateTimeOffset Timestamp);
