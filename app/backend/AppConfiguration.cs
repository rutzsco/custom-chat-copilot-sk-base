// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi;

public static class AppConfiguration
{
    public static int SearchIndexDocumentCount { get; private set; }

    public static string AzureStorageAccountConnectionString { get; private set; }

    public static string DataProtectionKeyContainer { get; private set; }

    public static bool EnableDataProtectionBlobKeyStorage { get; private set; }

    public static void Load(IConfiguration configuration)
    {
        SearchIndexDocumentCount = configuration.GetValue<int>("SearchIndexDocumentCount", 15);

        AzureStorageAccountConnectionString = configuration.GetValue<string>("AzureStorageAccountConnectionString");
        DataProtectionKeyContainer = configuration.GetValue<string>("SearchIndexSourceFieldName", "dataprotectionkeys");
        EnableDataProtectionBlobKeyStorage = configuration.GetValue<bool>("EnableDataProtectionBlobKeyStorage", true);
    }
}

public static class AppConfigurationSetting
{
    public static string AzureStorageAccountConnectionString { get; } = "AzureStorageAccountConnectionString";
    public static string AzureStorageUserUploadContainer { get; } = "AzureStorageUserUploadContainer";

    public static string IngestionPipelineAPI { get; } = "IngestionPipelineAPI";

    public static string IngestionPipelineAPIKey { get; } = "IngestionPipelineAPIKey";
    
}
