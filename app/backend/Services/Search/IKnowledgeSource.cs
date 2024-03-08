// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Search;

public interface IKnowledgeSource
{
    string FormatAsOpenAISourceText();
    string GetFilepath();
    string GetContent();
}
