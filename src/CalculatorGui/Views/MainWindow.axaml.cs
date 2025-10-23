using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using InterpreterLib;

namespace CalculatorGui.Views;

public partial class MainWindow : Window
{
    // UI elements - using underscore prefix to avoid conflict with generated fields
    private TextBox? _inputBox;
    private TextBox? _outputBox;
    private TextBlock? _errorBox;
    private Button? _evaluateButton;
    private Button? _clearButton;

    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // Find UI elements
        _inputBox = this.FindControl<TextBox>("inputBox");
        _outputBox = this.FindControl<TextBox>("outputBox");
        _errorBox = this.FindControl<TextBlock>("errorBox");
        _evaluateButton = this.FindControl<Button>("evaluateButton");
        _clearButton = this.FindControl<Button>("clearButton");

        // Event handlers
        if (_evaluateButton != null)
        {
            _evaluateButton.Click += EvaluateButton_Click;
        }

        if (_clearButton != null)
        {
            _clearButton.Click += ClearButton_Click;
        }
    }

    private void EvaluateButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_inputBox != null && _outputBox != null && _errorBox != null)
            {
                var tokens = Interpreter.lexer(_inputBox.Text ?? "");
                var (_, result) = Interpreter.parseNeval(tokens);
                _outputBox.Text = result.ToString();
                _outputBox.Focusable = true;
                _outputBox.IsHitTestVisible = true;
                _errorBox.Text = "";
            }
        }
        catch (Exception ex)
        {
            if (_outputBox != null && _errorBox != null)
            {
                _outputBox.Text = "";
                _outputBox.Focusable = false;
                _outputBox.IsHitTestVisible = false;
                _errorBox.Text = ex.Message;
            }
        }
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_inputBox != null)
        {
            _inputBox.Text = "";
        }

        if (_outputBox != null)
        {
            _outputBox.Text = "Results here...";
            _outputBox.Focusable = false;
            _outputBox.IsHitTestVisible = false;
        }

        if (_errorBox != null)
        {
            _errorBox.Text = "";
        }
    }
}