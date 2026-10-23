using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DimensionEngine;
using DimensionEngine.Scenes;
using DimensionEngine.Sim;
using DimensionEngine.Universe;
using DimensionSimulator.Controls;

namespace DimensionSimulator;

public partial class MainWindow : Window
{
    public sealed class BrowserItem
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public FontWeight Weight { get; set; } = FontWeight.Normal;
        public IBrush Brush { get; set; } = Brushes.Transparent;
        public SpaceObject? Object { get; set; }
        public bool IsHeader { get; set; }
        public override string ToString() => Title;
    }

    private readonly List<BrowserItem> _allItems = new();
    private bool _syncingTabs;
    private bool _syncingWarp;
    private readonly List<Avalonia.Controls.Primitives.ToggleButton> _warpButtons = new();

    public MainWindow()
    {
        InitializeComponent();

        Canvas.HudUpdate += UpdateHud;
        Canvas.SelectionChanged += ShowObjectInfo;
        Canvas.ModeChanged += OnModeChangedUi;

        BuildBrowser();
        BuildWarpButtons();
        UpdateDimButtons();
        ShowObjectInfo(null);
        UpdateSpeedLabel();

        var clock = Canvas.Engine.Clock;
        SpeedSlider.Value = SimClock.SliderFromSpeed(clock.DaysPerSecond);

        if (Environment.GetCommandLineArgs().Contains("--shots"))
            RunShotTour();
        if (Environment.GetCommandLineArgs().Contains("--verify-buttons"))
            RunButtonVerification();
    }

    /// <summary>
    /// Regression test for the dimension buttons: raises real Click routed events on
    /// every 3D..10D warp button and 4D..10D hypercube button, then asserts the engine
    /// landed in the matching dimension. Guards against captured loop-variable bugs
    /// (every click resolving to the same dimension).
    /// </summary>
    private void RunButtonVerification()
    {
        int failures = 0;

        void Check(string what, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {what}");
            if (!ok) failures++;
        }

        async void Run()
        {
            await Task.Delay(700); // let the window finish layout
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // --- solar system warp buttons (real Click events, not ApplyWarp calls) ---
                foreach (var b in _warpButtons)
                {
                    int expect = (int)(b.Tag ?? 0);
                    b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    int got = Canvas.Engine.WarpDim;
                    Check($"warp button {expect}D -> WarpDim == {expect} (got {got}D)", got == expect);
                }

                // --- deep space must share the same warp state machine ---
                SwitchMode(ViewMode.DeepSpace);
                Canvas.Engine.SetWarp(6);
                for (int i = 0; i < 50; i++) Canvas.Engine.Tick(1.0 / 60);
                Canvas.Engine.RenderFrame(800, 500, 1f);   // must not throw
                Check($"deep space shares the warp (6D active, WarpDim == {Canvas.Engine.WarpDim})",
                    Canvas.Engine.Warp.Active && Canvas.Engine.WarpDim == 6);
                Canvas.Engine.SetWarp(3);
                for (int i = 0; i < 30; i++) Canvas.Engine.Tick(1.0 / 60);

                // --- higher-dimensions hypercube buttons ---
                SwitchMode(ViewMode.HigherDimensions);
                foreach (var child in DimButtons.Children.OfType<Button>())
                {
                    int expect = (int)(child.Tag ?? 0);
                    child.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    string name = Canvas.Engine.Dims.CurrentShapeName;
                    Check($"hypercube button {expect}D -> shape '{name}'", name.EndsWith($"\u00b7 {expect}D"));
                }

                Console.WriteLine(failures == 0
                    ? "VERIFY-OK: all dimension buttons switch to their own dimension"
                    : $"VERIFY-FAILED: {failures} dimension button(s) broken");
            });
            await Task.Delay(200);
            ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!)
                .Shutdown(failures == 0 ? 0 : 1);
        }

        Run();
    }

    /// <summary>Auto-tour for verification: switch views and save window screenshots.</summary>
    private void RunShotTour()
    {
        string dir = "/home/z/my-project/download/ui-previews";
        System.IO.Directory.CreateDirectory(dir);

        async void CaptureAfter(int delayMs, string name)
        {
            await Task.Delay(delayMs);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var scale = VisualRoot?.RenderScaling ?? 1;
                var size = new PixelSize((int)(Width * scale), (int)(Height * scale));
                using var rtb = new RenderTargetBitmap(size, new Vector(96, 96));
                rtb.Render(this);
                using var fs = System.IO.File.Create(System.IO.Path.Combine(dir, name));
                rtb.Save(fs);
                Console.WriteLine($"[shot] {name}");
            });
        }

        var steps = new (int delay, string name, Action? before)[]
        {
            (0, "ui-1-solar-system.png", null),
            (1200, "ui-2-saturn-info.png", () =>
            {
                var saturn = Canvas.Engine.Solar.Objects.First(o => o.Id == "saturn");
                Canvas.FlyTo(saturn);
            }),
            (1200, "ui-3-solar-4d-warp.png", () =>
            {
                Canvas.Engine.Select(null);
                Canvas.Engine.Cam.SetImmediate(DimensionEngine.Maths.Vec3.Zero, 1500, 40, 26);
                ApplyWarp(4);
            }),
            (2800, "ui-4-solar-10d-warp.png", () => ApplyWarp(10)),
            (2800, "ui-5-ton618.png", () =>
            {
                ApplyWarp(3);
                SwitchMode(ViewMode.DeepSpace);
                var ton = Canvas.Engine.Deep.Objects.First(o => o.Id == "ton618");
                Canvas.FlyTo(ton);
            }),
            (1200, "ui-6-deep-space.png", () =>
            {
                Canvas.Engine.SetMode(ViewMode.DeepSpace);
                Canvas.Engine.Cam.SetImmediate(DimensionEngine.Maths.Vec3.Zero, 760, 25, 42);
                Canvas.Engine.Select(null);
                ShowObjectInfo(null);
            }),
            (1400, "ui-10-deep-4d-warp.png", () =>
            {
                Canvas.Engine.Select(null);
                ApplyWarp(4);          // deep space now warps too!
            }),
            (1200, "ui-11-deep-10d-warp.png", () => ApplyWarp(10)),
            (900, "ui-7-tesseract.png", () =>
            {
                ApplyWarp(3);              // restore flat space before leaving Deep Space
                SwitchMode(ViewMode.HigherDimensions);
                Canvas.Engine.Dims.SetHypercube(4);
                UpdateDimCard(Canvas.Engine.Dims);
            }),
            (1400, "ui-8-dekeract.png", () =>
            {
                Canvas.Engine.Dims.SetHypercube(10);
                UpdateDimCard(Canvas.Engine.Dims);
            }),
            (1600, "ui-9-glome.png", () =>
            {
                Canvas.Engine.Dims.SetGlome();
                UpdateDimCard(Canvas.Engine.Dims);
            }),
            (1300, null, null),
        };

        // schedule: run `before` on the UI thread at time t, then capture at t + settleMs
        // (layout + fly animation need time to catch up before RenderTargetBitmap sees them)
        int t = 600;
        foreach (var (delay, name, before) in steps)
        {
            t += delay;
            var nm = name;
            var act = before;
            if (act != null)
            {
                _ = Task.Delay(t).ContinueWith(_ => Dispatcher.UIThread.Post(act));
                t += 2600; // settle: layout pass + fly-to animation
            }
            if (nm != null) CaptureAfter(t, nm);
            else
            {
                _ = Task.Delay(t + 300).ContinueWith(_ =>
                    Dispatcher.UIThread.Post(() => ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).Shutdown()));
            }
        }
    }

    // ---------------- browser ----------------

    private void BuildBrowser()
    {
        _allItems.Clear();
        var solar = Canvas.Engine.Solar.Objects;
        var deep = Canvas.Engine.Deep.Objects;

        void AddHeader(string t)
        {
            _allItems.Add(new BrowserItem { Title = t, IsHeader = true, Weight = FontWeight.SemiBold, Brush = new SolidColorBrush(Color.FromRgb(0x5F, 0xE8, 0xFF)) });
        }
        void AddObj(SpaceObject o)
        {
            _allItems.Add(new BrowserItem
            {
                Title = o.Name,
                Subtitle = $"{o.Category} · {o.EmTag}",
                Object = o,
                Brush = new SolidColorBrush(Color.FromRgb(0xC6, 0xD2, 0xE6))
            });
        }

        AddHeader("SOLAR SYSTEM");
        AddObj(solar.First(o => o.Id == "sol"));
        foreach (var o in solar.Where(o => o.Category == "Planet")) AddObj(o);
        foreach (var o in solar.Where(o => o.Category == "Dwarf Planet")) AddObj(o);
        foreach (var o in solar.Where(o => o.Category == "Moon")) AddObj(o);
        foreach (var o in solar.Where(o => o.Category == "Comet")) AddObj(o);

        AddHeader("BLACK HOLES");
        foreach (var o in deep.Where(o => o.Category == "Black Hole")) AddObj(o);
        AddHeader("STARS & COMPACT OBJECTS");
        foreach (var o in deep.Where(o => o.Category is "Star" or "Neutron Star" or "White Dwarf")) AddObj(o);
        AddHeader("NEBULAE");
        foreach (var o in deep.Where(o => o.Category == "Nebula")) AddObj(o);
        AddHeader("GALAXIES");
        foreach (var o in deep.Where(o => o.Category == "Galaxy")) AddObj(o);
        AddHeader("EXOPLANETS");
        foreach (var o in deep.Where(o => o.Category == "Exoplanet")) AddObj(o);

        BrowserList.ItemsSource = _allItems;
    }

    private void OnSearch(object? sender, TextChangedEventArgs e)
    {
        string q = SearchBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(q)) { BrowserList.ItemsSource = _allItems; return; }
        BrowserList.ItemsSource = _allItems.Where(i => !i.IsHeader &&
            (i.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
             (i.Subtitle?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))).ToList();
    }

    private void OnBrowserSelect(object? sender, SelectionChangedEventArgs e)
    {
        if (BrowserList.SelectedItem is BrowserItem item && item.Object != null)
        {
            Canvas.FlyTo(item.Object);
            ToastMsg($"Traveling to {item.Object.Name}…");
        }
    }

    // ---------------- view tabs ----------------

    private void OnTabSolar(object? sender, RoutedEventArgs e) => SwitchMode(ViewMode.SolarSystem);
    private void OnTabDeep(object? sender, RoutedEventArgs e) => SwitchMode(ViewMode.DeepSpace);
    private void OnTabDims(object? sender, RoutedEventArgs e) => SwitchMode(ViewMode.HigherDimensions);

    private void SwitchMode(ViewMode mode)
    {
        if (_syncingTabs) return;
        Canvas.SetMode(mode);
    }

    private void OnModeChangedUi(ViewMode mode)
    {
        _syncingTabs = true;
        TabSolar.IsChecked = mode == ViewMode.SolarSystem;
        TabDeep.IsChecked = mode == ViewMode.DeepSpace;
        TabDims.IsChecked = mode == ViewMode.HigherDimensions;
        _syncingTabs = false;

        bool dims = mode == ViewMode.HigherDimensions;
        BrowserPanel.IsVisible = !dims;
        DimPanel.IsVisible = dims;
        BrowserTitle.Text = dims ? "DIMENSION EXPLORER" : "OBJECTS";
        if (dims) UpdateDimCard(Canvas.Engine.Dims);
        else ShowObjectInfo(Canvas.Engine.Selected);
    }

    // ---------------- dimension warp (solar system 4D..10D) ----------------

    private void BuildWarpButtons()
    {
        for (int n = 3; n <= 10; n++)
        {
            var b = new Avalonia.Controls.Primitives.ToggleButton
            {
                Content = n == 3 ? "3D" : $"{n}D",
                Tag = n,
                MinWidth = 40,
                Margin = new Thickness(0, 0, 4, 4)
            };
            int dim = n; // per-iteration copy — never capture the loop variable itself
            b.Click += (_, _) => ApplyWarp(dim);
            WarpButtons.Children.Add(b);
            _warpButtons.Add(b);
        }
        SyncWarpButtons();
    }

    private void ApplyWarp(int nRaw)
    {
        if (_syncingWarp) return;
        int n = Math.Clamp(nRaw, 3, 10);
        Canvas.Engine.SetWarp(n);
        SyncWarpButtons();
        if (n >= 4)
        {
            var p = DimensionEngine.ND.WarpProfile.For(n);
            WarpState.Text = $"{n}D · {p.Title}";
            WarpState.Foreground = new SolidColorBrush(Color.FromRgb(p.Accent.Red, p.Accent.Green, p.Accent.Blue));
            WarpCard.Text = p.Science;
            ToastMsg($"Solar System & Deep Space warped into {n} dimensions — {p.Tag}");
        }
        else
        {
            WarpState.Text = "3D · normal space";
            WarpState.Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x7C, 0x9C));
            WarpCard.Text = "Select 4D-10D to throw both the Solar System and Deep Space into higher-dimensional space: orbits twist, stars and black holes swell and shrink as they rotate through hidden axes. 3D restores normal view.";
            ToastMsg("Back to 3D — flatland restored");
        }
    }

    private void SyncWarpButtons()
    {
        _syncingWarp = true;
        int cur = Canvas.Engine.WarpDim;
        foreach (var b in _warpButtons)
            b.IsChecked = (int)(b.Tag ?? 0) == cur;
        _syncingWarp = false;
    }

    // ---------------- higher dimensions UI ----------------

    private void UpdateDimButtons()
    {
        for (int n = 4; n <= 10; n++)
        {
            var b = new Button
            {
                Content = $"{n}D",
                MinWidth = 44,
                MinHeight = 34,
                Margin = new Thickness(0, 0, 4, 4),
                Tag = n
            };
            int dim = n; // per-iteration copy — never capture the loop variable itself
            b.Click += (_, _) =>
            {
                Canvas.Engine.Dims.SetHypercube(dim);
                UpdateDimCard(Canvas.Engine.Dims);
                ToastMsg($"{dim}D hypercube: {1 << dim} vertices, {(long)dim << (dim - 1)} edges");
            };
            DimButtons.Children.Add(b);
        }
    }

    private void UpdateDimCard(DimensionsScene dims)
    {
        DimCardTitle.Text = dims.CurrentShapeName;
        DimCardMath.Text = dims.CurrentShapeNote;

        int? n = null;
        if (dims.CurrentShapeName.Contains("· "))
        {
            var parts = dims.CurrentShapeName.Split('·');
            if (int.TryParse(parts[^1].Trim().Replace("D", ""), out int nn)) n = nn;
        }
        if (n != null && n >= 0 && n <= 10)
        {
            var info = DimensionData.Info[n.Value];
            DimCardPhysics.Text = info.PhysicsNote;
        }
        else if (dims.CurrentShapeName.Contains("Glome"))
            DimCardPhysics.Text = "The set of all points at fixed distance in 4D — the 4D analog of a sphere. Rotating through the 4th axis makes it appear to turn inside out.";
        else if (dims.CurrentShapeName.Contains("Klein"))
            DimCardPhysics.Text = "A surface with a single side and no boundary. In 4D it passes through itself without intersecting — impossible in 3D.";
    }

    private void OnShapeHypercube(object? sender, RoutedEventArgs e)
    {
        // keeps the currently selected dimension — re-apply last n (default 4)
        Canvas.Engine.Dims.SetHypercube(LastDim);
        UpdateDimCard(Canvas.Engine.Dims);
    }
    private void OnShapeSimplex(object? sender, RoutedEventArgs e)
    {
        Canvas.Engine.Dims.SetSimplex(Math.Min(LastDim, 10));
        UpdateDimCard(Canvas.Engine.Dims);
    }
    private void OnShapeOrthoplex(object? sender, RoutedEventArgs e)
    {
        Canvas.Engine.Dims.SetOrthoplex(Math.Min(LastDim, 10));
        UpdateDimCard(Canvas.Engine.Dims);
    }
    private void OnShapeGlome(object? sender, RoutedEventArgs e)
    {
        Canvas.Engine.Dims.SetGlome();
        UpdateDimCard(Canvas.Engine.Dims);
    }
    private void OnShapeKlein(object? sender, RoutedEventArgs e)
    {
        Canvas.Engine.Dims.SetKlein();
        UpdateDimCard(Canvas.Engine.Dims);
    }

    public static int LastDim = 4;

    private void OnRotSpeed(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (Canvas.Engine.Mode == ViewMode.HigherDimensions)
            Canvas.Engine.Dims.RotationSpeed = RotSpeed.Value;
    }

    private void OnProjDist(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (Canvas.Engine.Mode == ViewMode.HigherDimensions)
        {
            Canvas.Engine.Dims.ProjectionDistance = ProjDist.Value;
            if (Canvas.Engine.Dims.CurrentShape != null)
                Canvas.Engine.Dims.CurrentShape.PerspectiveDistance = ProjDist.Value;
        }
    }

    // ---------------- info panel ----------------

    private void ShowObjectInfo(SpaceObject? o)
    {
        try
        {
            InfoPanel.Children.Clear();

            if (o == null)
            {
                InfoPanel.Children.Add(Header("EXPLORER GUIDE"));
                InfoPanel.Children.Add(Body("Select any object on the canvas — or browse the list on the left — to see its real scientific data: mass, radius, temperature, gravity, orbital mechanics and more."));
                InfoPanel.Children.Add(Body("Every body moves on a true Keplerian orbit driven by the simulation clock. Wind the speed slider from minutes per second up to a million years per second."));
                InfoPanel.Children.Add(Header("BLACK HOLES"));
                InfoPanel.Children.Add(Body("TON 618, Sagittarius A*, M87*, Phoenix A, Cygnus X-1, Gaia BH1 and the GW150914 remnant are simulated with event horizons, photon rings, Doppler-beamed accretion disks, gravitational-lensing arcs and relativistic jets."));
                InfoPanel.Children.Add(Header("HIGHER DIMENSIONS"));
                InfoPanel.Children.Add(Body("Switch to the Higher Dimensions tab to spin hypercubes from 4D to 10D — 2^n vertices rotated through n-dimensional planes and successively projected down to your screen."));
                return;
            }

            InfoPanel.Children.Add(Header(o.Name.ToUpperInvariant()));
            InfoPanel.Children.Add(TagLine($"{o.Category} — {o.EmTag}"));

            var i = o.Info;
            void Row(string k, string? v)
            {
                if (string.IsNullOrWhiteSpace(v)) return;
                InfoPanel.Children.Add(KvRow(k, v));
            }

            Row("Type", i.Type);
            Row("Mass", i.MassText);
            Row("Radius", i.RadiusText);
            Row("Distance", i.DistanceText);
            Row("Temperature", i.TempText);
            Row("Spectral class", i.SpectralText);
            Row("Luminosity", i.LuminosityText);
            Row("Gravity", i.GravityText);
            Row("Escape velocity", i.EscapeVelText);
            Row("Day", i.DayText);
            Row("Year / period", i.YearText);
            Row("Rotation", i.RotationText);
            Row("Magnetic field", i.MagneticFieldText);
            Row("Moons", i.MoonsText);
            Row("Atmosphere", i.AtmosphereText);
            Row("Composition", i.CompositionText);
            Row("Size", i.SizeText);
            Row("Star count", i.StarCountText);
            Row("Age", i.AgeText);
            Row("Discovery", i.DiscoveryText);
            Row("Schwarzschild radius", i.SchwarzschildText);

            if (o is BlackHoleObject bh)
            {
                double rs = 2.953 * bh.MassSolarMasses; // km
                InfoPanel.Children.Add(Spacer(6));
                InfoPanel.Children.Add(Header("RELATIVITY CHECK"));
                InfoPanel.Children.Add(KvRow("Photon sphere", $"{FormatKm(rs * 1.5)} — light orbits here"));
                InfoPanel.Children.Add(KvRow("ISCO", $"{FormatKm(rs * 3)} — last stable orbit"));
                InfoPanel.Children.Add(KvRow("Time dilation at ISCO", $"a clock runs {1.0 / Math.Sqrt(1 - 1.0 / 3):F2}× slower than yours"));
            }

            if (i.Facts.Length > 0)
            {
                InfoPanel.Children.Add(Spacer(6));
                InfoPanel.Children.Add(Header("DID YOU KNOW?"));
                foreach (var f in i.Facts) InfoPanel.Children.Add(Bullet(f));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShowObjectInfo ERROR] {o?.Name}: {ex.Message}");
        }
    }

    private static string FormatKm(double km) => km switch
    {
        < 1e6 => $"{km:N0} km",
        < 1.496e8 => $"{km / 1e6:N1} million km",
        < 9.461e12 => $"{km / 1.496e8:N2} AU",
        _ => $"{km / 9.4607e12:N3} light-years"
    };

    private static TextBlock Header(string t) => new()
    {
        Text = t,
        FontSize = 13,
        FontWeight = FontWeight.SemiBold,
        Foreground = new SolidColorBrush(Color.FromRgb(0x5F, 0xE8, 0xFF)),
        Margin = new Thickness(0, 10, 0, 2),
        TextWrapping = TextWrapping.Wrap
    };

    private static TextBlock TagLine(string t) => new()
    {
        Text = t,
        FontSize = 11.5,
        Foreground = new SolidColorBrush(Color.FromRgb(0x8F, 0xA1, 0xC2)),
        TextWrapping = TextWrapping.Wrap
    };

    private static TextBlock Body(string t) => new()
    {
        Text = t,
        FontSize = 11.8,
        Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0xC6, 0xDE)),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 3, 0, 3)
    };

    private static StackPanel KvRow(string k, string v) => new()
    {
        Margin = new Thickness(0, 2, 0, 2),
        Children =
        {
            new TextBlock { Text = k.ToUpperInvariant(), FontSize = 9.5, Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x94)), LetterSpacing = 1 },
            new TextBlock { Text = v, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0xDA, 0xE4, 0xF4)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 0) }
        }
    };

    private static StackPanel Bullet(string t) => new()
    {
        Margin = new Thickness(0, 2, 0, 2),
        Children =
        {
            new TextBlock { Text = "•  " + t, FontSize = 11.5, Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0xD2, 0xE6)), TextWrapping = TextWrapping.Wrap }
        }
    };

    private static Control Spacer(double h) => new Border { Height = h };

    // ---------------- HUD / time ----------------

    private void UpdateHud()
    {
        var eng = Canvas.Engine;
        HudDistance.Text = $"◎ Camera: {eng.DistanceReadout()}";
        HudSpeed.Text = eng.Clock.Paused ? "⏸ paused" : $"▶ {eng.Clock.SpeedLabel}";
        HudDate.Text = $"⏱ {eng.Clock.DisplayDate:yyyy MMM dd HH:mm} UTC";
    }

    private void UpdateSpeedLabel()
    {
        SpeedLabel.Text = Canvas.Engine.Clock.SpeedLabel;
    }

    private void OnSpeedChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        Canvas.Engine.Clock.DaysPerSecond = SimClock.SpeedFromSlider01(SpeedSlider.Value);
        UpdateSpeedLabel();
    }

    private void OnPlayPause(object? sender, RoutedEventArgs e)
    {
        Canvas.Engine.Clock.Paused = !Canvas.Engine.Clock.Paused;
        BtnPlay.Content = Canvas.Engine.Clock.Paused ? "▶  Play" : "❚❚  Pause";
    }

    private void OnResetTime(object? sender, RoutedEventArgs e)
    {
        Canvas.Engine.Clock.Reset();
        ToastMsg("Clock reset to J2000.0 epoch");
    }

    private void OnOrbitsToggle(object? sender, RoutedEventArgs e)
    {
        Canvas.Engine.ShowOrbits = OrbitsToggle.IsChecked == true;
    }

    private void OnScaleToggle(object? sender, RoutedEventArgs e)
    {
        bool on = LabelsToggle.IsChecked == true;
        LabelsToggle.Content = on ? "Realistic scale: ON" : "Realistic scale: OFF";
        // educational note: true scale is impossible to visualize; this toggles sun size
        var sun = Canvas.Engine.Solar.Objects.FirstOrDefault(o => o.Id == "sol") as StarObject;
        if (sun != null) sun.VisualRadius = on ? 6.9 : 16;
        ToastMsg(on ? "Sun radius set closer to true ratio (planets would be invisible at real scale!)"
                    : "Visual scale restored");
    }

    // ---------------- misc ----------------

    private void OnScreenshot(object? sender, RoutedEventArgs e) => Canvas.SaveScreenshot();

    private void OnHelp(object? sender, RoutedEventArgs e)
    {
        ShowObjectInfo(null);
        ToastMsg("Drag to orbit · wheel to zoom · right-drag to pan · click to select · double-click to fly");
    }

    private void ToastMsg(string msg)
    {
        ToastText.Text = msg;
        Toast.Opacity = 1;
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.6) };
        t.Tick += (_, _) => { Toast.Opacity = 0; t.Stop(); };
        t.Start();
    }
}
