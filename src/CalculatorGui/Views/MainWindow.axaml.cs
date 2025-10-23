using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using InterpreterLib;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Linq;
using System.Collections.Generic;

namespace CalculatorGui.Views;

/// <summary>
/// Functional-style Main Window implementation
/// Principles: Expression-based design, minimal mutable state, pure functions where possible
/// </summary>
public partial class MainWindow : Window
{
    // Immutable theme state record for functional theme management
    private record ThemeState(bool IsDark);

    // Minimal mutable state - only for UI element references and theme state
    // Note: Avalonia GUI framework requires some imperative interaction
    private readonly TextBox? _inputBox;
    private readonly TextBox? _outputBox;
    private readonly TextBlock? _errorBox;
    private readonly Button? _evaluateButton;
    private readonly Button? _clearButton;
    private readonly MenuItem? _helpMenuItem;
    private readonly MenuItem? _themeMenuItem;

    // Theme state as immutable value - changed by returning new state
    private ThemeState _currentTheme = new ThemeState(IsDark: true);

    public MainWindow()
    {
        InitializeComponent();

        // Initialize UI element references (expression-based with pattern matching)
        _inputBox = this.FindControl<TextBox>("inputBox");
        _outputBox = this.FindControl<TextBox>("outputBox");
        _errorBox = this.FindControl<TextBlock>("errorBox");
        _evaluateButton = this.FindControl<Button>("evaluateButton");
        _clearButton = this.FindControl<Button>("clearButton");
        _helpMenuItem = this.FindControl<MenuItem>("helpMenuItem");
        _themeMenuItem = this.FindControl<MenuItem>("themeMenuItem");

        // Initialize default theme (Dark mode)
        ApplyTheme(_currentTheme);

        // Setup event handlers using lambda expressions
        SetupEventHandlers();
    }

    /// <summary>
    /// Setup event handlers using lambda expressions (higher-order functions)
    /// </summary>
    private void SetupEventHandlers()
    {
        // Attach event handlers as lambda expressions
        if (_evaluateButton != null)
            _evaluateButton.Click += (sender, e) => HandleEvaluation();

        if (_clearButton != null)
            _clearButton.Click += (sender, e) => HandleClear();

        if (_helpMenuItem != null)
            _helpMenuItem.Click += async (sender, e) => await HandleHelp();

        if (_themeMenuItem != null)
            _themeMenuItem.Click += (sender, e) => HandleThemeToggle();
    }

    /// <summary>
    /// Pure function: Evaluates expression and returns result or error
    /// </summary>
    private (bool success, string result, string error) EvaluateExpression(string input)
    {
        try
        {
            var tokens = Interpreter.lexer(input);
            var (_, result) = Interpreter.parseNeval(tokens);
            return (success: true, result: result.ToString(), error: string.Empty);
        }
        catch (Exception ex)
        {
            return (success: false, result: string.Empty, error: ex.Message);
        }
    }

    /// <summary>
    /// Handler: Orchestrates evaluation - applies pure function to UI
    /// </summary>
    private void HandleEvaluation()
    {
        var input = _inputBox?.Text ?? string.Empty;
        var (success, result, error) = EvaluateExpression(input);

        // Apply results to UI elements (unavoidable side effects)
        ApplyEvaluationResult(success, result, error);
    }

    /// <summary>
    /// Pure side effect: Applies evaluation result to UI
    /// </summary>
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

    /// <summary>
    /// Handler: Clears all input and output fields
    /// </summary>
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

    /// <summary>
    /// Handler: Toggles theme using immutable state transformation
    /// </summary>
    private void HandleThemeToggle()
    {
        // Functional state update: create new immutable state
        _currentTheme = new ThemeState(IsDark: !_currentTheme.IsDark);
        ApplyTheme(_currentTheme);
    }

    /// <summary>
    /// Pure function: Returns theme prefix based on theme state
    /// </summary>
    private string GetThemePrefix(ThemeState theme) =>
        theme.IsDark ? "Dark" : "Light";

    /// <summary>
    /// Pure function: Returns list of resource keys to update
    /// </summary>
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

    /// <summary>
    /// Applies theme by updating resources (expression-based with LINQ)
    /// </summary>
    private void ApplyTheme(ThemeState theme)
    {
        if (Resources == null) return;

        var prefix = GetThemePrefix(theme);
        var mappings = GetResourceMappings(prefix);

        // Functional iteration using LINQ/higher-order functions
        mappings
            .Where(m => Resources.TryGetResource(m.source, null, out _))
            .ToList()
            .ForEach(m => UpdateSingleResource(m.target, m.source));
    }

    /// <summary>
    /// Pure side effect: Updates a single resource
    /// </summary>
    private void UpdateSingleResource(string key, string sourceKey)
    {
        if (Resources != null && Resources.TryGetResource(sourceKey, null, out var resource))
        {
            Resources[key] = resource;
        }
    }

