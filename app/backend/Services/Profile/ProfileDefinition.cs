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

    public static ProfileDefinition RAG = new ProfileDefinition("Auto Service Advisor", "RAG", "UAL", new List<string> { "How do I change the oil?","What are the different maintenance intervals?","What is the air filter part number"}, new RAGSettingsSummary(DefaultSettings.GenerateSearchQueryPluginName, DefaultSettings.GenerateSearchQueryPluginQueryFunctionName, DefaultSettings.DocumentRetrievalPluginName, DefaultSettings.DocumentRetrievalPluginQueryFunctionName, "manuals"));
    //public static ProfileDefinition General = new ProfileDefinition("General", "Chat", "None", new List<string> { "Write a funciton in C# that will invoke a rest API" });
    public static List<ProfileDefinition> All;

    //public static ProfileDefinition GetProfile(string name)
    //{
    //    return All.FirstOrDefault(p => p.Name == name);
    //}


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

    public ProfileDefinition(string name, string approach, string securityModel, List<string> sampleQuestions, RAGSettingsSummary? ragSettingsSummary)
    {
        Name = name;
        Approach = approach;
        SecurityModel = securityModel;
        SampleQuestions = sampleQuestions;
        RAGSettings = ragSettingsSummary;
    }

    public string Name { get; set; }

    public string Approach { get; set; }

    public string SecurityModel { get; set; }

    public RAGSettingsSummary? RAGSettings { get; set; }

    public List<string> SampleQuestions { get; set; }

}


public record RAGSettingsSummary(string GenerateSearchQueryPluginName, string GenerateSearchQueryPluginQueryFunctionName, string DocumentRetrievalPluginName, string DocumentRetrievalPluginQueryFunctionName, string DocumentRetrievalIndexName);
