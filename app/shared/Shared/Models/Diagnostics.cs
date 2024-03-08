// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public class Diagnostics
{
    public Diagnostics()
    {
    }

    public Diagnostics(CompletionsDiagnostics answerDiagnostics, string modelDeploymentName, long workflowDurationMilliseconds)
    {
        AnswerDiagnostics = answerDiagnostics;
        ModelDeploymentName = modelDeploymentName;
        WorkflowDurationMilliseconds = workflowDurationMilliseconds;
    }

    public CompletionsDiagnostics AnswerDiagnostics { get; set; }

    public string ModelDeploymentName { get; set; }

    public long WorkflowDurationMilliseconds { get; set; }
}

public class CompletionsDiagnostics
{
    public CompletionsDiagnostics()
    {
    }

    public CompletionsDiagnostics(int completionTokens, int promptTokens, int totalTokens, long durationMilliseconds)
    {
        CompletionTokens = completionTokens;
        PromptTokens = promptTokens;
        TotalTokens = totalTokens;
        AnswerDurationMilliseconds = durationMilliseconds;
    }

    /// <summary> The number of tokens generated across all completions emissions. </summary>

    public int CompletionTokens { get; set; }
    /// <summary> The number of tokens in the provided prompts for the completions request. </summary>

    public int PromptTokens { get; set; }
    /// <summary> The total number of tokens processed for the completions request and response. </summary>

    public int TotalTokens { get; set; }

    public long AnswerDurationMilliseconds { get; set; }
}
