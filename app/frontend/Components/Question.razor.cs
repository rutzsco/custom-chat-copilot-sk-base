// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Question
{
    private static readonly MarkdownPipeline s_pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    [Parameter, EditorRequired] public required string UserQuestion { get; set; }
    [Parameter, EditorRequired] public required DateTime AskedOn { get; set; }
    private string? _userAnswerHTML;

    protected override void OnParametersSet()
    {
        _userAnswerHTML = Markdown.ToHtml(UserQuestion, s_pipeline);

        base.OnParametersSet();
    }
}
