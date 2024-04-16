// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;


public record SupportingContentRecord(string Title, string Content);
public record ThoughtRecord(string Title, string Description);

public record ResponseContext(SupportingContentRecord[] DataPoints, ThoughtRecord[] Thoughts);


public record ApproachResponse(
    string Answer,
    string? Thoughts,
    SupportingContentRecord[] DataPoints,
    string CitationBaseUrl,
    Guid MessageId,
    Guid ChatId,
    Diagnostics? Diagnostics,
    ResponseContext Context,
    string? Error = null);
