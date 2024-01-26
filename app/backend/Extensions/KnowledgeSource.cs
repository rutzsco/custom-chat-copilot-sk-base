// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

public class KnowledgeSource
{
    public required string filepath { get; set; }

    public required string content { get; set; }

    public string FormatAsOpenAISourceText()
    {
        return $"<source><name>{filepath}</name><content> {content.Replace('\r', ' ').Replace('\n', ' ')}</content></source>";
    }
}
