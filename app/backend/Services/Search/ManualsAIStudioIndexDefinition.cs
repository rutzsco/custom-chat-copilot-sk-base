﻿// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search;

public class ManualsAIStudioIndexDefinition : IKnowledgeSource
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

    public int GetPage()
    {
        return 0;
    }

    public static string IndexName = "manuals";
    public static string EmbeddingsFieldName = "contentVector";
    public static List<string> SelectFieldNames = new List<string> { "content", "filepath" };
}
