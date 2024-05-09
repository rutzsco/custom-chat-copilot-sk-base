// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class ChatHistoryResponse(
    string ChatId,
    string Prompt,
    string Answer,
    int Rating,
    string Feedback,
    string Model,
    long ElapsedMilliseconds,
    DateTimeOffset Timestamp);