    /// <summary>
    /// Handler: Displays help dialog asynchronously
    /// </summary>
    private async Task HandleHelp()
    {
        var dialog = CreateHelpDialog();
        await dialog.ShowDialog(this);
    }

    /// <summary>
    /// Pure function: Creates help dialog window (expression-based construction)
    /// </summary>
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

        // Apply current theme resources to dialog
        ApplyDialogResources(helpDialog);

        // Build content using expression-based composition
        helpDialog.Content = CreateHelpDialogContent(helpDialog);

        return helpDialog;
    }

    /// <summary>
    /// Pure function: Applies theme resources to dialog
    /// </summary>
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

    /// <summary>
    /// Pure function: Creates help dialog content using functional composition
    /// </summary>
    private ScrollViewer CreateHelpDialogContent(Window dialog)
    {
        var scrollViewer = new ScrollViewer
        {
            Margin = new Avalonia.Thickness(20),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var helpContent = new StackPanel { Spacing = 15 };

        // Build content using sequence of expressions
        var contentElements = CreateHelpContentElements(dialog);
        foreach (var element in contentElements)
        {
            helpContent.Children.Add(element);
        }

        scrollViewer.Content = helpContent;
        return scrollViewer;
    }

    /// <summary>
    /// Pure function: Returns sequence of help content UI elements
    /// Uses yield for lazy evaluation (functional iterator pattern)
    /// </summary>
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

        // Operators Section
        yield return CreateSectionHeader("Supported Operators");
        foreach (var item in GetOperatorHelpItems())
            yield return item;

        // Parentheses Section
        yield return CreateSectionHeader("Parentheses");
        yield return CreateHelpItem("Grouping", "( )", "(5 + 3) * 2");

        // Number Types Section
        yield return CreateSectionHeader("Number Types");
        foreach (var item in GetNumberTypeHelpItems())
            yield return item;

        // Rules Section
        yield return CreateSectionHeader("Expression Rules");
        foreach (var rule in GetExpressionRules())
            yield return CreateInfoText(rule);

        // Examples Section
        yield return CreateSectionHeader("Example Expressions");
        foreach (var example in GetExampleExpressions())
            yield return CreateExampleText(example);

        // Close button
        yield return CreateCloseButton(dialog);
    }

    /// <summary>
    /// Pure function: Returns operator help items as sequence
    /// </summary>
    private IEnumerable<Border> GetOperatorHelpItems() =>
        new[]
        {
            CreateHelpItem("Addition", "+", "5 + 3"),
            CreateHelpItem("Subtraction", "−", "10 - 4"),
            CreateHelpItem("Multiplication", "*", "6 * 7"),
            CreateHelpItem("Division", "/", "15 / 3"),
            CreateHelpItem("Remainder", "%", "17 % 5"),
            CreateHelpItem("Power", "^", "2 ^ 8"),
            CreateHelpItem("Unary Minus", "−", "-5 + 3")
        };

    /// <summary>
    /// Pure function: Returns number type help items as sequence
    /// </summary>
    private IEnumerable<Border> GetNumberTypeHelpItems() =>
        new[]
        {
            CreateHelpItem("Integers", "0-9", "42, 123, -7"),
            CreateHelpItem("Floating Point", ".", "3.14, -2.5, 0.001")
        };

    /// <summary>
    /// Pure function: Returns expression rules as sequence
    /// </summary>
    private IEnumerable<string> GetExpressionRules() =>
        new[]
        {
            "• BODMAS/BIDMAS order of operations applies",
            "• Left associative evaluation",
            "• Division by zero will produce an error",
            "• Supports nested parentheses"
        };

    /// <summary>
    /// Pure function: Returns example expressions as sequence
    /// </summary>
    private IEnumerable<string> GetExampleExpressions() =>
        new[]
        {
            "(5 + 3) * 2 - 4 / 2",
            "2 ^ 3 + 5 * (10 - 3)",
            "-10 + 20 / 2",
            "3.14 * 2.5 ^ 2"
        };

    /// <summary>
    /// Pure function: Creates close button with event handler
    /// </summary>
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

        // Lambda expression for event handler
        closeButton.Click += (s, e) => dialog.Close();

        return closeButton;
    }

    #region UI Element Factory Functions (Pure, Expression-Based)

    /// <summary>
    /// Pure function: Creates section header border element
    /// </summary>
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

    /// <summary>
    /// Pure function: Creates help item border with content
    /// </summary>
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

    /// <summary>
    /// Pure function: Creates help item content panel
    /// </summary>
    private StackPanel CreateHelpItemContent(string name, string symbol, string example)
    {
        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10
        };

        // Expression-based child addition using collection initializer pattern
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

    /// <summary>
    /// Pure function: Creates info text block
    /// </summary>
    private TextBlock CreateInfoText(string text) =>
        new TextBlock
        {
            Text = text,
            FontSize = 14,
            Foreground = Resources?["TextPrimary"] as IBrush,
            Margin = new Avalonia.Thickness(10, 3),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

    /// <summary>
    /// Pure function: Creates example text border
    /// </summary>
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