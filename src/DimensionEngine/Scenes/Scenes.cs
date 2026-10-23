using DimensionEngine.Maths;
using DimensionEngine.Render;
using DimensionEngine.Utils;
using DimensionEngine.Sim;
using DimensionEngine.ND;
using DimensionEngine.Universe;
using SkiaSharp;

namespace DimensionEngine.Scenes;

/// <summary>Base class for simulation views.</summary>
public abstract class Scene
{
    public string Name = "";
    public abstract List<SpaceObject> Objects { get; }
    public Vec3 DefaultCameraTarget = Vec3.Zero;
    public double DefaultCameraDistance = 1000;

    public virtual void Update(SimClock clock)
    {
        foreach (var o in Objects) o.Update(clock);
    }

    public virtual void Render(RenderContext ctx, SimClock clock, SpaceObject? selected)
    {
        // depth sort by camera distance (far first)
        var eye = ctx.Cam.Eye;
        var ordered = Objects.OrderByDescending(o => o.Position.DistanceTo(eye)).ToList();

        foreach (var o in ordered) o.RenderBack(ctx);
        foreach (var o in ordered)
        {
            o.RenderBody(ctx, ctx.Shader);
            o.RenderFront(ctx);
        }
        if (selected != null) DrawSelection(ctx, selected);
        DrawLabels(ctx, ordered, selected);
    }

    protected virtual void DrawLabels(RenderContext ctx, List<SpaceObject> ordered, SpaceObject? selected)
    {
        foreach (var o in ordered)
        {
            if (!o.Visible) continue;
            bool isSel = selected == o;
            // label policy: show if big enough, important, or selected
            double rPx = o.PxRadius;
            bool show = isSel || rPx > 26 || (rPx > 5 && o.Category is not "Moon") || (rPx > 2.5 && o.Category is "Planet" or "Star" or "Galaxy");
            if (!show) continue;
            float y = (float)(o.ScreenY - Math.Max(o.PxRadius, 3) - 22 * ctx.ScaleFactor);
            LabelRenderer.Draw(ctx, o.Name, o.EmTag, (float)o.ScreenX, y, isSel);
        }
    }

    protected void DrawSelection(RenderContext ctx, SpaceObject o)
    {
        if (!o.Visible) return;
        float r = (float)Math.Max(o.PxRadius + 10, 14);
        using var p = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.6f * ctx.ScaleFactor,
            Color = new SKColor(120, 235, 255, 220)
        };
        var rect = SKRect.Create((float)o.ScreenX - r, (float)o.ScreenY - r, r * 2, r * 2);
        ctx.Canvas.DrawRoundRect(rect, r * 0.35f, r * 0.35f, p);

        // corner ticks
        p.StrokeWidth = 2.4f * ctx.ScaleFactor;
        DrawTick(ctx, o.ScreenX - r, o.ScreenY - r, 1, 1, r * 0.32f, p);
        DrawTick(ctx, o.ScreenX + r, o.ScreenY - r, -1, 1, r * 0.32f, p);
        DrawTick(ctx, o.ScreenX - r, o.ScreenY + r, 1, -1, r * 0.32f, p);
        DrawTick(ctx, o.ScreenX + r, o.ScreenY + r, -1, -1, r * 0.32f, p);
    }
    private static void DrawTick(RenderContext ctx, double x, double y, int dx, int dy, double len, SKPaint p)
    {
        ctx.Canvas.DrawLine((float)x, (float)(y + dy * len), (float)(x + dx * len), (float)y, p);
    }
}

/// <summary>The Solar System view — with a full dimension-warp mode (4D..10D).</summary>
public sealed class SolarSystemScene : Scene
{
    public BeltSystem AsteroidBelt = new() { Name = "Asteroid Belt", InnerAu = 2.06, OuterAu = 3.4, UnitsPerAu = CatalogSolarSystem.AU, Count = 460 };
    public BeltSystem KuiperBelt = new() { Name = "Kuiper Belt", InnerAu = 30, OuterAu = 49, UnitsPerAu = CatalogSolarSystem.AU, Count = 700 };

    // world-line history for the 6D phase-space view
    private readonly Dictionary<string, Queue<Vec3>> _trails = new();

