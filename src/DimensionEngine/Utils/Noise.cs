using DimensionEngine.Maths;

namespace DimensionEngine.Utils;

/// <summary>
/// Seeded value noise + fractal Brownian motion, used for procedural planet textures,
/// nebula clouds and galaxy dust. Deterministic per seed — no external dependencies.
/// </summary>
public static class Noise
{
    private static int _hash(int x, int seed)
    {
        unchecked
        {
            int h = seed;
            h ^= x * 374761393;
            h = (h ^ (h >> 13)) * 1274126177;
            return h ^ (h >> 16);
        }
    }

    /// <summary>1D pseudo-random from integer lattice, in [0,1).</summary>
    public static double Rand1(int i, int seed)
    {
        uint h = (uint)_hash(i, seed);
        return (h & 0xFFFFFF) / (double)0x1000000;
    }

    /// <summary>2D pseudo-random from integer lattice, in [0,1).</summary>
    public static double Rand2(int x, int y, int seed)
    {
        unchecked
        {
            int h = seed;
            h ^= x * 374761393;
            h ^= y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            uint u = (uint)(h ^ (h >> 16));
            return (u & 0xFFFFFF) / (double)0x1000000;
        }
    }

    private static double Fade(double t) => t * t * (3 - 2 * t);

    /// <summary>2D value noise, smooth, in [0,1].</summary>
    public static double Value2(double x, double y, int seed)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        double xf = x - xi, yf = y - yi;
        double a = Rand2(xi, yi, seed);
        double b = Rand2(xi + 1, yi, seed);
        double c = Rand2(xi, yi + 1, seed);
        double d = Rand2(xi + 1, yi + 1, seed);
        double u = Fade(xf), v = Fade(yf);
        return MathUtil.Lerp(MathUtil.Lerp(a, b, u), MathUtil.Lerp(c, d, u), v);
    }

    /// <summary>3D value noise, smooth, in [0,1].</summary>
    public static double Value3(double x, double y, double z, int seed)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y), zi = (int)Math.Floor(z);
        double xf = x - xi, yf = y - yi, zf = z - zi;
        double c000 = Rand3(xi, yi, zi, seed), c100 = Rand3(xi + 1, yi, zi, seed);
        double c010 = Rand3(xi, yi + 1, zi, seed), c110 = Rand3(xi + 1, yi + 1, zi, seed);
        double c001 = Rand3(xi, yi, zi + 1, seed), c101 = Rand3(xi + 1, yi, zi + 1, seed);
        double c011 = Rand3(xi, yi + 1, zi + 1, seed), c111 = Rand3(xi + 1, yi + 1, zi + 1, seed);
        double u = Fade(xf), v = Fade(yf), w = Fade(zf);
        return MathUtil.Lerp(
            MathUtil.Lerp(MathUtil.Lerp(c000, c100, u), MathUtil.Lerp(c010, c110, u), v),
            MathUtil.Lerp(MathUtil.Lerp(c001, c101, u), MathUtil.Lerp(c011, c111, u), v), w);
    }

    private static double Rand3(int x, int y, int z, int seed)
    {
        unchecked
        {
            int h = seed;
            h ^= x * 374761393;
            h ^= y * 668265263;
            h ^= z * 2147483647;
            h = (h ^ (h >> 13)) * 1274126177;
            uint u = (uint)(h ^ (h >> 16));
            return (u & 0xFFFFFF) / (double)0x1000000;
        }
    }

    /// <summary>Fractal Brownian motion over 2D value noise, in [0,1] normalized.</summary>
    public static double Fbm2(double x, double y, int seed, int octaves = 5, double lacunarity = 2.0, double gain = 0.5)
    {
        double sum = 0, amp = 1, freq = 1, norm = 0;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * Value2(x * freq, y * freq, seed + o * 101);
            norm += amp;
            amp *= gain;
            freq *= lacunarity;
        }
        return sum / norm;
    }

    /// <summary>Fbm over 3D value noise — seamless-ish on spheres when sampled on normals.</summary>
    public static double Fbm3(double x, double y, double z, int seed, int octaves = 5, double lacunarity = 2.0, double gain = 0.5)
    {
        double sum = 0, amp = 1, freq = 1, norm = 0;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * Value3(x * freq, y * freq, z * freq, seed + o * 101);
            norm += amp;
            amp *= gain;
            freq *= lacunarity;
        }
        return sum / norm;
    }

    /// <summary>Ridged fbm — good for swirling clouds / nebula filaments.</summary>
    public static double Ridged2(double x, double y, int seed, int octaves = 5)
    {
        double sum = 0, amp = 1, freq = 1, norm = 0;
        for (int o = 0; o < octaves; o++)
        {
            double n = 1.0 - Math.Abs(Value2(x * freq, y * freq, seed + o * 131) * 2 - 1);
            sum += amp * n * n;
            norm += amp;
            amp *= 0.5;
            freq *= 2.0;
        }
        return sum / norm;
    }

    /// <summary>Seeded RNG helper.</summary>
    public static Random Rng(int seed) => new(seed);
}
