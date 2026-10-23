using Avalonia;
using System;

namespace DimensionSimulator;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (Array.IndexOf(args, "--shots") >= 0)
        {
            // preview tour: boot, render each view, save PNGs, exit
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
            return;
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
