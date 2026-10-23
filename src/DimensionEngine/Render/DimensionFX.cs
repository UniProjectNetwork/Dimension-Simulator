using System.Numerics;
using DimensionEngine.Maths;
using DimensionEngine.ND;
using DimensionEngine.Scenes;
using DimensionEngine.Universe;
using DimensionEngine.Utils;
using SkiaSharp;

namespace DimensionEngine.Render;

/// <summary>
/// Visual effects for the dimension-warp views: warped spacetime sheet,
/// tesseracts, probability ghosts, world-line trails, Calabi-Yau knots,
/// the E8 root mandala, vibrating strings, transition flash and HUD badge.
/// </summary>
public static class DimensionFX
{
    // ============================== spacetime sheet ==============================

    /// <summary>Grid on the ecliptic plane, displaced by a gravity bowl plus the
    /// dimension-specific warp mode (ripple / interference / funnel / folded / lattice / strings).</summary>
    public static void DrawSpacetimeGrid(RenderContext ctx, DimensionWarp warp, double extent)
    {
        var pf = warp.P;
        if (pf.Grid == WarpGrid.None) return;
        double k = warp.Blend;
        if (k < 0.02) return;

        var path = new SKPath();
        int lines = 13, samples = 36;
        for (int fam = 0; fam < 2; fam++)
        {
            for (int i = 0; i < lines; i++)
            {
                double c = extent * (i / (double)(lines - 1) * 2 - 1);
                bool started = false;
                for (int s = 0; s <= samples; s++)
                {
                    double a = extent * (s / (double)samples * 2 - 1);
                    double x = fam == 0 ? a : c;
                    double z = fam == 0 ? c : a;
                    var gp = GridPoint(x, z, extent, warp);
                    var (sx, sy, zv, front) = ctx.Project(new Vec3(gp.x, gp.y, gp.z));
                    if (!front) { started = false; continue; }
                    if (!started) { path.MoveTo((float)sx, (float)sy); started = true; }
                    else path.LineTo((float)sx, (float)sy);
                }
            }
        }
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f * ctx.ScaleFactor,
            Color = ColorUtil.WithAlpha(pf.Accent, (byte)(26 * k))
        };
        ctx.Canvas.DrawPath(path, paint);
        path.Dispose();

