// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MinimalApi.Services.HealthChecks;

public class CosmosDbReadinessHealthCheck(CosmosClient cosmosClient) : IHealthCheck
{
    private readonly CosmosClient _cosmosClient = cosmosClient;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Database? database = null;
        Dictionary<string, object> data = [];

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            await _cosmosClient.ReadAccountAsync().ConfigureAwait(false);

            data.Add("Account", $"CosmosDB account is accessible: {_cosmosClient.Endpoint}");
        }
        catch (Exception ex)
        {
            data.Add("Account", $"CosmosDB account is not accessible: {_cosmosClient.Endpoint}");
            return new HealthCheckResult(HealthStatus.Unhealthy, description: "CosmosDB is not accessible", exception: ex, data: data);
        }
#pragma warning restore CA1031 // Do not catch general exception types

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            database = _cosmosClient.GetDatabase(DefaultSettings.CosmosDBDatabaseName);

            await database.ReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            data.Add("Database", $"CosmosDB database is accessible: {DefaultSettings.CosmosDBDatabaseName}");
        }
        catch (Exception ex)
        {
            data.Add("Database", $"CosmosDB database is not accessible: {DefaultSettings.CosmosDBDatabaseName}");
            return new HealthCheckResult(HealthStatus.Unhealthy, description: "CosmosDB is not accessible", exception: ex, data: data);
        }
#pragma warning restore CA1031 // Do not catch general exception types

        if (database != null)
        {
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                await database.GetContainer(DefaultSettings.CosmosDBUserDocumentsCollectionName).ReadContainerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                data.Add("Container", $"CosmosDB container is accessible: {DefaultSettings.CosmosDBUserDocumentsCollectionName}");
            }
            catch (Exception ex)
            {
                data.Add("Container", $"CosmosDB container is not accessible: {DefaultSettings.CosmosDBUserDocumentsCollectionName}");
                return new HealthCheckResult(HealthStatus.Unhealthy, description: "CosmosDB is not accessible", exception: ex, data: data);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        return new HealthCheckResult(HealthStatus.Healthy, description: "CosmosDB is accessible", data: data);
    }
}
