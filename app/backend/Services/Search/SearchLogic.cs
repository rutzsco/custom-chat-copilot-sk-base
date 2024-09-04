// Copyright (c) Microsoft. All rights reserved.

using TiktokenSharp;

namespace MinimalApi.Services.Search;

public class SearchLogic<T> where T : IKnowledgeSource
{
    private readonly SearchClient _searchClient;
    private readonly OpenAIClient _openAIClient;
    private readonly string _embeddingModelName;
    private readonly string _embeddingFieldName;
    private readonly List<string> _selectFields;
    private readonly int _documentFilesCount;

    public SearchLogic(OpenAIClient openAIClient, SearchClientFactory factory, string indexName, string embeddingModelName, string embeddingFieldName, List<string> selectFields, int documentFilesCount)
    {
        _searchClient = factory.GetOrCreateClient(indexName);
        _openAIClient = openAIClient;
        _embeddingModelName = embeddingModelName;
        _embeddingFieldName = embeddingFieldName;
        _selectFields = selectFields;
        _documentFilesCount = documentFilesCount;
    }

    public async Task<KnowledgeSourceSummary> SearchAsync(string query, KernelArguments arguments)
    {
        // Generate the embedding for the query
        var queryEmbeddings = await GenerateEmbeddingsAsync(query, _openAIClient);
        var ragSettings = arguments.GetProfileRAGSettingsDefinition();

        var searchOptions = new SearchOptions
        {
            Size = _documentFilesCount,
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(queryEmbeddings.ToArray()) { KNearestNeighborsCount = ragSettings.KNearestNeighborsCount, Fields = { _embeddingFieldName }, Exhaustive = ragSettings.Exhaustive } }
            }
        };


        
        if (ragSettings.UseSemanticRanker)
        {
            searchOptions.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = ragSettings.SemanticConfigurationName
            };
            searchOptions.QueryType = SearchQueryType.Semantic;
        }

        foreach (var field in _selectFields)
        {
            searchOptions.Select.Add(field);
        }

        if (arguments.ContainsName(ContextVariableOptions.SelectedDocuments))
        {
            var sourcefiles = arguments[ContextVariableOptions.SelectedDocuments] as IEnumerable<string>;
            if (sourcefiles.Any())
            {
                var userId = arguments[ContextVariableOptions.UserId] as string;
                var sessionId = arguments[ContextVariableOptions.SessionId] as string;
                var sourcefilesString = string.Join(",", sourcefiles);
                searchOptions.Filter = $"entra_id eq '{userId}' and session_id eq '{sessionId}' and search.in(sourcefile, '{sourcefilesString}')";
            }
        }

        // Perform the search and build the results
        var response = await _searchClient.SearchAsync<T>(query, searchOptions);
        var list = new List<T>();
        foreach (var result in response.Value.GetResults())
        {
            list.Add(result.Document);
        }

        // Filter the results by the maximum request token size
        var sourceSummary = FilterByMaxRequestTokenSize(list, DefaultSettings.MaxRequestTokens, ragSettings.CitationUseSourcePage);
        return sourceSummary;
    }

    private KnowledgeSourceSummary FilterByMaxRequestTokenSize(IReadOnlyList<T> sources, int maxRequestTokens, bool citationUseSourcePage)
    {
        int sourceSize = 0;
        int tokenSize = 0;
        var documents = new List<IKnowledgeSource>();
        var tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
        var sb = new StringBuilder();
        foreach (var document in sources)
        {
            var text = document.FormatAsOpenAISourceText(citationUseSourcePage);
            sourceSize += text.Length;
            tokenSize += tikToken.Encode(text).Count;
            if (tokenSize > maxRequestTokens)
            {
                break;
            }
            documents.Add(document);
            sb.AppendLine(text);
        }
        return new KnowledgeSourceSummary(sb.ToString(), documents);
    }

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddingsAsync(string text, OpenAIClient openAIClient)
    {
        var response = await openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions(_embeddingModelName, new[] { text }));
        return response.Value.Data[0].Embedding;
    }
}
