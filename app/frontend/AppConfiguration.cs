// Copyright (c) Microsoft. All rights reserved.

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

        DisclaimerMessage = config.GetValue<string>("DisclaimerMessage", "DISCMLAIMER MESSAGE?");
    }

    public static string ColorPaletteLightPrimary { get; set; } = "#005eb8";
    public static string ColorPaletteLightSecondary { get; set; } = "#287FA4";
    public static string ColorPaletteLightAppbarBackground { get; set; } = "#84B1CB";
    public static string LogoImagePath { get; set; } = "icon-512.png";
    public static int LogoImageWidth { get; set; } = 150;
    public static string HelloText { get; set; } = "";

    public static bool ShowSampleQuestions { get; set; } = true;

    public static bool ShowPremiumAOAIToggleSelection { get; set; } = true;

    public static string DisclaimerMessage { get; set; } = string.Empty;

    public static string GetAppBarBackgroundBar()
    {
        return $"background-color: {ColorPaletteLightAppbarBackground};";
    }
}
