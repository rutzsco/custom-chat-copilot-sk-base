// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Newtonsoft.Json;

namespace MinimalApi.Services.Profile;

public class ProfileDefinition
{
    public static List<ProfileDefinition> All;

    public static void Load(IConfiguration configuration)
    {
        var fileName = configuration["ProfileFileName"];
        if (fileName == null)
        {
            fileName = "profiles";
        }
        All = LoadProflies(fileName);
    }

    private static List<ProfileDefinition> LoadProflies(string name)
    {
        var resourceName = $"MinimalApi.Services.Profile.{name}.json";
        var assembly = Assembly.GetExecutingAssembly();

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new ArgumentException($"The resource {resourceName} was not found.");
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                var jsonText = reader.ReadToEnd();
                List<ProfileDefinition> profiles = JsonConvert.DeserializeObject<List<ProfileDefinition>>(jsonText);
                return profiles;
            }
        }
    }

    public ProfileDefinition(string name, string id, string approach, string securityModel, List<string> securityModelGroupMembership, List<string> sampleQuestions, RAGSettingsSummary? ragSettingsSummary, AssistantEndpointSettingsSummary? assistantEndpointSettingsSummary)
    {
        Name = name;
        Id = id;
        Approach = approach;
        SecurityModel = securityModel;
        SampleQuestions = sampleQuestions;
        RAGSettings = ragSettingsSummary;
        AssistantEndpointSettings = assistantEndpointSettingsSummary;

        if (securityModelGroupMembership == null)
            SecurityModelGroupMembership = new List<string>();
        else
            SecurityModelGroupMembership = securityModelGroupMembership;
    }

    public string Name { get; set; }
    public string Id { get; set; }
    public string Approach { get; set; }

    public string SecurityModel { get; set; }
    public List<string> SecurityModelGroupMembership { get; set; }

    public RAGSettingsSummary? RAGSettings { get; set; }
    public AssistantEndpointSettingsSummary? AssistantEndpointSettings { get; set; }

    public string ChatSystemMessageFile { get; set; }

    public List<string> SampleQuestions { get; set; }
}


public class RAGSettingsSummary
{
    public string GenerateSearchQueryPluginName { get; set; }
    public string GenerateSearchQueryPluginQueryFunctionName { get; set; }
    public string DocumentRetrievalPluginName { get; set; }
    public string DocumentRetrievalPluginQueryFunctionName { get; set; }
    public string DocumentRetrievalIndexName { get; set; }

    public int DocumentRetrievalDocumentCount { get; set; }

    public string ChatSystemMessageFile { get; set; }
    public string StorageContianer { get; set; }
}

public class DocumentCollectionRAGSettings
{
    public string GenerateSearchQueryPluginName { get; set; }
    public string GenerateSearchQueryPluginQueryFunctionName { get; set; }
    public string DocumentRetrievalPluginName { get; set; }
    public string DocumentRetrievalPluginQueryFunctionName { get; set; }
    public string DocumentRetrievalIndexName { get; set; }

    public int DocumentRetrievalDocumentCount { get; set; }

    public string ChatSystemMessageFile { get; set; }
    public string StorageContianer { get; set; }
}

public class AssistantEndpointSettingsSummary
{
    public string APIEndpointSetting { get; set; }
    public string APIEndpointKeySetting { get; set; }
}