    public SolarSystemScene()
    {
        Name = "Solar System";
        ObjectsList = CatalogSolarSystem.Build();
        CatalogSolarSystem.LinkMoons(ObjectsList);
        AsteroidBelt.Build(1234);
        KuiperBelt.Build(5678);
        DefaultCameraTarget = Vec3.Zero;
        DefaultCameraDistance = 2900;
        AssignWarpGroups();
    }

    private void AssignWarpGroups()
    {
        int g = 1;
        foreach (var o in ObjectsList)
        {
            switch (o)
            {
                case StarObject: o.WarpGroup = 0; break;                       // Sun anchored at origin
                case MoonObject m when m.Parent != null: o.WarpGroup = m.Parent.WarpGroup; break;
                default: o.WarpGroup = g++; break;
            }
        }
    }

    private List<SpaceObject> ObjectsList;
    public override List<SpaceObject> Objects => ObjectsList;

    public override void Update(SimClock clock)
    {
        base.Update(clock);   // warp is ticked by SimulationEngine (shared with Deep Space)
    }

    public override void Render(RenderContext ctx, SimClock clock, SpaceObject? selected)
    {
        var warp = ctx.Warp;
        if (warp == null || !warp.Active)
        {
            AsteroidBelt.Render(ctx, clock.ElapsedDays, 1);
            KuiperBelt.Render(ctx, clock.ElapsedDays, 1);
            base.Render(ctx, clock, selected);
            return;
        }

        var pf = warp.P;
        double ext = 34 * CatalogSolarSystem.AU;   // Neptune-ish frame for the spacetime sheet

        // 1. warped spacetime sheet behind everything
        DimensionFX.DrawSpacetimeGrid(ctx, warp, ext);

        // 2. asteroid + Kuiper belts (displaced internally via ctx.Warp)
        AsteroidBelt.Render(ctx, clock.ElapsedDays, 1);
        KuiperBelt.Render(ctx, clock.ElapsedDays, 1);

        // 3. throw every body into hyperspace (remember each body's flat position,
        //    keyed by object so the later depth-sort cannot scramble the restore)
        var ordered = new List<SpaceObject>(Objects);
        var flat = new Dictionary<SpaceObject, Vec3>(ordered.Count);
        foreach (var o in ordered)
        {
            flat[o] = o.Position;
            o.Position = warp.Displace(o.Position, o.WarpGroup);
        }
        ordered.Sort((a, b) => b.Position.DistanceTo(ctx.Cam.Eye).CompareTo(a.Position.DistanceTo(ctx.Cam.Eye)));

        // 4. world-lines (6D phase space): record + draw behind everything else
        if (pf.Trails)
        {
            foreach (var o in ordered)
            {
                if (o is StarObject) continue;
                if (!_trails.TryGetValue(o.Id, out var q)) _trails[o.Id] = q = new Queue<Vec3>();
                q.Enqueue(o.Position);
                while (q.Count > 110) q.Dequeue();
            }
            foreach (var o in ordered)
                if (_trails.TryGetValue(o.Id, out var q))
                    DimensionFX.DrawTrail(ctx, q.ToArray(), pf.Accent, warp.Blend);
        }
        else if (_trails.Count > 0) _trails.Clear();

        // 5. orbit paths — warped inside PlanetObject.RenderBack
        foreach (var o in ordered) o.RenderBack(ctx);

        // 6. probability ghosts behind bodies (5D+)
        foreach (var o in ordered)
            if (o is PlanetObject po)
                DimensionFX.DrawGhosts(ctx, warp, po, GhostTint(po, pf));

        // 7. bodies — at 10D every world dissolves into a string resonance
        foreach (var o in ordered)
        {
            if (pf.Dissolve)
            {
                if (o is StarObject s) { DimensionFX.DrawStringStar(ctx, s, pf, warp.Blend); continue; }
                if (o is PlanetObject pl) { DimensionFX.DrawStringCore(ctx, pl, pf, warp.Blend); continue; }
            }
            o.RenderBody(ctx, ctx.Shader);
            o.RenderFront(ctx);
        }

        // 8. signature dimension effects
        if (pf.Tesseract)
        {
            DimensionFX.DrawTesseract(ctx, Vec3.Zero, ext * 0.055, warp.Time, pf.Accent, 165 * warp.Blend);
            if (selected != null && selected.Visible)
                DimensionFX.DrawTesseract(ctx, selected.Position, Math.Max(selected.VisualRadius * 3.0, 60),
                    warp.Time * 1.3, pf.Accent, 120 * warp.Blend);
        }
        if (pf.Knots)
            foreach (var o in ordered)
                if (o is PlanetObject { Visible: true } pk && pk.PxRadius > 1)
                    DimensionFX.DrawCalabiKnot(ctx, pk.ScreenX, pk.ScreenY, Math.Max(pk.PxRadius * 2.15, 12),
                        warp.Time + pk.WarpGroup, pf.Accent, warp.Blend);
        if (pf.E8Lattice)
        {
            var (esx, esy, ezv, efront) = ctx.Project(Vec3.Zero);
            if (efront) DimensionFX.DrawE8(ctx, esx, esy, ctx.PixelRadius(ext * 0.135, ezv), warp.Time, pf.Accent, warp.Blend);
        }
        if (pf.Strings)
        {
            // giant background string loops sweeping the frame (kept subtle)
            for (int i = 0; i < 2; i++)
                DimensionFX.DrawStringLoop(ctx, ctx.Width * (0.34 + 0.3 * i), ctx.Height * (0.46 + 0.08 * ((i + 1) % 2)),
                    ctx.Height * (0.34 + 0.08 * i), warp.Time * (0.5 + 0.2 * i) + i * 2.1,
                    pf.Accent, warp.Blend * 0.5, 0.13, 1.0f, 15, 0.32);
            // halos around bodies (9D) / resonant strings (10D)
            foreach (var o in ordered)
            {
                if (o is not PlanetObject { Visible: true } ps || ps.PxRadius <= 0.5) continue;
                double r = Math.Max(ps.PxRadius * (pf.Dissolve ? 2.9 : 2.1), 16);
                var tint = GhostTint(ps, pf);
                DimensionFX.DrawStringLoop(ctx, ps.ScreenX, ps.ScreenY, r, warp.Time + ps.WarpGroup * 1.7, tint, warp.Blend, 0.17, 1.3f, 190);
                if (pf.Dissolve)
                    DimensionFX.DrawStringLoop(ctx, ps.ScreenX, ps.ScreenY, r * 0.66, warp.Time * 1.35 + ps.WarpGroup, tint, warp.Blend, 0.22, 1.1f, 150, 0.3);
            }
            // the Sun becomes a double string resonance
            var sun = ordered.FirstOrDefault(o => o is StarObject { IsSun: true }) as StarObject;
            if (sun != null && sun.Visible)
            {
                double r = Math.Max(sun.PxRadius * 2.2, 30);
                DimensionFX.DrawStringLoop(ctx, sun.ScreenX, sun.ScreenY, r, warp.Time, sun.StarColor, warp.Blend, 0.14, 1.5f, 200, 0.5);
                DimensionFX.DrawStringLoop(ctx, sun.ScreenX, sun.ScreenY, r * 1.45, warp.Time * 0.8 + 1.3, pf.Accent, warp.Blend * 0.8, 0.18, 1.2f, 130, 0.42);
            }
        }

        // 9. restore flat world positions (screen state already captured)
        foreach (var o in ordered)
            o.Position = flat[o];

        // 10. selection + labels (screen-space)
        if (selected != null) DrawSelection(ctx, selected);
        DrawLabels(ctx, ordered, selected);

        // 11. interdimensional flash + HUD badge
        DimensionFX.DrawWarpFlash(ctx, warp, Vec3.Zero, ctx.Width, ctx.Height);
        DimensionFX.DrawWarpBadge(ctx, warp, ctx.Width, ctx.Height);
    }

