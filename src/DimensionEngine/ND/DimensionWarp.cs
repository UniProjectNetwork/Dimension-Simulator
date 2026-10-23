using DimensionEngine.Maths;
using DimensionEngine.Utils;
using SkiaSharp;

namespace DimensionEngine.ND;

public enum WarpGrid { None, Ripple, Interference, Funnel, Folded, Lattice, Strings }

/// <summary>
/// Look &amp; feel of one higher-dimensional view of the Solar System.
/// Every dimension 4..10 gets its own rotation planes, w-projection strength,
/// grid mode and signature effects, plus a science note shown in the UI.
/// </summary>
public sealed class WarpProfile
{
    public int D;
    public string Title = "";
    public string Tag = "";
    public string Science = "";
    public SKColor Accent = new(95, 232, 255);

    // --- hyperspace rotation (rad/s of view time) ---
    public double RotXW, RotYW, RotZW;

    // --- 4D -> 3D perspective projection ---
    public double ProjD = 2.45;      // projection distance in the 4th axis
    public double ScaleAmp = 1.0;    // how strongly the w-perspective swells space
    public double WSpread = 0.34;    // w-coordinate spread between orbit groups
    public double WHelixAmp;         // w-helix ripples along each orbit ring
    public double WHelixK = 3;       // helix waves per revolution
    public double WHelixSpeed = 1.1;

    // --- signature effects ---
    public double WobbleAmp;         // compactified-dimension vibration (fraction of radius)
    public double WobbleFast;        // high-frequency shimmer
    public int Ghosts;               // probability echoes per body (5D)
    public bool Tesseract;           // 4D cube wireframe around the Sun
    public bool Trails;              // world-line history (6D phase space)
    public bool Knots;               // Calabi-Yau style knots around bodies (7D)
    public bool Kaleido;             // mirrored echoes through the origin (8D)
    public bool E8Lattice;           // real 240-root E8 projection (8D)
    public bool Lissajous;           // non-closing wandering orbits (9D)
    public bool Strings;             // vibrating string loops (9D halos / 10D full)
    public bool Dissolve;            // bodies collapse into string resonances (10D)
    public WarpGrid Grid = WarpGrid.None;
    public double FunnelDepth = 0.06;   // funnel sheet depth (fraction of extent)

