// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search;

public interface IKnowledgeSource
{
    string FormatAsOpenAISourceText(bool useSourcepage = false);
    string GetFilepath(bool useSourcepage = false);
    string GetContent();
}