    internal static SKColor GhostTint(PlanetObject p, WarpProfile pf)
    {
        var c = PlanetTextureFactory.GetAverage(p.TextureKind);
        return ColorUtil.Lerp(c, pf.Accent, 0.3);
    }
}

/// <summary>The deep space map: stars, black holes, nebulae, galaxies, exoplanets.
/// Like the Solar System view, the whole map can be thrown into 4D-10D hyperspace.</summary>
public sealed class DeepSpaceScene : Scene
{
    /// <summary>World-line history for the 6D phase-space view (keyed by object id).</summary>
    private readonly Dictionary<string, Queue<Vec3>> _trails = new();

    public DeepSpaceScene()
    {
        Name = "Deep Space";
        ObjectsList = CatalogDeepSpace.Build();
        DefaultCameraTarget = Vec3.Zero;
        DefaultCameraDistance = 520;
        AssignWarpGroups();
    }

    /// <summary>Each system (star + its exoplanets) shares one warp group, so a star and
    /// its worlds travel through hyperspace as one coherent w-band; everything else
    /// (black holes, nebulae, galaxies, lone stars) gets its own band.</summary>
    private void AssignWarpGroups()
    {
        int g = 1;
        foreach (var o in ObjectsList)
        {
            if (o is PlanetObject { OrbitCenterStar: StarObject host })
                o.WarpGroup = host.WarpGroup;             // catalog order: host listed first
            else
                o.WarpGroup = g++;
        }
    }

