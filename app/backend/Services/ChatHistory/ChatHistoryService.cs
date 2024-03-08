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

    public async Task RecordChatMessageAsync(UserInformation user, ChatRequest chatRequest, ApproachResponse response)
    {
        var diagnostics = response.Diagnostics;
        var prompt = chatRequest.History.LastOrDefault().User;
        var chatMessage = new ChatMessageRecord(user.UserId, chatRequest.ChatId.ToString(), chatRequest.ChatTurnId.ToString(), prompt, response.Answer, response.Diagnostics);
        await _cosmosContainer.CreateItemAsync(chatMessage, partitionKey: new PartitionKey(chatMessage.ChatId));
    }

    public async Task RecordRatingAsync(UserInformation user, ChatRatingRequest chatRatingRequest)
    {
        var chatRatingId = chatRatingRequest.MessageId.ToString();
        var partitionKey = new PartitionKey(chatRatingRequest.ChatId.ToString());
        var response = await _cosmosContainer.ReadItemAsync<ChatMessageRecord>(chatRatingId,partitionKey);
        var existingChatRating = response.Resource;

        var rating = new ChatRating(chatRatingRequest.Feedback, chatRatingRequest.Rating);
        existingChatRating.Rating = rating;
        await _cosmosContainer.UpsertItemAsync(existingChatRating, partitionKey: partitionKey);
    }


    public async Task<List<ChatMessageRecord>> GetMostRecentRatingsItemsAsync(UserInformation user)
    {

        var query = _cosmosContainer.GetItemQueryIterator<ChatMessageRecord>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE c.rating != null AND c.userId = @username ORDER BY c.rating.timestamp DESC")
            .WithParameter("@username", user.UserId));

        var results = new List<ChatMessageRecord>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    public async Task<List<ChatMessageRecord>> GetMostRecentChatItemsAsync(UserInformation user)
    {
        var query = _cosmosContainer.GetItemQueryIterator<ChatMessageRecord>(
            new QueryDefinition("SELECT TOP 100 * FROM c WHERE c.userId = @username ORDER BY c.timestamp DESC")
            .WithParameter("@username", user.UserId));

        var results = new List<ChatMessageRecord>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }
}
