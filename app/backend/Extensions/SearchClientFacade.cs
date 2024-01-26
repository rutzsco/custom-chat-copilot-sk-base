// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

public class SearchClientFacade
{
    public SearchClientFacade(SearchClient contentSearchClient)
    {
        ContentSearchClient = contentSearchClient;
    }

    public SearchClient ContentSearchClient { get; set; }

}

