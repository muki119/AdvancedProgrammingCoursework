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
    private record ThemeState(bool IsDark); // tracks current theme state. true for dark mode , false for light mode

    private record EvaluationResult(bool Success, string Result, string Error) // represents result of expression evaluation. contains success flag , result string and error message
    {
        public static EvaluationResult CreateSuccess(string result) => // creates successful evaluation result with given result string
            new(true, result, string.Empty);

        public static EvaluationResult CreateError(string error) => // creates error evaluation result with given error message
            new(false, string.Empty, error);
    }

    private record UIControls( // encapsulates all UI control references from AXAML markup
        TextBox? InputBox,
        TextBox? OutputBox,
        TextBlock? ErrorBox,
        Button? EvaluateButton,
        Button? ClearButton,
        MenuItem? HelpMenuItem,
        MenuItem? ThemeMenuItem,
        Button? PlotButton
    );

    private readonly UIControls _controls; // stores references to UI controls
    private ThemeState _currentTheme = new(IsDark: true); // stores current theme state. defaults to dark mode





    // initializes  main window and its components
    public MainWindow()
    {
        InitializeComponent();
        _controls = InitializeControls();
        ApplyTheme(_currentTheme);
        SetupEventHandlers();
    }

    // retrieves UI control references from the AXAML markup
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

    // attaches event handlers to UI controls
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

    private EvaluationResult EvaluateExpression(string input) // evaluates expression string using F# interpreter. returns success with result or error message
    {
        if (string.IsNullOrWhiteSpace(input)) return EvaluationResult.CreateError("Please enter an expression");
        try
        {
            var tuple = Interpreter.parseNeval(Interpreter.lexer(input)).ToValueTuple(); // lex and parse input then evaluate
            return EvaluationResult.CreateSuccess(Interpreter.NumberToString(tuple.Item2));
        }
        catch (Exception ex) { return EvaluationResult.CreateError(ex.Message); }
    }

    private void HandleEvaluation() => // handles evaluate button click. evaluates input box text and applies result
        ApplyEvaluationResult(EvaluateExpression(_controls.InputBox?.Text ?? string.Empty));

    private void ApplyEvaluationResult(EvaluationResult result) // applies evaluation result to UI. updates output box or error box based on success
    {
        var o = _controls.OutputBox; var e = _controls.ErrorBox;
        if (o == null || e == null) return;
        if (result.Success)
        { o.Text = result.Result; o.Focusable = true; o.IsHitTestVisible = true; e.Text = string.Empty; }
        else
        { o.Text = string.Empty; o.Focusable = false; o.IsHitTestVisible = false; e.Text = result.Error; }
    }

    private void HandleClear() // handles clear button click. resets input box , error box and output box to default state
    {
        if (_controls.InputBox != null) _controls.InputBox.Text = string.Empty;
        if (_controls.ErrorBox != null) _controls.ErrorBox.Text = string.Empty;
        if (_controls.OutputBox != null)
        { _controls.OutputBox.Text = "Results here..."; _controls.OutputBox.Focusable = false; _controls.OutputBox.IsHitTestVisible = false; }
    }

    private void HandleThemeToggle() // handles theme toggle menu item click. switches between dark and light mode
    {
        _currentTheme = new ThemeState(IsDark: !_currentTheme.IsDark);
        ApplyTheme(_currentTheme);
    }

    private static readonly (string target, string suffix)[] ResourceMap = // maps resource names to their suffixes for theme switching
    {
        ("WindowBackground","Background"),("SurfaceBackground","Surface"),("BorderColor","Border"),
        ("TextPrimary","Text"),("TextSecondary","TextSecondary"),("AccentColor","Accent"),
        ("SuccessColor","Success"),("ErrorColor","Error"),("ButtonPrimary","ButtonPrimary"),
        ("ButtonPrimaryHover","ButtonPrimaryHover"),("ButtonSecondary","ButtonSecondary"),
        ("ButtonSecondaryHover","ButtonSecondaryHover"),("ErrorBackground","ErrorBackground")
    };

    private void ApplyTheme(ThemeState theme) // applies theme to window by updating resource dictionary with light or dark theme resources
    {
        if (Resources == null) return;
        var prefix = theme.IsDark ? "Dark" : "Light";
        foreach (var (target, suffix) in ResourceMap)
            if (Resources.TryGetResource(prefix + suffix, null, out var r)) Resources[target] = r;
    }

    private async Task HandleHelp() // displays help dialog window with expression syntax guide
    {
        var dialog = CreateHelpDialog();
        await dialog.ShowDialog(this);
    }

    private Window CreateHelpDialog() // creates and configures the help dialog window with title. dimensions and background
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

    private void ApplyDialogResources(Window dialog) // copies theme resources from main window to help dialog for consistent styling
    {
        if (Resources == null) return;
        var map = new (string k, string s)[] { ("WindowBg", "WindowBackground"), ("SurfaceBg", "SurfaceBackground"), ("BorderClr", "BorderColor"), ("TextPri", "TextPrimary"), ("TextSec", "TextSecondary"), ("AccentClr", "AccentColor"), ("SuccessClr", "SuccessColor"), ("ButtonPri", "ButtonPrimary") };
        foreach (var (k, s) in map)
            if (Resources.TryGetResource(s, null, out var r)) dialog.Resources[k] = r;
    }

    private ScrollViewer CreateHelpDialogContent(Window dialog) // creates the scrollable content container for the help dialog
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

    // generates all content elements for the help dialog
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

    // creates the title text block for the help dialog
    private TextBlock CreateTitleBlock(Window dialog) =>
        new()
        {
            Text = "Expression Syntax Guide",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = dialog.Resources?["AccentClr"] as IBrush ?? Brushes.Blue,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

    // returns help items for supported operators
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

    // returns help items for supported number types
    private IEnumerable<Border> GetNumberTypeHelpItems() =>
        new[]
        {
            ("Integers", "0-9", "42, 123, -7"),
            ("Floating Point", ".", "3.14, -2.25, 0.021")
        }
        .Select(nt => CreateHelpItem(nt.Item1, nt.Item2, nt.Item3));

    // returns expression evaluation rules
    private static IEnumerable<string> GetExpressionRules() =>
        new[]
        {
            "• BODMAS/BIDMAS order of operations applies",
            "• Left associative evaluation",
            "• Division by zero will produce an error",
            "• Supports nested parentheses"
        };

    // returns example expressions for demonstration
    private static IEnumerable<string> GetExampleExpressions() =>
        new[]
        {
            "(2 + 3) * 7 - 6 / 2",
            "2 ^ 4 + 2 * (30 - 3)",
            "-10 + 20 / 2",
            "3.14 * 3.5 ^ 2"
        };

    // creates the close button for the help dialog
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


    // opens dedicated plot window with current theme
    private void OpenPlotWindow()
    {
        var plotWindow = new PlotWindow();
        plotWindow.SetTheme(_currentTheme.IsDark);
        plotWindow.Show();
    }



    // creates a section header border element
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

    // creates a help item displaying name. symbol. and example
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

    // creates a text block for informational text
    private TextBlock CreateInfoText(string text) =>
        new()
        {
            Text = text,
            FontSize = 14,
            Foreground = Resources?["TextPrimary"] as IBrush,
            Margin = new Avalonia.Thickness(10, 3),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

    // creates a styled border for example expressions
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