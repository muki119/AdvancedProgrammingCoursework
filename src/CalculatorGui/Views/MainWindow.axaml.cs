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
    // UI control references tried data binding first but Avalonia's compiled bindings
    // were not working with theme switching, so i reverted to direct refs not ideal but it still
    private TextBox? _inputBox;
    private TextBox? _outputBox;
    private TextBlock? _errorBox;
    private Button? _evaluateButton;
    private Button? _clearButton;
    private MenuItem? _helpMenuItem;
    private MenuItem? _themeMenuItem;
    private Button? _plotButton;

    private bool _isDarkTheme = true;
    // TO DO THEME PREFERENCE TO LOCAL STORAGE SO IT DOESNT RESET EVERYTIME

    // constructor findcontrol uses runtime name lookup instead of binding done at compile time
    // it is slower but works for dynamic theme switching without full recompilation
    public MainWindow()
    {
        InitializeComponent();

        // tried to use NameScope binding but was too much hassle 
        _inputBox = this.FindControl<TextBox>("inputBox");
        _outputBox = this.FindControl<TextBox>("outputBox");
        _errorBox = this.FindControl<TextBlock>("errorBox");
        _evaluateButton = this.FindControl<Button>("evaluateButton");
        _clearButton = this.FindControl<Button>("clearButton");
        _helpMenuItem = this.FindControl<MenuItem>("helpMenuItem");
        _themeMenuItem = this.FindControl<MenuItem>("themeMenuItem");
        _plotButton = this.FindControl<Button>("plotButton");

        ThemeResourceHelper.Apply(Resources, _isDarkTheme);
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // keeps all view logic separate and let null check 
        // avalonia throws if you attach handlers to null controls so seperated them
        if (_clearButton != null)
            _clearButton.Click += OnClearClicked;

        if (_helpMenuItem != null)
            _helpMenuItem.Click += OnHelpClicked;

        if (_evaluateButton != null)
            _evaluateButton.Click += OnEvaluateClicked;


        if (_themeMenuItem != null)
            _themeMenuItem.Click += OnThemeClicked;

        if (_plotButton != null)
            _plotButton.Click += OnPlotClicked;
    }

    // event handlers for button clicks 
    private void OnEvaluateClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        HandleEvaluation();
    }
    private async void OnHelpClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await HandleHelp();
    }

    private void OnClearClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        HandleClear();
    }




    private void OnThemeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        HandleThemeToggle();
    }

    private void OnPlotClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenPlotWindow();
    }





    private void HandleEvaluation()
    {
        if (_outputBox == null || _errorBox == null) return;

        // text box can return null during init
        string input = _inputBox?.Text ?? "";

        if (string.IsNullOrWhiteSpace(input))
        {
            _errorBox.Text = "Please enter an expression";
            _outputBox.Text = "";
            return;
        }

        try
        {
            // tokenise -> parse -> eval (aio to interpreter)
            var tokens = Interpreter.lexer(input);
            var result = Interpreter.parseNeval(tokens);
            var tuple = result.ToValueTuple();
            string output = Interpreter.NumberToString(tuple.Item2);

            _outputBox.Text = output; // displays result
            _outputBox.Focusable = true;
            _outputBox.IsHitTestVisible = true;
            _errorBox.Text = "";
        }
        catch (Exception ex)
        {
            // exception from interpreter could be  any runtime errors
            // seeting focusable to false prevents the user from editing an invalid output
            _outputBox.Text = "";
            _outputBox.Focusable = false;
            _outputBox.IsHitTestVisible = false;
            _errorBox.Text = ex.Message;
        }
    }

    private void HandleClear()
    {
        if (_inputBox != null)

            _inputBox.Text = "";

        if (_errorBox != null)
            _errorBox.Text = "";

        if (_outputBox != null)
        {
            _outputBox.Text = "Results here...";
            _outputBox.Focusable = false;
            _outputBox.IsHitTestVisible = false;
        }
    }

    private void HandleThemeToggle()
    {
        _isDarkTheme = !_isDarkTheme;
        // themes stored in app.axaml but each window needs to reapply them on toggle
        // initially used dynamic resource binding but had issues with theme specific brushes not updating properly
        ThemeResourceHelper.Apply(Resources, _isDarkTheme);
    }

    // help dialog building UI in code instead of XAML 
    private async Task HandleHelp()
    {
        Window dialog = CreateHelpDialog();
        await dialog.ShowDialog(this);
    }



    private Window CreateHelpDialog()
    {
        // building dialog programmatically instead of XAML 
        //  easier to ref and update theme resources anytime
        var dialog = new Window
        {
            Title = "Expression Syntax Help",
            Width = 700,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Resources?["WindowBackground"] as IBrush
        };

        CopyDialogResources(dialog);
        dialog.Content = CreateHelpDialogContent(dialog);

        return dialog;
    }

    private void CopyDialogResources(Window dialog)
    {
        if (Resources == null) return;

        // dialogs don't inherit parent window theme resources automatically
        // b4 dialog would open in default theme despite app being dark
        //so manually copied the resources using in the dialog
        if (Resources.TryGetResource("WindowBackground", null, out var windowBg))
            dialog.Resources["WindowBg"] = windowBg;
        if (Resources.TryGetResource("SurfaceBackground", null, out var surfaceBg))
            dialog.Resources["SurfaceBg"] = surfaceBg;
        if (Resources.TryGetResource("BorderColor", null, out var borderClr))
            dialog.Resources["BorderClr"] = borderClr;
        if (Resources.TryGetResource("TextPrimary", null, out var textPri))
            dialog.Resources["TextPri"] = textPri;
        if (Resources.TryGetResource("TextSecondary", null, out var textSec))
            dialog.Resources["TextSec"] = textSec;
        if (Resources.TryGetResource("AccentColor", null, out var accentClr))
            dialog.Resources["AccentClr"] = accentClr;
        if (Resources.TryGetResource("SuccessColor", null, out var successClr))
            dialog.Resources["SuccessClr"] = successClr;
        if (Resources.TryGetResource("ButtonPrimary", null, out var buttonPri))
            dialog.Resources["ButtonPri"] = buttonPri;
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



    // builds all the content for the help dialog
    private List<Control> CreateHelpContentElements(Window dialog)
    {
        List<Control> elements = new List<Control>();

        elements.Add(CreateTitleBlock(dialog));

        elements.Add(CreateSectionHeader("Supported Operators"));
        foreach (var item in GetOperatorHelpItems())
            elements.Add(item);

        elements.Add(CreateSectionHeader("Parentheses"));
        elements.Add(CreateHelpItem("Grouping", "( )", "(5 + 3) * 2"));

        elements.Add(CreateSectionHeader("Number Types"));
        foreach (var item in GetNumberTypeHelpItems())
            elements.Add(item);

        elements.Add(CreateSectionHeader("Expression Rules"));
        foreach (var rule in GetExpressionRules())
            elements.Add(CreateInfoText(rule));

        elements.Add(CreateSectionHeader("Example Expressions"));
        foreach (var example in GetExampleExpressions())
            elements.Add(CreateExampleText(example));

        elements.Add(CreateCloseButton(dialog));

        return elements;
    }

    private TextBlock CreateTitleBlock(Window dialog)
    {
        var title = new TextBlock();
        title.Text = "Expression Syntax Guide";
        title.FontSize = 24;
        title.FontWeight = FontWeight.Bold;
        title.Foreground = dialog.Resources?["AccentClr"] as IBrush ?? Brushes.Blue;
        title.Margin = new Avalonia.Thickness(0, 0, 0, 10);
        return title;
    }

    // returns list of operator help items
    private List<Border> GetOperatorHelpItems()
    {
        List<Border> items = new List<Border>();
        items.Add(CreateHelpItem("Addition", "+", "5 + 3"));
        items.Add(CreateHelpItem("Subtraction", "−", "10 - 4"));
        items.Add(CreateHelpItem("Multiplication", "*", "6 * 7"));
        items.Add(CreateHelpItem("Division", "/", "15 / 3"));
        items.Add(CreateHelpItem("Remainder", "%", "17 % 5"));
        items.Add(CreateHelpItem("Power", "^", "2 ^ 8"));
        return items;
    }

    private List<Border> GetNumberTypeHelpItems()
    {
        List<Border> items = new List<Border>();
        items.Add(CreateHelpItem("Integers", "0-9", "42, 123, -7"));
        items.Add(CreateHelpItem("Floating Point", ".", "3.14, -2.25, 0.021"));
        return items;
    }


    private List<string> GetExpressionRules()
    {
        var rules = new List<string>();
        rules.Add("• BODMAS/BIDMAS order of operations applies");
        rules.Add("• Left associative evaluation");
        rules.Add("• Division by zero will produce an error");
        rules.Add("• Supports nested parentheses");
        return rules;
    }

    private List<string> GetExampleExpressions()
    {
        List<string> examples = new List<string>();
        examples.Add("(2 + 3) * 7 - 6 / 2");
        examples.Add("2 ^ 4 + 2 * (30 - 3)");
        examples.Add("-10 + 20 / 2");
        examples.Add("3.14 * 3.5 ^ 2");
        return examples;
    }



    private Button CreateCloseButton(Window dialog)
    {
        Button button = new Button();
        button.Content = "Close";
        button.Width = 120;
        button.Height = 40;
        button.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        button.Margin = new Avalonia.Thickness(0, 20, 0, 0);
        button.Background = dialog.Resources?["ButtonPri"] as IBrush ?? Brushes.Green;
        button.Foreground = new SolidColorBrush(Colors.White);
        button.CornerRadius = new Avalonia.CornerRadius(6);
        button.FontWeight = FontWeight.SemiBold;

        button.Click += (sender, e) => dialog.Close();
        return button;
    }

    // opens the plot window
    private void OpenPlotWindow()
    {
        PlotWindow plotWindow = new PlotWindow();
        plotWindow.SetTheme(_isDarkTheme);
        plotWindow.Show();
    }







    private Border CreateSectionHeader(string text)
    {
        TextBlock headerText = new TextBlock();
        headerText.Text = text;
        headerText.FontSize = 18;
        headerText.FontWeight = FontWeight.Bold;
        headerText.Foreground = Resources?["AccentColor"] as IBrush;

        Border border = new Border();
        border.Background = Resources?["SurfaceBackground"] as IBrush;
        border.BorderBrush = Resources?["BorderColor"] as IBrush;
        border.BorderThickness = new Avalonia.Thickness(0, 0, 0, 2);
        border.Padding = new Avalonia.Thickness(10, 8);
        border.Margin = new Avalonia.Thickness(0, 10, 0, 5);
        border.Child = headerText;
        return border;
    }





    private Border CreateHelpItem(string name, string symbol, string example)
    {
        Border border = new Border();
        border.Background = Resources?["SurfaceBackground"] as IBrush;
        border.BorderBrush = Resources?["BorderColor"] as IBrush;
        border.BorderThickness = new Avalonia.Thickness(1);
        border.CornerRadius = new Avalonia.CornerRadius(6);
        border.Padding = new Avalonia.Thickness(12, 10);
        border.Margin = new Avalonia.Thickness(0, 2);
        border.Child = CreateHelpItemContent(name, symbol, example);
        return border;
    }




    private StackPanel CreateHelpItemContent(string name, string symbol, string example)
    {
        StackPanel panel = new StackPanel();
        panel.Orientation = Avalonia.Layout.Orientation.Horizontal;
        panel.Spacing = 10;

        // name label
        TextBlock nameBlock = new TextBlock();
        nameBlock.Text = name + ":";
        nameBlock.Width = 140;
        nameBlock.FontSize = 14;
        nameBlock.Foreground = Resources?["TextPrimary"] as IBrush;
        nameBlock.FontWeight = FontWeight.SemiBold;
        panel.Children.Add(nameBlock);



        // symbol
        TextBlock symbolBlock = new TextBlock();
        symbolBlock.Text = symbol;
        symbolBlock.Width = 60;
        symbolBlock.FontSize = 16;
        symbolBlock.FontFamily = "Consolas, Monaco,monospace";
        symbolBlock.Foreground = Resources?["SuccessColor"] as IBrush;
        symbolBlock.FontWeight = FontWeight.Bold;
        panel.Children.Add(symbolBlock);

        // example
        TextBlock exampleBlock = new TextBlock();
        exampleBlock.Text = "Example: " + example;
        exampleBlock.FontSize = 13;
        exampleBlock.FontFamily = "Consolas,Monaco,monospace";
        exampleBlock.Foreground = Resources?["TextSecondary"] as IBrush;
        panel.Children.Add(exampleBlock);

        return panel;
    }

    private TextBlock CreateInfoText(string text)
    {
        TextBlock textBlock = new TextBlock();
        textBlock.Text = text;
        textBlock.FontSize = 14;
        textBlock.Foreground = Resources?["TextPrimary"] as IBrush;
        textBlock.Margin = new Avalonia.Thickness(10, 3);
        textBlock.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
        return textBlock;
    }



    private Border CreateExampleText(string example)
    {
        TextBlock exampleText = new TextBlock();
        exampleText.Text = example;
        exampleText.FontSize = 14;
        exampleText.FontFamily = "Consolas,Monaco,monospace";
        exampleText.Foreground = Resources?["SuccessColor"] as IBrush;

        Border border = new Border();
        border.Background = Resources?["SurfaceBackground"] as IBrush;
        border.BorderBrush = Resources?["BorderColor"] as IBrush;
        border.BorderThickness = new Avalonia.Thickness(1);
        border.CornerRadius = new Avalonia.CornerRadius(6);
        border.Padding = new Avalonia.Thickness(12, 10);
        border.Margin = new Avalonia.Thickness(0, 3);
        border.Child = exampleText;
        return border;
    }
}




