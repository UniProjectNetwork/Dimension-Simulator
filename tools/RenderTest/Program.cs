using DimensionEngine;
using DimensionEngine.Universe;

namespace RenderTest;

/// <summary>
/// Headless verification harness: renders representative frames from every
/// view mode into PNG files so the engine can be validated without a display.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--bench")) { Bench(); return; }
        string outDir = args.Length > 0 ? args[0] : "/home/z/my-project/download/previews";
        Directory.CreateDirectory(outDir);

        var eng = new SimulationEngine();
        Console.WriteLine($"Engine OK. Solar objects: {eng.Solar.Objects.Count}, Deep objects: {eng.Deep.Objects.Count}");

        int W = 1600, H = 900;

        // 1. Solar system overview
        Save(eng, Path.Combine(outDir, "01-solar-system.png"), W, H, "solar overview");
        Warm(eng, 0.5);

        // 2. Saturn close-up
        eng.SetMode(ViewMode.SolarSystem);
        var saturn = eng.Solar.Objects.First(o => o.Id == "saturn");
        eng.Select(saturn);
        eng.Cam.SetImmediate(saturn.Position, saturn.VisualRadius * 6.5, 40, 18);
        Save(eng, Path.Combine(outDir, "02-saturn.png"), W, H, "saturn closeup");

        // 3. Earth + Moon close-up
        var earth = eng.Solar.Objects.First(o => o.Id == "earth");
        eng.Select(earth);
        eng.Cam.SetImmediate(earth.Position, earth.VisualRadius * 7, 60, 20);
        Save(eng, Path.Combine(outDir, "03-earth.png"), W, H, "earth closeup");

        // 4. Jupiter closeup
        var jup = eng.Solar.Objects.First(o => o.Id == "jupiter");
        eng.Select(jup);
        eng.Cam.SetImmediate(jup.Position, jup.VisualRadius * 6, 30, 14);
        Save(eng, Path.Combine(outDir, "04-jupiter.png"), W, H, "jupiter closeup");

        // 5. Inner system
        eng.Select(null);
        eng.Cam.SetImmediate(DimensionEngine.Maths.Vec3.Zero, 640, 40, 30);
        Save(eng, Path.Combine(outDir, "05-inner-system.png"), W, H, "inner system");

        // 6. Deep space overview
        eng.SetMode(ViewMode.DeepSpace);
        Save(eng, Path.Combine(outDir, "06-deep-space.png"), W, H, "deep space overview");

        // 7. TON 618 close-up (black hole!)
        var ton = eng.Deep.Objects.First(o => o.Id == "ton618");
        eng.Select(ton);
        eng.Cam.SetImmediate(ton.Position, ton.VisualRadius * 7, 20, 12);
        Save(eng, Path.Combine(outDir, "07-ton618-blackhole.png"), W, H, "TON 618");

        // 8. Betelgeuse
        var betel = eng.Deep.Objects.First(o => o.Id == "betelgeuse");
        eng.Select(betel);
        eng.Cam.SetImmediate(betel.Position, betel.VisualRadius * 7, 0, 8);
        Save(eng, Path.Combine(outDir, "08-betelgeuse.png"), W, H, "betelgeuse");

        // 9. Andromeda galaxy
        var andro = eng.Deep.Objects.First(o => o.Id == "andromeda");
        eng.Select(andro);
        eng.Cam.SetImmediate(andro.Position, andro.VisualRadius * 2.8, 0, 30);
        Save(eng, Path.Combine(outDir, "09-andromeda.png"), W, H, "andromeda");

        // 10. Orion nebula
        var orion = eng.Deep.Objects.First(o => o.Id == "orion-neb");
        eng.Select(orion);
        eng.Cam.SetImmediate(orion.Position, orion.VisualRadius * 3.2, 0, 10);
        Save(eng, Path.Combine(outDir, "10-orion-nebula.png"), W, H, "orion nebula");

        // 11. Higher dims: tesseract
        eng.SetMode(ViewMode.HigherDimensions);
        eng.Dims.SetHypercube(4);
        for (int i = 0; i < 90; i++) eng.Tick(1.0 / 60);
        Save(eng, Path.Combine(outDir, "11-tesseract-4d.png"), W, H, "tesseract");

        // 12. Dekeract 10D
        eng.Dims.SetHypercube(10);
        for (int i = 0; i < 120; i++) eng.Tick(1.0 / 60);
        Save(eng, Path.Combine(outDir, "12-dekeract-10d.png"), W, H, "dekeract");

        // 13. Glome
        eng.Dims.SetGlome();
        for (int i = 0; i < 100; i++) eng.Tick(1.0 / 60);
        Save(eng, Path.Combine(outDir, "13-glome.png"), W, H, "glome");

        // 14-18. SOLAR SYSTEM DIMENSION WARP: 4D / 5D / 6D / 8D / 10D
        eng.SetMode(ViewMode.SolarSystem);
        eng.Select(null);
        eng.Cam.SetImmediate(DimensionEngine.Maths.Vec3.Zero, 1500, 40, 26);
        foreach (var (dim, name) in new[] { (4, "14-solar-4d-hyperspace"), (5, "15-solar-5d-kaluza"), (6, "16-solar-6d-phase"), (8, "17-solar-8d-e8"), (10, "18-solar-10d-strings") })
        {
            eng.SetWarp(dim);
            for (int i = 0; i < 240; i++) eng.Tick(1.0 / 60);   // let blend saturate + rotations evolve
            Save(eng, Path.Combine(outDir, $"{name}.png"), W, H, $"solar {dim}D");
        }
        eng.SetWarp(3);
        for (int i = 0; i < 60; i++) eng.Tick(1.0 / 60);
        Save(eng, Path.Combine(outDir, "19-solar-3d-restored.png"), W, H, "solar 3D restored");

        // 20-25. DEEP SPACE DIMENSION WARP: 4D / 5D / 6D / 8D / 10D / restored
        eng.SetMode(ViewMode.DeepSpace);
        eng.Select(null);
        eng.Cam.SetImmediate(DimensionEngine.Maths.Vec3.Zero, 760, 25, 42);
        foreach (var (dim, name) in new[] { (4, "20-deep-4d-hyperspace"), (5, "21-deep-5d-kaluza"), (6, "22-deep-6d-phase"), (8, "23-deep-8d-e8"), (10, "24-deep-10d-strings") })
        {
            eng.SetWarp(dim);
            for (int i = 0; i < 240; i++) eng.Tick(1.0 / 60);
            Save(eng, Path.Combine(outDir, $"{name}.png"), W, H, $"deep {dim}D");
        }
        eng.SetWarp(3);
        for (int i = 0; i < 60; i++) eng.Tick(1.0 / 60);
        Save(eng, Path.Combine(outDir, "25-deep-3d-restored.png"), W, H, "deep 3D restored");

        Console.WriteLine("All frames rendered.");
    }

    private static void Bench()
    {
        var eng = new SimulationEngine();
        eng.SetMode(ViewMode.DeepSpace);
        var all = eng.Deep.Objects;
        var backup = new List<SpaceObject>(all);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        void TimeSet(string label, Func<SpaceObject, bool> keep)
        {
            all.Clear();
            all.AddRange(backup.Where(keep));
            eng.RenderFrame(800, 450, 1f); // warm textures
            sw.Restart();
            eng.RenderFrame(800, 450, 1f);
            Console.WriteLine($"{label}: {sw.ElapsedMilliseconds} ms ({all.Count} objs)");
        }

        TimeSet("stars only", o => o is StarObject && o is not NeutronStarObject);
        TimeSet("nebulae only", o => o is NebulaObject);
        TimeSet("galaxies only", o => o is GalaxyObject);
        TimeSet("black holes only", o => o is BlackHoleObject);
        TimeSet("neutron stars only", o => o is NeutronStarObject);
        TimeSet("exoplanets only", o => o.Category == "Exoplanet");
        all.Clear(); all.AddRange(backup);
        sw.Restart();
        eng.RenderFrame(800, 450, 1f);
        Console.WriteLine($"full deep frame: {sw.ElapsedMilliseconds} ms");

        eng.SetMode(ViewMode.HigherDimensions);
        eng.Dims.SetHypercube(10);
        sw.Restart();
        eng.RenderFrame(800, 450, 1f);
        Console.WriteLine($"dekeract frame: {sw.ElapsedMilliseconds} ms");

        eng.SetMode(ViewMode.SolarSystem);
        sw.Restart();
        eng.RenderFrame(800, 450, 1f);
        Console.WriteLine($"solar frame: {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        eng.RenderFrame(1600, 900, 1f);
        Console.WriteLine($"solar frame 1600x900: {sw.ElapsedMilliseconds} ms");

        eng.SetWarp(4);
        for (int i = 0; i < 200; i++) eng.Tick(1.0 / 60);
        eng.RenderFrame(800, 450, 1f);
        sw.Restart();
        eng.RenderFrame(800, 450, 1f);
        Console.WriteLine($"solar 4D warp frame: {sw.ElapsedMilliseconds} ms");
        eng.SetWarp(10);
        for (int i = 0; i < 200; i++) eng.Tick(1.0 / 60);
        eng.RenderFrame(800, 450, 1f);
        sw.Restart();
        eng.RenderFrame(800, 450, 1f);
        Console.WriteLine($"solar 10D warp frame: {sw.ElapsedMilliseconds} ms");

        eng.SetMode(ViewMode.DeepSpace);
        eng.SetWarp(4);
        for (int i = 0; i < 200; i++) eng.Tick(1.0 / 60);
        eng.RenderFrame(800, 450, 1f);
        sw.Restart();
        eng.RenderFrame(800, 450, 1f);
        Console.WriteLine($"deep 4D warp frame: {sw.ElapsedMilliseconds} ms");
        eng.SetWarp(10);
        for (int i = 0; i < 200; i++) eng.Tick(1.0 / 60);
        eng.RenderFrame(800, 450, 1f);
        sw.Restart();
        eng.RenderFrame(800, 450, 1f);
        Console.WriteLine($"deep 10D warp frame: {sw.ElapsedMilliseconds} ms");
    }

    private static void Warm(SimulationEngine eng, double seconds)
    {
        int steps = (int)(seconds * 60);
        for (int i = 0; i < steps; i++) eng.Tick(1.0 / 60);
    }

    private static void Save(SimulationEngine eng, string path, int w, int h, string label)
    {
        try
        {
            var png = eng.ScreenshotPng(w, h);
            File.WriteAllBytes(path, png);
            Console.WriteLine($"[{label}] -> {path} ({png.Length / 1024} KB)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{label}] FAILED: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
