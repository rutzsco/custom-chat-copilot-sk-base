// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;


public record class DocumentSummary(
    string Id,
    string Name,
    string ContentType,
    long Size,
    DocumentProcessingStatus Status,
    string StatusMessage,
    double ProcessingProgress,
    DateTimeOffset Timestamp,
    string Metadata = "");
