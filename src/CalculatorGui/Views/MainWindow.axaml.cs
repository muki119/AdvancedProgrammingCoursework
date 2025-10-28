using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using InterpreterLib;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Linq;
using System.Collections.Generic;

namespace CalculatorGui.Views;
// NEED TO REWRITE THIS FILE TO BE MORE FUNCTIONAL AND IMPROVE CODE QUALITY

public partial class MainWindow : Window
{
    // state management of the light and dark mode theme
    private record ThemeState(bool IsDark);
    private readonly TextBox? _inputBox;
    private readonly TextBox? _outputBox;
    private readonly TextBlock? _errorBox;
    private readonly Button? _evaluateButton;
    private readonly Button? _clearButton;
    private readonly MenuItem? _helpMenuItem;
    private readonly MenuItem? _themeMenuItem;

    // setting the theme to dark by default
    private ThemeState _currentTheme = new ThemeState(IsDark: true);

    public MainWindow()
    {
        InitializeComponent();

        // init ui elements
        _inputBox = this.FindControl<TextBox>("inputBox");
        _outputBox = this.FindControl<TextBox>("outputBox");
        _errorBox = this.FindControl<TextBlock>("errorBox");
        _evaluateButton = this.FindControl<Button>("evaluateButton");
        _clearButton = this.FindControl<Button>("clearButton");
        _helpMenuItem = this.FindControl<MenuItem>("helpMenuItem");
        _themeMenuItem = this.FindControl<MenuItem>("themeMenuItem");

        ApplyTheme(_currentTheme);

        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {

        if (_evaluateButton != null)
            _evaluateButton.Click += (sender, e) => HandleEvaluation();

        if (_clearButton != null)
            _clearButton.Click += (sender, e) => HandleClear();

        if (_helpMenuItem != null)
            _helpMenuItem.Click += async (sender, e) => await HandleHelp();

        if (_themeMenuItem != null)
            _themeMenuItem.Click += (sender, e) => HandleThemeToggle();
    }

    // evaluates expression and returns result or error
    private (bool success, string result, string error) EvaluateExpression(string input)
    {
        try
        {
            var tokens = Interpreter.lexer(input);
            var output = Interpreter.parseNeval(tokens);
            var o = output.ToValueTuple();
            return (success: true, result: o.Item2.ToString(), error: string.Empty);
        }
        catch (Exception ex)
        {
            return (success: false, result: string.Empty, error: ex.Message);
        }
    }

    // evaluation, applies pure function to UI
    private void HandleEvaluation()
    {
        var input = _inputBox?.Text ?? string.Empty;
        var (success, result, error) = EvaluateExpression(input);

        // Apply results to UI elements (unavoidable side effects)
        ApplyEvaluationResult(success, result, error);
    }

    // applies evaluation result to UI
    private void ApplyEvaluationResult(bool success, string result, string error)
    {
        if (_outputBox == null || _errorBox == null) return;

        if (success)
        {
            _outputBox.Text = result;
            _outputBox.Focusable = true;
            _outputBox.IsHitTestVisible = true;
            _errorBox.Text = string.Empty;
        }
        else
        {
            _outputBox.Text = string.Empty;
            _outputBox.Focusable = false;
            _outputBox.IsHitTestVisible = false;
            _errorBox.Text = error;
        }
    }

    // clears all input and output fields
    private void HandleClear()
    {
        if (_inputBox != null) _inputBox.Text = string.Empty;
        if (_errorBox != null) _errorBox.Text = string.Empty;

        if (_outputBox != null)
        {
            _outputBox.Text = "Results here...";
            _outputBox.Focusable = false;
            _outputBox.IsHitTestVisible = false;
        }
    }

    // toggles theme using immutable state transformation
    private void HandleThemeToggle()
    {
        _currentTheme = new ThemeState(IsDark: !_currentTheme.IsDark);
        ApplyTheme(_currentTheme);
    }

    //  returns theme prefix based on theme state
    private string GetThemePrefix(ThemeState theme) =>
        theme.IsDark ? "Dark" : "Light";

    // returns list of resource keys to update
    private IEnumerable<(string target, string source)> GetResourceMappings(string prefix) =>
        new[]
        {
            ("WindowBackground", $"{prefix}Background"),
            ("SurfaceBackground", $"{prefix}Surface"),
            ("BorderColor", $"{prefix}Border"),
            ("TextPrimary", $"{prefix}Text"),
            ("TextSecondary", $"{prefix}TextSecondary"),
            ("AccentColor", $"{prefix}Accent"),
            ("SuccessColor", $"{prefix}Success"),
            ("ErrorColor", $"{prefix}Error"),
            ("ButtonPrimary", $"{prefix}ButtonPrimary"),
            ("ButtonPrimaryHover", $"{prefix}ButtonPrimaryHover"),
            ("ButtonSecondary", $"{prefix}ButtonSecondary"),
            ("ButtonSecondaryHover", $"{prefix}ButtonSecondaryHover"),
            ("ErrorBackground", $"{prefix}ErrorBackground")
        };

    // applies themes
    private void ApplyTheme(ThemeState theme)
    {
        if (Resources == null) return;

        var prefix = GetThemePrefix(theme);
        var mappings = GetResourceMappings(prefix);

        // functional iteration using high order functions
        mappings
            .Where(m => Resources.TryGetResource(m.source, null, out _))
            .ToList()
            .ForEach(m => UpdateSingleResource(m.target, m.source));
    }

    // updates a resource
    private void UpdateSingleResource(string key, string sourceKey)
    {
        if (Resources != null && Resources.TryGetResource(sourceKey, null, out var resource))
        {
            Resources[key] = resource;
        }
    }

    // handler that display help dialog asynchronously
    private async Task HandleHelp()
    {
        var dialog = CreateHelpDialog();
        await dialog.ShowDialog(this);
    }

    // creates help dialog window 
    private Window CreateHelpDialog()
    {
        var helpDialog = new Window
        {
            Title = "Expression Syntax Help",
            Width = 700,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Resources?["WindowBackground"] as IBrush
        };

        // apply current theme resources to dialog
        ApplyDialogResources(helpDialog);

        helpDialog.Content = CreateHelpDialogContent(helpDialog);

        return helpDialog;
    }

    // applies theme resources to dialog
    private void ApplyDialogResources(Window dialog)
    {
        if (Resources == null) return;

        var resourceMappings = new Dictionary<string, string>
        {
            ["WindowBg"] = "WindowBackground",
            ["SurfaceBg"] = "SurfaceBackground",
            ["BorderClr"] = "BorderColor",
            ["TextPri"] = "TextPrimary",
            ["TextSec"] = "TextSecondary",
            ["AccentClr"] = "AccentColor",
            ["SuccessClr"] = "SuccessColor",
            ["ButtonPri"] = "ButtonPrimary"
        };

        foreach (var (key, sourceKey) in resourceMappings)
        {
            if (Resources.TryGetResource(sourceKey, null, out var resource))
                dialog.Resources[key] = resource;
        }
    }

    // creates help dialog content using functional composition
    private ScrollViewer CreateHelpDialogContent(Window dialog)
    {
        var scrollViewer = new ScrollViewer
        {
            Margin = new Avalonia.Thickness(20),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var helpContent = new StackPanel { Spacing = 15 };

        // build content using sequence of expressions
        var contentElements = CreateHelpContentElements(dialog);
        foreach (var element in contentElements)
        {
            helpContent.Children.Add(element);
        }

        scrollViewer.Content = helpContent;
        return scrollViewer;
    }

    // returns sequence of help content UI elements
    private IEnumerable<Control> CreateHelpContentElements(Window dialog)
    {
        // Title
        yield return new TextBlock
        {
            Text = "Expression Syntax Guide",
            FontSize = 24,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = dialog.Resources?["AccentClr"] as IBrush ?? Brushes.Blue,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

        // operators 
        yield return CreateSectionHeader("Supported Operators");
        foreach (var item in GetOperatorHelpItems())
            yield return item;

        // brackets 
        yield return CreateSectionHeader("Parentheses");
        yield return CreateHelpItem("Grouping", "( )", "(5 + 3) * 2");

        // number types
        yield return CreateSectionHeader("Number Types");
        foreach (var item in GetNumberTypeHelpItems())
            yield return item;

        // rules 
        yield return CreateSectionHeader("Expression Rules");
        foreach (var rule in GetExpressionRules())
            yield return CreateInfoText(rule);

        // eg 
        yield return CreateSectionHeader("Example Expressions");
        foreach (var example in GetExampleExpressions())
            yield return CreateExampleText(example);

        // close button
        yield return CreateCloseButton(dialog);
    }

    // returns operator help items as sequence
    private IEnumerable<Border> GetOperatorHelpItems() =>
        new[]
        {
            CreateHelpItem("Addition", "+", "5 + 3"),
            CreateHelpItem("Subtraction", "−", "10 - 4"),
            CreateHelpItem("Multiplication", "*", "6 * 7"),
            CreateHelpItem("Division", "/", "15 / 3"),
            CreateHelpItem("Remainder", "%", "17 % 5"),
            CreateHelpItem("Power", "^", "2 ^ 8"),
        };

    // text for the boxes
    // returns number type 
    private IEnumerable<Border> GetNumberTypeHelpItems() =>
        new[]
        {
            CreateHelpItem("Integers", "0-9", "42, 123, -7"),
            CreateHelpItem("Floating Point", ".", "3.14, -2.25, 0.021")
        };


    // returns expression rules

    private IEnumerable<string> GetExpressionRules() =>
        new[]
        {
            "• BODMAS/BIDMAS order of operations applies",
            "• Left associative evaluation",
            "• Division by zero will produce an error",
            "• Supports nested parentheses"
        };

    // retrusn experssions
    private IEnumerable<string> GetExampleExpressions() =>
    new[]
    {
            "(2 + 3) * 7 - 6 / 2",
            "2 ^ 4 + 2 * (30 - 3)",
            "-10 + 20 / 2",
            "3.14 * 3.5 ^ 2"
    };

    // creates close button 
    private Button CreateCloseButton(Window dialog)
    {
        var closeButton = new Button
        {
            Content = "Close",
            Width = 120,
            Height = 40,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 20, 0, 0),
            Background = dialog.Resources?["ButtonPri"] as IBrush ?? Brushes.Green,
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new Avalonia.CornerRadius(6),
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };

        //lamabda expression for event handler
        closeButton.Click += (s, e) => dialog.Close();

        return closeButton;
    }

    #region UI Element Factory Functions (Pure, Expression-Based)

    // creates section header border element
    private Border CreateSectionHeader(string text) =>
        new Border
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
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Resources?["AccentColor"] as IBrush
            }
        };

