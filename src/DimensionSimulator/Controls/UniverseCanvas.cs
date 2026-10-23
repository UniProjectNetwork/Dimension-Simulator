using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DimensionEngine;
using DimensionEngine.Universe;
using SkiaSharp;

namespace DimensionSimulator.Controls;

/// <summary>
/// The main simulation viewport: owns the SimulationEngine, renders each frame into a
/// WriteableBitmap, and translates mouse/keyboard input into orbit / zoom / pan / pick /
/// fly-to interactions.
/// </summary>
public class UniverseCanvas : Control
{
    public SimulationEngine Engine { get; } = new();

    private WriteableBitmap? _bitmap;
    private TimeSpan _lastTick;
    private bool _dragging;
    private bool _panning;
    private Point _lastPoint;
    private Point _pressPoint;
    private DateTime _pressTime;
    private bool _movedSincePress;

    /// <summary>Fired ~5x per second with HUD stats (distance readout, speed label, date).</summary>
    public event Action? HudUpdate;
    /// <summary>Fired when the selected object changes.</summary>
    public event Action<SpaceObject?>? SelectionChanged;
    /// <summary>Fired when the view mode changes.</summary>
    public event Action<ViewMode>? ModeChanged;

    public UniverseCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
        DoubleTapped += OnDoubleTappedHandler;
    }

    private void OnDoubleTappedHandler(object? sender, TappedEventArgs e)
    {
        var pt = e.GetPosition(this);
        var picked = Engine.Pick(pt.X, pt.Y);
        if (picked != null)
        {
            Engine.Select(picked);
            SelectionChanged?.Invoke(picked);
            Engine.FlyTo(picked);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _lastTick = TimeSpan.FromMilliseconds(Environment.TickCount64);
        DispatcherTimer.Run(Tick, TimeSpan.FromMilliseconds(15));
        Focus();
    }

    private bool Tick()
    {
        var now = TimeSpan.FromMilliseconds(Environment.TickCount64);
        double dt = (now - _lastTick).TotalSeconds;
        _lastTick = now;
        dt = Math.Clamp(dt, 0, 0.1);
        Engine.Tick(dt);
        InvalidateVisual();
        HudUpdate?.Invoke();
        return true;
    }

    public override void Render(DrawingContext context)
    {
        double scale = VisualRoot?.RenderScaling ?? 1;
        int pw = (int)Math.Clamp(Bounds.Width * scale, 64, 2560);
        int ph = (int)Math.Clamp(Bounds.Height * scale, 64, 1600);

        var frame = Engine.RenderFrame(pw, ph, (float)scale);

        int w = frame.Width, h = frame.Height;
        if (_bitmap == null || _bitmap.PixelSize.Width != w || _bitmap.PixelSize.Height != h)
        {
            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);
        }

        var pix = frame.PeekPixels();
        if (pix != null && _bitmap != null)
        {
            using var fb = _bitmap.Lock();
            long bytes = (long)w * h * 4;
            unsafe
            {
                Buffer.MemoryCopy((void*)pix.GetPixels(), (void*)fb.Address, bytes, bytes);
            }
        }

        if (_bitmap != null)
        {
            var dest = new Rect(0, 0, Bounds.Width, Bounds.Height);
            context.DrawImage(_bitmap, dest);
        }
    }

    // ---------------- input ----------------

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        _lastPoint = pt.Position;
        _pressPoint = pt.Position;
        _pressTime = DateTime.UtcNow;
        _movedSincePress = false;
        if (pt.Properties.IsLeftButtonPressed) _dragging = true;
        if (pt.Properties.IsMiddleButtonPressed || pt.Properties.IsRightButtonPressed) _panning = true;
        e.Pointer.Capture(this);
        Focus();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);
        var pos = pt.Position;
        double dx = pos.X - _lastPoint.X;
        double dy = pos.Y - _lastPoint.Y;
        double moveX = pos.X - _pressPoint.X, moveY = pos.Y - _pressPoint.Y;
        if (moveX * moveX + moveY * moveY > 16) _movedSincePress = true;

        if (pt.Properties.IsLeftButtonPressed && _dragging)
        {
            Engine.Orbit(dx, dy);
            _lastPoint = pos;
            e.Handled = true;
        }
        else if (pt.Properties.IsRightButtonPressed || (pt.Properties.IsMiddleButtonPressed && _panning))
        {
            Engine.Pan(dx, dy, Bounds.Height);
            _lastPoint = pos;
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        bool wasClick = !_movedSincePress && (DateTime.UtcNow - _pressTime).TotalMilliseconds < 350;
        if (wasClick && _dragging)
        {
            var picked = Engine.Pick(_pressPoint.X, _pressPoint.Y);
            Engine.Select(picked);
            SelectionChanged?.Invoke(picked);
        }
        _dragging = false;
        _panning = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double factor = Math.Pow(1.18, e.Delta.Y);
        Engine.ZoomBy(factor);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Space:
                Engine.Clock.Paused = !Engine.Clock.Paused;
                HudUpdate?.Invoke();
                e.Handled = true;
                break;
            case Key.O:
                Engine.ShowOrbits = !Engine.ShowOrbits;
                e.Handled = true;
                break;
            case Key.Add or Key.OemPlus:
                Engine.ZoomBy(1.25);
                e.Handled = true;
                break;
            case Key.Subtract or Key.OemMinus:
                Engine.ZoomBy(0.8);
                e.Handled = true;
                break;
            case Key.Left: Engine.Orbit(-24, 0); e.Handled = true; break;
            case Key.Right: Engine.Orbit(24, 0); e.Handled = true; break;
            case Key.Up: Engine.Orbit(0, -18); e.Handled = true; break;
            case Key.Down: Engine.Orbit(0, 18); e.Handled = true; break;
            case Key.Escape:
                Engine.Select(null);
                SelectionChanged?.Invoke(null);
                e.Handled = true;
                break;
        }
    }

    // ---------------- public API for the window ----------------

    public void SetMode(ViewMode mode)
    {
        Engine.SetMode(mode);
        Engine.Select(null);
        SelectionChanged?.Invoke(null);
        ModeChanged?.Invoke(mode);
        InvalidateVisual();
    }

    public void FlyTo(SpaceObject o)
    {
        Engine.Select(o);
        SelectionChanged?.Invoke(o);
        Engine.FlyTo(o);
    }

    public async void SaveScreenshot()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top == null) return;
        var file = await top.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Save screenshot",
            SuggestedFileName = $"DimensionSimulator_{DateTime.Now:yyyyMMdd_HHmmss}",
            DefaultExtension = "png",
            FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("PNG image") { Patterns = new[] { "*.png" } } }
        });
        if (file == null) return;
        double scale = VisualRoot?.RenderScaling ?? 1;
        var png = Engine.ScreenshotPng(
            (int)Math.Clamp(Bounds.Width * scale, 64, 3840),
            (int)Math.Clamp(Bounds.Height * scale, 64, 2160), (float)scale);
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(png, 0, png.Length);
    }
}
