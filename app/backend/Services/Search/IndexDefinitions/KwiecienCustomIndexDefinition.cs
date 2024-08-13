// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search.IndexDefinitions;

public class KwiecienCustomIndexDefinition : IKnowledgeSource
{
    public required string content { get; set; }

    public required string sourcefile { get; set; }

    public required string sourcepage { get; set; }

    public required string pagenumber { get; set; }

    public string FormatAsOpenAISourceText(bool useSourcepage = false)
    {
        return $"<source><name>{GetFilepath(useSourcepage)}</name><content> {content.Replace('\r', ' ').Replace('\n', ' ')}</content></source>";
    }

    public string GetContent()
    {
        return content;
    }

    public string GetFilepath(bool useSourcepage = false)
    {
        if (useSourcepage)
            return sourcepage;

        return $"{sourcefile}#page={pagenumber}";
    }


    public static string EmbeddingsFieldName = "embedding";
    public static List<string> SelectFieldNames = new List<string> { "content", "sourcefile", "sourcepage", "pagenumber" };
    public static string Name = "KwiecienV1";
}
