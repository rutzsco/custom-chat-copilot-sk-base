// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

public static class DefaultSettings
{
    public static double Temperature = .3;

    public static int MaxResponseTokens = 1_024;
    public static int MaxRequestTokens = 6000;

    public static string GPT3ModelType = "gpt-3.5-turbo-16k";
    public static string GPT4ModelType = "gpt-4-32k";

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

    public static string CosmosDBDatabaseName = "ChatHistory";
    public static string CosmosDBCollectionName = "ChatTurn";

    public static string GenerateSearchQueryPluginName = "GenerateSearchQuery";
    public static string GenerateSearchQueryPluginQueryFunctionName = "GenerateSearchQuery";

    public static string ChatPluginName = "Chat";
    public static string ChatPluginFunctionName = "Chat";
}
