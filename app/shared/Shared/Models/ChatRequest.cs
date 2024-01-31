// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class ChatRequest(
    Guid ChatId,
    Guid ChatTurnId,
    ChatTurn[] History,
    Dictionary<string, bool> OptionFlags,
    Approach Approach,
    RequestOverrides? Overrides = null) : ApproachRequest(Approach)
{
    public string? LastUserQuestion => History?.LastOrDefault()?.User;
}

public record class ChatRatingRequest(Guid ChatId, Guid MessageId, int Rating, string Feedback, Approach Approach) : ApproachRequest(Approach);
