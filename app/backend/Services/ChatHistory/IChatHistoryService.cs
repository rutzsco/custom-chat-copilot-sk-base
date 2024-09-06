// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.ChatHistory;

public interface IChatHistoryService
{
    Task<List<ChatMessageRecord>> GetChatHistoryMessagesAsync(UserInformation user, string chatId);
    Task<List<ChatMessageRecord>> GetMostRecentChatItemsAsync(UserInformation user);
    Task<List<ChatMessageRecord>> GetMostRecentRatingsItemsAsync(UserInformation user);
    Task RecordChatMessageAsync(UserInformation user, ChatRequest chatRequest, ApproachResponse response);
    Task RecordRatingAsync(UserInformation user, ChatRatingRequest chatRatingRequest);
}