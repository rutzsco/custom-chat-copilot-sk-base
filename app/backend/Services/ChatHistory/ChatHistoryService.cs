// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client;
using Shared.Models;

namespace MinimalApi.Services.ChatHistory;


public class ChatHistoryService : IChatHistoryService
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
        var lastHistoryItem = chatRequest.History?.LastOrDefault();
        var prompt = lastHistoryItem?.User;
        if (prompt == null)
            throw new InvalidOperationException("The prompt cannot be null.");

        var chatMessage = new ChatMessageRecord(user.UserId, chatRequest.ChatId.ToString(), chatRequest.ChatTurnId.ToString(), prompt, response.Answer, response.Context);
        await _cosmosContainer.CreateItemAsync(chatMessage, partitionKey: new PartitionKey(chatMessage.ChatId));
    }

    public async Task RecordRatingAsync(UserInformation user, ChatRatingRequest chatRatingRequest)
    {
        var chatRatingId = chatRatingRequest.MessageId.ToString();
        var partitionKey = new PartitionKey(chatRatingRequest.ChatId.ToString());
        var response = await _cosmosContainer.ReadItemAsync<ChatMessageRecord>(chatRatingId, partitionKey);
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

    public async Task<List<ChatMessageRecord>> GetChatHistoryMessagesAsync(UserInformation user, string chatId)
    {
        var query = _cosmosContainer.GetItemQueryIterator<ChatMessageRecord>(
            new QueryDefinition("SELECT * FROM c WHERE c.userId = @username AND c.chatId = @chatid ORDER BY c.timestamp DESC")
            .WithParameter("@username", user.UserId)
            .WithParameter("@chatid", chatId));

        var results = new List<ChatMessageRecord>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }
}
