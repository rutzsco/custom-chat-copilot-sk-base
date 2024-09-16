// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record ChatTurn(string User, string? Assistant = null);
