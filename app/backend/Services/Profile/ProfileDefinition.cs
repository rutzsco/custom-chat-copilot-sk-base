// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Profile;

public class ProfileDefinition
{

    public static ProfileDefinition Auto = new ProfileDefinition("Auto Service Advisor", "RAG");
    public static ProfileDefinition General = new ProfileDefinition("General", "Chat");

    public static List<ProfileDefinition> All = new List<ProfileDefinition>
    {
        Auto,
        General
    };

    public ProfileDefinition(string name, string approach)
    {
        Name = name;
        Approach = approach;
    }

    public string Name { get; set; }

    public string Approach { get; set; }

}
