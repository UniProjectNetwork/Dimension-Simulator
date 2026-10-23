using DimensionEngine.Maths;
using DimensionEngine.Render;
using DimensionEngine.Utils;
using DimensionEngine.Sim;
using SkiaSharp;

namespace DimensionEngine.Universe;

/// <summary>Real scientific data attached to every simulated object.</summary>
public sealed class CelestialInfo
{
    public string Type = "";                 // e.g. "G-type main-sequence star"
    public string MassText = "";             // e.g. "1.989 × 10^30 kg (1 M☉)"
    public string RadiusText = "";
    public string TempText = "";
    public string DistanceText = "";         // from Earth / parent
    public string SpectralText = "";
    public string GravityText = "";
    public string EscapeVelText = "";
    public string DayText = "";
    public string YearText = "";
    public string MoonsText = "";
    public string AtmosphereText = "";
    public string CompositionText = "";
    public string LuminosityText = "";
    public string AgeText = "";
    public string DiscoveryText = "";
    public string SchwarzschildText = "";    // computed for compact objects
    public string SizeText = "";             // physical extent (nebulae, galaxies)
    public string RotationText = "";         // spin rate (pulsars etc.)
    public string MagneticFieldText = "";
    public string StarCountText = "";
    public string[] Facts = Array.Empty<string>();
}

/// <summary>Base class for everything renderable in a scene.</summary>
public abstract class SpaceObject
{
    public string Id = "";
    public string Name = "";
    public string Category = "";             // browser grouping
    public CelestialInfo Info = new();
    public Vec3 Position;
    public double VisualRadius = 4;          // scene units
    public string EmTag = "";                // short tag line under name
    public int WarpGroup;                    // hyperspace w-group for the dimension warp view

    // last-frame screen state (for hit-testing & labels)
    public double ScreenX, ScreenY, PxRadius, ZView;
    public bool Visible;

    public abstract void RenderBody(RenderContext ctx, SphereShader shader);

    /// <summary>Optional pre-body pass (orbit lines, back rings...).</summary>
    public virtual void RenderBack(RenderContext ctx) { }

    /// <summary>Optional post-body pass (labels handled by scene, front rings, moons...).</summary>
    public virtual void RenderFront(RenderContext ctx) { }

    public virtual void Update(SimClock clock) { }

    /// <summary>Distance from camera to this object's surface, in scene units.</summary>
    public double DistanceFromCamera(Vec3 eye) => Math.Max(0, Position.DistanceTo(eye) - VisualRadius);
}

// ============================ STARS ============================

public class StarObject : SpaceObject
{
    public SKColor StarColor;
    public double CoronaScale = 3.2;
    public bool IsSun;

    public override void RenderBody(RenderContext ctx, SphereShader shader)
    {
        var (sx, sy, zv, front) = ctx.Project(Position);
        if (zv < VisualRadius * 0.75) { Visible = false; return; }   // camera inside/nearly touching
        Visible = front; ScreenX = sx; ScreenY = sy; ZView = zv;
        if (!front) return;
        double rPx = ctx.PixelRadius(VisualRadius, zv);
        PxRadius = rPx;

        // corona / glow
        ctx.DrawGlow(sx, sy, Math.Max(rPx * Math.Min(CoronaScale, 2.6), 5), StarColor, IsSun ? 1.0 : 0.8);
        ctx.DrawGlow(sx, sy, Math.Max(rPx * 1.6, 2.5), ColorUtil.WithAlpha(StarColor, 160), 1.2);

        var tex = PlanetTextureFactory.Get(BodyTextureKind.Sun);
        shader.Render(ctx, sx, sy, rPx, zv, tex, Vec3.UnitZ, ColorUtil.WithAlpha(StarColor, 0), emissive: true, tint: StarColor);
    }
}

public sealed class NeutronStarObject : StarObject
{
    public double BeamPhase;
    public override void Update(SimClock clock) => BeamPhase += clock.LastDt * 2.4;
    public override void RenderFront(RenderContext ctx)
    {
        if (!Visible) return;
        Effects.DrawPulsarBeams(ctx, ScreenX, ScreenY, Math.Max(PxRadius, 1.5), BeamPhase);
    }
}

// ============================ PLANETS & MOONS ============================

public class PlanetObject : SpaceObject
{
    public BodyTextureKind TextureKind = BodyTextureKind.RockyGeneric;
    public SKColor Atmosphere = SKColors.Transparent;
    public double AtmosphereStrength = 0.5;
    public double SpinPeriodDays = 1;        // for texture rotation phase
    public double AxialTiltDeg;
    public double Specular;                  // 0..1 (oceans)
    public Vec3? LightSourceOverride;        // for exoplanets lit by their own star
    public KeplerOrbit? Orbit;
    public Vec3 OrbitCenter = Vec3.Zero;
    public PlanetObject? OrbitCenterRef;   // if set, follows this parent's position each frame
    public StarObject? OrbitCenterStar;    // for exoplanets orbiting a star in deep space
    public double SpinPhase;

    // rings
    public bool HasRings;
    public double RingInner = 1.4, RingOuter = 2.3, RingTiltDeg = 26;

