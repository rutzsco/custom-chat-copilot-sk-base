// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search;

public class KnowledgeSourceSummary
{
    public KnowledgeSourceSummary(string formattedSourceText, IEnumerable<IKnowledgeSource> sources)
    {
        FormattedSourceText = formattedSourceText;
        Sources = sources;
    }

    public string FormattedSourceText { get; set; }

    public IEnumerable<IKnowledgeSource> Sources { get; set; }
}
