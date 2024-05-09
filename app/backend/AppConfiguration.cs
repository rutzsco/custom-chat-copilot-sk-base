// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi;

public static class AppConfiguration
{
    public static string SearchIndexEmbeddingFieldName { get; private set; }
    public static string SearchIndexContentFieldName { get; private set; }
    public static string SearchIndexSourceFieldName { get; private set; }
    public static int SearchIndexDocumentCount { get; private set; }

    public static string AzureStorageAccountConnectionString { get; private set; }

    public static string DataProtectionKeyContainer { get; private set; }

    public static bool EnableDataProtectionBlobKeyStorage { get; private set; }

    public static void Load(IConfiguration configuration)
    {
        SearchIndexEmbeddingFieldName = configuration.GetValue<string>("SearchIndexEmbeddingFieldName", "contentVector");
        SearchIndexContentFieldName = configuration.GetValue<string>("SearchIndexContentFieldName", "content");
        SearchIndexSourceFieldName = configuration.GetValue<string>("SearchIndexSourceFieldName", "filepath");
        SearchIndexDocumentCount = configuration.GetValue<int>("SearchIndexDocumentCount", 15);

        AzureStorageAccountConnectionString = configuration.GetValue<string>("AzureStorageAccountConnectionString");
        DataProtectionKeyContainer = configuration.GetValue<string>("SearchIndexSourceFieldName", "dataprotectionkeys");
        EnableDataProtectionBlobKeyStorage = configuration.GetValue<bool>("EnableDataProtectionBlobKeyStorage", false);
    }
}

public static class AppConfigurationSetting
{
    public static string AzureStorageAccountConnectionString { get; } = "AzureStorageAccountConnectionString";
    public static string AzureStorageUserUploadContainer { get; } = "AzureStorageUserUploadContainer";

    
}
