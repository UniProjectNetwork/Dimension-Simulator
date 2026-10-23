using DimensionEngine.Maths;

namespace DimensionEngine.Sim;

/// <summary>
/// Keplerian orbit: computes true 3D position from classical orbital elements.
/// Solves Kepler's equation with Newton-Raphson each query (cheap: ≤6 iterations).
/// </summary>
public sealed class KeplerOrbit
{
    /// <summary>Semi-major axis in scene units (already scaled).</summary>
    public double A { get; }
    /// <summary>Eccentricity.</summary>
    public double E { get; }
    /// <summary>Inclination (rad).</summary>
    public double Inclination { get; }
    /// <summary>Longitude of ascending node (rad).</summary>
    public double AscendingNode { get; }
    /// <summary>Argument of periapsis (rad).</summary>
    public double PeriapsisArg { get; }
    /// <summary>Orbital period in simulated days.</summary>
    public double PeriodDays { get; }
    /// <summary>Mean anomaly at epoch (rad).</summary>
    public double MeanAnomaly0 { get; }

    public KeplerOrbit(double a, double e = 0, double inclinationDeg = 0,
        double ascendingNodeDeg = 0, double periapsisArgDeg = 0,
        double periodDays = 365.25, double meanAnomaly0Deg = 0)
    {
        A = a;
        E = e;
        Inclination = inclinationDeg * MathUtil.DegToRad;
        AscendingNode = ascendingNodeDeg * MathUtil.DegToRad;
        PeriapsisArg = periapsisArgDeg * MathUtil.DegToRad;
        PeriodDays = Math.Max(periodDays, 1e-6);
        MeanAnomaly0 = meanAnomaly0Deg * MathUtil.DegToRad;
    }

    /// <summary>Position at simulated time t (days), relative to the parent focus.</summary>
    public Vec3 PositionAt(double tDays)
    {
        double M = MeanAnomaly0 + 2 * Math.PI * (tDays / PeriodDays);
        M = M % (2 * Math.PI);
        double E_ = SolveEccentricAnomaly(M, E);

        double cosE = Math.Cos(E_), sinE = Math.Sin(E_);
        // position in orbital plane (perifocal frame)
        double xp = A * (cosE - E);
        double yp = A * Math.Sqrt(1 - E * E) * sinE;

        // rotate by argument of periapsis
        double cosw = Math.Cos(PeriapsisArg), sinw = Math.Sin(PeriapsisArg);
        double x1 = xp * cosw - yp * sinw;
        double y1 = xp * sinw + yp * cosw;

        // incline about X axis
        double cosi = Math.Cos(Inclination), sini = Math.Sin(Inclination);
        double y2 = y1 * cosi;
        double z2 = y1 * sini;

        // rotate by ascending node (about Z)
        double coso = Math.Cos(AscendingNode), sino = Math.Sin(AscendingNode);
        double x3 = x1 * coso - y2 * sino;
        double y3 = x1 * sino + y2 * coso;

        // Scene convention: Y is "up" — map ecliptic (x, y, z) → (x, z, y)
        return new Vec3(x3, z2, y3);
    }

    private static double SolveEccentricAnomaly(double M, double e)
    {
        double E = e < 0.8 ? M : Math.PI;
        for (int i = 0; i < 8; i++)
        {
            double f = E - e * Math.Sin(E) - M;
            double fp = 1 - e * Math.Cos(E);
            double dE = f / fp;
            E -= dE;
            if (Math.Abs(dE) < 1e-12) break;
        }
        return E;
    }

    /// <summary>Polyline of the full orbit for drawing (n points).</summary>
    public Vec3[] SamplePath(int n = 160)
    {
        var pts = new Vec3[n];
        for (int i = 0; i < n; i++)
        {
            double M = 2 * Math.PI * i / n;
            double E_ = SolveEccentricAnomaly(M, E);
            double cosE = Math.Cos(E_), sinE = Math.Sin(E_);
            double xp = A * (cosE - E);
            double yp = A * Math.Sqrt(1 - E * E) * sinE;

            double cosw = Math.Cos(PeriapsisArg), sinw = Math.Sin(PeriapsisArg);
            double x1 = xp * cosw - yp * sinw;
            double y1 = xp * sinw + yp * cosw;

            double cosi = Math.Cos(Inclination), sini = Math.Sin(Inclination);
            double y2 = y1 * cosi;
            double z2 = y1 * sini;

            double coso = Math.Cos(AscendingNode), sino = Math.Sin(AscendingNode);
            double x3 = x1 * coso - y2 * sino;
            double y3 = x1 * sino + y2 * coso;

            pts[i] = new Vec3(x3, z2, y3);
        }
        return pts;
    }

    /// <summary>Current speed (scene units per day) via vis-viva approximation.</summary>
    public double CurrentSpeedUnitsPerDay(double tDays)
    {
        double M = MeanAnomaly0 + 2 * Math.PI * (tDays / PeriodDays);
        M = M % (2 * Math.PI);
        double E_ = SolveEccentricAnomaly(M, E);
        double r = A * (1 - E * Math.Cos(E_));   // current radius
        // vis-viva in scene units: v^2 = GM(2/r - 1/a), with GM such that period holds:
        // T = 2π√(a³/GM) → GM = 4π²a³/T²
        double GM = 4 * Math.PI * Math.PI * A * A * A / (PeriodDays * PeriodDays);
        double v = Math.Sqrt(Math.Max(GM * (2.0 / Math.Max(r, 1e-9) - 1.0 / A), 0));
        return v;
    }
}
