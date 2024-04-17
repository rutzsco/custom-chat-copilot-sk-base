// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search.IndexDefinitions;

public class KwiecienCustomIndexDefinition : IKnowledgeSource
{
    public required string sourcefile { get; set; }

    public required string content { get; set; }

    public required string sourcepage { get; set; }

    public required string sourcepageview { get; set; }

    public string FormatAsOpenAISourceText()
    {
        return $"<source><name>{sourcepageview}</name><content> {content.Replace('\r', ' ').Replace('\n', ' ')}</content></source>";
    }

    public string GetContent()
    {
        return content;
    }

    public string GetFilepath()
    {
        return sourcepageview;
    }


    public static string EmbeddingsFieldName = "embedding";
    public static List<string> SelectFieldNames = new List<string> { "content", "sourcefile", "sourcepage", "sourcepageview" };
}
