// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Azure.AI.OpenAI;
using static MudBlazor.CategoryTypes;

namespace MinimalApi.Extensions;

internal static class SearchClientExtensions
{
    internal static async Task<IReadOnlyList<KnowledgeSource>> SimpleHybridSearchAsync(this SearchClientFacade searchClientFacade, OpenAIClient openAIClient, string query)
    {
        return await searchClientFacade.ContentSearchClient.ManualsSearchAsync(openAIClient, query);
    }

    internal static async Task<IReadOnlyList<KnowledgeSource>> ManualsSearchAsync(this SearchClient searchClient, OpenAIClient openAIClient, string query)
    {
        // Generate the embedding for the query  
        var queryEmbeddings = await GenerateEmbeddingsAsync(query, openAIClient);

        // Perform the vector similarity search  
        var searchOptions = new SearchOptions
        {
            Size = AppConfiguration.SearchIndexDocumentCount,
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(queryEmbeddings.ToArray()) { KNearestNeighborsCount = 3, Fields = { AppConfiguration.SearchIndexEmbeddingFieldName } } }
            },
            Select = { AppConfiguration.SearchIndexSourceFieldName, AppConfiguration.SearchIndexContentFieldName }
        };


        var response = await searchClient.SearchAsync<KnowledgeSource>(query, searchOptions);
        var list = new List<KnowledgeSource>();
        foreach (var result in response.Value.GetResults())
        {
            list.Add(result.Document);
        }
        return list;
    }

    private static async Task<ReadOnlyMemory<float>> GenerateEmbeddingsAsync(string text, OpenAIClient openAIClient)
    {
        var response = await openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions("text-embedding", new[] { text }));
        return response.Value.Data[0].Embedding;
    }
}
