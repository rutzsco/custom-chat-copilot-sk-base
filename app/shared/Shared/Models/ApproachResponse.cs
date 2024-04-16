// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;


public record SupportingContentRecord(string Title, string Content);
public record ThoughtRecord(string Title, string Description);

public record ResponseContext(SupportingContentRecord[] DataPoints, ThoughtRecord[] Thoughts, Guid MessageId, Guid ChatId, Diagnostics? Diagnostics);


public record ApproachResponse(
    string Answer,
    string? Thoughts,
    SupportingContentRecord[] DataPoints,
    string CitationBaseUrl,
    ResponseContext Context,
    string? Error = null);
