// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Newtonsoft.Json;

namespace MinimalApi.Services.Profile;

public class ProfileDefinition
{
    static ProfileDefinition()
    {
        Load();
    }

    public static List<ProfileDefinition> All;

    private static void Load()
    {
        All = LoadProflies("profiles");
    }

    public static List<ProfileDefinition> LoadProflies(string name)
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

    public ProfileDefinition(string name, string id, string approach, string securityModel, List<string> securityModelGroupMembership, List<string> sampleQuestions, RAGSettingsSummary? ragSettingsSummary)
    {
        Name = name;
        Id = id;
        Approach = approach;
        SecurityModel = securityModel;
        SampleQuestions = sampleQuestions;
        RAGSettings = ragSettingsSummary;

        if(securityModelGroupMembership == null)
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

    public List<string> SampleQuestions { get; set; }

}


public record RAGSettingsSummary(string GenerateSearchQueryPluginName, string GenerateSearchQueryPluginQueryFunctionName, string DocumentRetrievalPluginName, string DocumentRetrievalPluginQueryFunctionName, string DocumentRetrievalIndexName, string ChatSystemMessageFile, string StorageContianer);