        // funnel mode: add depth contour rings so the embedding diagram reads clearly
        if (pf.Grid == WarpGrid.Funnel)
        {
            for (int ring = 1; ring <= 5; ring++)
            {
                double rr = extent * 0.055 * ring * ring;
                if (rr > extent) break;
                var rpath = new SKPath();
                for (int s = 0; s <= 72; s++)
                {
                    double a = s / 72.0 * 2 * Math.PI;
                    var gp = GridPoint(Math.Cos(a) * rr, Math.Sin(a) * rr, extent, warp);
                    var (sx, sy, zv, front) = ctx.Project(new Vec3(gp.x, gp.y, gp.z));
                    if (!front) continue;
                    if (s == 0) rpath.MoveTo((float)sx, (float)sy); else rpath.LineTo((float)sx, (float)sy);
                }
                rpath.Close();
                using var rp = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.1f * ctx.ScaleFactor,
                    Color = ColorUtil.WithAlpha(pf.Accent, (byte)(40 * k))
                };
                ctx.Canvas.DrawPath(rpath, rp);
                rpath.Dispose();
            }
        }
    }

    private static (double x, double y, double z) GridPoint(double x, double z, double ext, DimensionWarp warp)
    {
        var pf = warp.P;
        double t = warp.Time;
        double k = warp.Blend;
        double r = Math.Sqrt(x * x + z * z);

        // gentle gravity bowl everywhere (Sun well)
        double y = -ext * 0.06 / (1 + Math.Pow(r / (ext * 0.14), 2));

        if (pf.Grid == WarpGrid.Funnel)
        {
            // deep general-relativity embedding funnel + frame-dragging swirl
            double depth = ext * pf.FunnelDepth;
            y = -depth * (1 - MathUtil.SmoothStep(0, ext * 0.38, r));
            double swirl = t * 0.22 + 1.1 * Math.Exp(-r / (ext * 0.3));
            double cs = Math.Cos(swirl), sn = Math.Sin(swirl);
            double nx = x * cs - z * sn, nz = x * sn + z * cs;
            x = nx; z = nz;
        }

        switch (pf.Grid)
        {
            case WarpGrid.Ripple:
                y += ext * 0.03 * Math.Sin(r / ext * 16 - t * 1.7) * Math.Exp(-r / (ext * 0.6));
                break;
            case WarpGrid.Interference:
            {
                double sx2 = Math.Cos(t * 0.5) * ext * 0.42, sz2 = Math.Sin(t * 0.5) * ext * 0.42;
                double r2 = Math.Sqrt((x - sx2) * (x - sx2) + (z - sz2) * (z - sz2));
                y += ext * 0.024 * Math.Sin(r / ext * 18 - t * 1.9) * Math.Exp(-r / (ext * 0.7))
                   + ext * 0.024 * Math.Sin(r2 / ext * 18 - t * 1.9) * Math.Exp(-r2 / (ext * 0.7));
                break;
            }
            case WarpGrid.Folded:
            {
                double amp = pf.D >= 9 ? 0.075 : 0.05;
                y += ext * amp * Math.Abs(Math.Sin(x / ext * 3.2 + t * 0.35)) * Math.Abs(Math.Sin(z / ext * 3.2 - t * 0.28));
                break;
            }
            case WarpGrid.Lattice:
            {
                double q = ext / 24;
                double xq = Math.Round(x / q) * q, zq = Math.Round(z / q) * q;
                y += ext * 0.012 * Math.Sin(xq * 0.031 + t * 0.6) * Math.Sin(zq * 0.027 - t * 0.5);
                break;
            }
            case WarpGrid.Strings:
                y += ext * 0.026 * Math.Sin(x / ext * 11 + t * 1.1) * Math.Sin(z / ext * 9 - t * 0.85);
                break;
        }

        if (pf.Grid != WarpGrid.Funnel)
            y += ext * 0.012 * Math.Sin(r / ext * 9 - t * 0.9);   // sheet always breathes a little

        return (x, y * k, z);
    }

    // ============================== tesseract ==============================

    /// <summary>A 4D hypercube wireframe (16 vertices, 32 edges) rotating in the
    /// XW / YZ / YW planes, projected 4D -> 3D -> screen around a world center.</summary>
    public static void DrawTesseract(RenderContext ctx, Vec3 center, double size, double time, SKColor col, double alpha)
    {
        if (alpha < 0.02) return;
        var v = new (double x, double y, double z, double w)[16];
        for (int i = 0; i < 16; i++)
            v[i] = ((i & 1) != 0 ? 1 : -1, (i & 2) != 0 ? 1 : -1, (i & 4) != 0 ? 1 : -1, (i & 8) != 0 ? 1 : -1);

        double a1 = time * 0.55, a2 = time * 0.38, a3 = time * 0.47;
        var scr = new (double x, double y, double k4)[16];
        for (int i = 0; i < 16; i++)
        {
            double x = v[i].x, y = v[i].y, z = v[i].z, w = v[i].w;
            Rot(ref x, ref w, a1);
            Rot(ref y, ref z, a2);
            Rot(ref y, ref w, a3);
            double k4 = MathUtil.Clamp(2.3 / Math.Max(2.3 - w * 0.55, 0.4), 0.5, 2.2);
            var (sx, sy, zv, front) = ctx.Project(center + new Vec3(x * k4 * size, y * k4 * size, z * k4 * size));
            if (!front) return;   // center behind camera: skip the whole wireframe
            scr[i] = (sx, sy, k4);
        }

        for (int i = 0; i < 16; i++)
        for (int j = i + 1; j < 16; j++)
        {
            if (BitOperations.PopCount((uint)(i ^ j)) != 1) continue;
            double k4 = (scr[i].k4 + scr[j].k4) * 0.5;
            var c = ColorUtil.Hsv(190 + (k4 - 1) * 95, 0.68, 1.0);
            float width = 1.25f * ctx.ScaleFactor;
            using (var glow = new SKPaint { IsAntialias = true, StrokeWidth = width * 3.2f, BlendMode = SKBlendMode.Plus, Color = ColorUtil.WithAlpha(c, (byte)(alpha * 0.16)) })
                ctx.Canvas.DrawLine((float)scr[i].x, (float)scr[i].y, (float)scr[j].x, (float)scr[j].y, glow);
            using var crisp = new SKPaint { IsAntialias = true, StrokeWidth = width, Color = ColorUtil.WithAlpha(c, (byte)(alpha * (0.4 + 0.6 * Math.Min(k4, 1.6) / 1.6))) };
            ctx.Canvas.DrawLine((float)scr[i].x, (float)scr[i].y, (float)scr[j].x, (float)scr[j].y, crisp);
        }
    }

    private static void Rot(ref double a, ref double b, double ang)
    {
        double c = Math.Cos(ang), s = Math.Sin(ang);
        double na = a * c - b * s, nb = a * s + b * c;
        a = na; b = nb;
    }

    // ============================== probability ghosts (5D) ==============================

    public static void DrawGhosts(RenderContext ctx, DimensionWarp warp, SpaceObject o, SKColor tint)
    {
        var pf = warp.P;
        int g = pf.Ghosts;
        double k = warp.Blend;
        if (g <= 0 || k < 0.05 || !o.Visible || o.PxRadius <= 0.5) return;
        for (int i = 1; i <= g; i++)
        {
            double ph = warp.Time * 0.45 + o.WarpGroup * 1.31 + i * 2.09;
            double dx = Math.Sin(ph) * o.PxRadius * 3.1;
            double dy = Math.Sin(ph * 1.37 + 1.2) * o.PxRadius * 1.7;
            double rr = o.PxRadius * (0.5 + 0.14 * ((i + o.WarpGroup) % 3));
            ctx.DrawGlow(o.ScreenX + dx, o.ScreenY + dy, Math.Max(rr * 1.9, 3), ColorUtil.WithAlpha(tint, (byte)(70 * k)), 0.7);
            using var p = new SKPaint { IsAntialias = true, Color = ColorUtil.WithAlpha(tint, (byte)(40 * k)) };
            ctx.Canvas.DrawCircle((float)(o.ScreenX + dx), (float)(o.ScreenY + dy), (float)rr, p);
        }
    }

    // ============================== world-line trails (6D) ==============================

    public static void DrawTrail(RenderContext ctx, Vec3[] hist, SKColor col, double k)
    {
        if (hist.Length < 8 || k < 0.05) return;
        const int buckets = 6;
        var paths = new SKPath?[buckets];
        for (int i = 1; i < hist.Length; i++)
        {
            var (ax, ay, az, af) = ctx.Project(hist[i - 1]);
            var (bx, by, bz, bf) = ctx.Project(hist[i]);
            if (!af || !bf) continue;
            int bk = Math.Min(buckets - 1, (int)((double)i / hist.Length * buckets));
            (paths[bk] ??= new SKPath()).MoveTo((float)ax, (float)ay);
            paths[bk]!.LineTo((float)bx, (float)by);
        }
        for (int b = 0; b < buckets; b++)
        {
            if (paths[b] == null) continue;
            byte alpha = (byte)(62 * k * (b + 1) / buckets);
            using var p = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (0.8f + 1.1f * b / (buckets - 1)) * ctx.ScaleFactor,
                Color = ColorUtil.WithAlpha(col, alpha)
            };
            ctx.Canvas.DrawPath(paths[b]!, p);
            paths[b]!.Dispose();
        }
    }

    // ============================== Calabi-Yau knot (7D) ==============================

    public static void DrawCalabiKnot(RenderContext ctx, double cx, double cy, double r, double time, SKColor col, double k)
    {
        if (r < 6 || k < 0.05) return;
        int n = 56;
        using var core = new SKPath();
        using var fr = new SKPath();
        using var fb = new SKPath();
        double c1 = Math.Cos(time * 0.8), s1 = Math.Sin(time * 0.8);
        double c2 = Math.Cos(time * 0.53), s2 = Math.Sin(time * 0.53);
        for (int i = 0; i <= n; i++)
        {
            double u = i / (double)n * 2 * Math.PI;
            double rr = 1 + 0.45 * Math.Cos(3 * u);
            double x = rr * Math.Cos(2 * u), y = Math.Sin(3 * u) * 0.85, z = rr * Math.Sin(2 * u);
            double x1 = x * c1 - z * s1, z1 = x * s1 + z * c1;
            double y1 = y * c2 - z1 * s2;
            double sx = cx + x1 * r, sy = cy - y1 * r * 0.9;
            var pt = new SKPoint((float)sx, (float)sy);
            if (i == 0) { core.MoveTo(pt); fr.MoveTo(pt); fb.MoveTo(pt); }
            else
            {
                core.LineTo(pt);
                fr.LineTo(new SKPoint((float)sx - 2f, (float)sy + 0.5f));
                fb.LineTo(new SKPoint((float)sx + 2f, (float)sy - 0.5f));
            }
        }
        using (var pg = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3.4f * ctx.ScaleFactor, BlendMode = SKBlendMode.Plus, Color = ColorUtil.WithAlpha(col, (byte)(46 * k)) })
            ctx.Canvas.DrawPath(core, pg);
        using (var pr = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.1f * ctx.ScaleFactor, Color = new SKColor(255, 90, 90, (byte)(52 * k)) })
            ctx.Canvas.DrawPath(fr, pr);
        using (var pb = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.1f * ctx.ScaleFactor, Color = new SKColor(90, 140, 255, (byte)(52 * k)) })
            ctx.Canvas.DrawPath(fb, pb);
        using var pc = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.15f * ctx.ScaleFactor, Color = ColorUtil.WithAlpha(ColorUtil.Lerp(col, new SKColor(255, 255, 255), 0.45), (byte)(165 * k)) };
        ctx.Canvas.DrawPath(core, pc);
    }

    // ============================== vibrating strings (9D halo / 10D) ==============================

    public static void DrawStringLoop(RenderContext ctx, double cx, double cy, double r, double time,
        SKColor col, double k, double amp = 0.16, float width = 1.2f, byte alpha = 200, double squash = 0.38)
    {
        if (r < 4 || k < 0.03) return;
        int n = 64;
        using var path = new SKPath();
        double crot = Math.Cos(time * 0.45), srot = Math.Sin(time * 0.45);
        double sq = squash + 0.12 * Math.Sin(time * 0.9);
        for (int i = 0; i <= n; i++)
        {
            double u = i / (double)n * 2 * Math.PI;
            double wob = 1 + amp * Math.Sin(3 * u + time * 2.1)
                           + amp * 0.6 * Math.Sin(5 * u - time * 1.6)
                           + amp * 0.35 * Math.Sin(8 * u + time * 2.9);
            double x = Math.Cos(u) * r * wob;
            double y = Math.Sin(u) * r * wob * sq;
            double rx = x * crot - y * srot, ry = x * srot + y * crot;
            var pt = new SKPoint((float)(cx + rx), (float)(cy + ry));
            if (i == 0) path.MoveTo(pt); else path.LineTo(pt);
        }
        path.Close();
        using (var glow = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width * 3.2f, BlendMode = SKBlendMode.Plus, Color = ColorUtil.WithAlpha(col, (byte)(alpha * 0.22 * k)) })
            ctx.Canvas.DrawPath(path, glow);
        using var core = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width * ctx.ScaleFactor, BlendMode = SKBlendMode.Plus, Color = ColorUtil.WithAlpha(col, (byte)(alpha * k)) };
        ctx.Canvas.DrawPath(path, core);
    }

    // ============================== E8 root mandala (8D) ==============================

    private static double[ , ]? _e8Roots;

    /// <summary>The 240 roots of E8: (+-1,+-1,0...) / sqrt2 pairs and (+-1/2 x8) with even minus signs.</summary>
    private static double[,] E8Roots()
    {
        if (_e8Roots != null) return _e8Roots!;
        var roots = new List<double[]>(240);
        double s2 = Math.Sqrt(2);
        for (int i = 0; i < 8; i++)
        for (int j = i + 1; j < 8; j++)
        for (int si = -1; si <= 1; si += 2)
        for (int sj = -1; sj <= 1; sj += 2)
        {
            var r = new double[8];
            r[i] = si / s2; r[j] = sj / s2;
            roots.Add(r);
        }
        for (int m = 0; m < 256; m++)
        {
            // half-integer roots: (+-1/2)^8 with an EVEN number of minus signs
            if ((BitOperations.PopCount((uint)m) & 1) != 0) continue;
            var r = new double[8];
            for (int b = 0; b < 8; b++) r[b] = ((m & (1 << b)) != 0 ? -0.5 : 0.5);
            roots.Add(r);
        }
        _e8Roots = new double[roots.Count, 8];
        for (int i = 0; i < roots.Count; i++)
        for (int j = 0; j < 8; j++)
            _e8Roots[i, j] = roots[i][j];
        return _e8Roots;
    }

    public static void DrawE8(RenderContext ctx, double cx, double cy, double radius, double time, SKColor col, double k)
    {
        if (radius < 20 || k < 0.05) return;
        var roots = E8Roots();
        double a1 = time * 0.13, a2 = time * 0.09, a3 = time * 0.07, a4 = time * 0.11;

        using var dot = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        for (int i = 0; i < roots.GetLength(0); i++)
        {
            double x1 = roots[i, 0], y1 = roots[i, 1], z1 = roots[i, 2], w1 = roots[i, 3];
            double x2 = roots[i, 4], y2 = roots[i, 5], z2 = roots[i, 6], w2 = roots[i, 7];
            Rot(ref x1, ref w1, a1); Rot(ref y1, ref z1, a2);
            Rot(ref x2, ref w2, a3); Rot(ref y2, ref z2, a4);
            double k1 = MathUtil.Clamp(2.1 / Math.Max(2.1 - w1 * 0.55, 0.4), 0.45, 2.2);
            double k2 = MathUtil.Clamp(2.1 / Math.Max(2.1 - w2 * 0.55, 0.4), 0.45, 2.2);
            double px = (x1 * k1 + x2 * k2 * 0.72) * radius;
            double py = (y1 * k1 + y2 * k2 * 0.72) * radius;
            double depth = (k1 + k2) * 0.5;
            dot.Color = ColorUtil.WithAlpha(col, (byte)(MathUtil.Clamp(24 + (depth - 0.8) * 70, 14, 95) * k));
            ctx.Canvas.DrawCircle((float)(cx + px), (float)(cy - py), 1.6f, dot);
        }
        // guide rings
        using var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f * ctx.ScaleFactor, Color = ColorUtil.WithAlpha(col, (byte)(32 * k)) };
        for (int g = 1; g <= 3; g++)
            ctx.Canvas.DrawCircle((float)cx, (float)cy, (float)(radius * g / 3.0), ring);
    }

    // ============================== transition flash ==============================

    public static void DrawWarpFlash(RenderContext ctx, DimensionWarp warp, Vec3 sunPos, int w, int h)
    {
        if (warp.Flash <= 0.02) return;
        var (sx, sy, zv, front) = ctx.Project(sunPos);
        if (!front) return;
        double f = Math.Sin(Math.Min(warp.Flash, 1) * Math.PI);
        var accent = warp.P.Accent;
        using (var paint = new SKPaint { IsAntialias = false, BlendMode = SKBlendMode.Plus })
        {
            paint.Shader = SKShader.CreateRadialGradient(
                new SKPoint((float)sx, (float)sy), Math.Max(w, h) * 0.75f,
                new[] { ColorUtil.WithAlpha(accent, (byte)(64 * f)), ColorUtil.WithAlpha(accent, 0) },
                new[] { 0f, 1f }, SKShaderTileMode.Clamp);
            ctx.Canvas.DrawRect(0, 0, w, h, paint);
        }
        using var ringP = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.4f * ctx.ScaleFactor, Color = ColorUtil.WithAlpha(accent, (byte)(140 * f)) };
        ctx.Canvas.DrawCircle((float)sx, (float)sy, (float)((1 - warp.Flash) * Math.Max(w, h) * 0.5 + 20), ringP);
    }

    // ============================== 10D dissolve bodies (shared by Solar & Deep Space) ==============================

    private static void SetScreen(SpaceObject o, RenderContext ctx, double visR)
    {
        var (sx, sy, zv, front) = ctx.Project(o.Position);
        o.Visible = front; o.ScreenX = sx; o.ScreenY = sy; o.ZView = zv;
        o.PxRadius = ctx.PixelRadius(visR, zv);
    }

    /// <summary>10D: a planet (or any compact world) collapses into a dark core wrapped in strings.</summary>
    public static void DrawStringCore(RenderContext ctx, SpaceObject o, WarpProfile pf, double k)
    {
        SetScreen(o, ctx, o.VisualRadius * 0.55);
        if (!o.Visible) return;
        if (o.PxRadius >= 1.2)
        {
            using var core = new SKPaint { IsAntialias = true, Color = new SKColor(6, 8, 16, (byte)(235 * k)) };
            ctx.Canvas.DrawCircle((float)o.ScreenX, (float)o.ScreenY, (float)o.PxRadius, core);
            using var rim = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.2f * ctx.ScaleFactor,
                Color = ColorUtil.WithAlpha(pf.Accent, (byte)(200 * k))
            };
            ctx.Canvas.DrawCircle((float)o.ScreenX, (float)o.ScreenY, (float)(o.PxRadius + 1), rim);
        }
        ctx.DrawGlow(o.ScreenX, o.ScreenY, Math.Max(o.PxRadius * 2.4, 6), ColorUtil.WithAlpha(pf.Accent, (byte)(80 * k)), 0.55);
    }

    /// <summary>10D: a star becomes a bright resonance above a dark singularity dot.</summary>
    public static void DrawStringStar(RenderContext ctx, StarObject s, WarpProfile pf, double k)
    {
        SetScreen(s, ctx, s.VisualRadius);
        if (!s.Visible) return;
        ctx.DrawGlow(s.ScreenX, s.ScreenY, Math.Max(s.PxRadius * Math.Min(s.CoronaScale, 2.6), 8), s.StarColor, Math.Max(0.35, 0.9 * k));
        using var core = new SKPaint
        {
            IsAntialias = true,
            Color = ColorUtil.WithAlpha(ColorUtil.Lerp(s.StarColor, new SKColor(255, 255, 255), 0.55), (byte)(210 * k))
        };
        ctx.Canvas.DrawCircle((float)s.ScreenX, (float)s.ScreenY, (float)(s.PxRadius * 0.8), core);
        using var dark = new SKPaint { IsAntialias = true, Color = new SKColor(5, 6, 12, (byte)(190 * k)) };
        ctx.Canvas.DrawCircle((float)s.ScreenX, (float)s.ScreenY, (float)(s.PxRadius * 0.42), dark);
    }

    // ============================== HUD badge ==============================

    public static void DrawWarpBadge(RenderContext ctx, DimensionWarp warp, int w, int h)
    {
        if (!warp.Active) return;
        var pf = warp.P;
        double k = Math.Min(1, warp.Blend * 1.5);
        using var p = new SKPaint { IsAntialias = true, Typeface = LabelRenderer.Typeface };
        string title = $"{pf.D}-DIMENSIONAL VIEW — {pf.Title.ToUpperInvariant()}";
        p.TextSize = 14.5f * ctx.ScaleFactor;
        p.Color = ColorUtil.WithAlpha(pf.Accent, (byte)(235 * k));
        float tw = p.MeasureText(title);
        ctx.Canvas.DrawText(title, w / 2f - tw / 2, 34f * ctx.ScaleFactor, p);

        p.TextSize = 11f * ctx.ScaleFactor;
        p.Color = new SKColor(150, 165, 195, (byte)(215 * k));
        float tw2 = p.MeasureText(pf.Tag);
        ctx.Canvas.DrawText(pf.Tag, w / 2f - tw2 / 2, 34f * ctx.ScaleFactor + 17f * ctx.ScaleFactor, p);
    }
}
