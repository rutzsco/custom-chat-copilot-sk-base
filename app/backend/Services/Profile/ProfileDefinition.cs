// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Newtonsoft.Json;

namespace MinimalApi.Services.Profile;

public class ProfileDefinition
{
    public static List<ProfileDefinition> All;

    public static void Load(IConfiguration configuration)
    {
        var profileConfig = configuration["ProfileConfiguration"];
        if (profileConfig != null)
        {
            var bytes = Convert.FromBase64String(profileConfig);
            var profiles = JsonConvert.DeserializeObject<List<ProfileDefinition>>(Encoding.UTF8.GetString(bytes));
            All = profiles;
            return;
        }


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
    public string ChatSystemMessage { get; set; }
    
    public List<string> SampleQuestions { get; set; }
}


public class RAGSettingsSummary
{
    public required string GenerateSearchQueryPluginName { get; set; }
    public required string GenerateSearchQueryPluginQueryFunctionName { get; set; }
    public required string DocumentRetrievalPluginName { get; set; }
    public required string DocumentRetrievalPluginQueryFunctionName { get; set; }
    public required string DocumentRetrievalIndexName { get; set; }

    public required int DocumentRetrievalDocumentCount { get; set; }
    public required string ChatSystemMessage { get; set; }
    public required string ChatSystemMessageFile { get; set; }
    public required string StorageContianer { get; set; }

    public required bool CitationUseSourcePage { get; set; }

    public required bool UseSemanticRanker { get; set; }
    public string? SemanticConfigurationName { get; set; }

    public required int KNearestNeighborsCount { get; set; } = 3;
    public required bool Exhaustive { get; set; } = false;

    public required IEnumerable<ProfileUserSelectionOption> ProfileUserSelectionOptions { get; set; }

}

public class ProfileUserSelectionOption
{
    public required string DisplayName { get; set; }
    public required string IndexFieldName { get; set; }
}

public class DocumentCollectionRAGSettings
{
    public required string GenerateSearchQueryPluginName { get; set; }
    public required string GenerateSearchQueryPluginQueryFunctionName { get; set; }
    public required string DocumentRetrievalPluginName { get; set; }
    public required string DocumentRetrievalPluginQueryFunctionName { get; set; }
    public required string DocumentRetrievalIndexName { get; set; }

    public int DocumentRetrievalDocumentCount { get; set; }

    public required string ChatSystemMessageFile { get; set; }
    public required string StorageContianer { get; set; }
}

public class AssistantEndpointSettingsSummary
{
    public required string APIEndpointSetting { get; set; }
    public required string APIEndpointKeySetting { get; set; }
}
