// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using MinimalApi.Services.Profile;
using MinimalApi.Services.Search;
using MinimalApi.Services.Search.IndexDefinitions;

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
        var profile = arguments[ContextVariableOptions.Profile] as ProfileDefinition;

        var searchLogic = new SearchLogic<AIStudioIndexDefinition>(_openAIClient, _searchClientFactory, profile.RAGSettings.DocumentRetrievalIndexName, _config["AOAIEmbeddingsDeployment"], AIStudioIndexDefinition.EmbeddingsFieldName, AIStudioIndexDefinition.SelectFieldNames);
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
        var profile = arguments[ContextVariableOptions.Profile] as ProfileDefinition;

        var searchLogic = new SearchLogic<AISearchIndexerIndexDefinintion>(_openAIClient, _searchClientFactory, profile.RAGSettings.DocumentRetrievalIndexName, _config["AOAIEmbeddingsDeployment"], AISearchIndexerIndexDefinintion.EmbeddingsFieldName, AISearchIndexerIndexDefinintion.SelectFieldNames);
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

    [KernelFunction("QueryV3"), Description("Search more information")]
    public async Task<string> QueryV3Async([Description("search query")] string searchQuery, KernelArguments arguments)
    {
        searchQuery = searchQuery.Replace("\"", string.Empty);
        arguments[ContextVariableOptions.Intent] = searchQuery;
        var profile = arguments[ContextVariableOptions.Profile] as ProfileDefinition;

        var searchLogic = new SearchLogic<KwiecienCustomIndexDefinition>(_openAIClient, _searchClientFactory, profile.RAGSettings.DocumentRetrievalIndexName, _config["AOAIEmbeddingsDeployment"], KwiecienCustomIndexDefinition.EmbeddingsFieldName, KwiecienCustomIndexDefinition.SelectFieldNames);
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
