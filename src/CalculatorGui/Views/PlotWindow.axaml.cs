using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using InterpreterLib;

namespace CalculatorGui.Views;

public partial class PlotWindow : Window
{
    private record PlotControls(
        TextBox? CoeffsBox,
        TextBox? XMinBox,
        TextBox? XMaxBox,
        TextBox? DxBox,
        Button? PlotButton,
        Button? ClearButton,
        Canvas? PlotCanvas,
        TextBlock? PlotError
    );

    private readonly PlotControls _controls;
    private bool _isDarkTheme = true;

    public PlotWindow()
    {
        InitializeComponent();
        _controls = InitializeControls();
        SetupEventHandlers();
        ApplyTheme(_isDarkTheme);
    }

    public void SetTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        ApplyTheme(isDark);
    }

    private PlotControls InitializeControls() =>
        new(
            CoeffsBox: this.FindControl<TextBox>("coeffsBox"),
            XMinBox: this.FindControl<TextBox>("xMinBox"),
            XMaxBox: this.FindControl<TextBox>("xMaxBox"),
            DxBox: this.FindControl<TextBox>("dxBox"),
            PlotButton: this.FindControl<Button>("plotButton"),
            ClearButton: this.FindControl<Button>("clearButton"),
            PlotCanvas: this.FindControl<Canvas>("plotCanvas"),
            PlotError: this.FindControl<TextBlock>("plotError")
        );

    private void SetupEventHandlers()
    {
        if (_controls.PlotButton != null)
            _controls.PlotButton.Click += (_, _) => HandlePlot();

        if (_controls.ClearButton != null)
            _controls.ClearButton.Click += (_, _) => HandleClear();
    }

    private void ApplyTheme(bool isDark) => ThemeResourceHelper.Apply(Resources, isDark);

    private void HandlePlot()
    {
        if (_controls is not
            {
                PlotCanvas: { } canvas,
                PlotError: { } error,
                CoeffsBox: { } coeffsBox,
                XMinBox: { } xMinBox,
                XMaxBox: { } xMaxBox,
                DxBox: { } dxBox
            })
            return;

        error.Text = string.Empty;
        var input = (coeffsBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            error.Text = "Please enter an expression or coefficients.";
            return;
        }

        if (!TryParseDouble(xMinBox.Text, out var xMin) ||
            !TryParseDouble(xMaxBox.Text, out var xMax) ||
            !TryParseDouble(dxBox.Text, out var dx))
        {
            error.Text = "Invalid range or Δx.";
            return;
        }

        if (dx <= 0 || xMax <= xMin)
        {
            error.Text = "Require xMax > xMin and Δx > 0.";
            return;
        }

        List<(double x, double y)> samples;
        if (!TryPlotExpression(input, xMin, xMax, dx, out samples, out var errorMsg))
        {
            if (TryParseCoefficients(input, out var coeffs))
            {
                samples = SamplePolynomial(coeffs, xMin, xMax, dx).ToList();
            }
            else
            {
                var msg = "Invalid input. ";
                if (!string.IsNullOrEmpty(errorMsg))
                    msg += errorMsg + " ";
                msg += "Use expression (e.g., x^2 + 3*x + 1) or coefficients (1, 3, 1).";
                error.Text = msg;
                return;
            }
        }

        if (samples.Count < 2)
        {
            error.Text = "Not enough valid points to plot. Check your expression.";
            return;
        }

        DrawPlot(canvas, samples, xMin, xMax);
    }

    private void HandleClear()
    {
        _controls.PlotCanvas?.Children.Clear();
        if (_controls.PlotError is { } error)
            error.Text = string.Empty;
    }

    private static bool TryParseDouble(string? s, out double value) =>
        double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);

    private static bool TryPlotExpression(string expr, double xMin, double xMax, double dx,
        out List<(double x, double y)> samples, out string errorMsg)
    {
        samples = new List<(double x, double y)>();
        errorMsg = string.Empty;

        if (string.IsNullOrWhiteSpace(expr) || expr.IndexOf('x', StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        try
        {
            Interpreter.clearVariables();
            var tokens = Interpreter.lexer(expr);

            for (double x = xMin; x <= xMax + 1e-9; x += dx)
            {
                Interpreter.setVariable("x", Interpreter.toNumberFloat(x));
                var y = Interpreter.toPrimativeFloat(Interpreter.parseNeval(tokens).Item2);
                if (double.IsNaN(y) || double.IsInfinity(y)) continue;
                samples.Add((x, y));
            }

            if (samples.Count == 0)
            {
                errorMsg = "All points resulted in invalid values (NaN/Infinity).";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMsg = $"Expression error: {ex.Message}";
            samples.Clear();
            return false;
        }
    }

    private static bool TryParseCoefficients(string input, out List<double> coeffs)
    {
        coeffs = new List<double>();
        foreach (var p in input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!double.TryParse(p.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
                return false;
            coeffs.Add(value);
        }
        return coeffs.Count > 0;
    }

    private static IEnumerable<(double x, double y)> SamplePolynomial(IReadOnlyList<double> coeffs, double xMin, double xMax, double dx)
    {
        for (double x = xMin; x <= xMax + 1e-9; x += dx)
        {
            double y = 0;
            foreach (var a in coeffs)
                y = y * x + a; // Horner's method
            yield return (x, y);
        }
    }

    private void DrawPlot(Canvas canvas, List<(double x, double y)> points, double xMin, double xMax)
    {
        canvas.Children.Clear();

        var w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 800;
        var h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 500;
        var xRange = xMax - xMin;

        var yMin = points.Min(p => p.y);
        var yMax = points.Max(p => p.y);
        var yPad = (yMax - yMin) * 0.1;
        yMin -= yPad;
        yMax += yPad;
        var yRange = yMax - yMin;

        var scale = Math.Min(w / xRange, h / yRange);
        var xOffset = (w - xRange * scale) / 2;
        var yOffset = (h - yRange * scale) / 2;

        double MapX(double x) => xOffset + (x - xMin) * scale;
        double MapY(double y) => h - yOffset - (y - yMin) * scale;

        DrawGrid(canvas, w, h, xMin, xMax, yMin, yMax, MapX, MapY);
        DrawAxes(canvas, w, h, xMin, xMax, yMin, yMax, MapX, MapY);
        DrawAxisLabels(canvas, w, h, xMin, xMax, yMin, yMax, MapX, MapY);

        var poly = new Polyline
        {
            Stroke = TryGetBrush("SuccessColor") ?? Brushes.Lime,
            StrokeThickness = 2.5,
            Points = new Avalonia.Collections.AvaloniaList<Point>()
        };

        foreach (var (x, y) in points)
            poly.Points.Add(new Point(MapX(x), MapY(y)));

        canvas.Children.Add(poly);
    }

    private void DrawGrid(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax,
        Func<double, double> mapX, Func<double, double> mapY)
    {
        var gridBrush = TryGetBrush("BorderColor") ?? Brushes.Gray;
        const double gridThickness = 0.5;

        var xStep = CalculateGridStep(xMax - xMin);
        for (double x = Math.Ceiling(xMin / xStep) * xStep; x <= xMax; x += xStep)
        {
            var px = mapX(x);
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(px, 0),
                EndPoint = new Point(px, h),
                Stroke = gridBrush,
                StrokeThickness = gridThickness,
                Opacity = 0.3
            });
        }

        var yStep = CalculateGridStep(yMax - yMin);
        for (double y = Math.Ceiling(yMin / yStep) * yStep; y <= yMax; y += yStep)
        {
            var py = mapY(y);
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(0, py),
                EndPoint = new Point(w, py),
                Stroke = gridBrush,
                StrokeThickness = gridThickness,
                Opacity = 0.3
            });
        }
    }

    private void DrawAxes(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax,
        Func<double, double> mapX, Func<double, double> mapY)
    {
        var axisBrush = TryGetBrush("TextPrimary") ?? Brushes.White;
        const double axisThickness = 1.5;

        if (0 >= yMin && 0 <= yMax)
        {
            var y0 = mapY(0);
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(0, y0),
                EndPoint = new Point(w, y0),
                Stroke = axisBrush,
                StrokeThickness = axisThickness
            });
        }

        if (0 >= xMin && 0 <= xMax)
        {
            var x0 = mapX(0);
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(x0, 0),
                EndPoint = new Point(x0, h),
                Stroke = axisBrush,
                StrokeThickness = axisThickness
            });
        }
    }

    private static double CalculateGridStep(double range)
    {
        range = Math.Max(range, 1e-6);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(range)));
        var normalized = range / magnitude;

        if (normalized <= 1.5) return magnitude * 0.2;
        if (normalized <= 3) return magnitude * 0.5;
        if (normalized <= 7) return magnitude;
        return magnitude * 2;
    }

    private void DrawAxisLabels(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax,
        Func<double, double> mapX, Func<double, double> mapY)
    {
        var textBrush = TryGetBrush("TextSecondary") ?? Brushes.Gray;
        const int fontSize = 11;

        var xStep = CalculateGridStep(xMax - xMin);
        for (double x = Math.Ceiling(xMin / xStep) * xStep; x <= xMax; x += xStep)
        {
            if (Math.Abs(x) < xStep * 0.01) continue;

            var label = new TextBlock
            {
                Text = x.ToString("F1"),
                FontSize = fontSize,
                Foreground = textBrush
            };

            Canvas.SetLeft(label, mapX(x) - 15);
            Canvas.SetTop(label, h - 20);
            canvas.Children.Add(label);
        }

        var yStep = CalculateGridStep(yMax - yMin);
        for (double y = Math.Ceiling(yMin / yStep) * yStep; y <= yMax; y += yStep)
        {
            if (Math.Abs(y) < yStep * 0.01) continue;

            var label = new TextBlock
            {
                Text = y.ToString("F1"),
                FontSize = fontSize,
                Foreground = textBrush
            };

            Canvas.SetLeft(label, 5);
            Canvas.SetTop(label, mapY(y) - 8);
            canvas.Children.Add(label);
        }

        if (0 >= xMin && 0 <= xMax && 0 >= yMin && 0 <= yMax)
        {
            var originLabel = new TextBlock
            {
                Text = "0",
                FontSize = fontSize,
                Foreground = textBrush,
                FontWeight = FontWeight.Bold
            };
            Canvas.SetLeft(originLabel, mapX(0) + 5);
            Canvas.SetTop(originLabel, mapY(0) + 5);
            canvas.Children.Add(originLabel);
        }
    }

    private IBrush? TryGetBrush(string key) =>
        Resources.TryGetResource(key, null, out var r) ? r as IBrush : null;
}
