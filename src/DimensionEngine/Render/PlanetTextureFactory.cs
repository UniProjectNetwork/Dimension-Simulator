using DimensionEngine.Maths;
using DimensionEngine.Utils;
using SkiaSharp;

namespace DimensionEngine.Render;

/// <summary>An equirectangular surface texture stored as BGRA uint pixels for fast sampling.</summary>
public sealed class SurfaceTexture
{
    public readonly uint[] Px;
    public readonly int W, H;

    public SurfaceTexture(int w, int h)
    {
        W = w; H = h;
        Px = new uint[w * h];
    }

    public uint Sample(double u, double v)
    {
        int x = (int)(u * W) % W; if (x < 0) x += W;
        int y = (int)(v * (H - 1));
        if (y < 0) y = 0; else if (y >= H) y = H - 1;
        return Px[y * W + x];
    }

    /// <summary>Bilinear sample for large close-ups.</summary>
    public uint SampleBilinear(double u, double v)
    {
        double fx = u * W;
        double fy = MathUtil.Clamp(v * (H - 1), 0, H - 1);
        int x0 = (int)Math.Floor(fx); int x1 = (x0 + 1) % W;
        int y0 = (int)Math.Floor(fy);
        int y1 = Math.Min(y0 + 1, H - 1);
        if (x0 < 0) x0 += W;
        double tx = fx - Math.Floor(fx), ty = fy - Math.Floor(fy);
        uint c00 = Px[y0 * W + x0], c10 = Px[y0 * W + x1];
        uint c01 = Px[y1 * W + x0], c11 = Px[y1 * W + x1];
        return LerpPx(LerpPx(c00, c10, tx), LerpPx(c01, c11, tx), ty);
    }

    private static uint LerpPx(uint a, uint b, double t)
    {
        uint ba = a & 0xFF, ga = (a >> 8) & 0xFF, ra = (a >> 16) & 0xFF, aa = (a >> 24) & 0xFF;
        uint bb = b & 0xFF, gb = (b >> 8) & 0xFF, rb = (b >> 16) & 0xFF, ab = (b >> 24) & 0xFF;
        uint r = (uint)(ra + (rb - ra) * t);
        uint g = (uint)(ga + (gb - ga) * t);
        uint bl = (uint)(ba + (bb - ba) * t);
        uint al = (uint)(aa + (ab - aa) * t);
        return bl | (g << 8) | (r << 16) | (al << 24);
    }
}

public enum BodyTextureKind
{
    Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, Neptune,
    Pluto, Io, Europa, Ganymede, Callisto, Titan, Triton, Enceladus, Charon,
    RockyGeneric, IceGeneric, Lava, Exo
}

/// <summary>
/// Generates procedural equirectangular textures for every kind of world.
/// Fully deterministic (seeded noise) — no external image assets needed.
/// </summary>
public static class PlanetTextureFactory
{
    private static readonly Dictionary<BodyTextureKind, SurfaceTexture> _cache = new();
    private static readonly Dictionary<BodyTextureKind, SKColor> _avgCache = new();
    private static readonly object _lock = new();

