// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace MinimalApi.Services.Documents;

public class DocumentIndexMerge
{
    public DocumentIndexMerge()
    {
        SearchAction = SearchActions.mergeOrUpload;
        DocId = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
    }
    public DocumentIndexMerge(string docId, string title)
    {
        SearchAction = SearchActions.mergeOrUpload;
        DocId = docId;
        Title = title;
        Description = title;
    }

    public DocumentIndexMerge(string docId, string title, string description)
    {
        SearchAction = SearchActions.mergeOrUpload;
        DocId = docId;
        Title = title;
        Description = description;
    }
    public DocumentIndexMerge(SearchActions searchAction, string docId, string title, string description)
    {
        SearchAction = searchAction;
        DocId = docId;
        Title = title;
        Description = description;
    }

    [JsonProperty("@search.action")]
    public SearchActions SearchAction { get; set; }

    [JsonProperty("docId")]
    public string DocId { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    public enum SearchActions
    {
        mergeOrUpload,
        merge,
        upload,
        delete
    }
}
