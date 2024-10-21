// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using Shared.Models;

namespace MinimalApi.Services.Documents;

public class DocumentIndexRequest
{
    public UploadDocumentsResponse Documents { get; set; }
    public string AccessToken { get; set; }

    public DocumentIndexRequest(UploadDocumentsResponse documents, string token)
    {
        Documents = documents;
        AccessToken = token;
    }
}

public class DocumentIndexMerge
{
    public DocumentIndexMerge()
    {
        SearchAction = SearchActions.mergeOrUpload;
        DocId = 0;
        Key = 0;
        Title = string.Empty;
        Description = string.Empty;
    }
    public DocumentIndexMerge(int docId, string title)
    {
        SearchAction = SearchActions.mergeOrUpload;
        DocId = docId;
        Key = docId;
        Title = title;
        Description = title;
    }

    public DocumentIndexMerge(int docId, string title, string description)
    {
        SearchAction = SearchActions.mergeOrUpload;
        DocId = docId;
        Key = docId;
        Title = title;
        Description = description;
    }
    public DocumentIndexMerge(SearchActions searchAction, int docId, string title, string description)
    {
        SearchAction = searchAction;
        DocId = docId;
        Key = docId;
        Title = title;
        Description = description;
    }

    [JsonProperty("@search.action")]
    public SearchActions SearchAction { get; set; }

    [JsonProperty("docId")]
    public int DocId { get; set; }

    [JsonProperty("key")]
    public int Key { get; set; }

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
