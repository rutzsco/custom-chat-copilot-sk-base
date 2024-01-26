// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi;

public static class AppConfiguration
{
    public static string SearchIndexEmbeddingFieldName { get; private set; }
    public static string SearchIndexContentFieldName { get; private set; }
    public static string SearchIndexSourceFieldName { get; private set; }
    public static int SearchIndexDocumentCount { get; private set; }

    public static void Load(IConfiguration configuration)
    {
        SearchIndexEmbeddingFieldName = configuration.GetValue<string>("SearchIndexEmbeddingFieldName", "contentVector");
        SearchIndexContentFieldName = configuration.GetValue<string>("SearchIndexContentFieldName", "content");
        SearchIndexSourceFieldName = configuration.GetValue<string>("SearchIndexSourceFieldName", "filepath");
        SearchIndexDocumentCount = configuration.GetValue<int>("SearchIndexDocumentCount", 15);
    }
}
