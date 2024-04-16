// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public interface IChatService
{
    IAsyncEnumerable<ChatChunkResponse> ReplyAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
