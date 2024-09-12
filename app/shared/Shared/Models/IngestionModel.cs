// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record IngestionRequest(string SourceCountainer, string ExtractContainer, string IndexStemName);
