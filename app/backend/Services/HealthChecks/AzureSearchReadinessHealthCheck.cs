// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MinimalApi.Services.Search;

namespace MinimalApi.Services.HealthChecks;

public class AzureSearchReadinessHealthCheck : IHealthCheck
{
    //private readonly SearchClient _searchClient;

    //public AzureSearchReadinessHealthCheck(SearchClientFactory factory)
    //{
    //    _searchClient = factory.GetOrCreateClient(indexName);
    //}

    //public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    //{
    //   Dictionary<string, object> data = [];

    //    try
    //    {
    //        await _searchClient.GetDocumentCountAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    //        data.Add("AI Search", $"AI Search is accessible: {_searchClient.ServiceName}");
    //    }
    //    catch (Exception ex)
    //    {
    //        data.Add("AI Search", $"AI Search is not accessible: {_searchClient.ServiceName}");
    //        return new HealthCheckResult(HealthStatus.Unhealthy, description: "AI Search is not accessible", exception: ex, data: data);
    //    }

    //    return new HealthCheckResult(HealthStatus.Healthy, description: "AI Search is accessible", data: data);
    //}
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
