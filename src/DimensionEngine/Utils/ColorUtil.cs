using SkiaSharp;

using DimensionEngine.Maths;

namespace DimensionEngine.Utils;

/// <summary>Color helpers for space rendering (temperature palettes, gradients, etc.).</summary>
public static class ColorUtil
{
    public static SKColor FromBytes(byte r, byte g, byte b, byte a = 255) => new(a, r, g, b);

    /// <summary>Linear interpolate two SKColors.</summary>
    public static SKColor Lerp(SKColor a, SKColor b, double t)
    {
        t = MathUtil.Clamp(t, 0, 1);
        return new SKColor(
            (byte)Math.Round(a.Red + (b.Red - a.Red) * t),
            (byte)Math.Round(a.Green + (b.Green - a.Green) * t),
            (byte)Math.Round(a.Blue + (b.Blue - a.Blue) * t),
            (byte)Math.Round(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    /// <summary>Blackbody-ish color from Kelvin temperature (approximation).</summary>
    public static SKColor FromTemperature(double kelvin)
    {
        kelvin = MathUtil.Clamp(kelvin, 1000, 40000);
        double t = kelvin / 100.0;
        double r, g, b;

        if (t <= 66) r = 255;
        else r = 329.7 * Math.Pow(t - 60, -0.1332);

        if (t <= 66) g = 99.47 * Math.Log(t) - 161.12;
        else g = 288.12 * Math.Pow(t - 60, -0.0755);

        if (t >= 66) b = 255;
        else if (t <= 19) b = 0;
        else b = 138.52 * Math.Log(t - 10) - 305.04;

        return new SKColor(
            (byte)MathUtil.Clamp(r, 0, 255),
            (byte)MathUtil.Clamp(g, 0, 255),
            (byte)MathUtil.Clamp(b, 0, 255));
    }

    /// <summary>Spectral-class base color (O B A F G K M).</summary>
    public static SKColor SpectralColor(string spectral)
    {
        if (string.IsNullOrEmpty(spectral)) return new SKColor(255, 244, 229);
        return char.ToUpperInvariant(spectral[0]) switch
        {
            'O' => new SKColor(155, 176, 255),
            'B' => new SKColor(170, 191, 255),
            'A' => new SKColor(222, 231, 255),
            'F' => new SKColor(255, 244, 234),
            'G' => new SKColor(255, 233, 178),
            'K' => new SKColor(255, 209, 148),
            'M' => new SKColor(255, 161, 112),
            'W' => new SKColor(200, 220, 255), // Wolf-Rayet / white dwarf
            'D' => new SKColor(210, 225, 255), // white dwarf
            _ => new SKColor(255, 244, 229),
        };
    }

    public static SKColor WithAlpha(SKColor c, byte alpha) => new(c.Red, c.Green, c.Blue, alpha);

    /// <summary>HSV -> SKColor (h 0..360, s 0..1, v 0..1).</summary>
    public static SKColor Hsv(double h, double s, double v, byte a = 255)
    {
        h = ((h % 360) + 360) % 360;
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;
        double r = 0, g = 0, b = 0;
        switch (h)
        {
            case < 60: r = c; g = x; break;
            case < 120: r = x; g = c; break;
            case < 180: g = c; b = x; break;
            case < 240: g = x; b = c; break;
            case < 300: r = x; b = c; break;
            default: r = c; b = x; break;
        }
        return new SKColor(
            (byte)MathUtil.Clamp((r + m) * 255, 0, 255),
            (byte)MathUtil.Clamp((g + m) * 255, 0, 255),
            (byte)MathUtil.Clamp((b + m) * 255, 0, 255), a);
    }
}
