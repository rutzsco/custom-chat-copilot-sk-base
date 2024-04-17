// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Profile;

public class ProfileDefinition
{

    public static ProfileDefinition Auto = new ProfileDefinition("Auto Service Advisor", "RAG", "UAL");
    public static ProfileDefinition General = new ProfileDefinition("General", "Chat", "None");

    public static List<ProfileDefinition> All = new List<ProfileDefinition>
    {
        Auto
        //General
    };

    public ProfileDefinition(string name, string approach, string securityModel)
    {
        Name = name;
        Approach = approach;
        SecurityModel = securityModel;
    }

    public string Name { get; set; }

    public string Approach { get; set; }

    public string SecurityModel { get; set; }

    public string Index { get; set; }

}
