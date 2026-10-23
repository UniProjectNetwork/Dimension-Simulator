using DimensionEngine.Maths;
using DimensionEngine.Utils;
using SkiaSharp;

namespace DimensionEngine.Render;

/// <summary>
/// Software sphere renderer: per-pixel ray-traced ball with equirect texture sampling,
/// Lambertian day/night terminator, atmosphere rim scattering and specular oceans.
/// Falls back to gradient dots for tiny apparent sizes. Includes a per-body bitmap cache.
/// </summary>
public sealed class SphereShader
{
    private SKBitmap? _bmp;
    private SKSurface? _surf;
    private uint[] _px = Array.Empty<uint>();
    private int _allocSize;

    public void Render(RenderContext ctx, double sx, double sy, double rPx, double zView,
        SurfaceTexture tex, Vec3 lightDirView, SKColor atmosphere, bool emissive = false,
        double ambient = 0.16, double specular = 0.0, double spinPhase = 0.0, byte brightness = 255,
        SKColor? dotColor = null, SKColor? tint = null)
    {
        if (rPx <= 0 || zView <= 1e-9) return;

        var canvas = ctx.Canvas;
        var dot = dotColor ?? atmosphere;
        if (dot.Alpha < 200) dot = ColorUtil.WithAlpha(dot == atmosphere && atmosphere.Alpha < 40 ? new SKColor(190, 190, 200) : dot, 235);

        if (rPx < 1.6)
        {
            // distant dot
            using var p = new SKPaint { IsAntialias = true, Color = dot };
            canvas.DrawCircle((float)sx, (float)sy, (float)Math.Max(rPx, 0.7), p);
            return;
        }
        if (rPx < 5)
        {
            // cheap gradient ball
            using var paint = new SKPaint { IsAntialias = true };
            paint.Shader = SKShader.CreateRadialGradient(
                new SKPoint((float)(sx - rPx * 0.35), (float)(sy - rPx * 0.35)), (float)(rPx * 1.7),
                new[] { dot, ColorUtil.WithAlpha(dot, 70) },
                new[] { 0.25f, 1f }, SKShaderTileMode.Clamp);
            canvas.DrawCircle((float)sx, (float)sy, (float)rPx, paint);
            return;
        }

        // cap internal resolution for huge close-ups; scale up on blit
        double rDraw = Math.Min(rPx, 1400);
        double rInt = Math.Min(rPx, 420);

        int size = (int)Math.Ceiling(rInt * 2 + 2);
        size = ((size + 15) / 16) * 16;
        if (_allocSize != size)
        {
            try
            {
                _bmp?.Dispose();
                _surf?.Dispose();
                _bmp = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
                _surf = _bmp.PeekPixels() != null ? SKSurface.Create(_bmp.PeekPixels()) : null;
                _px = new uint[size * size];
                _allocSize = size;
            }
            catch (Exception)
            {
                _bmp = null;
                _allocSize = 0;
                return;
            }
        }
        if (_bmp == null) return;
        Array.Clear(_px);

        double r = rInt;
        double c = size / 2.0;
        bool bilinear = rDraw > 64;

        // light dir in view space (unit)
        double lx = lightDirView.X, ly = lightDirView.Y, lz = lightDirView.Z;

        // inverse view rotation for cam->world normal (view matrix rows are the basis)
        var view = ctx.View;
        double r11 = view.M11, r12 = view.M21, r13 = view.M31;
        double r21 = view.M12, r22 = view.M22, r23 = view.M32;
        double r31 = view.M13, r32 = view.M23, r33 = view.M33;

        int texW = tex.W, texH = tex.H;
        byte amb = (byte)(255 * ambient * (brightness / 255.0));

        // tint factors for emissive bodies (star color)
        double tR = 1, tG = 1, tB = 1;
        if (tint is SKColor tc2) { tR = tc2.Red / 255.0; tG = tc2.Green / 255.0; tB = tc2.Blue / 255.0; }

        for (int py = 0; py < size; py++)
        {
            double ny = (py - c + 0.5) / r;              // +y down in screen
            for (int pxi = 0; pxi < size; pxi++)
            {
                double nx = (pxi - c + 0.5) / r;
                double rr = nx * nx + ny * ny;
                if (rr > 1.0) continue;
                double nz = Math.Sqrt(1 - rr);

                // camera-space normal (screen y down → flip y)
                double cx = nx, cy = -ny, cz = nz;

                // world normal = inverse-rotate cam normal
                double wx = r11 * cx + r12 * cy + r13 * cz;
                double wy = r21 * cx + r22 * cy + r23 * cz;
                double wz = r31 * cx + r32 * cy + r33 * cz;

                // texture coords from world normal + spin
                double u = Math.Atan2(wz, wx) / (2 * Math.PI) + spinPhase;
                double v = Math.Acos(MathUtil.Clamp(wy, -1, 1)) / Math.PI;
                uint tc = bilinear ? tex.SampleBilinear(u - Math.Floor(u), v) : tex.Sample(u - Math.Floor(u), v);

                byte tr = (byte)((tc >> 16) & 0xFF);
                byte tg = (byte)((tc >> 8) & 0xFF);
                byte tb = (byte)(tc & 0xFF);

                if (emissive)
                {
                    int er = (int)MathUtil.Clamp(tr * tR, 0, 255);
                    int eg = (int)MathUtil.Clamp(tg * tG, 0, 255);
                    int eb = (int)MathUtil.Clamp(tb * tB, 0, 255);
                    _px[py * size + pxi] = (uint)(eb | ((uint)eg << 8) | ((uint)er << 16) | 0xFF000000u);
                    continue;
                }

                // Lambert with soft terminator
                double ndl = wx * lx + wy * ly + wz * lz;
                double diff = MathUtil.SmoothStep(-0.08, 0.25, ndl);
                double light = ambient + diff * (1 - ambient);
                int ir = (tr * (int)(light * 255)) >> 8;
                int ig = (tg * (int)(light * 255)) >> 8;
                int ib = (tb * (int)(light * 255)) >> 8;

                // specular (oceans: bluish textures) — compute per-pixel only when enabled
                if (specular > 0 && diff > 0.6)
                {
                    // half-vector with view dir (0,0,1) in view space
                    double hx = lx, hy = ly, hz = lz + 1;
                    double hl = Math.Sqrt(hx * hx + hy * hy + hz * hz);
                    double spec = (wx * hx + wy * hy + wz * hz) / hl;
                    if (spec > 0)
                    {
                        spec = Math.Pow(spec, 42) * specular * diff;
                        ir = (int)Math.Min(255, ir + 255 * spec);
                        ig = (int)Math.Min(255, ig + 255 * spec);
                        ib = (int)Math.Min(255, ib + 255 * spec);
                    }
                }

                _px[py * size + pxi] = (uint)((byte)MathUtil.Clamp(ib, 0, 255) | ((uint)(byte)MathUtil.Clamp(ig, 0, 255) << 8) | ((uint)(byte)MathUtil.Clamp(ir, 0, 255) << 16) | 0xFF000000u);
            }
        }

        // blit pixels into bitmap
        var pix = _bmp!.PeekPixels();
        if (pix != null)
        {
            unsafe
            {
                fixed (uint* src = _px)
                {
                    Buffer.MemoryCopy((void*)src, (void*)pix.GetPixels(), (long)_px.Length * 4, (long)_px.Length * 4);
                }
            }
        }

        canvas.Save();
        canvas.ClipRect(SKRect.Create((float)(sx - rDraw - 1), (float)(sy - rDraw - 1), (float)(rDraw * 2 + 2), (float)(rDraw * 2 + 2)));
        canvas.DrawBitmap(_bmp, SKRect.Create((float)(sx - rDraw - 1), (float)(sy - rDraw - 1), (float)(rDraw * 2 + 2), (float)(rDraw * 2 + 2)));

        // atmosphere rim: radial gradient ring just outside the limb
        if (atmosphere.Alpha > 0 && !emissive)
        {
            using var rim = new SKPaint { IsAntialias = true };
            rim.Shader = SKShader.CreateRadialGradient(
                new SKPoint((float)sx, (float)sy), (float)(rDraw * 1.09),
                new[]
                {
                    ColorUtil.WithAlpha(atmosphere, 0),
                    ColorUtil.WithAlpha(atmosphere, (byte)(atmosphere.Alpha * 0.55)),
                    ColorUtil.WithAlpha(atmosphere, 0)
                },
                new[] { 0.82f, 0.93f, 1f }, SKShaderTileMode.Clamp);
            canvas.DrawCircle((float)sx, (float)sy, (float)(rDraw * 1.09), rim);
        }
        canvas.Restore();
    }
}
