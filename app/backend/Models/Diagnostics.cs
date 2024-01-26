// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace MinimalApi.Models
{
    public class Diagnostics
    {
        public Diagnostics(CompletionsDiagnostics answerDiagnostics)
        {
            AnswerDiagnostics = answerDiagnostics;
        }

        [JsonProperty("answerDiagnostics")]
        public CompletionsDiagnostics AnswerDiagnostics { get; }
    }

    public class CompletionsDiagnostics
    {
        public CompletionsDiagnostics(int completionTokens, int promptTokens, int totalTokens, long durationMilliseconds)
        {
            CompletionTokens = completionTokens;
            PromptTokens = promptTokens;
            TotalTokens = totalTokens;
            AnswerDurationMilliseconds = durationMilliseconds;
        }

        /// <summary> The number of tokens generated across all completions emissions. </summary>

        [JsonProperty("completionTokens")]
        public int CompletionTokens { get; set; }
        /// <summary> The number of tokens in the provided prompts for the completions request. </summary>

        [JsonProperty("promptTokens")]
        public int PromptTokens { get; set; }
        /// <summary> The total number of tokens processed for the completions request and response. </summary>

        [JsonProperty("totalTokens")]
        public int TotalTokens { get; set; }

        [JsonProperty("answerDurationMilliseconds")]
        public long AnswerDurationMilliseconds { get; set; }
    }
}
