// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class FeedbackResponse(
    string Prompt,
    string Answer,
    int Rating,
    string Feedback,
    DateTimeOffset Timestamp);
public record class ChatHistoryResponse(
    string Prompt,
    string Answer,
    DateTimeOffset Timestamp);