    private List<SpaceObject> ObjectsList;
    public override List<SpaceObject> Objects => ObjectsList;

    public override void Render(RenderContext ctx, SimClock clock, SpaceObject? selected)
    {
        var warp = ctx.Warp;
        if (warp == null || !warp.Active)
        {
            base.Render(ctx, clock, selected);
            return;
        }
        RenderWarped(ctx, warp, selected);
    }

    private void RenderWarped(RenderContext ctx, DimensionWarp warp, SpaceObject? selected)
    {
        var pf = warp.P;

        // sheet extent: a little beyond the farthest catalog object (log map, ~<=110 units)
        double ext = 70;
        foreach (var o in Objects) ext = Math.Max(ext, o.Position.Length * 1.12);

        // 1. warped spacetime sheet behind everything
        DimensionFX.DrawSpacetimeGrid(ctx, warp, ext);

        // 2. throw every object into hyperspace (remember each object's flat position,
        //    keyed by object so the later depth-sort cannot scramble the restore)
        var ordered = new List<SpaceObject>(Objects);
        var flat = new Dictionary<SpaceObject, Vec3>(ordered.Count);
        foreach (var o in ordered)
        {
            flat[o] = o.Position;
            o.Position = warp.Displace(o.Position, o.WarpGroup);
        }
        ordered.Sort((a, b) => b.Position.DistanceTo(ctx.Cam.Eye).CompareTo(a.Position.DistanceTo(ctx.Cam.Eye)));

        // 3. world-lines (6D phase space): record + draw behind everything else
        if (pf.Trails)
        {
            foreach (var o in ordered)
            {
                if (!_trails.TryGetValue(o.Id, out var q)) _trails[o.Id] = q = new Queue<Vec3>();
                q.Enqueue(o.Position);
                while (q.Count > 110) q.Dequeue();
            }
            foreach (var o in ordered)
                if (_trails.TryGetValue(o.Id, out var q))
                    DimensionFX.DrawTrail(ctx, q.ToArray(), pf.Accent, warp.Blend);
        }
        else if (_trails.Count > 0) _trails.Clear();

        // 4. orbit paths of exoplanets — warped inside PlanetObject.RenderBack via ctx.Warp
        foreach (var o in ordered) o.RenderBack(ctx);

        // 5. probability ghosts behind compact bodies (5D+)
        foreach (var o in ordered)
        {
            SKColor? tint = o switch
            {
                PlanetObject po => SolarSystemScene.GhostTint(po, pf),
                StarObject st => ColorUtil.Lerp(st.StarColor, pf.Accent, 0.3),
                BlackHoleObject => ColorUtil.Lerp(new SKColor(40, 44, 66), pf.Accent, 0.45),
                _ => null
            };
            if (tint != null) DimensionFX.DrawGhosts(ctx, warp, o, tint.Value);
        }

        // 6. bodies — at 10D compact worlds dissolve into string resonances
        foreach (var o in ordered)
        {
            if (pf.Dissolve)
            {
                if (o is StarObject s) { DimensionFX.DrawStringStar(ctx, s, pf, warp.Blend); continue; }
                if (o is PlanetObject pl) { DimensionFX.DrawStringCore(ctx, pl, pf, warp.Blend); continue; }
                if (o is BlackHoleObject bh) { RenderStringBlackHole(ctx, bh, pf, warp.Blend); continue; }
                // nebulae & galaxies keep their diffuse bodies — they are already almost "strings"
            }
            o.RenderBody(ctx, ctx.Shader);
            o.RenderFront(ctx);
        }

        // 7. signature dimension effects
        if (pf.Tesseract)
        {
            DimensionFX.DrawTesseract(ctx, Vec3.Zero, ext * 0.09, warp.Time, pf.Accent, 165 * warp.Blend);
            if (selected != null && selected.Visible)
                DimensionFX.DrawTesseract(ctx, selected.Position, Math.Max(selected.VisualRadius * 3.2, 26),
                    warp.Time * 1.3, pf.Accent, 120 * warp.Blend);
        }
        if (pf.Knots)
            foreach (var o in ordered)
                if (o.Visible && o.PxRadius > 1 && o is StarObject or PlanetObject or BlackHoleObject)
                    DimensionFX.DrawCalabiKnot(ctx, o.ScreenX, o.ScreenY, Math.Max(o.PxRadius * 2.15, 12),
                        warp.Time + o.WarpGroup, pf.Accent, warp.Blend);
        if (pf.E8Lattice)
        {
            var (esx, esy, ezv, efront) = ctx.Project(Vec3.Zero);
            if (efront) DimensionFX.DrawE8(ctx, esx, esy, ctx.PixelRadius(ext * 0.16, ezv), warp.Time, pf.Accent, warp.Blend);
        }
        if (pf.Strings)
        {
            // giant background string loops sweeping the frame (kept subtle)
            for (int i = 0; i < 2; i++)
                DimensionFX.DrawStringLoop(ctx, ctx.Width * (0.34 + 0.3 * i), ctx.Height * (0.46 + 0.08 * ((i + 1) % 2)),
                    ctx.Height * (0.34 + 0.08 * i), warp.Time * (0.5 + 0.2 * i) + i * 2.1,
                    pf.Accent, warp.Blend * 0.5, 0.13, 1.0f, 15, 0.32);
            // halos around compact bodies (9D) / resonant strings (10D)
            foreach (var o in ordered)
            {
                bool compact = o is StarObject or PlanetObject or BlackHoleObject;
                if (!compact || !o.Visible || o.PxRadius <= 0.5) continue;
                double r = Math.Max(o.PxRadius * (pf.Dissolve ? 2.9 : 2.1), 16);
                SKColor tint = o switch
                {
                    StarObject st => st.StarColor,
                    BlackHoleObject => pf.Accent,
                    PlanetObject p2 => SolarSystemScene.GhostTint(p2, pf),
                    _ => pf.Accent
                };
                DimensionFX.DrawStringLoop(ctx, o.ScreenX, o.ScreenY, r, warp.Time + o.WarpGroup * 1.7, tint, warp.Blend, 0.17, 1.3f, 190);
                if (pf.Dissolve)
                    DimensionFX.DrawStringLoop(ctx, o.ScreenX, o.ScreenY, r * 0.66, warp.Time * 1.35 + o.WarpGroup, tint, warp.Blend, 0.22, 1.1f, 150, 0.3);
            }
            // diffuse giants (nebulae/galaxies) get one slow resonant ring at 10D
            if (pf.Dissolve)
                foreach (var o in ordered)
                {
                    if (o is not (NebulaObject or GalaxyObject) || !o.Visible || o.PxRadius <= 4) continue;
                    DimensionFX.DrawStringLoop(ctx, o.ScreenX, o.ScreenY, o.PxRadius * 1.06,
                        warp.Time * 0.6 + o.WarpGroup, pf.Accent, warp.Blend * 0.75, 0.12, 1.1f, 110, 0.5);
                }
        }

        // 8. restore flat world positions (screen state already captured)
        foreach (var o in ordered)
            o.Position = flat[o];

        // 9. selection + labels (screen-space)
        if (selected != null) DrawSelection(ctx, selected);
        DrawLabels(ctx, ordered, selected);

        // 10. interdimensional flash + HUD badge
        DimensionFX.DrawWarpFlash(ctx, warp, Vec3.Zero, ctx.Width, ctx.Height);
        DimensionFX.DrawWarpBadge(ctx, warp, ctx.Width, ctx.Height);
    }

