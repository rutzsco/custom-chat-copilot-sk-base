// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search;

public interface IKnowledgeSource
{
    string FormatAsOpenAISourceText();

    public string GetFilepath();

    public string GetContent();
}
