namespace MinimalApi.Extensions;

public static class DefaultSettings
{
    public static int MaxRequestTokens = 6000;

    public static PromptExecutionSettings AISearchRequestSettings = new()
    {
        ExtensionData = new Dictionary<string, object>()
        {
            { "MaxTokens", 1024 },
            { "Temperature", 0.0 }
        }
    };
    public static PromptExecutionSettings AIChatRequestSettings = new()
    {
        ExtensionData = new Dictionary<string, object>()
        {
            { "MaxTokens", 1024 },
            { "Temperature", 0.0 }
        }
    };

    public static string DocumentRetrievalPluginName = "DocumentRetrieval";
    public static string DocumentRetrievalPluginQueryFunctionName = "Query";
    public static string DocumentRetrievalPluginQueryFunctionNameV2 = "QueryV2";

    public static string CosmosDBDatabaseName = "ChatHistory";
    public static string CosmosDBCollectionName = "ChatTurn";

    public static string GenerateSearchQueryPluginName = "GenerateSearchQuery";
    public static string GenerateSearchQueryPluginQueryFunctionName = "GenerateSearchQuery";

    public static string ChatPluginName = "Chat";
    public static string ChatPluginFunctionName = "Chat";
}
