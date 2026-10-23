using DimensionEngine.Camera;
using DimensionEngine.Maths;
using DimensionEngine.ND;
using DimensionEngine.Utils;
using SkiaSharp;

namespace DimensionEngine.Render;

/// <summary>
/// Per-frame rendering context: wraps the Skia canvas plus camera matrices and
/// exposes projection helpers used by all renderers.
/// </summary>
public sealed class RenderContext
{
    public SKCanvas Canvas = null!;
    public OrbitCamera Cam = null!;
    public Mat4 View;
    public Mat4 Proj;
    public int Width;
    public int Height;
    public double Dt;                 // real seconds this frame
    public double SimDays;            // simulated elapsed days
    public double TimeScaleDaysPerSec;
    public DimensionWarp? Warp;       // active dimension warp (solar system only, null = flat 3D)

    public float ScaleFactor = 1f;    // DPI scaling

    public bool ShowOrbits = true;    // toggle for orbit lines

    public SphereShader Shader = new();

    public (double x, double y, double zView, bool inFront) Project(Vec3 world)
    {
        var v = View.TransformPoint(world);
        var c = Proj.TransformPoint(v);
        bool front = v.Z > 1e-9;
        return (Width / 2.0 + c.X * Width / 2.0, Height / 2.0 - c.Y * Height / 2.0, v.Z, front);
    }

    public double PixelRadius(double worldRadius, double zView)
        => Cam.PixelRadius(worldRadius, zView, Height);

    /// <summary>Transform world direction into camera/view space (no translation).</summary>
    public Vec3 WorldDirToView(Vec3 dir) => View.TransformDir(dir);

    // ---------- shared paints ----------

    public void DrawGlow(double cx, double cy, double radius, SKColor color, double intensity = 0.8)
    {
        radius = Math.Min(radius, 1200);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        };
        int steps = 3;
        for (int i = steps; i >= 1; i--)
        {
            double f = i / (double)steps;
            double r = radius * (0.55 + 0.45 * (1 - f) + 0.55 * f);
            paint.Shader = SKShader.CreateRadialGradient(
                new SKPoint((float)cx, (float)cy), (float)r,
                new[]
                {
                    ColorUtil.WithAlpha(color, (byte)MathUtil.Clamp(intensity * 90 * (1.05 - f), 0, 255)),
                    ColorUtil.WithAlpha(color, 0)
                },
                new[] { 0f, 1f }, SKShaderTileMode.Clamp);
            Canvas.DrawCircle((float)cx, (float)cy, (float)r, paint);
        }
    }

    public void DrawLine(Vec3 a, Vec3 b, SKColor color, float width = 1f, bool fade = true)
    {
        var (ax, ay, az, af) = Project(a);
        var (bx, by, bz, bf) = Project(b);
        if (!af && !bf) return;
        byte alpha = color.Alpha;
        if (fade)
        {
            double d = (az + bz) * 0.5;
            alpha = (byte)MathUtil.Clamp(alpha * FadeAlpha(d), 0, 255);
        }
        if (alpha < 4) return;
        using var p = new SKPaint { IsAntialias = true, Color = ColorUtil.WithAlpha(color, alpha), StrokeWidth = width * ScaleFactor, Style = SKPaintStyle.Stroke };
        Canvas.DrawLine((float)ax, (float)ay, (float)bx, (float)by, p);
    }

    /// <summary>Distance-based alpha fade so distant clutter disappears gracefully.</summary>
    public double FadeAlpha(double zView, double fadeStart = 4000, double fadeEnd = 90000)
    {
        double t = 1.0 - MathUtil.SmoothStep(fadeStart, fadeEnd, zView);
        return MathUtil.Clamp(t, 0, 1);
    }
}
