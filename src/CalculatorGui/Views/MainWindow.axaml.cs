using Avalonia.Controls;
using System;
using InterpreterLib;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace CalculatorGui.Views;

public partial class MainWindow : Window
{
    private readonly record struct EvaluationResult(bool Success, string Result, string Error)
    {
        public static EvaluationResult FromSuccess(string result) => new(true, result, string.Empty);
        public static EvaluationResult FromError(string error) => new(false, string.Empty, error);
    }

    private record UIControls(
        TextBox? InputBox,
        TextBox? OutputBox,
        TextBlock? ErrorBox,
        Button? EvaluateButton,
        Button? ClearButton,
        MenuItem? HelpMenuItem,
        MenuItem? ThemeMenuItem,
        Button? PlotButton
    );

    private readonly UIControls _controls;
    private bool _isDarkTheme = true;

    public MainWindow()
    {
        InitializeComponent();
        _controls = InitializeControls();
        ThemeResourceHelper.Apply(Resources, _isDarkTheme);
        SetupEventHandlers();
    }

    private UIControls InitializeControls() =>
        new(
            InputBox: this.FindControl<TextBox>("inputBox"),
            OutputBox: this.FindControl<TextBox>("outputBox"),
            ErrorBox: this.FindControl<TextBlock>("errorBox"),
            EvaluateButton: this.FindControl<Button>("evaluateButton"),
            ClearButton: this.FindControl<Button>("clearButton"),
            HelpMenuItem: this.FindControl<MenuItem>("helpMenuItem"),
            ThemeMenuItem: this.FindControl<MenuItem>("themeMenuItem"),
            PlotButton: this.FindControl<Button>("plotButton")
        );

    private void SetupEventHandlers()
    {
        if (_controls.EvaluateButton != null)
            _controls.EvaluateButton.Click += (_, _) => HandleEvaluation();

        if (_controls.ClearButton != null)
            _controls.ClearButton.Click += (_, _) => HandleClear();

        if (_controls.HelpMenuItem != null)
            _controls.HelpMenuItem.Click += async (_, _) => await HandleHelp();

        if (_controls.ThemeMenuItem != null)
            _controls.ThemeMenuItem.Click += (_, _) => HandleThemeToggle();

        if (_controls.PlotButton != null)
            _controls.PlotButton.Click += (_, _) => OpenPlotWindow();
    }

    private EvaluationResult EvaluateExpression(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return EvaluationResult.FromError("Please enter an expression");
        try
        {
            var tuple = Interpreter.parseNeval(Interpreter.lexer(input)).ToValueTuple();
            return EvaluationResult.FromSuccess(Interpreter.NumberToString(tuple.Item2));
        }
        catch (Exception ex) { return EvaluationResult.FromError(ex.Message); }
    }

    private void HandleEvaluation() =>
        ApplyEvaluationResult(EvaluateExpression(_controls.InputBox?.Text ?? string.Empty));

    private void ApplyEvaluationResult(EvaluationResult result)
    {
        if (_controls is not { OutputBox: { } output, ErrorBox: { } error }) return;
        if (result.Success)
        {
            output.Text = result.Result;
            output.Focusable = output.IsHitTestVisible = true;
            error.Text = string.Empty;
        }
        else
        {
            output.Text = string.Empty;
            output.Focusable = output.IsHitTestVisible = false;
            error.Text = result.Error;
        }
    }

    private void HandleClear()
    {
        if (_controls.InputBox is { } input) input.Text = string.Empty;
        if (_controls.ErrorBox is { } error) error.Text = string.Empty;
        if (_controls.OutputBox is { } output)
        {
            output.Text = "Results here...";
            output.Focusable = output.IsHitTestVisible = false;
        }
    }

    private void HandleThemeToggle()
    {
        _isDarkTheme = !_isDarkTheme;
        ThemeResourceHelper.Apply(Resources, _isDarkTheme);
    }

    private async Task HandleHelp()
    {
        var dialog = CreateHelpDialog();
        await dialog.ShowDialog(this);
    }

    private Window CreateHelpDialog()
    {
        var dialog = new Window
        {
            Title = "Expression Syntax Help",
            Width = 700,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Resources?["WindowBackground"] as IBrush
        };

        ApplyDialogResources(dialog);
        dialog.Content = CreateHelpDialogContent(dialog);

        return dialog;
    }

    private void ApplyDialogResources(Window dialog)
    {
        if (Resources == null) return;
        var map = new (string k, string s)[] { ("WindowBg", "WindowBackground"), ("SurfaceBg", "SurfaceBackground"), ("BorderClr", "BorderColor"), ("TextPri", "TextPrimary"), ("TextSec", "TextSecondary"), ("AccentClr", "AccentColor"), ("SuccessClr", "SuccessColor"), ("ButtonPri", "ButtonPrimary") };
        foreach (var (k, s) in map)
            if (Resources.TryGetResource(s, null, out var r)) dialog.Resources[k] = r;
    }

    private ScrollViewer CreateHelpDialogContent(Window dialog)
    {
        var content = new StackPanel { Spacing = 15 };
        var elements = CreateHelpContentElements(dialog);

        foreach (var element in elements)
            content.Children.Add(element);

        return new ScrollViewer
        {
            Margin = new Avalonia.Thickness(20),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = content
        };
    }

