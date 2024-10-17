// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models;
public record ProfileSummary(string Id, string Name, string Description, ProfileApproach Approach, List<string> SampleQuestions, List<UserPromptTemplate> PromptTemplates, bool SupportsUserSelectionOptions, bool SupportsFileUpload);

public enum ProfileApproach
{
    Chat,
    UserDocumentChat,
    RAG,
    EndpointAssistant,
    EndpointAssistantV2
};
public record UserPromptTemplate(string Name, string PromptTemplate, UserPromptTemplateVariable[] Variables);
public class UserPromptTemplateVariable
{
    public string Name { get; set; }
    public string Value { get; set; }
}