    /// <summary>10D: a black hole keeps its event-horizon identity — a dark core with an
    /// accent rim and an accretion shimmer, framed by the string pass.</summary>
    private static void RenderStringBlackHole(RenderContext ctx, BlackHoleObject bh, WarpProfile pf, double k)
    {
        var (sx, sy, zv, front) = ctx.Project(bh.Position);
        bh.Visible = front; bh.ScreenX = sx; bh.ScreenY = sy; bh.ZView = zv;
        if (!front) return;
        double rPx = ctx.PixelRadius(bh.VisualRadius, zv);
        bh.PxRadius = rPx;
        if (rPx < 1) return;

        // faint accretion shimmer (elliptical glow, keeps the BH reading as a BH)
        ctx.DrawGlow(sx, sy, Math.Max(rPx * 2.2, 8), ColorUtil.WithAlpha(pf.Accent, (byte)(60 * k)), 0.5);
        using var disc = new SKPaint { IsAntialias = true, Color = new SKColor(3, 4, 9, (byte)(250 * k)) };
        ctx.Canvas.DrawCircle((float)sx, (float)sy, (float)rPx, disc);
        using var rim = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.3f * ctx.ScaleFactor,
            Color = ColorUtil.WithAlpha(pf.Accent, (byte)(215 * k))
        };
        ctx.Canvas.DrawCircle((float)sx, (float)sy, (float)(rPx + 0.8), rim);
        // photon-ring echo
        using var ring = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f * ctx.ScaleFactor,
            Color = ColorUtil.WithAlpha(ColorUtil.Lerp(pf.Accent, new SKColor(255, 255, 255), 0.4), (byte)(90 * k))
        };
        ctx.Canvas.DrawCircle((float)sx, (float)sy, (float)(rPx * 1.45), ring);
    }
}

