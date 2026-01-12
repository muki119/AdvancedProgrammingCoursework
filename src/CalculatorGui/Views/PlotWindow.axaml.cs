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
    // UI control references
    // (using nullable refs since controls might not be in view during initialization)
    private TextBox? _coeffsBox;
    private TextBox? _xMinBox;
    private TextBox? _xMaxBox;
    private TextBox? _dxBox;
    private Button? _plotButton;
    private Button? _clearButton;
    private Canvas? _plotCanvas;
    private TextBlock? _plotError;

    private bool _isDarkTheme = true;
    // TODO EXTRACT THEME HANDLING IF MULTIPLE WINDOWS NEED IT

    // constructor  sets up the plot window
    public PlotWindow()
    {
        InitializeComponent();

        // findcontrol same pattern as MainWindow runtime lookup for easiness
        _coeffsBox = this.FindControl<TextBox>("coeffsBox");
        _xMinBox = this.FindControl<TextBox>("xMinBox");
        _xMaxBox = this.FindControl<TextBox>("xMaxBox");
        _dxBox = this.FindControl<TextBox>("dxBox");
        _plotButton = this.FindControl<Button>("plotButton");
        _clearButton = this.FindControl<Button>("clearButton");
        _plotCanvas = this.FindControl<Canvas>("plotCanvas");
        _plotError = this.FindControl<TextBlock>("plotError");

        SetupEventHandlers();
        ApplyTheme(_isDarkTheme);
    }

    public void SetTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        ApplyTheme(isDark);
    }

    private void ApplyTheme(bool isDark)
    {
        // same theme system as main window centralised within app.axaml
        ThemeResourceHelper.Apply(Resources, isDark);
    }

    private void SetupEventHandlers()
    {
        // canvas or textBlock might not init perfectly on first load
        if (_plotButton != null)
            _plotButton.Click += OnPlotClicked;

        if (_clearButton != null)
            _clearButton.Click += OnClearClicked;
    }

    private void OnPlotClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // checks all controls exist before proceeding 
        // checks against errors with XAML or incomplete inits)
        if (_plotCanvas == null || _plotError == null || _coeffsBox == null ||
            _xMinBox == null || _xMaxBox == null || _dxBox == null)
        {
            return;
        }

        // clear previous error state
        _plotError.Text = "";
        string input = (_coeffsBox.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            _plotError.Text = "Please enter an expression or coefficients.";
            return;
        }

        // parse range inputs
        double xMin, xMax, dx;
        if (!TryParseDouble(_xMinBox.Text, out xMin) ||
            !TryParseDouble(_xMaxBox.Text, out xMax) ||
            !TryParseDouble(_dxBox.Text, out dx))
        {
            _plotError.Text = "Invalid range or Δx.";
            return;
        }

        // prevent errors with very small dx values
        // clamp to avoid UI freezing when sampling lots of points
        if (dx < 0.0001) dx = 0.0001;

        if (dx <= 0 || xMax <= xMin)
        {
            _plotError.Text = "Require xMax > xMin and Δx > 0.";
            return;
        }

        List<(double x, double y)> samples;
        string errorMsg;

        // try expression first (most common case), then fall back to polynomial coefficients
        if (!TryPlotExpression(input, xMin, xMax, dx, out samples, out errorMsg))
        {
            List<double> coeffs;
            if (TryParseCoefficients(input, out coeffs))
            {
                samples = SamplePolynomial(coeffs, xMin, xMax, dx);
            }
            else
            {
                string msg = "Invalid input. ";
                if (!string.IsNullOrEmpty(errorMsg))
                    msg = msg + errorMsg + " ";
                msg = msg + "Use expression (e.g., x^2 + 3*x + 1) or coefficients (1, 3, 1).";
                _plotError.Text = msg;
                return;
            }
        }

        // needs minimum 2 points to draw a meaningful curve
        if (samples.Count < 2)
        {
            _plotError.Text = "Not enough valid points to plot. Check your expression.";
            return;
        }

        DrawPlot(_plotCanvas, samples, xMin, xMax);
    }

    private void OnClearClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_plotCanvas != null)
            _plotCanvas.Children.Clear();
        if (_plotError != null)
            _plotError.Text = "";
    }








    // tries to parse a string to double
    private static bool TryParseDouble(string? s, out double value)
    {
        return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryPlotExpression(string expr, double xMin, double xMax, double dx,
        out List<(double x, double y)> samples, out string errorMsg)
    {
        samples = new List<(double x, double y)>();
        errorMsg = string.Empty;

        if (string.IsNullOrWhiteSpace(expr) || expr.IndexOf('x', StringComparison.OrdinalIgnoreCase) < 0) // no 'x' found then not an expression
            return false;

        try
        {
            Interpreter.clearVariables();              // reset variable table
            var tokens = Interpreter.lexer(expr);       // tokenize expression once

            // sample expression at regular intervals from xMin to xMax
            for (double x = xMin; x <= xMax + dx * 0.5; x += dx) // small buffer so we hit the end of the interval
            {
                Interpreter.setVariable("x", Interpreter.toNumberFloat(x)); // set x in symbol table
                var y = Interpreter.toPrimativeFloat(Interpreter.parseNeval(tokens).Item2); // evaluate y = f(x)
                if (double.IsNaN(y) || double.IsInfinity(y)) continue;      // skip invalid points
                samples.Add((x, y));
            }

            // ensure we have some valid samples
            if (samples.Count == 0)
            {
                errorMsg = "All points resulted in invalid values (NaN/Infinity).";
                return false;
            }

            return true;
        }
        catch (Exception ex) // catch any errors during parsing/evaluation then throw 
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

    // uses Horner's method
    private static List<(double x, double y)> SamplePolynomial(List<double> coeffs, double xMin, double xMax, double dx)
    {
        List<(double x, double y)> points = new List<(double x, double y)>();

        for (double x = xMin; x <= xMax + dx * 0.5; x += dx) // include right edge 
        {

            double y = 0;
            for (int i = 0; i < coeffs.Count; i++)
            {
                y = y * x + coeffs[i];
            }
            points.Add((x, y));
        }




        return points;
    }

    // main plotting function draws grid axes labels and curve
    private void DrawPlot(Canvas canvas, List<(double x, double y)> points, double xMin, double xMax)
    {
        canvas.Children.Clear();

        // get canvas dimensions with fallback defaults
        double w = canvas.Bounds.Width;
        double h = canvas.Bounds.Height;
        if (w <= 0) w = 800;
        if (h <= 0) h = 500;

        double xRange = xMax - xMin;

        // find min and max y values
        double yMin = double.MaxValue;
        double yMax = double.MinValue;
        foreach (var point in points)
        {
            if (point.y < yMin) yMin = point.y;
            if (point.y > yMax) yMax = point.y;
        }


        double yPad = (yMax - yMin) * 0.1;
        yMin = yMin - yPad;
        yMax = yMax + yPad;
        double yRange = yMax - yMin;

        // calculate scaling
        double scale = Math.Min(w / xRange, h / yRange);
        double xOffset = (w - xRange * scale) / 2;
        double yOffset = (h - yRange * scale) / 2;

        // draw all the components
        DrawGrid(canvas, w, h, xMin, xMax, yMin, yMax, xOffset, yOffset, scale);
        DrawAxes(canvas, w, h, xMin, xMax, yMin, yMax, xOffset, yOffset, scale);
        DrawAxisLabels(canvas, w, h, xMin, xMax, yMin, yMax, xOffset, yOffset, scale);

        // draw the curve
        Polyline poly = new Polyline();
        poly.Stroke = TryGetBrush("SuccessColor") ?? Brushes.Lime;
        poly.StrokeThickness = 2.5;
        poly.Points = new Avalonia.Collections.AvaloniaList<Point>();

        foreach (var point in points)
        {
            double px = xOffset + (point.x - xMin) * scale;
            double py = h - yOffset - (point.y - yMin) * scale;
            poly.Points.Add(new Point(px, py));
        }

        canvas.Children.Add(poly);
    }

    // draws the grid lines
    private void DrawGrid(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax,
        double xOffset, double yOffset, double scale)
    {
        IBrush gridBrush = TryGetBrush("BorderColor") ?? Brushes.Gray; // grid line color fallback to gray

        double xStep = CalculateGridStep(xMax - xMin); // calculate grid spacing between lines for x axes 
        for (double x = Math.Ceiling(xMin / xStep) * xStep; x <= xMax; x += xStep)
        {
            double px = xOffset + (x - xMin) * scale; // pixel of x pos
            Line line = new Line();
            line.StartPoint = new Point(px, 0);
            line.EndPoint = new Point(px, h);
            line.Stroke = gridBrush;
            line.StrokeThickness = 0.5;
            line.Opacity = 0.3;
            canvas.Children.Add(line);
        }

        double yStep = CalculateGridStep(yMax - yMin); // calculate grid spacing between lines  for horizontal lines
        for (double y = Math.Ceiling(yMin / yStep) * yStep; y <= yMax; y += yStep) // 
        {
            double py = h - yOffset - (y - yMin) * scale;
            Line line = new Line();
            line.StartPoint = new Point(0, py);
            line.EndPoint = new Point(w, py);
            line.Stroke = gridBrush;
            line.StrokeThickness = 0.5;
            line.Opacity = 0.3;
            canvas.Children.Add(line);
        }
    }

    // draws the x and y axes
    private void DrawAxes(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax,
        double xOffset, double yOffset, double scale)
    {
        IBrush axisBrush = TryGetBrush("TextPrimary") ?? Brushes.White; // axis lines color falls to white




        // draw x axis if 0 is in range
        if (0 >= yMin && 0 <= yMax)
        {
            double y0 = h - yOffset - (0 - yMin) * scale;
            Line xAxis = new Line();
            xAxis.StartPoint = new Point(0, y0);
            xAxis.EndPoint = new Point(w, y0);
            xAxis.Stroke = axisBrush;
            xAxis.StrokeThickness = 1.5;
            canvas.Children.Add(xAxis);
        }





        // draw y axis if 0 is in range
        if (0 >= xMin && 0 <= xMax)
        {
            double x0 = xOffset + (0 - xMin) * scale;
            Line yAxis = new Line();
            yAxis.StartPoint = new Point(x0, 0);
            yAxis.EndPoint = new Point(x0, h);
            yAxis.Stroke = axisBrush;
            yAxis.StrokeThickness = 1.5;
            canvas.Children.Add(yAxis);
        }
    }

    // calculates grid line spacing
    private static double CalculateGridStep(double range)
    {
        if (range <= 0.0)
            return 1.0; // avoid incorrect values

        // rough step to snap to 1 / 2 / 5 multiples of a power of ten
        var rough = range / 8.0;
        var pow = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var scaled = rough / pow;

        // choose unit based on scaled value
        double unit;
        if (scaled < 2) unit = 1;
        else if (scaled < 5) unit = 2;
        else unit = 5;

        return unit * pow;
    }

    // draws the axis labels basically the same as grid lines but with text
    private void DrawAxisLabels(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax,
        double xOffset, double yOffset, double scale)
    {
        IBrush textBrush = TryGetBrush("TextSecondary") ?? Brushes.Gray; // text color fallback to gray

        // x axis labels 
        double xStep = CalculateGridStep(xMax - xMin);
        for (double x = Math.Ceiling(xMin / xStep) * xStep; x <= xMax; x += xStep)
        {
            if (Math.Abs(x) < xStep * 0.01) continue;  // skip zero

            TextBlock label = new TextBlock();
            label.Text = x.ToString("0.###");
            label.FontSize = 11;
            label.Foreground = textBrush;



            double px = xOffset + (x - xMin) * scale;
            Canvas.SetLeft(label, px - 15);
            Canvas.SetTop(label, h - 20);
            canvas.Children.Add(label);
        }

        // y axis labls
        double yStep = CalculateGridStep(yMax - yMin);
        for (double y = Math.Ceiling(yMin / yStep) * yStep; y <= yMax; y += yStep)
        {
            if (Math.Abs(y) < yStep * 0.01) continue;  // skip zero

            TextBlock label = new TextBlock();
            label.Text = y.ToString("0.###");
            label.FontSize = 11;
            label.Foreground = textBrush;

            double py = h - yOffset - (y - yMin) * scale;
            Canvas.SetLeft(label, 5);
            Canvas.SetTop(label, py - 8);
            canvas.Children.Add(label);
        }

        // origin label
        if (0 >= xMin && 0 <= xMax && 0 >= yMin && 0 <= yMax)
        {
            TextBlock originLabel = new TextBlock();
            originLabel.Text = "0";
            originLabel.FontSize = 11;
            originLabel.Foreground = textBrush;
            originLabel.FontWeight = FontWeight.Bold;

            double x0 = xOffset + (0 - xMin) * scale;
            double y0 = h - yOffset - (0 - yMin) * scale;
            Canvas.SetLeft(originLabel, x0 + 5);
            Canvas.SetTop(originLabel, y0 + 5);
            canvas.Children.Add(originLabel);
        }
    }

    // helper to get a brush from resources
    private IBrush? TryGetBrush(string key)
    {
        object? result;
        if (Resources.TryGetResource(key, null, out result))
        {
            return result as IBrush;
        }
        return null;
    }
}




