// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace MinimalApi.Services.Documents;

public class GetIndexRequest
{
    [JsonProperty("index_stem_name")]
    public string index_stem_name { get; set; }
}

public class ProcessingData
{
    [JsonProperty("source_container")]
    public string source_container { get; set; }

    [JsonProperty("extract_container")]
    public string extract_container { get; set; }

    [JsonProperty("prefix_path")]
    public string prefix_path { get; set; }

    [JsonProperty("entra_id")]
    public string entra_id { get; set; }

    [JsonProperty("session_id")]
    public string session_id { get; set; }

    [JsonProperty("index_name")]
    public string index_name { get; set; }

    [JsonProperty("index_stem_name")]
    public string index_stem_name { get; set; }

    [JsonProperty("cosmos_record_id")]
    public string cosmos_record_id { get; set; }

    [JsonProperty("automatically_delete")]
    public bool automatically_delete { get; set; }

}

