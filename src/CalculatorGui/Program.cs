using Avalonia;
using System;
using Microsoft.FSharp;
using InterpreterLib;
namespace CalculatorGui;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args) // entry point for application. initializes Avalonia framework and starts the GUI
    {
        var testCall = Interpreter.testCall; // test call to verify F# library connection
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() // configures Avalonia app builder with platform detection and font settings
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}