    private IEnumerable<Control> CreateHelpContentElements(Window dialog)
    {
        yield return CreateTitleBlock(dialog);

        yield return CreateSectionHeader("Supported Operators");
        foreach (var item in GetOperatorHelpItems())
            yield return item;

        yield return CreateSectionHeader("Parentheses");
        yield return CreateHelpItem("Grouping", "( )", "(5 + 3) * 2");

        yield return CreateSectionHeader("Number Types");
        foreach (var item in GetNumberTypeHelpItems())
            yield return item;

        yield return CreateSectionHeader("Expression Rules");
        foreach (var rule in GetExpressionRules())
            yield return CreateInfoText(rule);

        yield return CreateSectionHeader("Example Expressions");
        foreach (var example in GetExampleExpressions())
            yield return CreateExampleText(example);

        yield return CreateCloseButton(dialog);
    }

    private TextBlock CreateTitleBlock(Window dialog) =>
        new()
        {
            Text = "Expression Syntax Guide",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = dialog.Resources?["AccentClr"] as IBrush ?? Brushes.Blue,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

    private IEnumerable<Border> GetOperatorHelpItems() =>
        new[]
        {
            ("Addition", "+", "5 + 3"),
            ("Subtraction", "−", "10 - 4"),
            ("Multiplication", "*", "6 * 7"),
            ("Division", "/", "15 / 3"),
            ("Remainder", "%", "17 % 5"),
            ("Power", "^", "2 ^ 8")
        }
        .Select(op => CreateHelpItem(op.Item1, op.Item2, op.Item3));

    private IEnumerable<Border> GetNumberTypeHelpItems() =>
        new[]
        {
            ("Integers", "0-9", "42, 123, -7"),
            ("Floating Point", ".", "3.14, -2.25, 0.021")
        }
        .Select(nt => CreateHelpItem(nt.Item1, nt.Item2, nt.Item3));

    private static IEnumerable<string> GetExpressionRules() =>
        new[]
        {
            "• BODMAS/BIDMAS order of operations applies",
            "• Left associative evaluation",
            "• Division by zero will produce an error",
            "• Supports nested parentheses"
        };

    private static IEnumerable<string> GetExampleExpressions() =>
        new[]
        {
            "(2 + 3) * 7 - 6 / 2",
            "2 ^ 4 + 2 * (30 - 3)",
            "-10 + 20 / 2",
            "3.14 * 3.5 ^ 2"
        };

    private Button CreateCloseButton(Window dialog)
    {
        var button = new Button
        {
            Content = "Close",
            Width = 120,
            Height = 40,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 20, 0, 0),
            Background = dialog.Resources?["ButtonPri"] as IBrush ?? Brushes.Green,
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new Avalonia.CornerRadius(6),
            FontWeight = FontWeight.SemiBold
        };

        button.Click += (_, _) => dialog.Close();
        return button;
    }


    private void OpenPlotWindow()
    {
        var plotWindow = new PlotWindow();
        plotWindow.SetTheme(_isDarkTheme);
        plotWindow.Show();
    }



    private Border CreateSectionHeader(string text) =>
        new()
        {
            Background = Resources?["SurfaceBackground"] as IBrush,
            BorderBrush = Resources?["BorderColor"] as IBrush,
            BorderThickness = new Avalonia.Thickness(0, 0, 0, 2),
            Padding = new Avalonia.Thickness(10, 8),
            Margin = new Avalonia.Thickness(0, 10, 0, 5),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Resources?["AccentColor"] as IBrush
            }
        };

    private Border CreateHelpItem(string name, string symbol, string example) =>
        new()
        {
            Background = Resources?["SurfaceBackground"] as IBrush,
            BorderBrush = Resources?["BorderColor"] as IBrush,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(12, 10),
            Margin = new Avalonia.Thickness(0, 2),
            Child = CreateHelpItemContent(name, symbol, example)
        };

    private StackPanel CreateHelpItemContent(string name, string symbol, string example)
    {
        var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        foreach (var c in new Control[]
        {
            new TextBlock{ Text = $"{name}:", Width=140, FontSize=14, Foreground = Resources?["TextPrimary"] as IBrush, FontWeight = FontWeight.SemiBold },
            new TextBlock{ Text = symbol, Width=60, FontSize=16, FontFamily = "Consolas,Monaco,monospace", Foreground = Resources?["SuccessColor"] as IBrush, FontWeight = FontWeight.Bold },
            new TextBlock{ Text = $"Example: {example}", FontSize=13, FontFamily = "Consolas,Monaco,monospace", Foreground = Resources?["TextSecondary"] as IBrush }
        }) panel.Children.Add(c);
        return panel;
    }

    private TextBlock CreateInfoText(string text) =>
        new()
        {
            Text = text,
            FontSize = 14,
            Foreground = Resources?["TextPrimary"] as IBrush,
            Margin = new Avalonia.Thickness(10, 3),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

    private Border CreateExampleText(string example) =>
        new()
        {
            Background = Resources?["SurfaceBackground"] as IBrush,
            BorderBrush = Resources?["BorderColor"] as IBrush,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(12, 10),
            Margin = new Avalonia.Thickness(0, 3),
            Child = new TextBlock
            {
                Text = example,
                FontSize = 14,
                FontFamily = "Consolas,Monaco,monospace",
                Foreground = Resources?["SuccessColor"] as IBrush
            }
        };


}