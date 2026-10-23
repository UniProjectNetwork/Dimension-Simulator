using DimensionEngine.Maths;
using DimensionEngine.Utils;
using SkiaSharp;

namespace DimensionEngine.Render;

/// <summary>
/// Collection of specialized renderers: background starfield, Milky Way band,
/// nebula sprites, spiral galaxies, black holes (event horizon + relativistic accretion
/// disk + lensing arcs + jets), planetary rings, pulsar beams and orbit paths.
/// </summary>
public static class Effects
{
    // ===================== STARFIELD =====================

    private static Vec3[]? _starDirs;
    private static float[]? _starSize;
    private static SKColor[]? _starColor;
    private static Vec3[]? _bandDirs;

    private static void BuildStarfield()
    {
        var rng = Noise.Rng(20260101);
        int n = 1900;
        _starDirs = new Vec3[n];
        _starSize = new float[n];
        _starColor = new SKColor[n];
        for (int i = 0; i < n; i++)
        {
            double u = rng.NextDouble() * 2 - 1;
            double phi = rng.NextDouble() * Math.PI * 2;
            double s = Math.Sqrt(1 - u * u);
            _starDirs[i] = new Vec3(s * Math.Cos(phi), u, s * Math.Sin(phi));
            double mag = rng.NextDouble();
            _starSize[i] = (float)(0.6 + 1.5 * mag * mag);
            double temp = 2600 + rng.NextDouble() * 11000;
            _starColor[i] = ColorUtil.FromTemperature(temp);
            byte a = (byte)(90 + 165 * mag);
            _starColor[i] = ColorUtil.WithAlpha(_starColor[i], a);
        }
        // Milky Way band: clustered near the galactic plane (an inclined great circle)
        int m = 2600;
        _bandDirs = new Vec3[m];
        for (int i = 0; i < m; i++)
        {
            double lon = rng.NextDouble() * Math.PI * 2;
            double lat = (rng.NextDouble() + rng.NextDouble() + rng.NextDouble() - 1.5) * 0.22; // triangular cluster
            var p = new Vec3(Math.Cos(lat) * Math.Cos(lon), Math.Sin(lat), Math.Cos(lat) * Math.Sin(lon));
            _bandDirs[i] = p.RotateAround(Vec3.UnitX.Normalized(), 62 * MathUtil.DegToRad);
        }
    }

    public static void DrawStarfield(RenderContext ctx)
    {
        if (_starDirs == null) BuildStarfield();
        var canvas = ctx.Canvas;
        var view = ctx.View;
        float halfW = ctx.Width / 2f, halfH = ctx.Height / 2f;
        double f = 1.0 / Math.Tan(ctx.Cam.ProjFovY / 2);

        using var p = new SKPaint { IsAntialias = true };

        // Milky Way soft band
        p.BlendMode = SKBlendMode.Plus;
        for (int i = 0; i < _bandDirs!.Length; i += 2)
        {
            var d = view.TransformDir(_bandDirs[i]);
            if (d.Z <= 0.05) continue;
            double k = f / d.Z;
            float x = (float)(halfW + d.X * k * halfH);
            float y = (float)(halfH - d.Y * k * halfH);
            p.Color = new SKColor(150, 160, 190, 26);
            canvas.DrawCircle(x, y, 1.6f + (float)d.Z * 2.2f, p);
        }

        // main stars
        p.BlendMode = SKBlendMode.SrcOver;
        for (int i = 0; i < _starDirs!.Length; i++)
        {
            var d = view.TransformDir(_starDirs[i]);
            if (d.Z <= 0.02) continue;
            double k = f / d.Z;
            float x = (float)(halfW + d.X * k * halfH);
            float y = (float)(halfH - d.Y * k * halfH);
            p.Color = _starColor![i];
            canvas.DrawCircle(x, y, _starSize![i], p);
        }
    }

    // ===================== NEBULA SPRITES =====================

    private static readonly Dictionary<int, SKBitmap> _nebulaCache = new();

