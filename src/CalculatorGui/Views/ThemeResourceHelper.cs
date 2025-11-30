using Avalonia;
using Avalonia.Controls;

namespace CalculatorGui.Views;

internal static class ThemeResourceHelper
{
    private static readonly (string Target, string Suffix)[] ResourceMap =
    {
        ("WindowBackground", "Background"),
        ("SurfaceBackground", "Surface"),
        ("BorderColor", "Border"),
        ("TextPrimary", "Text"),
        ("TextSecondary", "TextSecondary"),
        ("AccentColor", "Accent"),
        ("SuccessColor", "Success"),
        ("ErrorColor", "Error"),
        ("ButtonPrimary", "ButtonPrimary"),
        ("ButtonPrimaryHover", "ButtonPrimaryHover"),
        ("ButtonSecondary", "ButtonSecondary"),
        ("ButtonSecondaryHover", "ButtonSecondaryHover"),
        ("ErrorBackground", "ErrorBackground")
    };

    public static void Apply(IResourceDictionary? resources, bool isDark)
    {
        if (resources == null) return;
        var prefix = isDark ? "Dark" : "Light";
        foreach (var (target, suffix) in ResourceMap)
            if (TryResolve(resources, prefix + suffix, out var value))
                resources[target] = value;
    }

    private static bool TryResolve(IResourceDictionary resources, string key, out object? value)
    {
        if (resources.TryGetResource(key, null, out value))
            return true;

        var appResources = Application.Current?.Resources;
        return appResources?.TryGetResource(key, null, out value) == true;
    }
}
