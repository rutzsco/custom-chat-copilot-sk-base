// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace MinimalApi.Services.Documents;

public class DocumentUpload
{
    public DocumentUpload()
    {
        Timestamp = DateTimeOffset.Now;
        Id = string.Empty;
        UserId = string.Empty;
        BlobName = string.Empty;
        SourceName = string.Empty;
        ContentType = string.Empty;
        Size = 0;
        RetrivalIndexName = string.Empty;
        SessionId = string.Empty;
        Status = DocumentProcessingStatus.New;
        Metadata = string.Empty;
        StatusMessage = string.Empty;
    }
    public DocumentUpload(string id, string userId, string blobName, string sourceName, string contentType, long size, string retrivalIndexName, string sessionId, DocumentProcessingStatus status)
    {
        Timestamp = DateTimeOffset.Now;
        Id = id;
        UserId = userId;
        BlobName = blobName;
        SourceName = sourceName;
        ContentType = contentType;
        Size = size;
        RetrivalIndexName = retrivalIndexName;
        SessionId = sessionId;
        Status = status;
        Metadata = string.Empty;
        StatusMessage = string.Empty;
    }
    public DocumentUpload(string id, string userId, string blobName, string sourceName, string contentType, long size, string retrivalIndexName, string sessionId, DocumentProcessingStatus status, string metadata)
    {
        Timestamp = DateTimeOffset.Now;
        Id = id;
        UserId = userId;
        BlobName = blobName;
        SourceName = sourceName;
        ContentType = contentType;
        Size = size;
        RetrivalIndexName = retrivalIndexName;
        SessionId = sessionId;
        Status = status;
        Metadata = metadata;
        StatusMessage = string.Empty;
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

    [JsonProperty("status_message")]
    public string StatusMessage { get; set; }

    [JsonProperty("processing_progress")]
    public double ProcessingProgress { get; set; }

    [JsonProperty("retrivalIndexName")]
    public string RetrivalIndexName { get; set; }

    [JsonProperty("sessionId")]
    public string SessionId { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonProperty("metadata")]
    public string Metadata { get; set; }
}