    /// <summary>Bake a nebula cloud sprite (384x384) from fbm; colors are lerped in RGB.</summary>
    public static SKBitmap GetNebulaSprite(int seed, SKColor colorA, SKColor colorB)
    {
        lock (_nebulaCache)
        {
            if (_nebulaCache.TryGetValue(seed, out var b)) return b;

            int size = 384;
            var bmp = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
            var px = new uint[size * size];
            for (int y = 0; y < size; y++)
            {
                double fy = (y - size / 2.0) / (size / 2.0);
                for (int x = 0; x < size; x++)
                {
                    double fx = (x - size / 2.0) / (size / 2.0);
                    double r = Math.Sqrt(fx * fx + fy * fy);
                    if (r > 1) { px[y * size + x] = 0; continue; }

                    double n = Noise.Fbm2((fx * 0.5 + 1) * 3.4, (fy * 0.5 + 1) * 3.4, seed, 6);
                    double fil = Noise.Ridged2((fx * 0.5 + 1) * 5.2, (fy * 0.5 + 1) * 5.2, seed + 40, 5);
                    double density = MathUtil.Clamp(n * 1.25 + fil * 0.4 - 0.62, 0, 1) * (1 - r * r * 0.85);
                    if (density < 0.02) { px[y * size + x] = 0; continue; }

                    double mixT = MathUtil.Clamp(fy * 0.5 + 0.5 + (n - 0.5) * 0.6, 0, 1);
                    var c = ColorUtil.Lerp(colorA, colorB, mixT);
                    c = ColorUtil.Lerp(c, SKColors.White, fil * 0.25);
                    byte a = (byte)MathUtil.Clamp(density * 205, 0, 255);
                    // premultiply
                    byte pr = (byte)(c.Red * a / 255), pg = (byte)(c.Green * a / 255), pb = (byte)(c.Blue * a / 255);
                    px[y * size + x] = (uint)(pb | ((uint)pg << 8) | ((uint)pr << 16) | ((uint)a << 24));
                }
            }
            var pix = bmp.PeekPixels();
            if (pix != null)
            {
                unsafe
                {
                    fixed (uint* src = px)
                        Buffer.MemoryCopy((void*)src, (void*)pix.GetPixels(), (long)px.Length * 4, (long)px.Length * 4);
                }
            }
            _nebulaCache[seed] = bmp;
            return bmp;
        }
    }

