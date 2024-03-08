// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class FeedbackResponse(
    string Prompt,
    string Answer,
    int Rating,
    string Feedback,
    string Model,
    long ElapsedMilliseconds,
    DateTimeOffset Timestamp);
public record class ChatHistoryResponse(
    string Prompt,
    string Answer,
    string Model,
    long ElapsedMilliseconds,
    DateTimeOffset Timestamp);
