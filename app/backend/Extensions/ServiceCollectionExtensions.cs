
// Copyright (c) Microsoft. All rights reserved.

using System.Security.Policy;
using Azure;
using Azure.Storage;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MinimalApi.Services.ChatHistory;
using MinimalApi.Services.HealthChecks;
using MinimalApi.Services.Documents;
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

        services.AddSingleton<SearchClientFactory>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new SearchClientFactory(config, null, new AzureKeyCredential(config[AppConfigurationSetting.AzureSearchServiceKey]));
        });

        // Add ChatHistory and document upload services if the connection string is provided
        if (string.IsNullOrEmpty(configuration[AppConfigurationSetting.CosmosDBConnectionString]))
        {
            services.AddSingleton<IChatHistoryService,ChatHistoryServiceStub>();
            services.AddSingleton<IDocumentService, DocumentServiceSub>();
            services.AddHttpClient();
        }
        else
        {
            services.AddSingleton((sp) => {
                var config = sp.GetRequiredService<IConfiguration>();
                var cosmosDBConnectionString = config[AppConfigurationSetting.CosmosDBConnectionString];
                CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(cosmosDBConnectionString);
                return configurationBuilder
                        .Build();
            }); ;
            services.AddSingleton<IChatHistoryService, ChatHistoryService>();
            services.AddSingleton<IDocumentService, DocumentService>();
            services.AddHttpClient<DocumentService, DocumentService>();
        }

        services.AddSingleton<ChatService>();
        services.AddSingleton<ReadRetrieveReadChatService>();
        services.AddSingleton<ReadRetrieveReadStreamingChatService>();
        services.AddSingleton<EndpointChatService>();
        services.AddSingleton<AzureBlobStorageService>();
        services.AddHttpClient<IngestionService, IngestionService>();
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

        if (string.IsNullOrEmpty(configuration[AppConfigurationSetting.CosmosDBConnectionString]))
        {
            services.AddSingleton<IChatHistoryService, ChatHistoryServiceStub>();
        }
        else
        {
            services.AddSingleton<IChatHistoryService, ChatHistoryService>();
        }

        services.AddSingleton<ChatService>();
        services.AddSingleton<ReadRetrieveReadChatService>();
        services.AddSingleton<ReadRetrieveReadStreamingChatService>();

        services.AddSingleton<AzureBlobStorageService>();
        services.AddSingleton<DocumentService>();
        services.AddHttpClient<DocumentService, DocumentService>();
        services.AddHttpClient<IngestionService, IngestionService>();
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

    internal static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<CosmosDbReadinessHealthCheck>("CosmosDB Readiness Health Check", failureStatus: HealthStatus.Degraded, tags: ["readiness"]);
        services.AddHealthChecks().AddCheck<AzureStorageReadinessHealthCheck>("Azure Storage Readiness Health Check", failureStatus: HealthStatus.Degraded, tags: ["readiness"]);
        //TODO: this is commented out until a refactor of the profiles is done. The Search Index must exist in order to check its readiness.
        //services.AddHealthChecks().AddCheck<AzureSearchReadinessHealthCheck>("Azure Search Readiness Health Check", failureStatus: HealthStatus.Degraded, tags: ["readiness"]);

        return services;
    }

    internal static WebApplication MapCustomHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
            ResponseWriter = WriteResponse
        });

        app.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/healthz/startup", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        return app;
    }

    internal static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions { Indented = true };

        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteStartObject("results");

            foreach (var healthReportEntry in healthReport.Entries)
            {
                jsonWriter.WriteStartObject(healthReportEntry.Key);
                jsonWriter.WriteString("status",
                    healthReportEntry.Value.Status.ToString());
                jsonWriter.WriteString("description",
                    healthReportEntry.Value.Description);
                jsonWriter.WriteStartObject("data");

                foreach (var item in healthReportEntry.Value.Data)
                {
                    jsonWriter.WritePropertyName(item.Key);

                    JsonSerializer.Serialize(jsonWriter, item.Value,
                        item.Value?.GetType() ?? typeof(object));
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        return context.Response.WriteAsync(
            Encoding.UTF8.GetString(memoryStream.ToArray()));
    }

}
