// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using MinimalApi.Services.Search;

namespace MinimalApi.Services.Skills;

public sealed class RetrieveRelatedDocumentSkill
{
    private readonly IConfiguration _config;
    private readonly SearchClientFactory _searchClientFactory;
    private readonly OpenAIClient _openAIClient;

    public RetrieveRelatedDocumentSkill(IConfiguration config, SearchClientFactory searchClientFactory, OpenAIClient openAIClient)
    {
        _config= config;
        _searchClientFactory = searchClientFactory;
        _openAIClient = openAIClient;
    }

    [KernelFunction("Query"), Description("Search more information")]
    public async Task<string> QueryAsync([Description("search query")] string searchQuery, KernelArguments arguments)
    {
        searchQuery = searchQuery.Replace("\"", string.Empty);
        arguments[ContextVariableOptions.Intent] = searchQuery;

        var searchLogic = new SearchLogic<AIStudioIndexDefinition>(_openAIClient, _searchClientFactory, AIStudioIndexDefinition.IndexName, _config["AOAIEmbeddingsDeployment"], AIStudioIndexDefinition.EmbeddingsFieldName, AIStudioIndexDefinition.SelectFieldNames);
        var result = await searchLogic.SearchAsync(searchQuery);

        if (!result.Sources.Any())
        {
            arguments[ContextVariableOptions.Knowledge] = "NO_SOURCES";
            return "NO_SOURCES";
        }
   
        arguments[ContextVariableOptions.Knowledge] = result.FormattedSourceText;
        arguments[ContextVariableOptions.KnowledgeSummary] = result;
        return result.FormattedSourceText;
    }

    [KernelFunction("QueryV2"), Description("Search more information")]
    public async Task<string> QueryV2Async([Description("search query")] string searchQuery, KernelArguments arguments)
    {
        searchQuery = searchQuery.Replace("\"", string.Empty);
        arguments[ContextVariableOptions.Intent] = searchQuery;

        var searchLogic = new SearchLogic<AISearchIndexerIndexDefinintion>(_openAIClient, _searchClientFactory, AISearchIndexerIndexDefinintion.IndexName, _config["AOAIEmbeddingsDeployment"], AISearchIndexerIndexDefinintion.EmbeddingsFieldName, AISearchIndexerIndexDefinintion.SelectFieldNames);
        var result = await searchLogic.SearchAsync(searchQuery);

        if (!result.Sources.Any())
        {
            arguments[ContextVariableOptions.Knowledge] = "NO_SOURCES";
            return "NO_SOURCES";
        }

        arguments[ContextVariableOptions.Knowledge] = result.FormattedSourceText;
        arguments[ContextVariableOptions.KnowledgeSummary] = result;
        return result.FormattedSourceText;
    }
}
