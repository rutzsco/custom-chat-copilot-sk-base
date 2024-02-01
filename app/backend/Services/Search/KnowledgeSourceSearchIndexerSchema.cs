// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search;

public class KnowledgeSourceSearchIndexerSchema : IKnowledgeSource
{
    public required string filepath { get; set; }

    public required string content { get; set; }

    public string FormatAsOpenAISourceText()
    {
        return $"<source><name>{filepath}</name><content> {content.Replace('\r', ' ').Replace('\n', ' ')}</content></source>";
    }

    public string GetContent()
    {
        return content;
    }

    public string GetFilepath()
    {
        return filepath;
    }
}
