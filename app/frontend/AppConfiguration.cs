﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;

namespace ClientApp;

public static class AppConfiguration
{

    public static void Load(IConfiguration config)
    {
        LogoImagePath = config.GetValue<string>("LogoImagePath", "icon-512.png");
        ColorPaletteLightPrimary = config.GetValue<string>("ColorPaletteLightPrimary", "#84B1CB");
        ColorPaletteLightSecondary = config.GetValue<string>("ColorPaletteLightSecondary", "#287FA4");
        ColorPaletteLightAppbarBackground = config.GetValue<string>("ColorPaletteLightAppbarBackground", "#84B1CB");

        HelloText = config.GetValue<string>("HelloText", "How can I help you today?");

        ShowSampleQuestions = config.GetValue<bool>("ShowSampleQuestions", true);
        ShowPremiumAOAIToggleSelection = config.GetValue<bool>("ShowPremiumAOAIToggleSelection", true);

        ExampleQuestion1 = config.GetValue<string>("ExampleQuestion1", "Q1?");
        ExampleQuestion2 = config.GetValue<string>("ExampleQuestion2", "Q2?");
        ExampleQuestion3 = config.GetValue<string>("ExampleQuestion3", "Q3?");
    }

    public static string ExampleQuestion1 { get; set; } = "Q1?";
    public static string ExampleQuestion2 { get; set; } = "Q2?";
    public static string ExampleQuestion3 { get; set; } = "Q3?";
    public static string ColorPaletteLightPrimary { get; set; } = "#005eb8";
    public static string ColorPaletteLightSecondary { get; set; } = "#287FA4";
    public static string ColorPaletteLightAppbarBackground { get; set; } = "#84B1CB";
    public static string LogoImagePath { get; set; } = "icon-512.png";
    public static int LogoImageWidth { get; set; } = 150;
    public static string HelloText { get; set; } = "";

    public static bool ShowSampleQuestions { get; set; } = true;

    public static bool ShowPremiumAOAIToggleSelection { get; set; } = true;
}
