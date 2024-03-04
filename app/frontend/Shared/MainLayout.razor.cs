// Copyright (c) Microsoft. All rights reserved.

using static System.Net.WebRequestMethods;

namespace ClientApp.Shared;

public sealed partial class MainLayout
{
    private readonly MudTheme _theme = new()
    {
        Palette = new PaletteLight
        {
            Primary = AppConfiguration.ColorPaletteLightPrimary,
            AppbarBackground = AppConfiguration.ColorPaletteLightAppbarBackground, 
            Secondary = AppConfiguration.ColorPaletteLightSecondary
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#1277bd",
        }
    };
    private bool _drawerOpen = true;
    private bool _settingsOpen = false;
    private SettingsPanel? _settingsPanel;

    private bool _isDarkTheme
    {
        get => LocalStorage.GetItem<bool>(StorageKeys.PrefersDarkTheme);
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersDarkTheme, value);
    }

    private bool _isReversed
    {
        get => LocalStorage.GetItem<bool?>(StorageKeys.PrefersReversedConversationSorting) ?? true;
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersReversedConversationSorting, value);
    }

    private bool _isRightToLeft =>
        Thread.CurrentThread.CurrentUICulture is { TextInfo.IsRightToLeft: true };

    [Inject] public required NavigationManager Nav { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }

    private bool SettingsDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "ask" or "chat" => false,
        _ => true
    };

    private string LogoImagePath
    {
        get
        {
            return AppConfiguration.LogoImagePath;
        }
    }

    private int LogoImageWidth
    {
        get
        {
            return AppConfiguration.LogoImageWidth;
        }
    }

    private bool SortDisabled
    {
        get
        {
            return new Uri(Nav.Uri).Segments.LastOrDefault() switch
            {
                "documents" => true,
                _ => false
            };
        }
    }

    private void OnMenuClicked() => _drawerOpen = !_drawerOpen;

    private void OnThemeChanged() => _isDarkTheme = !_isDarkTheme;

    private void OnIsReversedChanged() => _isReversed = !_isReversed;
}
