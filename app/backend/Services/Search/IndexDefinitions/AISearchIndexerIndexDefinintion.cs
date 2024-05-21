// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;

namespace MinimalApi.Services.Search.IndexDefinitions;

public class AISearchIndexerIndexDefinintion : IKnowledgeSource
{
    public required string title { get; set; }

    public required string chunk { get; set; }

    public required string chunk_id { get; set; }

    public string FormatAsOpenAISourceText()
    {
        return $"<source><name>{title}#page={GetPage()}</name><content> {chunk.Replace('\r', ' ').Replace('\n', ' ')}</content></source>";
    }

    public string GetContent()
    {
        return chunk;
    }

    public string GetFilepath()
    {
        return title;
    }

    public int GetPage()
    {
        try
        {
            string pattern = "_pages_(\\d+)";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(chunk_id);
            string pageNumber = match.Groups[1].Value;

            return Convert.ToInt32(pageNumber);
        }
        catch
        {
            return 0;
        }
    }

    public static string EmbeddingsFieldName = "text_vector";
    public static List<string> SelectFieldNames = new List<string> { "title", "chunk_id", "chunk" };
}
