// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Profile;

public class ProfileDefinition
{

    public static ProfileDefinition RAG = new ProfileDefinition("Auto Service Advisor", "RAG", "UAL", new RAGSettingsSummary(DefaultSettings.GenerateSearchQueryPluginName, DefaultSettings.GenerateSearchQueryPluginQueryFunctionName, DefaultSettings.DocumentRetrievalPluginName, DefaultSettings.DocumentRetrievalPluginQueryFunctionName));
    public static ProfileDefinition General = new ProfileDefinition("General", "Chat", "None");

    public static List<ProfileDefinition> All = new List<ProfileDefinition>
    {
        RAG,
        General
    };

    public static ProfileDefinition GetProfile(string name)
    {
        return All.FirstOrDefault(p => p.Name == name);
    }


    public ProfileDefinition(string name, string approach, string securityModel, RAGSettingsSummary? ragSettingsSummary = null)
    {
        Name = name;
        Approach = approach;
        SecurityModel = securityModel;
        RAGSettingsSummary = ragSettingsSummary;
    }

    public string Name { get; set; }

    public string Approach { get; set; }

    public string SecurityModel { get; set; }

    public RAGSettingsSummary? RAGSettingsSummary { get; set; }

}


public record RAGSettingsSummary(string GenerateSearchQueryPluginName, string GenerateSearchQueryPluginQueryFunctionName, string DocumentRetrievalPluginName, string DocumentRetrievalPluginQueryFunctionName);
