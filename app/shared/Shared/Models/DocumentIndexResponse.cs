// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace MinimalApi.Services.Documents;

public class DocumentIndexResponse
{
    public List<DocumentIndexReturnValue> DocumentIndexResults = [];

    public int DocumentCount { get; set; }
    public int IndexedCount { get; set; }
    public bool AllFilesIndexed { get; set; }
    public string ErrorMessage { get; set; }
    public string EndPointUrl { get; set; }

    public static DocumentIndexResponse FromError(string error) => new([], error);

    public DocumentIndexResponse()
    {
        DocumentIndexResults = [];
        IndexedCount = 0;
        DocumentCount = 0;
        AllFilesIndexed = true;
        ErrorMessage = string.Empty;
        EndPointUrl = string.Empty;
    }
    public DocumentIndexResponse(List<DocumentIndexReturnValue> results, string error)
    {
        DocumentIndexResults = results;
        IndexedCount = 0;
        DocumentCount = 0;
        AllFilesIndexed = true;
        ErrorMessage = error;
        EndPointUrl = string.Empty;
    }
    public DocumentIndexResponse(string responseString)
    {
        DocumentIndexResults = JsonConvert.DeserializeObject<List<DocumentIndexReturnValue>>(responseString) ?? [];
        DocumentCount = DocumentIndexResults.Count;
        IndexedCount = DocumentIndexResults.Where(d => d.Status).Count();
        AllFilesIndexed = DocumentIndexResults.Count == DocumentIndexResults.Where(d => d.Status).Count();
        ErrorMessage = string.Empty;
        EndPointUrl = string.Empty;
    }
}

public class DocumentIndexReturnValue
{
    public DocumentIndexReturnValue()
    {
        Key = string.Empty;
        Status = false;
        Message = string.Empty;
        HttpStatusCode = 404;
    }
    public DocumentIndexReturnValue(string key, bool status, string message, int statusCode)
    {
        Key = key;
        Status = status;
        Message = message;
        HttpStatusCode = statusCode;
    }

    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("status")]
    public bool Status { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("statusCode")]
    public int HttpStatusCode { get; set; }
}