    public static WarpProfile For(int d)
    {
        var p = new WarpProfile { D = d };
        switch (d)
        {
            case 4:
                p.Title = "Hyperspace";
                p.Tag = "Rotation through the 4th spatial axis (ana/kata)";
                p.Science = "The Solar System is embedded in a fourth spatial axis, traditionally called ana and kata. " +
                    "The whole scene rotates continuously through the XW and YW planes, so every body periodically slides " +
                    "toward - or away from - our 3D slice: planets swell and shrink, orbit rings twist into flowing 4D curves, " +
                    "and a tesseract (the 4D cube, 16 vertices and 32 edges) frames the Sun. A 4D being would see all of this " +
                    "at once, exactly as we can see an entire flat drawing on a sheet of paper.";
                p.Accent = new SKColor(95, 232, 255);
                p.RotXW = 0.34; p.RotYW = 0.21; p.RotZW = 0.10;
                p.WSpread = 0.34;
                p.WHelixAmp = 0.20; p.WHelixK = 3; p.WHelixSpeed = 1.15;
                p.Tesseract = true;
                p.Grid = WarpGrid.Ripple;
                break;

            case 5:
                p.Title = "Kaluza-Klein Superposition";
                p.Tag = "Probability echoes over a fifth degree of freedom";
                p.Science = "A fifth dimension in the spirit of Kaluza and Klein, who unified gravity and electromagnetism " +
                    "by curling one extra coordinate into spacetime. Each planet now exists in several places at once: " +
                    "translucent probability echoes drift beside every world while two wave sources interfere on the " +
                    "spacetime sheet. Orbits split into superposed bands, as if the Solar System is exploring many possible " +
                    "histories simultaneously.";
                p.Accent = new SKColor(167, 139, 250);
                p.RotXW = 0.28; p.RotYW = 0.30; p.RotZW = 0.18;
                p.WSpread = 0.40;
                p.WHelixAmp = 0.15; p.WHelixK = 3; p.WHelixSpeed = 0.9;
                p.Ghosts = 3;
                p.Grid = WarpGrid.Interference;
                break;

            case 6:
                p.Title = "Phase Space";
                p.Tag = "World-lines on a curved funnel sheet";
                p.Science = "Six dimensions read as phase space: three axes of position plus three of momentum. " +
                    "The simulation now draws each body's world-line - its past trailing behind it through the warped sheet - " +
                    "while space bends into a deep funnel around the Sun, the classic embedding diagram of curved " +
                    "general-relativistic spacetime. Planets keep a faint future echo ahead of them on the funnel slope.";
                p.Accent = new SKColor(77, 216, 196);
                p.RotXW = 0.22; p.RotYW = 0.16; p.RotZW = 0.06;
                p.WSpread = 0.30;
                p.WHelixAmp = 0.18; p.WHelixK = 2; p.WHelixSpeed = 0.8;
                p.Trails = true;
                p.Ghosts = 1;
                p.Grid = WarpGrid.Funnel;
                p.FunnelDepth = 0.17;
                break;

            case 7:
                p.Title = "Compactification";
                p.Tag = "Calabi-Yau knots around every world";
                p.Science = "Beyond six dimensions the extra coordinates are thought to be compactified - curled up to " +
                    "sub-atomic size in intricate geometries. Every world is now wrapped in a rotating Calabi-Yau style " +
                    "knot, a miniature of the folded 6-dimensional shapes string theorists use to hide the extra dimensions. " +
                    "Space itself creases and folds, and each body oscillates with the harmonics of tightly curled dimensions.";
                p.Accent = new SKColor(244, 114, 208);
                p.RotXW = 0.25; p.RotYW = 0.34; p.RotZW = 0.22;
                p.WSpread = 0.44;
                p.WHelixAmp = 0.30; p.WHelixK = 5; p.WHelixSpeed = 1.2;
                p.Knots = true;
                p.WobbleAmp = 0.018; p.WobbleFast = 0.004;
                p.Grid = WarpGrid.Folded;
                break;

            case 8:
                p.Title = "E8 Lattice";
                p.Tag = "240 roots of the exceptional Lie group E8";
                p.Science = "Eight dimensions host E8, the largest exceptional Lie group: 248 symmetries built from 240 " +
                    "root vectors in an 8-dimensional lattice. Some physicists have proposed E8 as the seed of all particles " +
                    "and forces. The real 240-root projection surrounds the Sun as a golden mandala, bodies cast mirrored " +
                    "echoes through the origin, and the spacetime sheet quantizes into a lattice.";
                p.Accent = new SKColor(245, 200, 107);
                p.RotXW = 0.18; p.RotYW = 0.18; p.RotZW = 0.18;
                p.WSpread = 0.50;
                p.WHelixAmp = 0.22; p.WHelixK = 4; p.WHelixSpeed = 1.0;
                p.E8Lattice = true;
                p.Kaleido = true;
                p.WobbleAmp = 0.012;
                p.Grid = WarpGrid.Lattice;
                break;

            case 9:
                p.Title = "Spacetime Fold";
                p.Tag = "Folding space, wandering orbits, string halos";
                p.Science = "The brink of string theory's full 10-dimensional spacetime. Space folds like fabric so that " +
                    "distant regions of the Solar System can momentarily touch; orbits abandon closed ellipses for wandering " +
                    "Lissajous paths; and every body grows a vibrating halo. Matter is beginning to dissolve into pure " +
                    "oscillation - the last stop before the string realm.";
                p.Accent = new SKColor(255, 154, 92);
                p.RotXW = 0.30; p.RotYW = 0.26; p.RotZW = 0.34;
                p.WSpread = 0.52;
                p.WHelixAmp = 0.34; p.WHelixK = 7; p.WHelixSpeed = 1.4;
                p.Lissajous = true;
                p.Strings = true;
                p.WobbleAmp = 0.016;
                p.Grid = WarpGrid.Folded;
                break;

            case 10:
                p.Title = "Superstrings";
                p.Tag = "9 space + 1 time dimension - everything vibrates";
                p.Science = "Superstring reality: nine space dimensions plus one of time. Every particle is a tiny " +
                    "vibrating string, and the vibration mode decides what we call an electron, a quark or a photon. " +
                    "Accordingly every world in the Solar System dissolves into nested resonant loops above a dark core, " +
                    "the spacetime sheet ripples like a bundle of strings, and giant background loops sweep through the frame.";
                p.Accent = new SKColor(232, 244, 255);
                p.RotXW = 0.24; p.RotYW = 0.30; p.RotZW = 0.20;
                p.WSpread = 0.46;
                p.WHelixAmp = 0.28; p.WHelixK = 5; p.WHelixSpeed = 1.3;
                p.Strings = true;
                p.Dissolve = true;
                p.WobbleFast = 0.003;
                p.Grid = WarpGrid.Strings;
                break;
        }
        return p;
    }
}

/// <summary>
/// State machine + math for the dimension-warp view. Owns the active dimension,
/// an eased blend (0 = flat 3D, 1 = full N-D), a view-time accumulator, and the
/// core <see cref="Displace"/> function that throws any world point into the
/// selected hyperspace: 4D embedding, plane rotations, 4D-to-3D perspective,
/// then dimension-specific oscillation terms.
/// </summary>
public sealed class DimensionWarp
{
    public int Dim { get; private set; } = 3;      // dimension currently displayed (fades out)
    public double Blend { get; private set; }      // eased warp strength 0..1
    public double Time;                            // view-time accumulator (real seconds)
    public double Flash { get; private set; }      // transition flash, 1 -> 0

