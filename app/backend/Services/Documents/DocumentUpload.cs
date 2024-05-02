// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace MinimalApi.Services.Documents;

public class DocumentUpload
{
    public DocumentUpload(string id, string userId, string blobName, string sourceName, string contentType, long size, DocumentProcessingStatus status)
    {
        Timestamp = DateTimeOffset.Now;
        Id = id;
        UserId = userId;
        BlobName = blobName;
        SourceName = sourceName;
        ContentType = contentType;
        Size = size;
        Status = status;
    }


    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("userId")]
    public string UserId { get; set; }

    [JsonProperty("blobName")]
    public string BlobName { get; set; }

    [JsonProperty("sourceName")]
    public string SourceName { get; set; }

    [JsonProperty("contentType")]
    public string ContentType { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("status")]
    public DocumentProcessingStatus Status { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}
