// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models;
public record ProfileSummary(string Name, string Description, ProfileApproach Approach, List<string> SampleQuestions);

public enum ProfileApproach
{
    Chat,
    UserDocumentChat,
    RAG,
    EndpointAssistant,
    EndpointAssistantV2
};
