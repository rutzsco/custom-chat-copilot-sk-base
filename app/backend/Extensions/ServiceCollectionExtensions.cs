﻿
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
using Microsoft.Azure.Cosmos.Linq;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using System.Net.Http;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.TextGeneration;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        var sp = services.BuildServiceProvider();
        var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

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

        services.AddScoped<OpenAIClientFacade>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var deployedModelName3 = config["AOAIStandardChatGptDeployment"];
            var azureOpenAiServiceEndpoint3 = config["AOAIStandardServiceEndpoint"];
            var azureOpenAiServiceKey3 = config["AOAIStandardServiceKey"];

            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint3);

            var deployedModelName4 = config["AOAIPremiumChatGptDeployment"];
            var azureOpenAiServiceEndpoint4 = config["AOAIPremiumServiceEndpoint"];
            var azureOpenAiServiceKey4 = config["AOAIPremiumServiceKey"];
            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint4);

            // Build Plugins
            var searchClientFactory = sp.GetRequiredService<SearchClientFactory>();

            AzureOpenAIClient? openAIClient3 = null;
            AzureOpenAIClient? openAIClient4 = null;

            if (config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalClientID) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalClientSecret) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureTenantID) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureAuthorityHost) != null)
            {
                SetupOpenAIClientsUsingOnBehalfOfOthersFlowAndSubscriptionKey(sp, httpContextAccessor, config, azureOpenAiServiceEndpoint3, out openAIClient3, out openAIClient4);
            }
            else
            {
                ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey3);
                ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey4);

                openAIClient3 = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint3), new AzureKeyCredential(azureOpenAiServiceKey3));
                openAIClient4 = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint4), new AzureKeyCredential(azureOpenAiServiceKey4));
            }

            var retrieveRelatedDocumentPlugin3 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient3);

            var retrieveRelatedDocumentPlugin4 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient4);

            var generateSearchQueryPlugin = new GenerateSearchQuerySkill();
            var chatPlugin = new ChatSkill();

            Kernel? kernel3 = null;
            Kernel? kernel4 = null;
            IKernelBuilder? builder3 = null;
            IKernelBuilder? builder4 = null;

            if (openAIClient3 != null)
            {
                Func<IServiceProvider, object?, AzureOpenAIChatCompletionService> factory3 = (serviceProvider, _) => new(deployedModelName3, openAIClient3, null, serviceProvider.GetService<ILoggerFactory>());

                var kernel3Builder = Kernel.CreateBuilder();
                kernel3Builder.Services.AddKeyedScoped<IChatCompletionService>(null, factory3);
                kernel3Builder.Services.AddKeyedScoped<ITextGenerationService>(null, factory3);

                kernel3 = kernel3Builder.Build();
            }
            else
            {
                // Build Kernels
                kernel3 = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deployedModelName3, azureOpenAiServiceEndpoint3, azureOpenAiServiceKey3)
                .Build();
            }

            if (openAIClient4 != null)
            {
                Func<IServiceProvider, object?, AzureOpenAIChatCompletionService> factory4 = (serviceProvider, _) => new(deployedModelName3, openAIClient4, null, serviceProvider.GetService<ILoggerFactory>());

                var kernel4Builder = Kernel.CreateBuilder();
                kernel4Builder.Services.AddKeyedScoped<IChatCompletionService>(null, factory4);
                kernel4Builder.Services.AddKeyedScoped<ITextGenerationService>(null, factory4);

                kernel4 = kernel4Builder.Build();

            }
            else
            {
                kernel4 = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deployedModelName4, azureOpenAiServiceEndpoint4, azureOpenAiServiceKey4)
                .Build();
            }

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

        if (!string.IsNullOrEmpty(configuration[AppConfigurationSetting.CosmosDBConnectionString]))
        {
            services.AddSingleton((sp) => {
                var config = sp.GetRequiredService<IConfiguration>();
                var cosmosDBConnectionString = config[AppConfigurationSetting.CosmosDBConnectionString];
                CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(cosmosDBConnectionString);
                return configurationBuilder
                        .Build();
            });
        }

        RegisterDomainServices(services, configuration);

        return services;
    }

    internal static IServiceCollection AddAzureWithMICredentialsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var sp = services.BuildServiceProvider();
        var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        DefaultAzureCredential azureCredential = new(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = configuration[AppConfigurationSetting.UserAssignedManagedIdentityClientId]
        });

        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var azureStorageAccountEndpoint = configuration[AppConfigurationSetting.AzureStorageAccountEndpoint];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountEndpoint);

            var blobServiceClient = new BlobServiceClient(new Uri(azureStorageAccountEndpoint), azureCredential);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var azureStorageContainer = configuration[AppConfigurationSetting.AzureStorageContainer];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        services.AddScoped(sp =>
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

            AzureOpenAIClient? openAIClient3 = null;
            AzureOpenAIClient? openAIClient4 = null;

            if (config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalClientID) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalClientSecret) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureTenantID) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureAuthorityHost) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalOpenAIAudience) != null)
            {
                SetupOpenAIClientsUsingOnBehalfOfOthersFlowAndSubscriptionKey(sp, httpContextAccessor, config, azureOpenAiServiceEndpoint3, out openAIClient3, out openAIClient4);
            }
            else
            {
                openAIClient3 = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint3), azureCredential);
                openAIClient4 = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint3), azureCredential);
            }

            // Build Plugins
            var searchClientFactory = sp.GetRequiredService<SearchClientFactory>();

            var retrieveRelatedDocumentPlugin3 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient3);
            var retrieveRelatedDocumentPlugin4 = new RetrieveRelatedDocumentSkill(config, searchClientFactory, openAIClient4);

            var generateSearchQueryPlugin = new GenerateSearchQuerySkill();
            var chatPlugin = new ChatSkill();

            Kernel? kernel3 = null;
            Kernel? kernel4 = null;

            // Build Kernels
            if (config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalClientID) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalClientSecret) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureTenantID) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureAuthorityHost) != null
                && config.GetValue<string>(AppConfigurationSetting.AzureServicePrincipalOpenAIAudience) != null)
            {
                Func<IServiceProvider, object?, AzureOpenAIChatCompletionService> factory3 = (serviceProvider, _) => new(deployedModelName3, openAIClient3, null, serviceProvider.GetService<ILoggerFactory>());

                var kernel3Builder = Kernel.CreateBuilder();
                kernel3Builder.Services.AddKeyedScoped<IChatCompletionService>(null, factory3);
                kernel3Builder.Services.AddKeyedScoped<ITextGenerationService>(null, factory3);

                kernel3 = kernel3Builder.Build();

                Func<IServiceProvider, object?, AzureOpenAIChatCompletionService> factory4 = (serviceProvider, _) => new(deployedModelName4, openAIClient4, null, serviceProvider.GetService<ILoggerFactory>());

                var kernel4Builder = Kernel.CreateBuilder();
                kernel4Builder.Services.AddKeyedScoped<IChatCompletionService>(null, factory4);
                kernel4Builder.Services.AddKeyedScoped<ITextGenerationService>(null, factory4);
                kernel4 = kernel4Builder.Build();
            }
            else
            {
                kernel3 = Kernel.CreateBuilder()
                   .AddAzureOpenAIChatCompletion(deployedModelName3, azureOpenAiServiceEndpoint3, azureCredential)
                   .Build();

                kernel4 = Kernel.CreateBuilder()
                   .AddAzureOpenAIChatCompletion(deployedModelName4, azureOpenAiServiceEndpoint4, azureCredential)
                   .Build();
            }

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
            var client = new CosmosClient(cosmosDBEndpoint, azureCredential);
            return client;
        });

        services.AddSingleton<SearchClientFactory>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new SearchClientFactory(config, azureCredential);
        });

        if (!string.IsNullOrEmpty(configuration[AppConfigurationSetting.CosmosDBEndpoint]))
        {
            services.AddSingleton((sp) => {
                var config = sp.GetRequiredService<IConfiguration>();
                var endpoint = config[AppConfigurationSetting.CosmosDBEndpoint];
                CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(endpoint,azureCredential);
                return configurationBuilder
                        .Build();
            });
        }

        RegisterDomainServices(services, configuration);

        return services;
    }

    private static void RegisterDomainServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add ChatHistory and document upload services if the connection string is provided
        if (string.IsNullOrEmpty(configuration[AppConfigurationSetting.CosmosDBConnectionString]) && string.IsNullOrEmpty(configuration[AppConfigurationSetting.CosmosDBEndpoint]))
        {
            services.AddScoped<IChatHistoryService, ChatHistoryServiceStub>();
            services.AddScoped<IDocumentService, DocumentServiceSub>();
            services.AddHttpClient();
        }
        else
        {
            services.AddScoped<IChatHistoryService, ChatHistoryService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddHttpClient<DocumentService, DocumentService>();
        }

        services.AddScoped<ChatService>();
        services.AddScoped<ReadRetrieveReadChatService>();
        services.AddScoped<ReadRetrieveReadStreamingChatService>();
        services.AddScoped<EndpointChatService>();
        services.AddScoped<AzureBlobStorageService>();
        services.AddHttpClient<IngestionService, IngestionService>();
    }

    private static void SetupOpenAIClientsUsingOnBehalfOfOthersFlowAndSubscriptionKey(IServiceProvider sp, IHttpContextAccessor httpContextAccessor, IConfiguration config, string? azureOpenAiServiceEndpoint3, out AzureOpenAIClient? openAIClient3, out AzureOpenAIClient? openAIClient4)
    {
        var credential = new OnBehalfOfCredential(
                            tenantId: config[AppConfigurationSetting.AzureTenantID],
                            clientId: config[AppConfigurationSetting.AzureServicePrincipalClientID],
                            clientSecret: config[AppConfigurationSetting.AzureServicePrincipalClientSecret],
                            userAssertion: httpContextAccessor.HttpContext?.Request?.Headers[AppConfigurationSetting.XMsTokenAadAccessToken],
                            new OnBehalfOfCredentialOptions
                            {
                                AuthorityHost = new Uri(config[AppConfigurationSetting.AzureAuthorityHost])
                            });
        
        var httpClient = sp.GetService<IHttpClientFactory>().CreateClient();

        //if the configuration specifies a subscription key, add it to the request headers
        if (config.GetValue<string>(AppConfigurationSetting.OcpApimSubscriptionKey) != null)
        {
            httpClient.DefaultRequestHeaders.Add(AppConfigurationSetting.OcpApimSubscriptionKey, config[AppConfigurationSetting.OcpApimSubscriptionKey]);
        }

        openAIClient3 = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint3), credential, new AzureOpenAIClientOptions
        {
            Audience = config[AppConfigurationSetting.AzureServicePrincipalOpenAIAudience],
            Transport = new HttpClientPipelineTransport(httpClient)
        });
        
        openAIClient4 = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint3), credential, new AzureOpenAIClientOptions
        {
            Audience = config[AppConfigurationSetting.AzureServicePrincipalOpenAIAudience],
            Transport = new HttpClientPipelineTransport(httpClient)
        });
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
