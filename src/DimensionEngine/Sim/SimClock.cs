using DimensionEngine.Maths;

namespace DimensionEngine.Sim;

/// <summary>
/// Simulation clock: advances simulated time by a configurable speed
/// (simulated days per real second), spanning hours to millions of years per second.
/// </summary>
public sealed class SimClock
{
    /// <summary>Simulated time since app start, in days.</summary>
    public double ElapsedDays { get; private set; }

    /// <summary>Simulated days advanced per real second. 0 ≈ paused.</summary>
    public double DaysPerSecond { get; set; } = 2.0;

    public bool Paused { get; set; }

    /// <summary>Wall-clock start reference for the display date (J2000.0 = 2000-01-01 12:00 TT).</summary>
    public static readonly DateTime Epoch = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public DateTime DisplayDate => Epoch.AddDays(ElapsedDays);

    /// <summary>Last real frame delta (seconds), stored for per-object animation.</summary>
    public double LastDt { get; private set; }

    /// <summary>Simulated elapsed time in Julian years.</summary>
    public double ElapsedYears => ElapsedDays / 365.25;

    /// <summary>Reset to the J2000.0 epoch.</summary>
    public void Reset() => ElapsedDays = 0;

    /// <summary>Advance the clock.</summary>
    public void Tick(double realDtSeconds)
    {
        LastDt = realDtSeconds;
        if (Paused || DaysPerSecond == 0) return;
        ElapsedDays += DaysPerSecond * realDtSeconds;
    }

    /// <summary>Human-readable speed label.</summary>
    public string SpeedLabel
    {
        get
        {
            double d = DaysPerSecond;
            return d switch
            {
                < 1.0 / 24.0 => $"{d * 24 * 60:F0} sim-min / s",
                < 1.0 => $"{d * 24:F1} sim-hours / s",
                < 60 => $"{d:F1} sim-days / s",
                < 365.25 => $"{d / 30.44:F1} sim-months / s",
                < 365.25 * 1000 => $"{d / 365.25:F1} sim-years / s",
                < 365.25e6 => $"{d / 365.25e3:F0} kyr / s",
                _ => $"{d / 365.25e6:F1} Myr / s"
            };
        }
    }

    /// <summary>Map a UI slider 0..1 to log-scaled speed: 2 minutes/s → 1 Myr/s.</summary>
    public static double SpeedFromSlider01(double t)
    {
        t = MathUtil.Clamp(t, 0, 1);
        double minPerSec = 2.0;
        double myrPerSec = 1.0;
        double daysMin = minPerSec / 1440.0;
        double daysMax = myrPerSec * 365.25e6;
        double d = daysMin * Math.Pow(daysMax / daysMin, t);
        return d;
    }

    /// <summary>Inverse of SpeedFromSlider01.</summary>
    public static double SliderFromSpeed(double daysPerSecond)
    {
        double minPerSec = 2.0;
        double myrPerSec = 1.0;
        double daysMin = minPerSec / 1440.0;
        double daysMax = myrPerSec * 365.25e6;
        double t = Math.Log(daysPerSecond / daysMin) / Math.Log(daysMax / daysMin);
        return MathUtil.Clamp(t, 0, 1);
    }
}