/// <summary>The Higher Dimensions view (4D–10D polytopes and friends).</summary>
public sealed class DimensionsScene : Scene
{
    private readonly List<SpaceObject> _empty = new();
    public override List<SpaceObject> Objects => _empty;

    public NDimensionalShape? CurrentShape;
    public string CurrentShapeName = "";
    public string CurrentShapeNote = "";

    // glome / klein point clouds (projected per-frame)
    public double[][]? PointCloud;     // nD points
    public int PointCloudDims;

    public double ProjectionDistance = 2.2;
    public double ShapeScale = 92;

    public DimensionsScene()
    {
        Name = "Higher Dimensions";
        DefaultCameraTarget = Vec3.Zero;
        DefaultCameraDistance = 900;
        SetHypercube(4);
    }

    public void SetHypercube(int n)
    {
        CurrentShape = NDimensionalShape.Hypercube(n);
        CurrentShape!.Scale = n >= 9 ? 58 : n >= 8 ? 66 : n >= 6 ? 80 : 92;
        CurrentShape.PerspectiveDistance = ProjectionDistance;
        PointCloud = null;
        var di = DimensionData.Info[Math.Min(n, 10)];
        CurrentShapeName = $"{di.ShapeName} · {n}D";
        int verts = 1 << n;
        long edges = (long)n << (n - 1);
        CurrentShapeNote = $"{verts:N0} vertices · {edges:N0} edges";
        RotationSpeed = 1.0;
    }

    public void SetSimplex(int n)
    {
        CurrentShape = NDimensionalShape.Simplex(n);
        CurrentShape!.Scale = ShapeScale;
        CurrentShape.PerspectiveDistance = ProjectionDistance;
        PointCloud = null;
        CurrentShapeName = $"Simplex · {n}D";
        CurrentShapeNote = $"{n + 1} vertices · fully connected";
    }

    public void SetOrthoplex(int n)
    {
        CurrentShape = NDimensionalShape.Orthoplex(n);
        CurrentShape!.Scale = ShapeScale;
        CurrentShape.PerspectiveDistance = ProjectionDistance;
        PointCloud = null;
        CurrentShapeName = $"Orthoplex · {n}D";
        CurrentShapeNote = $"{2 * n} vertices";
    }

    public void SetGlome()
    {
        PointCloud = NDimensionalShape.GlomePoints(30, 22);
        PointCloudDims = 4;
        CurrentShape = null;
        CurrentShapeName = "Glome (3-Sphere)";
        CurrentShapeNote = "Hypersurface of the 4D ball · rotating in XW+YZ";
        _pcSpin = 0;
    }

