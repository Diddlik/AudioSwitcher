using Avalonia;
using Velopack;

namespace AudioSwitcher;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        using var mutex = new Mutex(true, @"Local\AudioSwitcher.Singleton", out var isFirstInstance);
        if (!isFirstInstance) return;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseWin32().UseSkia().LogToTrace();
}
