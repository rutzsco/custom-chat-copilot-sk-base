// Copyright (c) Microsoft. All rights reserved.


namespace MinimalApi.Services.ChatHistory;

public class ChatHistoryServiceStub : IChatHistoryService
{
    public Task<List<ChatMessageRecord>> GetChatHistoryMessagesAsync(UserInformation user, string chatId)
    {
        return Task.FromResult(new List<ChatMessageRecord>());
    }

    public Task<List<ChatMessageRecord>> GetMostRecentChatItemsAsync(UserInformation user)
    {
        return Task.FromResult(new List<ChatMessageRecord>());
    }

    public Task<List<ChatMessageRecord>> GetMostRecentRatingsItemsAsync(UserInformation user)
    {
        return Task.FromResult(new List<ChatMessageRecord>());
    }

    public Task RecordChatMessageAsync(UserInformation user, ChatRequest chatRequest, ApproachResponse response)
    {
        return Task.CompletedTask;
    }

    public Task RecordRatingAsync(UserInformation user, ChatRatingRequest chatRatingRequest)
    {
        return Task.CompletedTask;
    }
}