    private int _target = 3;
    private WarpProfile _profile = new() { D = 3 };

    public bool Active => Dim >= 4 && Blend > 0.004;
    public WarpProfile P => _profile;
    public int TargetDim => _target;

    public void SetDimension(int n)
    {
        n = Math.Clamp(n, 3, 10);
        if (n == _target) return;
        _target = n;
        if (n >= 4)
        {
            Dim = n;
            _profile = WarpProfile.For(n);
            Blend = Math.Min(Blend, 0.1);          // snap into the new look, flash covers the switch
            Flash = 1;
        }
    }

    public void Tick(double dt)
    {
        Time += dt;
        double goal = _target >= 4 ? 1.0 : 0.0;
        double rate = _target >= 4 ? 1.15 : 2.0;
        Blend += (goal - Blend) * Math.Min(1.0, dt * rate);
        if (goal == 0 && Blend < 0.005) { Blend = 0; Dim = 3; }
        if (goal == 1 && Blend > 0.995) Blend = 1;
        Flash = Math.Max(0, Flash - dt * 0.8);
    }

    private static double Ease(double x) => x * x * (3 - 2 * x);

    private static void Rot(ref double a, ref double b, double ang)
    {
        if (ang == 0) return;
        double c = Math.Cos(ang), s = Math.Sin(ang);
        double na = a * c - b * s;
        double nb = a * s + b * c;
        a = na; b = nb;
    }

    /// <summary>
    /// Displace one world point into the active hyperspace. Bodies and the samples
    /// of their orbit rings call this with the same group index, so a planet always
    /// sits exactly on its warped ring. The Sun (group 0 at the origin) stays anchored.
    /// </summary>
    public Vec3 Displace(Vec3 p, int group, double wPhase = 0)
    {
        if (!Active) return p;
        double k = Ease(Blend);
        double r = p.Length;
        if (r < 1e-6) return p;

        var pf = _profile;
        double u = Math.Atan2(p.Z, p.X) / (2 * Math.PI) + 0.5;   // azimuth fraction (ring-consistent)

        // --- embed the unit direction in 4D with a w offset, rotate through hyperspace planes ---
        double x = p.X / r, y = p.Y / r, z = p.Z / r;
        double w = pf.WSpread * (((group % 11) / 11.0) * 1.7 - 0.85)
                 + pf.WHelixAmp * Math.Sin(u * 2 * Math.PI * pf.WHelixK + Time * pf.WHelixSpeed + group * 0.83 + wPhase)
                 + 0.11 * Math.Sin(Time * 0.42 + group * 1.37);            // slow breathing through w
        Rot(ref x, ref w, Time * pf.RotXW);
        Rot(ref z, ref w, Time * pf.RotZW);
        Rot(ref y, ref w, Time * pf.RotYW);

        // --- 4D -> 3D perspective: w proximity swells or shrinks the 3D shadow ---
        double k4 = pf.ProjD / Math.Max(pf.ProjD - w * 0.5, 0.42);
        k4 = MathUtil.Clamp(k4, 0.62, 2.15);
        double swell = 1 + (k4 - 1) * k * pf.ScaleAmp;

        // bend the direction toward the rotated 3D projection; soft-normalize so a body
        // passing near the w-axis shrinks toward a point instead of vanishing
        double len3 = Math.Sqrt(x * x + y * y + z * z);
        if (len3 < 1e-6) len3 = 1e-6;
        double soft = 0.14 + 0.86 * Math.Min(len3, 1.0);
        double qx = x / len3 * soft * r * swell;
        double qy = y / len3 * soft * r * swell;
        double qz = z / len3 * soft * r * swell;

        // --- compactified vibration (7D+) ---
        if (pf.WobbleAmp > 0)
        {
            double a = r * pf.WobbleAmp * k, g = group;
            qx += a * Math.Sin(Time * 1.7 + g * 2.1);
            qy += a * 0.55 * Math.Sin(Time * 2.3 + g * 1.3);
            qz += a * Math.Sin(Time * 1.45 + g * 3.1);
        }
        if (pf.WobbleFast > 0)
        {
            double a = r * pf.WobbleFast * k;
            qx += a * Math.Sin(Time * 8.7 + group * 4.1);
            qy += a * Math.Sin(Time * 9.3 + group * 2.9);
            qz += a * Math.Sin(Time * 7.9 + group * 5.2);
        }

        // --- 9D: orbits stop closing and wander on Lissajous paths ---
        if (pf.Lissajous)
        {
            double a = r * 0.045 * k, g = group;
            qx += a * Math.Sin(Time * 0.31 + g * 2.1);
            qy += a * 0.4 * Math.Sin(Time * 0.23 + g * 0.9);
            qz += a * Math.Sin(Time * 0.27 + g * 3.3);
        }
        return new Vec3(qx, qy, qz);
    }
}
