﻿// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi;

public static class AppConfiguration
{
    public static int SearchIndexDocumentCount { get; private set; }

    public static string AzureStorageAccountConnectionString { get; private set; }

    public static string DataProtectionKeyContainer { get; private set; }

    public static bool EnableDataProtectionBlobKeyStorage { get; private set; }

    public static string UserDocumentUploadBlobStorageContentContainer { get; private set; }
    public static string UserDocumentUploadBlobStorageExtractContainer { get; private set; }

    public static void Load(IConfiguration configuration)
    {
        SearchIndexDocumentCount = configuration.GetValue<int>("SearchIndexDocumentCount", 15);

        AzureStorageAccountConnectionString = configuration.GetValue<string>("AzureStorageAccountConnectionString");
        DataProtectionKeyContainer = configuration.GetValue<string>("SearchIndexSourceFieldName", "dataprotectionkeys");
        EnableDataProtectionBlobKeyStorage = configuration.GetValue<bool>("EnableDataProtectionBlobKeyStorage", true);

        UserDocumentUploadBlobStorageContentContainer = configuration.GetValue<string>("UserDocumentUploadBlobStorageContentContainer", "content");

        UserDocumentUploadBlobStorageExtractContainer = configuration.GetValue<string>("UserDocumentUploadBlobStorageExtractContainer", "content-extract");
    }
}

public static class AppConfigurationSetting
{
    public static string UseManagedIdentityResourceAccess { get; } = "UseManagedIdentityResourceAccess";
    
    // CosmosDB
    public static string CosmosDBEndpoint { get; } = "CosmosDBEndpoint";
    public static string CosmosDBConnectionString { get; } = "CosmosDBConnectionString";

    // Azure Search
    public static string AzureSearchServiceEndpoint { get; } = "AzureSearchServiceEndpoint";
    public static string AzureSearchServiceKey { get; } = "AzureSearchServiceKey";
    // Azure Storage
    public static string AzureStorageAccountEndpoint { get; } = "AzureStorageAccountEndpoint";
    public static string AzureStorageAccountConnectionString { get; } = "AzureStorageAccountConnectionString";
    public static string AzureStorageUserUploadContainer { get; } = "AzureStorageUserUploadContainer";
    public static string AzureStorageContainer { get; } = "AzureStorageContainer";
    

    // Ingestion Pipeline
    public static string IngestionPipelineAPI { get; } = "IngestionPipelineAPI";
    public static string IngestionPipelineAPIKey { get; } = "IngestionPipelineAPIKey";
    
}
