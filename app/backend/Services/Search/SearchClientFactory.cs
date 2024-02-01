// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using Azure;
using Azure.Search.Documents;

namespace MinimalApi.Services.Search;

public class SearchClientFactory
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string,SearchClient> _clients = new ConcurrentDictionary<string, SearchClient>();

    public SearchClientFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SearchClient GetOrCreateClient(string indexName)
    {
        // Check if a client for the given index already exists
        if (_clients.TryGetValue(indexName, out var client))
        {
            return client;
        }

        // Create a new client for the index
        var newClient = CreateClientForIndex(indexName);
        _clients[indexName] = newClient;
        return newClient;
    }

    private SearchClient CreateClientForIndex(string indexName)
    {
        var sc = new SearchClient(new Uri(_configuration["AzureSearchServiceEndpoint"]), indexName, new AzureKeyCredential(_configuration["AzureSearchServiceKey"]));
        return sc;
    }
}
