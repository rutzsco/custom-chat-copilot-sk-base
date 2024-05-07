// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace MinimalApi.Services.Documents;

public class GetIndexResponse
{
    public string IndexStemName { get; set; }
}
public class ProcessingData
{
    [JsonProperty("source_container")]
    public string SourceContainer { get; set; }

    [JsonProperty("extract_container")]
    public string ExtractContainer { get; set; }

    [JsonProperty("prefix_path")]
    public string PrefixPath { get; set; }

    [JsonProperty("entra_id")]
    public string EntraId { get; set; }

    [JsonProperty("session_id")]
    public string SessionId { get; set; }

    [JsonProperty("index_name")]
    public string IndexName { get; set; }

    [JsonProperty("cosmos_record_id")]
    public string CosmosRecordId { get; set; }

    [JsonProperty("automatically_delete")]
    public bool AutomaticallyDelete { get; set; }

}

