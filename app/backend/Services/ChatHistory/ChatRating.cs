// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace MinimalApi.Services.ChatHistory;

/// <summary>
/// Information about a single chat message.
/// </summary>
public class ChatRating
{
    public ChatRating(string feedback,
                      int rating)
    {
        Timestamp = DateTimeOffset.Now;
        Feedback = feedback;
        Rating = rating;
    }

    /// <summary>
    /// Content of the message.
    /// </summary>
    [JsonProperty("rating")]
    public int Rating { get; set; }

    /// <summary>
    /// Content of the message.
    /// </summary>
    [JsonProperty("feedback")]
    public string Feedback { get; set; }

    /// <summary>
    /// Timestamp of the message.
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}