    public override void Update(SimClock clock)
    {
        if (OrbitCenterRef != null)
            OrbitCenter = OrbitCenterRef.Position;
        else if (OrbitCenterStar != null)
            OrbitCenter = OrbitCenterStar.Position;
        if (Orbit != null)
            Position = OrbitCenter + Orbit.PositionAt(clock.ElapsedDays);
        SpinPhase += (clock.Paused ? 0 : clock.LastDt * clock.DaysPerSecond) / Math.Max(SpinPeriodDays, 1e-6) / 2 * Math.PI;
        SpinPhase %= 2 * Math.PI;
    }

    public override void RenderBack(RenderContext ctx)
    {
        if (Orbit != null && ctx.ShowOrbits)
        {
            var path = Orbit.SamplePath(140);
            var w = ctx.Warp;
            if (w != null && w.Active)
            {
                // orbit thrown into hyperspace: every sample warped with the body's group
                var pts = new Vec3[path.Length];
                for (int i = 0; i < path.Length; i++)
                    pts[i] = w.Displace(OrbitCenter + path[i], WarpGroup);
                Effects.DrawOrbitPath(ctx, pts, new SKColor(120, 165, 215, 72), 1f);

                // 5D+: a superposed probability band - the same orbit with shifted w phase
                if (w.P.Ghosts > 0)
                {
                    var pts2 = new Vec3[path.Length];
                    for (int i = 0; i < path.Length; i++)
                        pts2[i] = w.Displace(OrbitCenter + path[i] * 1.035, WarpGroup, Math.PI * 0.9);
                    Effects.DrawOrbitPath(ctx, pts2, new SKColor(w.P.Accent.Red, w.P.Accent.Green, w.P.Accent.Blue, (byte)(70 * w.Blend)), 1f);
                }
            }
            else
            {
                var pts = new Vec3[path.Length];
                for (int i = 0; i < path.Length; i++) pts[i] = OrbitCenter + path[i];
                byte ringA = (byte)(ctx.Warp?.P.Dissolve == true ? 40 : 66);   // rings fade at 10D: strings take over
                Effects.DrawOrbitPath(ctx, pts, new SKColor(120, 150, 190, ringA), 1f);
            }
        }
        // ring back half — drawn before the planet body for occlusion
        if (HasRings)
        {
            var (sx, sy, zv, front) = ctx.Project(Position);
            if (front && zv > VisualRadius)
            {
                double rPx = ctx.PixelRadius(VisualRadius, zv);
                Effects.DrawRings(ctx, sx, sy, rPx, RingInner, RingOuter, RingTiltDeg, back: true);
            }
        }
    }

    public override void RenderBody(RenderContext ctx, SphereShader shader)
    {
        var (sx, sy, zv, front) = ctx.Project(Position);
        if (zv < VisualRadius * 0.75) { Visible = false; return; }
        Visible = front; ScreenX = sx; ScreenY = sy; ZView = zv;
        if (!front) return;
        double rPx = ctx.PixelRadius(VisualRadius, zv);
        PxRadius = rPx;

        var lightDir = ComputeLightDir(ctx);
        var tex = PlanetTextureFactory.Get(TextureKind);
        var dotCol = Atmosphere.Alpha > 40 ? Atmosphere : PlanetTextureFactory.GetAverage(TextureKind);
        shader.Render(ctx, sx, sy, rPx, zv, tex, lightDir, Atmosphere, false, 0.14, Specular, SpinPhase, dotColor: dotCol);
    }

    protected Vec3 ComputeLightDir(RenderContext ctx)
    {
        if (OrbitCenterStar != null)
            return (OrbitCenterStar.Position - Position).Normalized();
        if (LightSourceOverride != null)
            return (LightSourceOverride.Value - Position).Normalized();
        return (Vec3.Zero - Position).Normalized(); // sun at scene origin by default
    }

    public override void RenderFront(RenderContext ctx)
    {
        if (!Visible) return;
        if (HasRings)
        {
            Effects.DrawRings(ctx, ScreenX, ScreenY, PxRadius, RingInner, RingOuter, RingTiltDeg, back: false);
        }
        if (Atmosphere.Alpha > 0 && PxRadius >= 5)
        {
            // soft outer halo
            ctx.DrawGlow(ScreenX, ScreenY, PxRadius * 1.35, ColorUtil.WithAlpha(Atmosphere, (byte)(70 * AtmosphereStrength)), 0.55);
        }
    }

    public void RenderRingBack(RenderContext ctx)
    {
        if (!Visible || !HasRings) return;
        Effects.DrawRings(ctx, ScreenX, ScreenY, PxRadius, RingInner, RingOuter, RingTiltDeg, back: true);
    }
}

public sealed class MoonObject : PlanetObject
{
    public PlanetObject Parent = null!;
}

// ============================ BLACK HOLES ============================

public sealed class BlackHoleObject : SpaceObject
{
    public double MassSolarMasses = 10;
    public double DiskInner = 1.45, DiskOuter = 4.2;
    public double DiskTiltDeg = 12;
    public bool Jets;
    public double DiskPhase;
    public double SpinSpeed = 0.35;   // rad/s of disk swirl