    public void SetKlein()
    {
        PointCloud = NDimensionalShape.KleinBottlePoints(56, 28);
        PointCloudDims = 4;
        CurrentShape = null;
        CurrentShapeName = "Klein Bottle";
        CurrentShapeNote = "Immersed 4D surface · no inside or outside";
        _pcSpin = 0;
    }

    private double _pcSpin;
    public double RotationSpeed = 1.0;

    public override void Update(SimClock clock)
    {
        double dt = clock.LastDt * RotationSpeed;
        if (CurrentShape != null)
            CurrentShape.Update(dt);
        else if (PointCloud != null)
            _pcSpin += dt * 0.5;
    }

    public override void Render(RenderContext ctx, SimClock clock, SpaceObject? selected)
    {
        var canvas = ctx.Canvas;

        if (CurrentShape != null)
        {
            DrawNDShape(ctx, CurrentShape);
        }
        else if (PointCloud != null)
        {
            DrawPointCloud(ctx, PointCloud, PointCloudDims, _pcSpin);
        }

        DrawCenterInfo(ctx);
    }

    private void DrawNDShape(RenderContext ctx, NDimensionalShape shape)
    {
        var edges = shape.GetProjectedEdges();
        var canvas = ctx.Canvas;
        float cx = ctx.Width / 2f, cy = ctx.Height / 2f;

        using var pLine = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, BlendMode = SKBlendMode.Plus };
        using var pDot = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };

        // adapt visual weight to edge count (dekeract has 5,120 edges!)
        int edgeCount = shape.Edges.Count;
        float glowW = Math.Max(0.8f, (float)(shape.Scale * 0.008) * (edgeCount > 2500 ? 0.5f : edgeCount > 1000 ? 0.7f : 1f));
        bool additive = edgeCount <= 1000;          // high-n shapes: normal blend to avoid blowout
        pLine.BlendMode = additive ? SKBlendMode.Plus : SKBlendMode.SrcOver;
        pDot.BlendMode = additive ? SKBlendMode.Plus : SKBlendMode.SrcOver;
        byte aGlow = additive ? (byte)130 : (byte)22;
        byte aCrisp = edgeCount > 2500 ? (byte)92 : edgeCount > 1000 ? (byte)150 : (byte)235;

        foreach (var (a, b, da, db) in edges)
        {
            var ca = DepthColor(da);
            var cb = DepthColor(db);

            float ax = (float)(cx + a.X), ay = (float)(cy - a.Y);
            float bx = (float)(cx + b.X), by = (float)(cy - b.Y);

            if (additive)
            {
                pLine.Shader = SKShader.CreateLinearGradient(new SKPoint(ax, ay), new SKPoint(bx, by),
                    new[] { ColorUtil.WithAlpha(ca, aGlow), ColorUtil.WithAlpha(cb, aGlow) },
                    null, SKShaderTileMode.Clamp);
                pLine.StrokeWidth = glowW * 3.0f;
                canvas.DrawLine(ax, ay, bx, by, pLine);
            }

            pLine.Shader = SKShader.CreateLinearGradient(new SKPoint(ax, ay), new SKPoint(bx, by),
                new[] { ColorUtil.WithAlpha(ca, aCrisp), ColorUtil.WithAlpha(cb, aCrisp) },
                null, SKShaderTileMode.Clamp);
            pLine.StrokeWidth = glowW;
            canvas.DrawLine(ax, ay, bx, by, pLine);
        }

        // vertex dots (skip when extremely numerous)
        if (edgeCount <= 1000)
        {
            var proj = ProjectVertices(shape);
            for (int i = 0; i < proj.Count; i++)
            {
                var (x, y, d) = proj[i];
                var c = DepthColor(d);
                pDot.Color = ColorUtil.WithAlpha(c, 235);
                canvas.DrawCircle((float)(cx + x), (float)(cy - y), glowW * 2.0f, pDot);
                pDot.Color = ColorUtil.WithAlpha(SKColors.White, 190);
                canvas.DrawCircle((float)(cx + x), (float)(cy - y), glowW * 0.85f, pDot);
            }
        }
    }

    private List<(double x, double y, double depth)> ProjectVertices(NDimensionalShape shape)
    {
        // replicate the shape's projection for vertex dots (cheap: reuse edge endpoints + colors)
        var list = new List<(double, double, double)>();
        foreach (var e in shape.GetProjectedEdges())
        {
            list.Add((e.A.X, e.A.Y, e.DepthA));
            list.Add((e.B.X, e.B.Y, e.DepthB));
        }
        return list;
    }

    private static SKColor DepthColor(double t)
    {
        // cyan → violet → magenta ramp by "extra-dimension depth"
        if (t < 0.5) return ColorUtil.Hsv(186 - 40 * t * 2, 0.75, 1.0);
        return ColorUtil.Hsv(300 + (t - 0.5) * 60, 0.72, 1.0);
    }

    private void DrawPointCloud(RenderContext ctx, double[][] pts, int dims, double spin)
    {
        var canvas = ctx.Canvas;
        float cx = ctx.Width / 2f, cy = ctx.Height / 2f;
        using var p = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        double scale = ShapeScale * 0.8;

        // rotate in XY & XW planes
        double cxy = Math.Cos(spin * 0.7), sxy = Math.Sin(spin * 0.7);
        double cxw = Math.Cos(spin), sxw = Math.Sin(spin);
        double d4 = 3.2;

        foreach (var pt in pts)
        {
            double x = pt[0], y = pt[1], z = pt[2], w = pt.Length > 3 ? pt[3] : 0;
            // XY spin
            double x1 = x * cxy - y * sxy, y1 = x * sxy + y * cxy;
            // XW spin
            double x2 = x1 * cxw - w * sxw, w2 = x1 * sxw + w * cxw;
            // 4D→3D perspective
            double k4 = MathUtil.Clamp(d4 / Math.Max(d4 - w2 * 0.5, 1e-6), 0.6, 2.4);
            double x3 = x2 * k4, y3 = y1 * k4, z3 = z * k4;
            // 3D→2D camera-style projection
            double k3 = MathUtil.Clamp(3.6 / Math.Max(3.6 - z3, 1e-6), 0.6, 2.4);
            double sx = cx + x3 * k3 * scale;
            double sy = cy - y3 * k3 * scale;

            var c = ColorUtil.Hsv(190 + (w2 * 0.5 + 1) * 90, 0.65, 1.0);
            p.Color = ColorUtil.WithAlpha(c, 130);
            canvas.DrawCircle((float)sx, (float)sy, 2.3f, p);
        }
    }

    private void DrawCenterInfo(RenderContext ctx)
    {
        LabelRenderer.DrawBig(ctx, CurrentShapeName, CurrentShapeNote, ctx.Width / 2f, ctx.Height - 46f);
    }
}

