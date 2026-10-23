using DimensionEngine.Camera;
using DimensionEngine.Maths;
using DimensionEngine.ND;
using DimensionEngine.Render;
using DimensionEngine.Scenes;
using DimensionEngine.Sim;
using DimensionEngine.Universe;
using SkiaSharp;

namespace DimensionEngine;

public enum ViewMode { SolarSystem, DeepSpace, HigherDimensions }

/// <summary>
/// Facade driving the whole simulation: owns the clock, camera, scenes and selection;
/// exposes input (orbit/zoom/pan/pick), search, and frame rendering to a Skia canvas.
/// </summary>
public sealed class SimulationEngine
{
    public SimClock Clock { get; } = new();
    public OrbitCamera Cam { get; } = new();
    public SolarSystemScene Solar { get; }
    public DeepSpaceScene Deep { get; }
    public DimensionsScene Dims { get; }

    public ViewMode Mode { get; private set; } = ViewMode.SolarSystem;
    public SpaceObject? Selected { get; set; }

    /// <summary>
    /// Dimension warp state (3 = flat space, 4..10 = hyperspace). One shared state
    /// machine drives BOTH the Solar System and the Deep Space view.
    /// </summary>
    public DimensionWarp Warp { get; } = new();

    /// <summary>Throw the current view into n-dimensional space (n = 3 restores flat space).</summary>
    public void SetWarp(int n) => Warp.SetDimension(n);
    public int WarpDim => Warp.TargetDim;

    public bool ShowOrbits
    {
        get => _showOrbits;
        set { _showOrbits = value; }
    }
    private bool _showOrbits = true;

    public SimulationEngine()
    {
        Solar = new SolarSystemScene();
        Deep = new DeepSpaceScene();
        Dims = new DimensionsScene();
        ApplySceneDefaults();
    }

    public Scene CurrentScene => Mode switch
    {
        ViewMode.SolarSystem => Solar,
        ViewMode.DeepSpace => Deep,
        _ => Dims
    };

    public void SetMode(ViewMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        Selected = null;
        ApplySceneDefaults();
    }

    private void ApplySceneDefaults()
    {
        var s = CurrentScene;
        Cam.MinDistance = Mode switch { ViewMode.SolarSystem => 6, ViewMode.DeepSpace => 3, _ => 100 };
        Cam.MaxDistance = Mode switch { ViewMode.SolarSystem => 26000, ViewMode.DeepSpace => 4000, _ => 3000 };
        Cam.SetImmediate(s.DefaultCameraTarget, s.DefaultCameraDistance);
        if (Mode == ViewMode.SolarSystem)
            Cam.SetImmediate(s.DefaultCameraTarget, s.DefaultCameraDistance, yawDeg: 40, pitchDeg: 28);
        else if (Mode == ViewMode.DeepSpace)
            Cam.SetImmediate(s.DefaultCameraTarget, s.DefaultCameraDistance, yawDeg: 25, pitchDeg: 42);
    }

    public void Tick(double realDt)
    {
        Clock.Tick(realDt);
        Cam.Tick(realDt);
        Warp.Tick(Clock.LastDt);        // warp evolves in every mode, so views stay in sync
        CurrentScene.Update(Clock);
    }

    // ---------------- input ----------------

    public void Orbit(double dx, double dy) => Cam.Orbit(dx, dy);
    public void ZoomBy(double factor) => Cam.ZoomBy(factor);
    public void Pan(double dx, double dy, double viewportH) => Cam.Pan(dx, dy, viewportH);

    /// <summary>Find the object under a screen point (nearest within its radius or 14 px).</summary>
    public SpaceObject? Pick(double x, double y)
    {
        SpaceObject? best = null;
        double bestD = double.MaxValue;
        foreach (var o in CurrentScene.Objects)
        {
            if (!o.Visible) continue;
            double dx = o.ScreenX - x, dy = o.ScreenY - y;
            double d = Math.Sqrt(dx * dx + dy * dy);
            double hitR = Math.Max(o.PxRadius + 6, 14);
            if (d <= hitR && d < bestD) { bestD = d; best = o; }
        }
        return best;
    }

    public void Select(SpaceObject? o)
    {
        Selected = o;
    }

    public void FlyTo(SpaceObject o)
    {
        double dist = Math.Max(o.VisualRadius * 5.2, o.VisualRadius + 1.5);
        if (o is GalaxyObject) dist = o.VisualRadius * 2.6;
        if (o is NebulaObject) dist = o.VisualRadius * 2.8;
        Cam.FlyTo(o.Position, dist);
    }

    public void FlyToObjectById(string id)
    {
        var all = Solar.Objects.Concat(Deep.Objects);
        var target = all.FirstOrDefault(o => o.Id == id);
        if (target == null) return;
        if (Solar.Objects.Contains(target) && Mode != ViewMode.SolarSystem) SetMode(ViewMode.SolarSystem);
        else if (Deep.Objects.Contains(target) && Mode != ViewMode.DeepSpace) SetMode(ViewMode.DeepSpace);
        Select(target);
        FlyTo(target);
    }

