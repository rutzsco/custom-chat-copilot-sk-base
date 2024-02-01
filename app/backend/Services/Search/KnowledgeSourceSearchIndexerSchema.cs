// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search;

public class KnowledgeSourceSearchIndexerSchema : IKnowledgeSource
{
    public required string title { get; set; }

    public required string chunk { get; set; }

    public required string chunk_id { get; set; }

    public string FormatAsOpenAISourceText()
    {
        return $"<source><name>{title}</name><content> {chunk.Replace('\r', ' ').Replace('\n', ' ')}</content></source>";
    }

    public string GetContent()
    {
        return chunk;
    }

    public string GetFilepath()
    {
        return title;
    }

    public static string IndexName = "manuals-vi";
    public static string EmbeddingsFieldName = "vector";
    public static List<string> SelectFieldNames = new List<string> { "title", "chunk_id", "chunk" };
}
