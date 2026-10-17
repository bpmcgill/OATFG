using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OATFG.UI.Services;

/// <summary>
/// Swaps the application-level theme ResourceDictionary at runtime. Control styles in
/// Base.xaml resolve every brush through DynamicResource, so changing the theme re-skins
/// the whole window without touching the view.
/// </summary>
public sealed partial class ThemeManager : ObservableObject
{
    public static ThemeManager Instance { get; } = new();

    public IReadOnlyList<string> AvailableThemes { get; } =
    [
        "Cyberpunk Dark",
        "Cyberpunk Light",
        "Cyberpunk Dark Pastel",
        "Cyberpunk Light Pastel"
    ];

    private static readonly IReadOnlyDictionary<string, string> ThemeSources = new Dictionary<string, string>
    {
        ["Cyberpunk Dark"] = "Themes/CyberpunkDark.xaml",
        ["Cyberpunk Light"] = "Themes/CyberpunkLight.xaml",
        ["Cyberpunk Dark Pastel"] = "Themes/CyberpunkDarkPastel.xaml",
        ["Cyberpunk Light Pastel"] = "Themes/CyberpunkLightPastel.xaml"
    };

    [ObservableProperty]
    private string _currentTheme = "Cyberpunk Dark";

    partial void OnCurrentThemeChanged(string value)
    {
        if (Application.Current is null || !ThemeSources.TryGetValue(value, out string? source))
        {
            return;
        }

        var merged = Application.Current.Resources.MergedDictionaries;

        // drop the previous theme dictionary (Base.xaml stays)
        ResourceDictionary? previous = merged.FirstOrDefault(d =>
            d.Source is not null &&
            d.Source.OriginalString.StartsWith("Themes/", StringComparison.Ordinal) &&
            !d.Source.OriginalString.EndsWith("Base.xaml", StringComparison.Ordinal));
        if (previous is not null)
        {
            merged.Remove(previous);
        }

        merged.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
    }
}