    public List<SpaceObject> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<SpaceObject>();
        var all = Solar.Objects.Concat(Deep.Objects).Distinct();
        return all
            .Where(o => o.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || o.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || o.EmTag.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(24)
            .ToList();
    }

    // ---------------- rendering ----------------

    private SKBitmap? _frame;
    private int _fw, _fh;

    /// <summary>Render the current frame into an SKBitmap (reused buffer), and return it.</summary>
    public SKBitmap RenderFrame(int width, int height, float dpiScale = 1f)
    {
        width = Math.Max(64, width);
        height = Math.Max(64, height);

        if (_frame == null || _fw != width || _fh != height)
        {
            _frame?.Dispose();
            _frame = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _fw = width; _fh = height;
        }

        using var surface = SKSurface.Create(_frame.PeekPixels());
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(4, 5, 12));

        // deep-space background gradient + vignette-ish
        using (var bg = new SKPaint())
        {
            bg.Shader = SKShader.CreateRadialGradient(
                new SKPoint(width * 0.5f, height * 0.42f), Math.Max(width, height) * 0.85f,
                new[] { new SKColor(13, 16, 34), new SKColor(4, 5, 12) },
                new[] { 0f, 1f }, SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, width, height, bg);
        }

        var view = Cam.ViewMatrix();
        double aspect = width / (double)height;
        double far = Mode switch { ViewMode.SolarSystem => 400000, ViewMode.DeepSpace => 800000, _ => 10000 };
        var proj = Cam.ProjMatrix(aspect, 0.05, far);

        var ctx = new RenderContext
        {
            Canvas = canvas,
            Cam = Cam,
            View = view,
            Proj = proj,
            Width = width,
            Height = height,
            Dt = Clock.LastDt,
            SimDays = Clock.ElapsedDays,
            TimeScaleDaysPerSec = Clock.DaysPerSecond,
            ScaleFactor = dpiScale,
            ShowOrbits = ShowOrbits,
            Warp = Mode is ViewMode.SolarSystem or ViewMode.DeepSpace ? Warp : null
        };

        if (Mode != ViewMode.HigherDimensions)
            Effects.DrawStarfield(ctx);

        if (Mode == ViewMode.HigherDimensions)
        {
            Dims.Render(ctx, Clock, null);
        }
        else
        {
            // draw selected's ring-back before its body handled inside objects; selection ring after
            CurrentScene.Render(ctx, Clock, Selected);
        }

        return _frame;
    }

    /// <summary>Render one frame and encode as PNG (for RenderTest & screenshots).</summary>
    public byte[] ScreenshotPng(int width, int height, float dpi = 1f)
    {
        var bmp = RenderFrame(width, height, dpi);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 92);
        return data.ToArray();
    }

    // ---------------- science readouts ----------------

    /// <summary>Convert camera→target distance into real-world readable text for the current mode.</summary>
    public string DistanceReadout()
    {
        double d = Cam.Distance;
        return Mode switch
        {
            ViewMode.SolarSystem => FormatDistanceKm(d / CatalogSolarSystem.AU * 149_597_870.7, allowAu: true),
            ViewMode.DeepSpace => LightYearsFromMap(d),
            _ => $"{d:F0} abstract units"
        };
    }

    private static string LightYearsFromMap(double units)
    {
        // the logarithmic map spans 0 .. ~342 units (0 .. 13.5 Gly, farthest object GN-z11);
        // beyond that radius the log scale simply has no inverse, so cap the readout
        if (units > 36 * Math.Log10(1 + 13.5e9 / 4.2465))
            return "> 13.5 billion light-years (beyond map)";
        double ly = 4.2465 * (Math.Pow(10, units / 36.0) - 1);
        if (ly < 0.01) return $"{ly * 63241:F0} AU";
        if (ly < 1) return $"{ly * 63241:F0} AU";
        if (ly < 1000) return $"{ly:F1} light-years";
        if (ly < 1e6) return $"{ly / 1000:F1}k light-years";
        if (ly < 1e9) return $"{ly / 1e6:F2} million ly";
        return $"{ly / 1e9:F2} billion light-years";
    }

    public static string FormatDistanceKm(double km, bool allowAu = false)
    {
        if (allowAu && km > 0.05 * 149_597_870.7)
        {
            double au = km / 149_597_870.7;
            return au < 0.01 ? $"{km:N0} km" : $"{au:N2} AU ({LightTime(km)})";
        }
        return $"{km:N0} km ({LightTime(km)})";
    }

    public static string LightTime(double km)
    {
        double s = km / 299792.458;
        if (s < 1) return $"{s * 1000:F0} light-ms";
        if (s < 60) return $"{s:F1} light-sec";
        if (s < 3600) return $"{s / 60:F1} light-min";
        if (s < 86400) return $"{s / 3600:F1} light-hours";
        if (s < 86400 * 365.25) return $"{s / 86400:F1} light-days";
        return $"{s / (86400 * 365.25):F2} light-years";
    }
}
