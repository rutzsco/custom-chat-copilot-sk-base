// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.Profile;

namespace MinimalApi.Services;

public interface IChatService
{
    IAsyncEnumerable<ChatChunkResponse> ReplyAsync(ProfileDefinition profile, ChatRequest request, CancellationToken cancellationToken = default);
}
