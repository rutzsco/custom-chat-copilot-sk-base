// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.ComponentModel;
using TiktokenSharp;

namespace MinimalApi.Services.Skills;

public sealed class RetrieveRelatedDocumentSkill
{
    private readonly SearchClientFacade _searchClientFacade;
    private readonly OpenAIClient _openAIClient;

    public RetrieveRelatedDocumentSkill(SearchClientFacade searchClientFacade, OpenAIClient openAIClient)
    {
        _searchClientFacade = searchClientFacade;
        _openAIClient = openAIClient;
    }

    [KernelFunction("Query"), Description("Search more information")]
    public async Task<string> QueryAsync(
        [Description("search query")] string searchQuery,
        KernelArguments arguments)
    {
        searchQuery = searchQuery.Replace("\"", string.Empty);
        arguments["intent"] = searchQuery;

        IReadOnlyList<KnowledgeSource> sources = new List<KnowledgeSource>();
        sources = await _searchClientFacade.SimpleHybridSearchAsync(_openAIClient, searchQuery);
        if (!sources.Any())
        {
            arguments["knowledge"] = "NO_SOURCES";
            return "NO_SOURCES";
        }

        int sourceSize = 0;
        int tokenSize = 0;
        var documents = new List<KnowledgeSource>();
        var sb = new StringBuilder();
        var tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
        foreach (var document in sources)
        {
            var text = document.FormatAsOpenAISourceText();
            sourceSize += text.Length;
            tokenSize += tikToken.Encode(text).Count;
            if (tokenSize > DefaultSettings.MaxRequestTokens)
            {
                break;
            }
            documents.Add(document);
            sb.AppendLine(text);
        }
        var documentContents = sb.ToString();

        var result = sb.ToString();
        arguments["knowledge"] = result;
        arguments["knowledge-json"] = JsonSerializer.Serialize(documents);
        return result;
    }
}
