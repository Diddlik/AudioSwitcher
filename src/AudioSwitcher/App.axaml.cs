using AudioSwitcher.Services;
using AudioSwitcher.ViewModels;
using AudioSwitcher.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AudioSwitcher;

public partial class App : Application
{
    private GlobalHotkeyService? _hotkeyService;
    private MainWindowViewModel? _viewModel;
    private TrayIcon? _trayIcon;
    private AutoUpdateService? _autoUpdateService;
    private readonly CancellationTokenSource _updateCancellation = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hotkeyService = new GlobalHotkeyService();
            _viewModel = new MainWindowViewModel(
                new ConfigurationStore(),
                new AudioDeviceService(),
                new StartupService(),
                _hotkeyService);

            var window = new MainWindow { DataContext = _viewModel };
            desktop.MainWindow = window;
            CreateTrayIcon(desktop, window);
            _autoUpdateService = new AutoUpdateService();
            _ = CheckForUpdatesAsync();

            if (desktop.Args?.Contains("--minimized", StringComparer.OrdinalIgnoreCase) == true)
            {
                window.Opened += (_, _) => window.Hide();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        var openItem = new NativeMenuItem("Öffnen");
        openItem.Click += (_, _) => ShowWindow(window);
        var exitItem = new NativeMenuItem("Beenden");
        exitItem.Click += (_, _) => Exit(desktop, window);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AudioSwitcher/Assets/avalonia-logo.ico"))),
            ToolTipText = "AudioSwitcher",
            Menu = new NativeMenu { Items = { openItem, new NativeMenuItemSeparator(), exitItem } },
        };
        _trayIcon.Clicked += (_, _) => ShowWindow(window);
        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private static void ShowWindow(MainWindow window)
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    private void Exit(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        _updateCancellation.Cancel();
        try
        {
            _autoUpdateService?.SchedulePendingUpdate();
        }
        catch (Exception exception)
        {
            _viewModel?.ReportBackgroundError($"Update konnte nicht vorbereitet werden: {exception.Message}");
        }

        TrayIcon.SetIcons(this, null);
        _trayIcon?.Dispose();
        _viewModel?.Dispose();
        _hotkeyService?.Dispose();
        window.CloseForExit();
        desktop.Shutdown();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await _autoUpdateService!.CheckAndDownloadAsync(
                message => Dispatcher.UIThread.Post(() => _viewModel?.ReportBackgroundStatus(message)),
                _updateCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Dispatcher.UIThread.Post(() =>
                _viewModel?.ReportBackgroundError($"Update-Prüfung fehlgeschlagen: {exception.Message}"));
        }
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        foreach (var plugin in BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray())
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