    public static SurfaceTexture Get(BodyTextureKind kind)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(kind, out var t)) return t;
            t = Generate(kind);
            _cache[kind] = t;
            return t;
        }
    }

    /// <summary>Average texture color — used for distant-dot rendering.</summary>
    public static SKColor GetAverage(BodyTextureKind kind)
    {
        lock (_avgCache)
        {
            if (_avgCache.TryGetValue(kind, out var c)) return c;
            var tex = Get(kind);
            long r = 0, g = 0, b = 0; int n = 0;
            for (int i = 0; i < tex.Px.Length; i += 17)
            {
                uint p = tex.Px[i];
                if (((p >> 24) & 0xFF) < 128) continue;
                r += (p >> 16) & 0xFF; g += (p >> 8) & 0xFF; b += p & 0xFF; n++;
            }
            if (n == 0) n = 1;
            c = new SKColor((byte)(r / n), (byte)(g / n), (byte)(b / n));
            _avgCache[kind] = c;
            return c;
        }
    }

    private static uint Bgra(byte b, byte g, byte r, byte a = 255)
        => b | ((uint)g << 8) | ((uint)r << 16) | ((uint)a << 24);

    private static SurfaceTexture Generate(BodyTextureKind kind)
    {
        int w = 512, h = 256;
        var tex = new SurfaceTexture(w, h);
        int seed = kind switch
        {
            BodyTextureKind.Sun => 11, BodyTextureKind.Mercury => 22, BodyTextureKind.Venus => 33,
            BodyTextureKind.Earth => 44, BodyTextureKind.Moon => 55, BodyTextureKind.Mars => 66,
            BodyTextureKind.Jupiter => 77, BodyTextureKind.Saturn => 88, BodyTextureKind.Uranus => 99,
            BodyTextureKind.Neptune => 111, BodyTextureKind.Pluto => 122, BodyTextureKind.Io => 133,
            BodyTextureKind.Europa => 144, BodyTextureKind.Ganymede => 155, BodyTextureKind.Callisto => 166,
            BodyTextureKind.Titan => 177, BodyTextureKind.Triton => 188, BodyTextureKind.Enceladus => 199,
            BodyTextureKind.Charon => 211, BodyTextureKind.RockyGeneric => 222, BodyTextureKind.IceGeneric => 233,
            BodyTextureKind.Lava => 244, _ => 255
        };

        for (int y = 0; y < h; y++)
        {
            double v = y / (double)(h - 1);           // 0 = north pole
            double lat = (0.5 - v) * Math.PI;          // -pi/2..pi/2
            for (int x = 0; x < w; x++)
            {
                double u = x / (double)w;
                uint c = kind switch
                {
                    BodyTextureKind.Sun => SunPx(u, v, seed),
                    BodyTextureKind.Mercury => MercuryPx(u, v, seed),
                    BodyTextureKind.Venus => VenusPx(u, v, seed),
                    BodyTextureKind.Earth => EarthPx(u, v, lat, seed),
                    BodyTextureKind.Moon => MoonPx(u, v, seed),
                    BodyTextureKind.Mars => MarsPx(u, v, lat, seed),
                    BodyTextureKind.Jupiter => JupiterPx(u, v, lat, seed),
                    BodyTextureKind.Saturn => SaturnPx(u, v, lat, seed),
                    BodyTextureKind.Uranus => UranusPx(u, v, lat, seed),
                    BodyTextureKind.Neptune => NeptunePx(u, v, lat, seed),
                    BodyTextureKind.Pluto => PlutoPx(u, v, lat, seed),
                    BodyTextureKind.Io => IoPx(u, v, seed),
                    BodyTextureKind.Europa => EuropaPx(u, v, seed),
                    BodyTextureKind.Ganymede => GanymedePx(u, v, seed),
                    BodyTextureKind.Callisto => CallistoPx(u, v, seed),
                    BodyTextureKind.Titan => TitanPx(u, v, seed),
                    BodyTextureKind.Triton => TritonPx(u, v, seed),
                    BodyTextureKind.Enceladus => EnceladusPx(u, v, seed),
                    BodyTextureKind.Charon => CharonPx(u, v, lat, seed),
                    BodyTextureKind.RockyGeneric => MoonPx(u, v, seed),
                    BodyTextureKind.IceGeneric => EnceladusPx(u, v, seed),
                    BodyTextureKind.Lava => LavaPx(u, v, seed),
                    _ => ExoPx(u, v, lat, seed),
                };
                tex.Px[y * w + x] = c;
            }
        }
        return tex;
    }

    // ---------- per-kind pixel functions ----------

    private static uint SunPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 40, v * 20, seed, 4);
        double gran = Noise.Value2(u * 160, v * 80, seed + 7);
        double b = 0.84 + 0.14 * n + 0.07 * (gran - 0.5);
        return Bgra((byte)MathUtil.Clamp(255 * b * 0.55, 0, 255), (byte)MathUtil.Clamp(235 * b, 0, 255), (byte)MathUtil.Clamp(255 * b, 0, 255));
    }

    private static uint MercuryPx(double u, double v, int seed)
    {
        double baseN = Noise.Fbm2(u * 10, v * 5, seed, 5);
        byte g = (byte)MathUtil.Clamp(95 + 70 * baseN, 0, 255);
        uint c = Bgra(g, g, (byte)(g * 1.04));
        return Craterize(c, u, v, seed, 0.55, 46);
    }

    private static uint VenusPx(double u, double v, int seed)
    {
        double swirl = Noise.Fbm2(u * 6 + Noise.Fbm2(u * 3, v * 3, seed + 3, 3) * 2.2, v * 9, seed, 5);
        double band = Math.Sin(v * Math.PI * 5 + swirl * 4) * 0.5 + 0.5;
        double t = 0.55 + 0.25 * swirl + 0.2 * band;
        return Bgra((byte)(150 * t + 60), (byte)(115 * t + 70), (byte)(60 * t + 60));
    }

    private static uint EarthPx(double u, double v, double lat, int seed)
    {
        // continents
        double cont = Noise.Fbm2(u * 5, v * 5, seed, 6);
        double polar = Math.Abs(lat) / (Math.PI / 2);
        bool land = cont > 0.535;
        byte r, g, b;
        if (land)
        {
            double alt = (cont - 0.535) * 12;
            // latitude-based biomes: ice, tundra, forest, desert, jungle
            double dry = Noise.Fbm2(u * 8, v * 8, seed + 5, 4);
            if (polar > 0.86) { r = 235; g = 240; b = 245; }
            else if (polar > 0.72) { r = (byte)(150 + 40 * dry); g = (byte)(150 + 40 * dry); b = (byte)(140 + 30 * dry); }
            else if (Math.Abs(lat) < 0.35 && dry > 0.55) { r = 196; g = 168; b = 108; }   // desert belt
            else { r = (byte)(60 + 40 * alt); g = (byte)(105 + 45 * alt); b = (byte)(45 + 25 * alt); }
            // mountains snow caps
            if (alt > 0.72) { r = 225; g = 225; b = 228; }
        }
        else
        {
            double depth = Noise.Fbm2(u * 7, v * 7, seed + 11, 4);
            r = (byte)(8 + 20 * depth);
            g = (byte)(35 + 50 * depth);
            b = (byte)(85 + 70 * depth);
        }
        // clouds layer
        double cl = Noise.Fbm2(u * 9 + 13, v * 7, seed + 77, 5);
        if (cl > 0.585)
        {
            double ca = MathUtil.Clamp((cl - 0.585) * 6, 0, 1);
            r = (byte)(r * (1 - ca) + 250 * ca);
            g = (byte)(g * (1 - ca) + 252 * ca);
            b = (byte)(b * (1 - ca) + 255 * ca);
        }
        // polar sea ice
        if (polar > 0.92 && !land)
        {
            double t = (polar - 0.92) / 0.08;
            r = (byte)(r * (1 - t) + 232 * t);
            g = (byte)(g * (1 - t) + 240 * t);
            b = (byte)(b * (1 - t) + 248 * t);
        }
        return Bgra(b, g, r);
    }

    private static uint MoonPx(double u, double v, int seed)
    {
        double baseN = Noise.Fbm2(u * 9, v * 5, seed, 5);
        byte g = (byte)MathUtil.Clamp(120 + 85 * baseN, 0, 255);
        uint c = Bgra(g, g, g);
        return Craterize(c, u, v, seed, 0.62, 60);
    }

    private static uint MarsPx(double u, double v, double lat, int seed)
    {
        double n = Noise.Fbm2(u * 8, v * 6, seed, 6);
        double dark = Noise.Fbm2(u * 4 + 2, v * 3, seed + 9, 4);
        byte r = (byte)MathUtil.Clamp(150 + 70 * n - 30 * dark, 0, 255);
        byte g = (byte)MathUtil.Clamp(75 + 40 * n - 15 * dark, 0, 255);
        byte b = (byte)MathUtil.Clamp(45 + 25 * n, 0, 255);
        // polar caps
        double polar = Math.Abs(lat) / (Math.PI / 2);
        if (polar > 0.88)
        {
            double t = (polar - 0.88) / 0.12;
            r = (byte)(r * (1 - t) + 245 * t); g = (byte)(g * (1 - t) + 245 * t); b = (byte)(b * (1 - t) + 240 * t);
        }
        uint c = Bgra(b, g, r);
        return Craterize(c, u, v, seed, 0.3, 36);
    }

    private static uint JupiterPx(double u, double v, double lat, int seed)
    {
        double turb = Noise.Fbm2(u * 8, v * 3, seed, 5) - 0.5;
        double bands = Math.Sin((lat + turb * 0.22) * 14);
        double t = bands * 0.5 + 0.5;
        // creamy zones & brownish belts
        byte r = (byte)(170 + 60 * t);
        byte g = (byte)(135 + 45 * t);
        byte b = (byte)(100 + 30 * t);
        // Great Red Spot (around lat -22°, lon 60°)
        double dLon = u - 0.16; if (dLon > 0.5) dLon -= 1; if (dLon < -0.5) dLon += 1;
        double spot = (dLon * 6.2) * (dLon * 6.2) + ((lat + 0.40) * 9.0) * ((lat + 0.40) * 9.0);
        if (spot < 1.0)
        {
            double s = 1 - spot;
            r = (byte)(r * (1 - s * 0.9) + 205 * s * 0.9);
            g = (byte)(g * (1 - s * 0.9) + 82 * s * 0.9);
            b = (byte)(b * (1 - s * 0.9) + 55 * s * 0.9);
        }
        return Bgra(b, g, r);
    }

    private static uint SaturnPx(double u, double v, double lat, int seed)
    {
        double turb = Noise.Fbm2(u * 5, v * 2, seed, 4) - 0.5;
        double bands = Math.Sin((lat + turb * 0.15) * 11);
        double t = bands * 0.5 + 0.5;
        byte r = (byte)(205 + 40 * t);
        byte g = (byte)(180 + 35 * t);
        byte b = (byte)(130 + 30 * t);
        return Bgra(b, g, r);
    }

    private static uint UranusPx(double u, double v, double lat, int seed)
    {
        double n = Noise.Fbm2(u * 4, v * 2, seed, 4);
        double t = 0.75 + 0.18 * n + 0.06 * Math.Sin(lat * 9);
        return Bgra((byte)(168 * t + 30), (byte)(216 * t + 25), (byte)(224 * t + 20));
    }

    private static uint NeptunePx(double u, double v, double lat, int seed)
    {
        double n = Noise.Fbm2(u * 5, v * 2.5, seed, 4);
        double t = 0.7 + 0.22 * n + 0.08 * Math.Sin(lat * 7);
        byte r = (byte)(52 * t), g = (byte)(95 * t), b = (byte)(200 * t + 20);
        // Great Dark Spot
        double dLon = u - 0.62; if (dLon > 0.5) dLon -= 1; if (dLon < -0.5) dLon += 1;
        double spot = (dLon * 5.5) * (dLon * 5.5) + ((lat + 0.36) * 8.0) * ((lat + 0.36) * 8.0);
        if (spot < 1.0) { double s = (1 - spot) * 0.55; r = (byte)(r * (1 - s)); g = (byte)(g * (1 - s)); b = (byte)(b * (1 - s * 0.6)); }
        return Bgra(b, g, r);
    }

    private static uint PlutoPx(double u, double v, double lat, int seed)
    {
        double n = Noise.Fbm2(u * 7, v * 5, seed, 5);
        byte r = (byte)MathUtil.Clamp(175 + 55 * n, 0, 255);
        byte g = (byte)MathUtil.Clamp(140 + 50 * n, 0, 255);
        byte b = (byte)MathUtil.Clamp(110 + 45 * n, 0, 255);
        // "heart" — bright Tombaugh Regio
        double dLon = u - 0.30; if (dLon > 0.5) dLon -= 1; if (dLon < -0.5) dLon += 1;
        double heart = (dLon * 5.0) * (dLon * 5.0) + ((lat + 0.12) * 5.5) * ((lat + 0.12) * 5.5);
        if (heart < 1.0)
        {
            double s = MathUtil.Clamp((1 - heart) * 1.6, 0, 1);
            r = (byte)(r * (1 - s) + 245 * s); g = (byte)(g * (1 - s) + 240 * s); b = (byte)(b * (1 - s) + 230 * s);
        }
        return Bgra(b, g, r);
    }

    private static uint IoPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 10, v * 6, seed, 5);
        byte r = (byte)MathUtil.Clamp(190 + 60 * n, 0, 255);
        byte g = (byte)MathUtil.Clamp(160 + 60 * n, 0, 255);
        byte b = (byte)MathUtil.Clamp(60 + 40 * n, 0, 255);
        // volcanic dark spots
        double s = Noise.Fbm2(u * 14, v * 9, seed + 4, 3);
        if (s > 0.62) { double t = (s - 0.62) * 5; r = (byte)(r * (1 - t * 0.7)); g = (byte)(g * (1 - t * 0.5)); b = (byte)(b * (1 - t * 0.2)); }
        return Bgra(b, g, r);
    }

    private static uint EuropaPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 6, v * 4, seed, 4);
        byte r = (byte)MathUtil.Clamp(215 + 30 * n, 0, 255);
        byte g = (byte)MathUtil.Clamp(208 + 30 * n, 0, 255);
        byte b = (byte)MathUtil.Clamp(195 + 30 * n, 0, 255);
        // brown lineae: thin ridges via ridged noise
        double l1 = Noise.Ridged2(u * 18, v * 10, seed + 8, 3);
        double l2 = Noise.Ridged2(u * 26 + 7, v * 16, seed + 15, 3);
        if (l1 > 0.86 || l2 > 0.88)
        {
            double t = MathUtil.Clamp(Math.Max(l1 - 0.86, l2 - 0.88) * 14, 0, 1);
            r = (byte)(r * (1 - t * 0.5) + 150 * t * 0.5);
            g = (byte)(g * (1 - t * 0.55) + 90 * t * 0.55);
            b = (byte)(b * (1 - t * 0.6) + 70 * t * 0.6);
        }
        return Bgra(b, g, r);
    }

    private static uint GanymedePx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 8, v * 5, seed, 5);
        byte g = (byte)MathUtil.Clamp(115 + 65 * n, 0, 255);
        uint c = Bgra((byte)(g * 0.9), g, (byte)(g * 1.02));
        return Craterize(c, u, v, seed, 0.4, 40);
    }

    private static uint CallistoPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 9, v * 5, seed, 5);
        byte g = (byte)MathUtil.Clamp(85 + 60 * n, 0, 255);
        uint c = Bgra((byte)(g * 0.88), g, (byte)(g * 0.95));
        return Craterize(c, u, v, seed, 0.72, 70);
    }

    private static uint TitanPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 6, v * 3, seed, 5);
        double t = 0.7 + 0.3 * n;
        return Bgra((byte)(50 * t + 30), (byte)(110 * t + 60), (byte)(190 * t + 50));
    }

    private static uint TritonPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 7, v * 4, seed, 4);
        byte r = (byte)MathUtil.Clamp(200 + 40 * n, 0, 255);
        byte g = (byte)MathUtil.Clamp(175 + 45 * n, 0, 255);
        byte b = (byte)MathUtil.Clamp(160 + 50 * n, 0, 255);
        // cantaloupe terrain + south polar pink cap
        double polar = (v - 0.62);
        if (polar > 0)
        {
            double t = MathUtil.Clamp(polar * 3.2, 0, 1);
            r = (byte)(r * (1 - t) + 228 * t); g = (byte)(g * (1 - t) + 175 * t); b = (byte)(b * (1 - t) + 165 * t);
        }
        return Bgra(b, g, r);
    }

    private static uint EnceladusPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 8, v * 5, seed, 4);
        byte c = (byte)MathUtil.Clamp(228 + 25 * n, 0, 255);
        return Bgra((byte)(c * 1.02), (byte)(c * 1.01), c);
    }

    private static uint CharonPx(double u, double v, double lat, int seed)
    {
        double n = Noise.Fbm2(u * 8, v * 5, seed, 5);
        byte g = (byte)MathUtil.Clamp(120 + 55 * n, 0, 255);
        uint c = Bgra(g, (byte)(g * 0.97), (byte)(g * 0.95));
        // Mordor Macula — dark red north pole
        if (lat > 0.85)
        {
            double t = MathUtil.Clamp((lat - 0.85) / 0.15, 0, 1);
            byte r = (byte)(g * (1 - t) + 110 * t);
            byte gg = (byte)(g * 0.97 * (1 - t) + 60 * t);
            byte b = (byte)(g * 0.95 * (1 - t) + 45 * t);
            return Bgra(b, gg, r);
        }
        return Craterize(c, u, v, seed, 0.42, 44);
    }

    private static uint LavaPx(double u, double v, int seed)
    {
        double n = Noise.Fbm2(u * 8, v * 5, seed, 5);
        double cracks = Noise.Ridged2(u * 12, v * 7, seed + 3, 4);
        byte r = (byte)MathUtil.Clamp(120 + 120 * n + 40 * cracks, 0, 255);
        byte g = (byte)MathUtil.Clamp(35 + 60 * cracks, 0, 255);
        byte b = (byte)MathUtil.Clamp(15 + 20 * cracks * n, 0, 255);
        return Bgra(b, g, r);
    }

    private static uint ExoPx(double u, double v, double lat, int seed)
    {
        // Generic habitable-looking exoworld: teal oceans + sage continents + clouds
        double cont = Noise.Fbm2(u * 5, v * 5, seed, 6);
        bool land = cont > 0.55;
        byte r, g, b;
        if (land)
        {
            double alt = (cont - 0.55) * 14;
            r = (byte)(70 + 60 * alt); g = (byte)(120 + 40 * alt); b = (byte)(80 + 30 * alt);
        }
        else
        {
            r = (byte)(15 + 15 * cont); g = (byte)(70 + 40 * cont); b = (byte)(110 + 60 * cont);
        }
        double cl = Noise.Fbm2(u * 9 + 13, v * 7, seed + 77, 5);
        if (cl > 0.6)
        {
            double ca = MathUtil.Clamp((cl - 0.6) * 5, 0, 1);
            r = (byte)(r * (1 - ca) + 250 * ca); g = (byte)(g * (1 - ca) + 250 * ca); b = (byte)(b * (1 - ca) + 252 * ca);
        }
        return Bgra(b, g, r);
    }

    /// <summary>Stamp simple craters onto a color (used by rocky worlds).</summary>
    private static uint Craterize(uint c, double u, double v, int seed, double density, int count)
    {
        for (int i = 0; i < count; i++)
        {
            double cu = Noise.Rand1(i * 2 + 1, seed);
            double cv = 0.08 + 0.84 * Noise.Rand1(i * 2 + 2, seed);
            double rad = 0.008 + 0.03 * Noise.Rand1(i * 3 + 5, seed);
            double dLon = u - cu; if (dLon > 0.5) dLon -= 1; if (dLon < -0.5) dLon += 1;
            double dx = dLon, dy = (v - cv) * 0.55; // squash near poles not handled; fine
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < rad)
            {
                double t = d / rad;
                double shade = t < 0.75 ? -0.18 * (1 - t / 0.75) : 0.10;  // dark floor, bright rim
                byte b = (byte)MathUtil.Clamp((c & 0xFF) * (1 + shade), 0, 255);
                byte g = (byte)MathUtil.Clamp(((c >> 8) & 0xFF) * (1 + shade), 0, 255);
                byte r = (byte)MathUtil.Clamp(((c >> 16) & 0xFF) * (1 + shade), 0, 255);
                return Bgra(b, g, r);
            }
            // cheap early exit when far (density controls skip)
            if (density < 0.5 && i > count * density) break;
        }
        return c;
    }
}