    public override void Update(SimClock clock) => DiskPhase += SpinSpeed * clock.LastDt;

    public override void RenderBody(RenderContext ctx, SphereShader shader)
    {
        var (sx, sy, zv, front) = ctx.Project(Position);
        Visible = front; ScreenX = sx; ScreenY = sy; ZView = zv;
        if (!front) return;
        double rPx = ctx.PixelRadius(VisualRadius, zv);
        PxRadius = rPx;
        double jetLen = ctx.PixelRadius(VisualRadius * 6, zv);
        Effects.DrawBlackHole(ctx, sx, sy, rPx, DiskInner, DiskOuter, DiskTiltDeg, DiskPhase, Jets, jetLen);
    }
}

// ============================ NEBULAE & GALAXIES ============================

public sealed class NebulaObject : SpaceObject
{
    public int Seed;
    public SKColor ColorA = new(180, 60, 160);
    public SKColor ColorB = new(40, 160, 190);
    public SKBitmap? Sprite;

    public override void RenderBody(RenderContext ctx, SphereShader shader)
    {
        Sprite ??= Effects.GetNebulaSprite(Seed, ColorA, ColorB);
        var (sx, sy, zv, front) = ctx.Project(Position);
        Visible = front; ScreenX = sx; ScreenY = sy; ZView = zv;
        if (!front) return;
        double rPx = ctx.PixelRadius(VisualRadius, zv);
        PxRadius = rPx;
        double alpha = MathUtil.Clamp(ctx.FadeAlpha(zv, 0, 260000), 0.06, 1);
        Effects.DrawNebula(ctx, Sprite, sx, sy, rPx, alpha);
    }
}

public sealed class GalaxyObject : SpaceObject
{
    public Effects.GalaxyParticles Particles = null!;
    public double Spin;
    public double SpinRate = 0.0006;  // rad per sim-day

    public override void Update(SimClock clock) => Spin = clock.ElapsedDays * SpinRate;

    public override void RenderBody(RenderContext ctx, SphereShader shader)
    {
        var (sx, sy, zv, front) = ctx.Project(Position);
        Visible = front; ScreenX = sx; ScreenY = sy; ZView = zv;
        if (!front) return;
        double rPx = ctx.PixelRadius(VisualRadius, zv);
        PxRadius = rPx;
        if (rPx >= 2.5)
            Effects.DrawGalaxy(ctx, Particles, Position, VisualRadius, Spin);
        else
            ctx.DrawGlow(sx, sy, Math.Max(rPx, 2.2), new SKColor(225, 220, 255), 0.6);
    }
}

// ============================ BELTS ============================

public sealed class BeltSystem
{
    public string Name = "Asteroid Belt";
    public double InnerAu, OuterAu;
    public double UnitsPerAu;
    public int Count = 420;
    private double[] _a = Array.Empty<double>();
    private double[] _phase = Array.Empty<double>();
    private double[] _incl = Array.Empty<double>();
    private double[] _period = Array.Empty<double>();

    public void Build(int seed)
    {
        var rng = Noise.Rng(seed);
        _a = new double[Count]; _phase = new double[Count];
        _incl = new double[Count]; _period = new double[Count];
        for (int i = 0; i < Count; i++)
        {
            _a[i] = MathUtil.Lerp(InnerAu, OuterAu, rng.NextDouble());
            _phase[i] = rng.NextDouble() * Math.PI * 2;
            _incl[i] = (rng.NextDouble() - 0.5) * 0.22;
            // Kepler's 3rd law: T ∝ a^1.5 (years)
            _period[i] = Math.Pow(_a[i], 1.5) * 365.25;
        }
    }

    public void Render(RenderContext ctx, double simDays, double beltVisRadius)
    {
        if (_a.Length == 0) Build(777);
        var canvas = ctx.Canvas;
        var w = ctx.Warp;
        bool warpActive = w != null && w.Active;
        using var p = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.SrcOver };
        for (int i = 0; i < Count; i++)
        {
            double ang = _phase[i] + 2 * Math.PI * (simDays / _period[i]);
            double r = _a[i] * UnitsPerAu;
            double x = Math.Cos(ang) * r;
            double z = Math.Sin(ang) * r;
            double y = Math.Sin(_incl[i]) * r * 0.18;
            var p3 = new Vec3(x, y, z);
            if (warpActive) p3 = w!.Displace(p3, 1);   // belt shares one coherent w-group
            var (sx, sy, zv, front) = ctx.Project(p3);
            if (!front) continue;
            double fade = ctx.FadeAlpha(zv, 2000, 60000);
            if (fade < 0.05) continue;
            p.Color = warpActive
                ? new SKColor(w!.P.Accent.Red, w.P.Accent.Green, w.P.Accent.Blue, (byte)(110 * fade * w.Blend))
                : new SKColor(150, 145, 135, (byte)(130 * fade));
            canvas.DrawCircle((float)sx, (float)sy, 0.9f, p);
        }
    }
}
