
// Copyright (c) Microsoft. All rights reserved.

using Azure;
using Microsoft.Azure.Cosmos.Fluent;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.Skills;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    private static readonly DefaultAzureCredential s_azureCredential = new();

    internal static IServiceCollection AddAzureServices(this IServiceCollection services)
    {
        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccountEndpoint = config["AzureStorageAccountEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountEndpoint);

            var blobServiceClient = new BlobServiceClient(
                new Uri(azureStorageAccountEndpoint), s_azureCredential);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config["AzureStorageContainer"];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });


        services.AddSingleton<OpenAIClientFacade>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var deployedModelName3 = config["AzureOpenAi3ChatGptDeployment"];
            var azureOpenAiServiceEndpoint3 = config["AzureOpenAi3ServiceEndpoint"];
            var azureOpenAiServiceKey3 = config["AzureOpenAi3ServiceKey"];

            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey3);

            var deployedModelName4 = config["AzureOpenAi4ChatGptDeployment"];
            var azureOpenAiServiceEndpoint4 = config["AzureOpenAi4ServiceEndpoint"];
            var azureOpenAiServiceKey4 = config["AzureOpenAi4ServiceKey"];
            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey4);

            var searchClientFacade = sp.GetRequiredService<SearchClientFacade>();
            var openAIClient = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint3), new AzureKeyCredential(azureOpenAiServiceKey3));
            var retrieveRelatedDocumentPlugin = new RetrieveRelatedDocumentSkill(searchClientFacade, openAIClient);
            var generateSearchQueryPlugin = new GenerateSearchQuerySkill();
            var chatPlugin = new ChatSkill();

            Kernel kernel3 = Kernel.CreateBuilder()
               .AddAzureOpenAIChatCompletion(deployedModelName3, azureOpenAiServiceEndpoint3, azureOpenAiServiceKey3)
               .Build();

            Kernel kernel4 = Kernel.CreateBuilder()
               .AddAzureOpenAIChatCompletion(deployedModelName4, azureOpenAiServiceEndpoint4, azureOpenAiServiceKey4)
               .Build();

            kernel3.ImportPluginFromObject(retrieveRelatedDocumentPlugin, DefaultSettings.DocumentRetrievalPluginName);
            kernel3.ImportPluginFromObject(generateSearchQueryPlugin, DefaultSettings.GenerateSearchQueryPluginName);
            kernel3.ImportPluginFromObject(chatPlugin, DefaultSettings.ChatPluginName);
            kernel4.ImportPluginFromObject(retrieveRelatedDocumentPlugin, DefaultSettings.DocumentRetrievalPluginName);
            kernel4.ImportPluginFromObject(generateSearchQueryPlugin, DefaultSettings.GenerateSearchQueryPluginName);
            kernel4.ImportPluginFromObject(chatPlugin, DefaultSettings.ChatPluginName);

            return new OpenAIClientFacade(kernel3, kernel4, openAIClient);
        });
        services.AddSingleton((sp) => {
            var config = sp.GetRequiredService<IConfiguration>();
            var cosmosDBConnectionString = config["CosmosDBConnectionString"];
            CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(cosmosDBConnectionString);
            return configurationBuilder
                    .Build();
        }); ;


        services.AddSingleton<SearchClientFacade>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchServiceEndpoint, azureSearchManualsIndex, azureSearchServiceKey) = (config["AzureSearchServiceEndpoint"], config["AzureSearchContentIndex"], config["AzureSearchServiceKey"]);

            var contentSearchClient = new SearchClient(new Uri(azureSearchServiceEndpoint), azureSearchManualsIndex, new AzureKeyCredential(azureSearchServiceKey));

            return new SearchClientFacade(contentSearchClient);
        });

        services.AddSingleton<ChatHistoryService>();
        services.AddSingleton<ReadRetrieveReadChatServiceEnhanced>();
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
