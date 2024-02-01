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
        arguments["intent"] = searchQuery;

        var searchLogic = new SearchLogic<KnowledgeSourceAIStudioSchema>(_openAIClient, _searchClientFactory, KnowledgeSourceAIStudioSchema.IndexName, _config["AOAIEmbeddingsDeployment"], KnowledgeSourceAIStudioSchema.EmbeddingsFieldName, KnowledgeSourceAIStudioSchema.SelectFieldNames);
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
        arguments["intent"] = searchQuery;

        var searchLogic = new SearchLogic<KnowledgeSourceSearchIndexerSchema>(_openAIClient, _searchClientFactory, KnowledgeSourceSearchIndexerSchema.IndexName, _config["AOAIEmbeddingsDeployment"], KnowledgeSourceSearchIndexerSchema.EmbeddingsFieldName, KnowledgeSourceSearchIndexerSchema.SelectFieldNames);
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
