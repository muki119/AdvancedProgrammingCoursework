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
    private record PlotControls( // encapsulates all plot control references from AXAML markup
        TextBox? CoeffsBox,
        TextBox? XMinBox,
        TextBox? XMaxBox,
        TextBox? DxBox,
        Button? PlotButton,
        Button? ClearButton,
        Canvas? PlotCanvas,
        TextBlock? PlotError
    );

    private readonly PlotControls _controls; // stores references to plot UI controls
    private bool _isDarkTheme = true; // tracks current theme state. defaults to dark mode

    public PlotWindow() // initializes plot window and its components. sets up controls. event handlers and theme
    {
        InitializeComponent();
        _controls = InitializeControls();
        SetupEventHandlers();
        ApplyTheme(_isDarkTheme);
    }

    public void SetTheme(bool isDark) // sets theme for plot window. called from main window to sync theme
    {
        _isDarkTheme = isDark;
        ApplyTheme(isDark);
    }

    private PlotControls InitializeControls() => // retrieves plot control references from AXAML markup using FindControl
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

    private void SetupEventHandlers() // attaches event handlers to plot button and clear button
    {
        if (_controls.PlotButton != null)
            _controls.PlotButton.Click += (_, _) => HandlePlot();

        if (_controls.ClearButton != null)
            _controls.ClearButton.Click += (_, _) => HandleClear();
    }

    private void ApplyTheme(bool isDark) // applies theme to window by updating resource dictionary with light or dark theme resources
    {
        var prefix = isDark ? "Dark" : "Light"; // determine prefix based on theme mode
        var mappings = new[] // maps resource names to their suffixes for theme switching
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
            ("ButtonSecondaryHover", $"{prefix}ButtonSecondaryHover")
        };

        foreach (var (target, source) in mappings) // iterate through mappings and update resources
            if (Resources.TryGetResource(source, null, out var r))
                Resources[target] = r;
    }

    private void HandlePlot() // handles plot button click. validates input. parses expression or coefficients. samples points and draws plot
    {
        if (_controls.PlotCanvas == null || _controls.PlotError == null || // check if all required controls are available
            _controls.CoeffsBox == null || _controls.XMinBox == null ||
            _controls.XMaxBox == null || _controls.DxBox == null)
            return;

        _controls.PlotError.Text = string.Empty; // clear previous error message
        var input = (_controls.CoeffsBox.Text ?? string.Empty).Trim(); // get input text and trim whitespace

        if (string.IsNullOrWhiteSpace(input)) // validate input is not empty
        {
            _controls.PlotError.Text = "Please enter an expression or coefficients.";
            return;
        }

        if (!TryParseDouble(_controls.XMinBox.Text, out var xMin) || // parse range and step parameters
            !TryParseDouble(_controls.XMaxBox.Text, out var xMax) ||
            !TryParseDouble(_controls.DxBox.Text, out var dx))
        {
            _controls.PlotError.Text = "Invalid range or Δx.";
            return;
        }

        if (dx <= 0 || xMax <= xMin) // validate range and step are valid
        {
            _controls.PlotError.Text = "Require xMax > xMin and Δx > 0.";
            return;
        }

        List<(double x, double y)> samples;

        if (TryPlotExpression(input, xMin, xMax, dx, out samples, out var errorMsg)) // try parsing as expression first. e.g.. "x^2 + 3*x + 1"
        {
            // successfully plotted as expression
        }
        else if (TryParseCoefficients(input, out var coeffs)) // fall back to coefficient parsing. e.g.. "1. 3. 1"
        {
            samples = SamplePolynomial(coeffs, xMin, xMax, dx).ToList();
        }
        else
        {
            var msg = "Invalid input. "; // build error message with details
            if (!string.IsNullOrEmpty(errorMsg))
                msg += errorMsg + " ";
            msg += "Use expression (e.g., x^2 + 3*x + 1) or coefficients (1, 3, 1).";
            _controls.PlotError.Text = msg;
            return;
        }

        if (samples.Count < 2) // check if enough points were sampled for plotting
        {
            _controls.PlotError.Text = "Not enough valid points to plot. Check your expression.";
            return;
        }

        DrawPlot(_controls.PlotCanvas, samples, xMin, xMax); // draw the plot on canvas
    }

    private void HandleClear() // handles clear button click. clears canvas and error message
    {
        if (_controls.PlotCanvas != null)
            _controls.PlotCanvas.Children.Clear(); // remove all children from canvas

        if (_controls.PlotError != null)
            _controls.PlotError.Text = string.Empty; // clear error text
    }

    private static bool TryParseDouble(string? s, out double value) => // tries to parse string to double using invariant culture. returns true if successful
        double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);

    private static bool TryPlotExpression(string expr, double xMin, double xMax, double dx, // tries to plot expression using F# interpreter. e.g.. "x^2 + 3*x + 1". lex and parse once. evaluate multiple times (loop.invariant code motion optimization)
        out List<(double x, double y)> samples, out string errorMsg)
    {
        samples = new List<(double x, double y)>(); // initialize empty list for samples
        errorMsg = string.Empty; // initialize empty error message

        if (string.IsNullOrWhiteSpace(expr)) // check if expression is empty
            return false;

        if (!expr.ToLower().Contains('x')) // check if expression contains 'x' variable
            return false;

        try
        {
            Interpreter.clearVariables(); // clear any previous variable bindings before plotting
            var tokens = Interpreter.lexer(expr); // lex and parse expression once outside the loop. common sub.expression elimination optimization

            for (double x = xMin; x <= xMax + 1e-9; x += dx) // sample the expression by evaluating the PARSED tokens at different x values. this avoids re.lexing and re.parsing on every iteration
            {
                Interpreter.setVariable("x", x); // bind the current x value to the interpreter's variable context
                var result = Interpreter.parseNeval(tokens);
                var y = result.Item2;

                if (double.IsNaN(y) || double.IsInfinity(y)) // check for invalid results
                    continue; // skip invalid points. eg division by zero

                samples.Add((x, y)); // add valid point to samples list
            }

            if (samples.Count == 0) // check if any valid points were sampled
            {
                errorMsg = "All points resulted in invalid values (NaN/Infinity).";
                return false;
            }

            return samples.Count > 0;
        }
        catch (Exception ex)
        {
            errorMsg = $"Expression error: {ex.Message}"; // capture exception message for display
            samples.Clear();
            return false;
        }
    }

    private static bool TryParseCoefficients(string input, out List<double> coeffs) // tries to parse comma.separated coefficients. e.g.. "1. 3. 1" for x^2 + 3x + 1
    {
        coeffs = new List<double>(); // initialize empty list for coefficients
        var parts = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries); // split input by comma or semicolon
        foreach (var p in parts) // iterate through each part and try to parse as double
        {
            if (!double.TryParse(p.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                return false; // return false if parsing fails
            coeffs.Add(d); // add parsed coefficient to list
        }
        return coeffs.Count > 0; // return true if at least one coefficient was parsed
    }

    private static IEnumerable<(double x, double y)> SamplePolynomial(IReadOnlyList<double> coeffs, double xMin, double xMax, double dx) // samples polynomial using Horner's method. reduces n multiplications to n.1. example: x^2 + 3x + 1 = (1*x + 3)*x + 1 (2 muls instead of 3)
    {
        for (double x = xMin; x <= xMax + 1e-9; x += dx) // iterate through x range with step dx
        {
            double y = 0;
            foreach (var a in coeffs) // horners method: O(n) instead of O(n²) for naive evaluation
                y = y * x + a;
            yield return (x, y); // return sampled point
        }
    }

    private void DrawPlot(Canvas canvas, List<(double x, double y)> points, double xMin, double xMax) // draws plot on canvas. calculates scaling. draws grid. axes. labels and polynomial curve
    {
        canvas.Children.Clear(); // clear any existing children from canvas

        var w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 800; // pre.compute canvas dimensions. common sub.expression elimination
        var h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 500;

        var xRange = xMax - xMin; // pre.compute x range. loop.invariant code motion

        var yMin = points.Min(p => p.y); // compute y range from data
        var yMax = points.Max(p => p.y);
        var yRange = yMax - yMin;



        var yPad = yRange * 0.1; // apply padding. constant folding
        yMin -= yPad;
        yMax += yPad;
        yRange = yMax - yMin;

        // use uniform scaling. maintain 1:1 aspect ratio so slopes are visually accurate. 45 degree line has slope 1
        var scale = Math.Min(w / xRange, h / yRange); // choose smaller scale to fit both x and y ranges

        var xScale = scale; // uniform scale for x axis
        var yScale = scale; // uniform scale for y axis. same as x for 1:1 aspect ratio

        // center the plot if one dimension has extra space
        var xOffset = (w - xRange * xScale) / 2; // horizontal centering offset
        var yOffset = (h - yRange * yScale) / 2; // vertical centering offset

        double MapX(double x) => xOffset + (x - xMin) * xScale; // optimized mapping functions with centering
        double MapY(double y) => h - yOffset - (y - yMin) * yScale;

        DrawGrid(canvas, w, h, xMin, xMax, yMin, yMax, MapX, MapY); // draw grid lines

        DrawAxes(canvas, w, h, xMin, xMax, yMin, yMax, MapX, MapY); // draw coordinate axes

        DrawAxisLabels(canvas, w, h, xMin, xMax, yMin, yMax, MapX, MapY); // draw axis labels

        var poly = new Polyline // create polynomial curve polyline
        {
            Stroke = TryGetBrush("SuccessColor") ?? Brushes.Lime,
            StrokeThickness = 2.5,
            Points = new Avalonia.Collections.AvaloniaList<Point>()
        };

        foreach (var (x, y) in points) // optimized point addition. avoid repeated property lookups
            poly.Points.Add(new Point(MapX(x), MapY(y)));

        canvas.Children.Add(poly); // add polyline to canvas
    }

    private void DrawGrid(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax, // draws grid lines on canvas. both vertical and horizontal
        Func<double, double> mapX, Func<double, double> mapY)
    {
        var gridBrush = TryGetBrush("BorderColor") ?? Brushes.Gray; // get grid brush from theme
        var gridThickness = 0.5;

        var xStep = CalculateGridStep(xMax - xMin); // calculate grid step for vertical lines
        for (double x = Math.Ceiling(xMin / xStep) * xStep; x <= xMax; x += xStep) // draw vertical grid lines
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

        var yStep = CalculateGridStep(yMax - yMin); // calculate grid step for horizontal lines
        for (double y = Math.Ceiling(yMin / yStep) * yStep; y <= yMax; y += yStep) // draw horizontal grid lines
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

    private void DrawAxes(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax, // draws coordinate axes on canvas. x.axis at y=0 and y.axis at x=0 if within range
        Func<double, double> mapX, Func<double, double> mapY)
    {
        var axisBrush = TryGetBrush("TextPrimary") ?? Brushes.White; // get axis brush from theme
        var axisThickness = 1.5;

        if (0 >= yMin && 0 <= yMax) // draw x.axis at y=0 if within range
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

        if (0 >= xMin && 0 <= xMax) // draw y.axis at x=0 if within range
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

    private static double CalculateGridStep(double range) // calculates appropriate grid step based on range. returns nice round numbers
    {
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(range))); // calculate order of magnitude
        var normalized = range / magnitude; // normalize range to 1.10

        if (normalized <= 1.5) return magnitude * 0.2; // return appropriate step based on normalized value
        if (normalized <= 3) return magnitude * 0.5;
        if (normalized <= 7) return magnitude;
        return magnitude * 2;
    }

    private void DrawAxisLabels(Canvas canvas, double w, double h, double xMin, double xMax, double yMin, double yMax, // draws axis labels on canvas. x.axis labels at bottom. y.axis labels at left. origin label at intersection
        Func<double, double> mapX, Func<double, double> mapY)
    {
        var textBrush = TryGetBrush("TextSecondary") ?? Brushes.Gray; // get text brush from theme
        var fontSize = 11;

        var xStep = CalculateGridStep(xMax - xMin); // calculate step for x.axis labels
        for (double x = Math.Ceiling(xMin / xStep) * xStep; x <= xMax; x += xStep) // draw x.axis labels
        {
            if (Math.Abs(x) < xStep * 0.01) continue; // skip origin label

            var px = mapX(x);
            var label = new TextBlock
            {
                Text = x.ToString("F1"),
                FontSize = fontSize,
                Foreground = textBrush
            };

            Canvas.SetLeft(label, px - 15); // position label below x.axis
            Canvas.SetTop(label, h - 20);
            canvas.Children.Add(label);
        }

        var yStep = CalculateGridStep(yMax - yMin); // calculate step for y.axis labels
        for (double y = Math.Ceiling(yMin / yStep) * yStep; y <= yMax; y += yStep) // draw y.axis labels
        {
            if (Math.Abs(y) < yStep * 0.01) continue; // skip origin label

            var py = mapY(y);
            var label = new TextBlock
            {
                Text = y.ToString("F1"),
                FontSize = fontSize,
                Foreground = textBrush
            };

            Canvas.SetLeft(label, 5); // position label to left of y.axis
            Canvas.SetTop(label, py - 8);
            canvas.Children.Add(label);
        }

        if (0 >= xMin && 0 <= xMax && 0 >= yMin && 0 <= yMax) // draw origin label if origin is visible
        {
            var originLabel = new TextBlock
            {
                Text = "0",
                FontSize = fontSize,
                Foreground = textBrush,
                FontWeight = FontWeight.Bold
            };
            Canvas.SetLeft(originLabel, mapX(0) + 5); // position label at origin
            Canvas.SetTop(originLabel, mapY(0) + 5);
            canvas.Children.Add(originLabel);
        }
    }

    private IBrush? TryGetBrush(string key) => // tries to get brush from resource dictionary. returns null if not found
        Resources.TryGetResource(key, null, out var r) ? r as IBrush : null;
}
