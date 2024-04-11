// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class ChatChunkResponse(string Text, ApproachResponse? FinalResult = null);
