using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using CalculatorGui.ViewModels;
using CalculatorGui.Views;

namespace CalculatorGui;

public partial class App : Application
{
    public override void Initialize() // initializes application. loads AXAML resources
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() // called when framework initialization is complete. sets up main window and data context
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation(); // disable data annotation validation plugin
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation() // disables Avalonia data annotation validation plugin. removes all data annotation validators from binding plugins
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove) // iterate through and remove each data annotation plugin
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}