/// <summary>Simple text label renderer (canvas HUD).</summary>
public static class LabelRenderer
{
    private static SKTypeface? _typeface;
    public static SKTypeface Typeface => _typeface ??= SKTypeface.FromFamilyName(
        "DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        ?? SKTypeface.Default;

    public static void Draw(RenderContext ctx, string name, string tag, float x, float y, bool selected)
    {
        var canvas = ctx.Canvas;
        float scale = ctx.ScaleFactor;
        using var p = new SKPaint { IsAntialias = true, Typeface = Typeface };
        float size = 12.5f * scale;
        p.TextSize = size;
        p.Color = selected ? new SKColor(150, 240, 255) : new SKColor(215, 225, 240, 205);
        float w = p.MeasureText(name);
        canvas.DrawText(name, x - w / 2, y, p);

        if (!string.IsNullOrEmpty(tag))
        {
            p.TextSize = size * 0.78f;
            p.Color = new SKColor(150, 165, 190, 175);
            float w2 = p.MeasureText(tag);
            canvas.DrawText(tag, x - w2 / 2, y + size * 1.05f, p);
        }
    }

    public static void DrawBig(RenderContext ctx, string name, string tag, float x, float y)
    {
        var canvas = ctx.Canvas;
        float scale = ctx.ScaleFactor;
        using var p = new SKPaint { IsAntialias = true, Typeface = Typeface };
        p.TextSize = 17f * scale;
        p.Color = new SKColor(160, 245, 255, 235);
        float w = p.MeasureText(name);
        canvas.DrawText(name, x - w / 2, y, p);
        if (!string.IsNullOrEmpty(tag))
        {
            p.TextSize = 12.5f * scale;
            p.Color = new SKColor(150, 165, 195, 200);
            float w2 = p.MeasureText(tag);
            canvas.DrawText(tag, x - w2 / 2, y + 19f * scale, p);
        }
    }
}
