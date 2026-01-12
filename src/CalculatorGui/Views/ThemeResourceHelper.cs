using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;

namespace CalculatorGui.Views;

internal static class ThemeResourceHelper
{
    // res mapping converts theme resources keys to actual names
    // b4 using direct binding in XAML but dynamic resource updates
    // was unreliable while toggling between themes

    private static readonly List<KeyValuePair<string, string>> ResourceMap = new()
    {
        new("WindowBackground", "Background"),
        new("SurfaceBackground", "Surface"),
        new("SuccessColor", "Success"),
        new("ErrorColor", "Error"),
        new("ButtonPrimary", "ButtonPrimary"),
        new("ButtonPrimaryHover", "ButtonPrimaryHover"),
        new("BorderColor", "Border"),
        new("TextPrimary", "Text"),
        new("TextSecondary", "TextSecondary"),
        new("AccentColor", "Accent"),
        new("ButtonSecondary", "ButtonSecondary"),
        new("ButtonSecondaryHover", "ButtonSecondaryHover"),
        new("ErrorBackground", "ErrorBackground")
    };

    // applies theme resources to a dictionary by prefixing with dark or light
    // this lets us swap between theme more easily
    public static void Apply(IResourceDictionary? resources, bool isDark)
    {
        if (resources == null) return;
        var prefix = isDark ? "Dark" : "Light";
        foreach (var kv in ResourceMap)
        {
            var key = prefix + kv.Value;
            if (TryResolve(resources, key, out var value))
                resources[kv.Key] = value;
        }
    }

    // try to resolve a resource irst in window resources then app resources
    // the order matters window resources take precedence for local settings
    private static bool TryResolve(IResourceDictionary resources, string key, out object? value)
    {
        if (resources.TryGetResource(key, null, out value))
            return true;

        var appResources = Application.Current?.Resources;
        return appResources?.TryGetResource(key, null, out value) == true;
    }
}
