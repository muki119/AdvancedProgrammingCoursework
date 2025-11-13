# Plotting Feature Test Guide

## ✅ Implementation Complete!

Your plotter now supports **BOTH** expression input methods:

### Method 1: Direct Expression (Recommended) ✨

Enter mathematical expressions using `x` as the variable:

- **Lines**: `2*x + 3` or `5*x - 1`
- **Quadratics**: `x^2 + 3*x + 1` or `2*x^2 - 4*x + 2`
- **Cubics**: `x^3 - 4*x + 1` or `2*x^3 + x^2 - 3*x + 5`
- **Higher order**: `x^4 - 5*x^2 + 4`

### Method 2: Coefficient Entry (Fallback)

Enter comma-separated coefficients (highest degree first):

- For `x² + 3x + 1`: enter `1, 3, 1`
- For `2x - 5`: enter `2, -5`

## How It Works

1. **Expression Priority**: The plotter first tries to parse as an expression with `x`
2. **F# Interpreter**: Uses your existing `InterpreterLib` to evaluate expressions
3. **Auto-sampling**: Evaluates the expression at multiple x values (based on range and Δx)
4. **Grid & Axes**: Automatically displays grid lines and coordinate axes
5. **Fallback**: If no `x` found, tries coefficient parsing

## Test Cases

### Simple Line (Order 1)

```
Expression: 2*x + 3
Range: -10 to 10
Δx: 0.5
Expected: Straight line with slope 2, y-intercept 3
```

### Quadratic (Order 2)

```
Expression: x^2 - 4*x + 3
Range: -2 to 6
Δx: 0.1
Expected: Parabola opening upward, roots at x=1 and x=3
```

### Cubic (Order 3)

```
Expression: x^3 - 3*x
Range: -3 to 3
Δx: 0.05
Expected: S-shaped curve passing through origin
```

## According to Coursework Brief ✅

- ✅ **Lines (y = ax + b)**: `2*x + 3`
- ✅ **Polynomials (y = axⁿ + ...)**: `x^3 + 2*x^2 - x + 5`
- ✅ **Range & step control**: GUI inputs for x min/max and Δx
- ✅ **Dedicated plotting area**: Separate window with canvas
- ✅ **Visual representation**: Grid lines, axes, smooth curves
- ✅ **Piecewise linear approximation**: Uses small Δx for smooth curves

## Advantages Over Coefficient Entry

1. **Intuitive**: Users enter math expressions naturally
2. **Flexible**: Works with any expression (not just polynomials)
3. **No conversion**: Don't need to extract coefficients manually
4. **Extensible**: Can later add trig functions (sin, cos) when implemented
5. **Matches coursework**: Brief shows expressions like "y = ax + b"
