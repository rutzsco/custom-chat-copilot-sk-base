// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client;
using Shared.Models;

namespace MinimalApi.Services.ChatHistory;


public class ChatHistoryService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _cosmosContainer;

    public ChatHistoryService(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;

        var db = _cosmosClient.GetDatabase(DefaultSettings.CosmosDBDatabaseName);
        _cosmosContainer = db.GetContainer(DefaultSettings.CosmosDBCollectionName);
    }

    public async Task RecordChatMessageAsync(ChatRequest chatRequest, ApproachResponse response)
    {
        var prompt = chatRequest.History.LastOrDefault().User;
        var chatMessage = new ChatMessageRecord("Anonymous", chatRequest.ChatId.ToString(), chatRequest.ChatTurnId.ToString(), prompt, response.Answer);
        await _cosmosContainer.CreateItemAsync(chatMessage, partitionKey: new PartitionKey(chatMessage.ChatId));
    }

    public async Task RecordRatingAsync(ChatRatingRequest chatRatingRequest)
    {
        var chatRatingId = chatRatingRequest.MessageId.ToString();
        var partitionKey = new PartitionKey(chatRatingRequest.ChatId.ToString());
        var response = await _cosmosContainer.ReadItemAsync<ChatMessageRecord>(chatRatingId,partitionKey);
        var existingChatRating = response.Resource;

        var rating = new ChatRating(chatRatingRequest.Feedback, chatRatingRequest.Rating);
        existingChatRating.Rating = rating;
        await _cosmosContainer.UpsertItemAsync(existingChatRating, partitionKey: partitionKey);
    }


    public async Task<List<ChatMessageRecord>> GetMostRecentRatingsItemsAsync()
    {
        var query = _cosmosContainer.GetItemQueryIterator<ChatMessageRecord>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE c.rating != null ORDER BY c.rating.timestamp DESC"));

        var results = new List<ChatMessageRecord>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    public async Task<List<ChatMessageRecord>> GetMostRecentChatItemsAsync()
    {
        var query = _cosmosContainer.GetItemQueryIterator<ChatMessageRecord>(
            new QueryDefinition("SELECT TOP 100 * FROM c ORDER BY c.timestamp DESC"));

        var results = new List<ChatMessageRecord>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }
}
