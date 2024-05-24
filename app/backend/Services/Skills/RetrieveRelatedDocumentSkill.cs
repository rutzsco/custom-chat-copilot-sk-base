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

    [KernelFunction("Retrieval"), Description("Search more information")]
    public async Task<string> RetrievalAsync([Description("search query")] string searchQuery, KernelArguments arguments)
    {
        searchQuery = searchQuery.Replace("\"", string.Empty);
        arguments[ContextVariableOptions.Intent] = searchQuery;
        var profile = arguments[ContextVariableOptions.Profile] as ProfileDefinition;

        var searchLogic = ResolveRetrievalLogic(_openAIClient, _searchClientFactory, profile.RAGSettings, _config["AOAIEmbeddingsDeployment"], profile.RAGSettings.DocumentRetrievalPluginQueryFunctionName);
        var result = await searchLogic(searchQuery, arguments);

        if (!result.Sources.Any())
        {
            arguments[ContextVariableOptions.Knowledge] = "NO_SOURCES";
            return "NO_SOURCES";
        }

        arguments[ContextVariableOptions.Knowledge] = result.FormattedSourceText;
        arguments[ContextVariableOptions.KnowledgeSummary] = result;
        return result.FormattedSourceText;
    }

    private Func<string, KernelArguments,Task<KnowledgeSourceSummary>> ResolveRetrievalLogic(OpenAIClient client, SearchClientFactory factory, RAGSettingsSummary ragSettings, string embeddingModelName, string version)
    {
        async Task<KnowledgeSourceSummary> func1(string searchQuery, KernelArguments arguments)
        {
            var logic = new SearchLogic<AIStudioIndexDefinition>(client, factory, ragSettings.DocumentRetrievalIndexName, embeddingModelName, AIStudioIndexDefinition.EmbeddingsFieldName, AIStudioIndexDefinition.SelectFieldNames, ResolveDocumentCount(ragSettings.DocumentRetrievalDocumentCount));
            return await logic.SearchAsync(searchQuery, arguments);
        }

        async Task<KnowledgeSourceSummary> func2(string searchQuery, KernelArguments arguments)
        {
            var logic = new SearchLogic<AISearchIndexerIndexDefinintion>(client, factory, ragSettings.DocumentRetrievalIndexName, embeddingModelName, AISearchIndexerIndexDefinintion.EmbeddingsFieldName, AISearchIndexerIndexDefinintion.SelectFieldNames, ResolveDocumentCount(ragSettings.DocumentRetrievalDocumentCount));
            return await logic.SearchAsync(searchQuery, arguments);
        }

        async Task<KnowledgeSourceSummary> func3(string searchQuery, KernelArguments arguments)
        {
            var logic = new SearchLogic<KwiecienCustomIndexDefinition>(client, factory, ragSettings.DocumentRetrievalIndexName, embeddingModelName, KwiecienCustomIndexDefinition.EmbeddingsFieldName, KwiecienCustomIndexDefinition.SelectFieldNames, ResolveDocumentCount(ragSettings.DocumentRetrievalDocumentCount));
            return await logic.SearchAsync(searchQuery, arguments);
        }

        async Task<KnowledgeSourceSummary> func4(string searchQuery, KernelArguments arguments)
        {
            var logic = new SearchLogic<KwiecienCustomIndexDefinitionV2>(client, factory, ragSettings.DocumentRetrievalIndexName, embeddingModelName, KwiecienCustomIndexDefinitionV2.EmbeddingsFieldName, KwiecienCustomIndexDefinitionV2.SelectFieldNames, ResolveDocumentCount(ragSettings.DocumentRetrievalDocumentCount));
            return await logic.SearchAsync(searchQuery, arguments);
        }

        if (version == AIStudioIndexDefinition.Name)
            return func1;
        if (version == AISearchIndexerIndexDefinintion.Name)
            return func2;
        if (version == KwiecienCustomIndexDefinition.Name)
            return func3;
        if (version == KwiecienCustomIndexDefinitionV2.Name)
            return func4;

        throw new InvalidOperationException("Invalid search implementation.");
    }

    private static int ResolveDocumentCount(int documentRetrievalDocumentCount)
    {
        if (documentRetrievalDocumentCount > 0)
        {
            return documentRetrievalDocumentCount;
        }
        return AppConfiguration.SearchIndexDocumentCount;
    }
}
