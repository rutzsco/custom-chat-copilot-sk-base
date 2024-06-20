
// Copyright (c) Microsoft. All rights reserved.

using System.Security.Policy;
using Azure;
using Azure.Storage;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Search;
using MinimalApi.Services.Skills;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    private static readonly DefaultAzureCredential s_azureCredential = new();

    internal static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        
        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccountConnectionString = config[AppConfigurationSetting.AzureStorageAccountConnectionString];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountConnectionString);

            var blobServiceClient = new BlobServiceClient(azureStorageAccountConnectionString);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var azureStorageContainer = configuration[AppConfigurationSetting.AzureStorageContainer];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });


        services.AddSingleton<OpenAIClientFacade>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var deployedModelName3 = config["AOAIStandardChatGptDeployment"];
            var azureOpenAiServiceEndpoint3 = config["AOAIStandardServiceEndpoint"];
            var azureOpenAiServiceKey3 = config["AOAIStandardServiceKey"];

            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey3);

            var deployedModelName4 = config["AOAIPremiumChatGptDeployment"];
            var azureOpenAiServiceEndpoint4 = config["AOAIPremiumServiceEndpoint"];
            var azureOpenAiServiceKey4 = config["AOAIPremiumServiceKey"];
            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey4);

            // Build Plugins
            var searchClientFactory = sp.GetRequiredService<SearchClientFactory>();
            var openAIClient3 = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint3), new AzureKeyCredential(azureOpenAiServiceKey3));
            var retrieveRelatedDocumentPlugin3 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient3);
            
            var openAIClient4 = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint3), new AzureKeyCredential(azureOpenAiServiceKey3));
            var retrieveRelatedDocumentPlugin4 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient4);
            
            var generateSearchQueryPlugin = new GenerateSearchQuerySkill();
            var chatPlugin = new ChatSkill();

            // Build Kernels
            Kernel kernel3 = Kernel.CreateBuilder()
               .AddAzureOpenAIChatCompletion(deployedModelName3, azureOpenAiServiceEndpoint3, azureOpenAiServiceKey3)
               .Build();

            Kernel kernel4 = Kernel.CreateBuilder()
               .AddAzureOpenAIChatCompletion(deployedModelName4, azureOpenAiServiceEndpoint4, azureOpenAiServiceKey4)
               .Build();

            kernel3.ImportPluginFromObject(retrieveRelatedDocumentPlugin3, DefaultSettings.DocumentRetrievalPluginName);
            kernel3.ImportPluginFromObject(generateSearchQueryPlugin, DefaultSettings.GenerateSearchQueryPluginName);
            kernel3.ImportPluginFromObject(chatPlugin, DefaultSettings.ChatPluginName);

            kernel4.ImportPluginFromObject(retrieveRelatedDocumentPlugin4, DefaultSettings.DocumentRetrievalPluginName);
            kernel4.ImportPluginFromObject(generateSearchQueryPlugin, DefaultSettings.GenerateSearchQueryPluginName);
            kernel4.ImportPluginFromObject(chatPlugin, DefaultSettings.ChatPluginName);

            return new OpenAIClientFacade(deployedModelName3, kernel3, deployedModelName4, kernel4);
        });
        services.AddSingleton((sp) => {
            var config = sp.GetRequiredService<IConfiguration>();
            var cosmosDBConnectionString = config[AppConfigurationSetting.CosmosDBConnectionString];
            CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(cosmosDBConnectionString);
            return configurationBuilder
                    .Build();
        }); ;


        services.AddSingleton<SearchClientFactory>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new SearchClientFactory(config, null, new AzureKeyCredential(config[AppConfigurationSetting.AzureSearchServiceKey]));
        });

        services.AddSingleton<ChatHistoryService>();

        services.AddSingleton<ChatService>();
        services.AddSingleton<ReadRetrieveReadChatService>();
        services.AddSingleton<ReadRetrieveReadStreamingChatService>();

        services.AddSingleton<AzureBlobStorageService>();
        services.AddSingleton<DocumentService>();
        services.AddHttpClient<DocumentService, DocumentService>();

        return services;
    }

    internal static IServiceCollection AddAzureWithMICredentialsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var azureStorageAccountEndpoint = configuration[AppConfigurationSetting.AzureStorageAccountEndpoint];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountEndpoint);

            var blobServiceClient = new BlobServiceClient(new Uri(azureStorageAccountEndpoint), s_azureCredential);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var azureStorageContainer = configuration[AppConfigurationSetting.AzureStorageContainer];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });


        services.AddSingleton<OpenAIClientFacade>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var deployedModelName3 = config["AOAIStandardChatGptDeployment"];
            var azureOpenAiServiceEndpoint3 = config["AOAIStandardServiceEndpoint"];

            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint3);

            var deployedModelName4 = config["AOAIPremiumChatGptDeployment"];
            var azureOpenAiServiceEndpoint4 = config["AOAIPremiumServiceEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint4);

            // Build Plugins
            var searchClientFactory = sp.GetRequiredService<SearchClientFactory>();
            var openAIClient3 = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint3), s_azureCredential);
            var retrieveRelatedDocumentPlugin3 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient3);

            var openAIClient4 = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint3), s_azureCredential);
            var retrieveRelatedDocumentPlugin4 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient4);

            var generateSearchQueryPlugin = new GenerateSearchQuerySkill();
            var chatPlugin = new ChatSkill();

            // Build Kernels
            Kernel kernel3 = Kernel.CreateBuilder()
               .AddAzureOpenAIChatCompletion(deployedModelName3, azureOpenAiServiceEndpoint3, s_azureCredential)
               .Build();

            Kernel kernel4 = Kernel.CreateBuilder()
               .AddAzureOpenAIChatCompletion(deployedModelName4, azureOpenAiServiceEndpoint4, s_azureCredential)
               .Build();

            kernel3.ImportPluginFromObject(retrieveRelatedDocumentPlugin3, DefaultSettings.DocumentRetrievalPluginName);
            kernel3.ImportPluginFromObject(generateSearchQueryPlugin, DefaultSettings.GenerateSearchQueryPluginName);
            kernel3.ImportPluginFromObject(chatPlugin, DefaultSettings.ChatPluginName);

            kernel4.ImportPluginFromObject(retrieveRelatedDocumentPlugin4, DefaultSettings.DocumentRetrievalPluginName);
            kernel4.ImportPluginFromObject(generateSearchQueryPlugin, DefaultSettings.GenerateSearchQueryPluginName);
            kernel4.ImportPluginFromObject(chatPlugin, DefaultSettings.ChatPluginName);

            return new OpenAIClientFacade(deployedModelName3, kernel3, deployedModelName4, kernel4);
        });
        services.AddSingleton((sp) => {
            var config = sp.GetRequiredService<IConfiguration>();
            var cosmosDBEndpoint = config[AppConfigurationSetting.CosmosDBEndpoint];
            var client = new CosmosClient(cosmosDBEndpoint, s_azureCredential);
            return client;
        }); ;


        services.AddSingleton<SearchClientFactory>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new SearchClientFactory(config, s_azureCredential);
        });

        services.AddSingleton<ChatHistoryService>();

        services.AddSingleton<ChatService>();
        services.AddSingleton<ReadRetrieveReadChatService>();
        services.AddSingleton<ReadRetrieveReadStreamingChatService>();

        services.AddSingleton<AzureBlobStorageService>();
        services.AddSingleton<DocumentService>();
        services.AddHttpClient<DocumentService, DocumentService>();

        return services;
    }

    internal static IServiceCollection AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return services;
    }
}