    // creates help item border with content

    private Border CreateHelpItem(string name, string symbol, string example) =>
        new Border
        {
            Background = Resources?["SurfaceBackground"] as IBrush,
            BorderBrush = Resources?["BorderColor"] as IBrush,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(12, 10),
            Margin = new Avalonia.Thickness(0, 2),
            Child = CreateHelpItemContent(name, symbol, example)
        };

    // creates help item panel
    private StackPanel CreateHelpItemContent(string name, string symbol, string example)
    {
        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10
        };


        var children = new Control[]
        {
            new TextBlock
            {
                Text = $"{name}:",
                Width = 140,
                FontSize = 14,
                Foreground = Resources?["TextPrimary"] as IBrush,
                FontWeight = Avalonia.Media.FontWeight.SemiBold
            },
            new TextBlock
            {
                Text = symbol,
                Width = 60,
                FontSize = 16,
                FontFamily = "Consolas,Monaco,monospace",
                Foreground = Resources?["SuccessColor"] as IBrush,
                FontWeight = Avalonia.Media.FontWeight.Bold
            },
            new TextBlock
            {
                Text = $"Example: {example}",
                FontSize = 13,
                FontFamily = "Consolas,Monaco,monospace",
                Foreground = Resources?["TextSecondary"] as IBrush
            }
        };

        foreach (var child in children)
            panel.Children.Add(child);

        return panel;
    }

    /// creates info text block
    private TextBlock CreateInfoText(string text) =>
    new TextBlock
    {
        Text = text,
        FontSize = 14,
        Foreground = Resources?["TextPrimary"] as IBrush,
        Margin = new Avalonia.Thickness(10, 3),
        TextWrapping = Avalonia.Media.TextWrapping.Wrap
    };

    private Border CreateExampleText(string example) =>
        new Border
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

    #endregion
}