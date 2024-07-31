// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace MinimalApi.Services.ChatHistory;


    public class ChatSessionRecord
    { 
        public string ChatId { get; set; }

        public List<ChatMessageRecord> Messages { get; set; }
    }


    /// <summary>
    /// Information about a single chat message.
    /// </summary>
    public class ChatMessageRecord
{
    public ChatMessageRecord(string userId,
                             string chatId,
                             string chatTurnId,
                             string message,
                             string content,
                             ResponseContext context)
    {
        Timestamp = DateTimeOffset.Now;
        UserId = userId;
        ChatId = chatId;
        Content = content;
        Prompt = message;
        Type = "Message";
        Id = chatTurnId;
        Context = context;
    }

    /// <summary>
    /// Id of the message.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// Timestamp of the message.
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Id of the user who sent this message.
    /// </summary>
    [JsonProperty("userId")]
    public string UserId { get; set; }

    /// <summary>
    /// Id of the chat this message belongs to.
    /// </summary>
    [JsonProperty("chatId")]
    public string ChatId { get; set; }

    /// <summary>
    /// Content of the chat prompt.
    /// </summary>
    [JsonProperty("prompt")]
    public string Prompt { get; set; }

    /// <summary>
    /// Content of the message.
    /// </summary>
    [JsonProperty("content")]
    public string Content { get; set; }

    /// <summary>
    /// Document Type (message or rating)
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("rating")]
    public ChatRating Rating { get; set; }

    [JsonProperty("context")]
    public ResponseContext Context { get; set; }
}