    public static void DrawNebula(RenderContext ctx, SKBitmap sprite, double sx, double sy, double screenRadius, double alpha)
    {
        if (screenRadius < 2 || alpha <= 0.01) return;
        var canvas = ctx.Canvas;
        using var p = new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus,
            Color = new SKColor(255, 255, 255, (byte)MathUtil.Clamp(alpha * 255, 0, 230))
        };
        var dst = SKRect.Create((float)(sx - screenRadius), (float)(sy - screenRadius), (float)(screenRadius * 2), (float)(screenRadius * 2));
        canvas.DrawBitmap(sprite, dst, p);
    }

    // ===================== GALAXIES =====================

    public sealed class GalaxyParticles
    {
        public Vec3[] Pos = Array.Empty<Vec3>();
        public SKColor[] Col = Array.Empty<SKColor>();
        public float[] Size = Array.Empty<float>();
        public double InclinationDeg;
        public double SpinSpeed = 0.004;   // rad per sim-year-ish; scaled by caller
    }

    public static GalaxyParticles MakeSpiralGalaxy(int seed, int count = 2400, int arms = 2)
    {
        var rng = Noise.Rng(seed);
        var g = new GalaxyParticles();
        var pos = new List<Vec3>(count);
        var col = new List<SKColor>(count);
        var size = new List<float>(count);

        for (int i = 0; i < count; i++)
        {
            double t = Math.Pow(rng.NextDouble(), 0.72);       // radial distribution
            double armIndex = rng.Next(arms);
            double baseAngle = armIndex * (2 * Math.PI / arms);
            double spiral = baseAngle + t * 4.4;               // log-ish spiral winding
            double scatter = (rng.NextDouble() - 0.5) * (0.35 + 0.65 * t) * 0.9;
            double ang = spiral + scatter;
            double rad = 0.12 + t * 0.88;
            double x = Math.Cos(ang) * rad;
            double z = Math.Sin(ang) * rad;
            double y = (rng.NextDouble() + rng.NextDouble() - 1) * 0.05 * (1.2 - t);  // thin disk
            pos.Add(new Vec3(x, y, z));

            double coreDist = rad;
            SKColor c;
            if (coreDist < 0.22) c = ColorUtil.Hsv(38, 0.35, 1.0);          // golden core
            else if (rng.NextDouble() < 0.045) c = ColorUtil.Hsv(320, 0.55, 1.0); // HII regions
            else c = ColorUtil.Hsv(210 + rng.NextDouble() * 30, 0.30, 0.85 + rng.NextDouble() * 0.15); // blue arms
            col.Add(ColorUtil.WithAlpha(c, (byte)(110 + 120 * rng.NextDouble())));
            size.Add((float)(0.8 + 1.5 * rng.NextDouble()));
        }
        g.Pos = pos.ToArray();
        g.Col = col.ToArray();
        g.Size = size.ToArray();
        return g;
    }

    public static void DrawGalaxy(RenderContext ctx, GalaxyParticles g, Vec3 center, double worldRadius, double spinAngle)
    {
        var (sx, sy, zv, front) = ctx.Project(center);
        if (!front) return;
        double rPx = ctx.PixelRadius(worldRadius, zv);
        if (rPx < 3) { ctx.DrawGlow(sx, sy, 2.5, new SKColor(220, 215, 255), 0.5); return; }

        var canvas = ctx.Canvas;
        using var p = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };

        // rotate particle disk around Y by spin, incline around X
        double incl = g.InclinationDeg * MathUtil.DegToRad;
        double cs = Math.Cos(spinAngle), ss = Math.Sin(spinAngle);
        double ci = Math.Cos(incl), si = Math.Sin(incl);
        double f = 1.0 / Math.Tan(ctx.Cam.ProjFovY / 2);
        float halfH = ctx.Height / 2f;

        for (int i = 0; i < g.Pos.Length; i++)
        {
            var p3 = g.Pos[i];
            double x = p3.X * cs - p3.Z * ss;
            double z = p3.X * ss + p3.Z * cs;
            double y = p3.Y;
            // incline about X axis
            double y2 = y * ci - z * si;
            double z2 = y * si + z * ci;
            var world = center + new Vec3(x, y2, z2) * worldRadius;
            var v = ctx.View.TransformPoint(world);
            if (v.Z <= 1e-9) continue;
            var c2 = ctx.Proj.TransformPoint(v);
            float px = (float)(ctx.Width / 2.0 + c2.X * ctx.Width / 2.0);
            float py = (float)(ctx.Height / 2.0 - c2.Y * ctx.Height / 2.0);
            p.Color = g.Col[i];
            canvas.DrawCircle(px, py, g.Size[i] * (float)MathUtil.Clamp(rPx / 260, 0.35, 2.2), p);
        }

        // core glow
        ctx.DrawGlow(sx, sy, rPx * 0.42, new SKColor(255, 236, 190), 1.1);
        ctx.DrawGlow(sx, sy, rPx * 0.16, new SKColor(255, 252, 240), 1.4);
    }

    // ===================== BLACK HOLES =====================

    /// <summary>
    /// Stylized-but-motivated black hole: photon ring, event horizon, relativistic
    /// accretion disk with Doppler beaming, gravitational-lensing arcs of the disk's
    /// far side bent above/below the shadow, and optional relativistic jets.
    /// </summary>
    public static void DrawBlackHole(RenderContext ctx, double sx, double sy, double rShadowPx,
        double diskInnerR, double diskOuterR, double diskTiltDeg, double diskPhase,
        bool jets, double jetLengthPx)
    {
        if (rShadowPx < 0.8) return;
        var canvas = ctx.Canvas;

        // extremely close black hole: render only horizon + photon ring (camera near/flyby)
        bool extremeClose = rShadowPx > 480;

        double tilt = diskTiltDeg * MathUtil.DegToRad;
        double sinT = Math.Sin(tilt), cosT = Math.Cos(tilt);

        // ---- lensing arcs (far side of disk bent over/under the shadow) ----
        if (rShadowPx > 2.5 && !extremeClose)
        {
            using var arc = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)Math.Max(1.5, rShadowPx * 0.16),
                BlendMode = SKBlendMode.Plus
            };
            arc.Shader = SKShader.CreateRadialGradient(new SKPoint((float)sx, (float)sy), (float)(rShadowPx * 1.5),
                new[] { new SKColor(255, 210, 150, 200), new SKColor(255, 140, 60, 0) },
                new[] { 0.55f, 1f }, SKShaderTileMode.Clamp);
            var arcRect = SKRect.Create((float)(sx - rShadowPx * 1.5), (float)(sy - rShadowPx * 1.5), (float)(rShadowPx * 3), (float)(rShadowPx * 3));
            // top arc
            using var pathTop = new SKPath();
            pathTop.AddArc(arcRect, 195, 150);
            canvas.DrawPath(pathTop, arc);
            using var pathBot = new SKPath();
            pathBot.AddArc(arcRect, 15, 150);
            canvas.DrawPath(pathBot, arc);
        }

        // ---- accretion disk: back half, then (caller draws horizon), front half ----
        void DrawDiskHalf(bool back)
        {
            if (rShadowPx < 1.5 || extremeClose) return;
            int steps = 46;
            using var p = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
            double inner = diskInnerR, outer = diskOuterR;

            for (int i = 0; i < steps; i++)
            {
                double t0 = i / (double)steps, t1 = (i + 1) / (double)steps;
                double r0 = MathUtil.Lerp(inner, outer, t0), r1 = MathUtil.Lerp(inner, outer, t1);

                // temperature ramp: white-hot inner → orange → deep red outer
                var c = ColorUtil.Lerp(new SKColor(255, 244, 224), new SKColor(255, 132, 32), t0);
                c = ColorUtil.Lerp(c, new SKColor(190, 48, 12), MathUtil.Clamp((t0 - 0.55) * 2.2, 0, 1));
                byte a = (byte)(215 * (1 - Math.Pow(t0, 1.6)));

                // draw as segments around ellipse; doppler factor: approaching side brighter
                int segs = Math.Max(26, (int)(rShadowPx / 1.1));
                for (int s = 0; s < segs; s++)
                {
                    double a0 = 2 * Math.PI * s / segs + diskPhase;
                    double a1 = 2 * Math.PI * (s + 1.02) / segs + diskPhase;
                    // ellipse squash by tilt; back half is y < 0 side (screen up)
                    bool isBackSeg = Math.Sin(a0) < 0;
                    if (isBackSeg != back) continue;
                    double doppler = 1 + 0.85 * Math.Cos(a0);   // left approaching
                    var cc = ColorUtil.WithAlpha(c, (byte)MathUtil.Clamp(a * MathUtil.Clamp(doppler, 0.18, 1.9), 0, 255));
                    p.Color = cc;
                    p.StrokeWidth = (float)Math.Max(1, (r1 - r0) * rShadowPx * 1.15);
                    double x0 = sx + Math.Cos(a0) * r0 * rShadowPx;
                    double y0 = sy + Math.Sin(a0) * r0 * rShadowPx * sinT;
                    double x1 = sx + Math.Cos(a1) * r1 * rShadowPx;
                    double y1 = sy + Math.Sin(a1) * r1 * rShadowPx * sinT;
                    canvas.DrawLine((float)x0, (float)y0, (float)x1, (float)y1, p);
                }
            }
        }

        // glow behind everything — fade out when the camera is close (inside the glow)
        double glowFade = rShadowPx > 700 ? MathUtil.Clamp(700 / rShadowPx, 0.12, 1) : 1.0;
        if (glowFade > 0.15 && !extremeClose)
            ctx.DrawGlow(sx, sy, rShadowPx * Math.Max(2.2, diskOuterR * 1.35), ColorUtil.WithAlpha(new SKColor(255, 150, 60), (byte)(255 * glowFade)), 0.5 * glowFade);

        DrawDiskHalf(back: true);

        // ---- jets (relativistic beams along the spin axis) ----
        if (jets && rShadowPx > 2 && !extremeClose)
        {
            using var jp = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
            float len = (float)(jetLengthPx);
            for (int dir = -1; dir <= 1; dir += 2)
            {
                jp.Shader = SKShader.CreateLinearGradient(
                    new SKPoint((float)sx, (float)sy),
                    new SKPoint((float)sx, (float)(sy - dir * len)),
                    new[] { new SKColor(170, 220, 255, 110), new SKColor(120, 160, 255, 0) },
                    new[] { 0f, 1f }, SKShaderTileMode.Clamp);
                using var path = new SKPath();
                path.MoveTo((float)(sx - rShadowPx * 0.08), (float)sy);
                path.LineTo((float)(sx + rShadowPx * 0.08), (float)sy);
                path.LineTo((float)(sx + rShadowPx * 0.30), (float)(sy - dir * len));
                path.LineTo((float)(sx - rShadowPx * 0.30), (float)(sy - dir * len));
                path.Close();
                canvas.DrawPath(path, jp);
            }
        }
        if (rShadowPx > 1.2)
        {
            using var ring = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(255, 246, 230, 235),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)Math.Max(1.2, rShadowPx * 0.05),
                BlendMode = SKBlendMode.Plus
            };
            canvas.DrawCircle((float)sx, (float)sy, (float)(rShadowPx * 1.02), ring);
        }

        // ---- event horizon (pure black) ----
        using var hole = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        canvas.DrawCircle((float)sx, (float)sy, (float)rShadowPx, hole);

        DrawDiskHalf(back: false);
    }

    // ===================== PLANETARY RINGS =====================

    /// <summary>Draw ring system around a planet. Call with back=true before the body, back=false after.</summary>
    public static void DrawRings(RenderContext ctx, double sx, double sy, double rPlanetPx,
        double ringInner, double ringOuter, double tiltDeg, bool back)
    {
        if (rPlanetPx < 2.2) return;
        // effective on-screen squash blends the ring tilt with the camera pitch
        double pitchDeg = ctx.Cam.Pitch * MathUtil.RadToDeg;
        double eff = Math.Abs(Math.Sin((tiltDeg + pitchDeg) * MathUtil.DegToRad));
        double squash = MathUtil.Clamp(eff, 0.045, 0.95);

        var canvas = ctx.Canvas;
        int bands = 26;
        using var p = new SKPaint { IsAntialias = true };

        for (int i = 0; i < bands; i++)
        {
            double t = i / (double)(bands - 1);
            double rr = MathUtil.Lerp(ringInner, ringOuter, t);   // multiplier of planet radius
            // Cassini division & band density profile
            double density = 0.75 + 0.25 * Math.Sin(t * 40);
            if (Math.Abs(t - 0.63) < 0.045) density *= 0.12;          // Cassini division
            if (t < 0.06) density *= 0.35;
            byte alpha = (byte)MathUtil.Clamp(150 * density * MathUtil.Clamp(rPlanetPx / 40, 0.25, 1), 0, 255);
            if (alpha < 3) continue;

            var color = ColorUtil.Lerp(new SKColor(196, 178, 140), new SKColor(160, 146, 120), t);
            p.Color = ColorUtil.WithAlpha(color, alpha);
            p.StrokeWidth = (float)Math.Max(0.8, rPlanetPx * (ringOuter - ringInner) / bands * 1.05);
            p.Style = SKPaintStyle.Stroke;

            // ellipse arc: back half (above center when tilted) or front half
            double start = back ? 180 : 0;
            float rx = (float)(rr * rPlanetPx);
            float ry = (float)(rr * rPlanetPx * squash);
            using var path = new SKPath();
            path.AddArc(SKRect.Create((float)(sx - rx), (float)(sy - ry), rx * 2, ry * 2), (float)start, 180);
            canvas.DrawPath(path, p);
        }
    }

    // ===================== PULSAR =====================

    public static void DrawPulsarBeams(RenderContext ctx, double sx, double sy, double rPx, double spinPhase)
    {
        if (rPx < 1) return;
        var canvas = ctx.Canvas;
        using var p = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        double len = Math.Max(rPx * 14, 40);
        for (int dir = -1; dir <= 1; dir += 2)
        {
            double ang = spinPhase; // beam sweeps
            double dx = Math.Cos(ang) * len;
            double dy = Math.Sin(ang) * len * dir;
            p.Shader = SKShader.CreateLinearGradient(
                new SKPoint((float)sx, (float)sy), new SKPoint((float)(sx + dx), (float)(sy + dy)),
                new[] { new SKColor(190, 225, 255, 190), new SKColor(140, 190, 255, 0) },
                new[] { 0f, 1f }, SKShaderTileMode.Clamp);
            p.StrokeWidth = (float)Math.Max(1.5, rPx * 0.5);
            canvas.DrawLine((float)sx, (float)sy, (float)(sx + dx), (float)(sy + dy), p);
        }
    }

    // ===================== ORBIT PATH =====================

    public static void DrawOrbitPath(RenderContext ctx, Vec3[] path, SKColor color, float width = 1f)
    {
        using var p = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = width * ctx.ScaleFactor,
            BlendMode = SKBlendMode.SrcOver
        };
        bool started = false;
        var skPath = new SKPath();
        foreach (var pt in path)
        {
            var (x, y, zv, front) = ctx.Project(pt);
            if (!front) { started = false; continue; }
            byte a = color.Alpha;
            if (!started) { skPath.MoveTo((float)x, (float)y); started = true; }
            else skPath.LineTo((float)x, (float)y);
        }
        p.Color = color;
        ctx.Canvas.DrawPath(skPath, p);
    }
}
