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
    private AboutWindowViewModel? _aboutViewModel;
    private ProfileNotificationService? _profileNotificationService;
    private readonly LocalizationService _text = new();
    private readonly CancellationTokenSource _updateCancellation = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hotkeyService = new GlobalHotkeyService(_text);
            _viewModel = new MainWindowViewModel(
                new ConfigurationStore(),
                new AudioDeviceService(),
                new StartupService(),
                _hotkeyService,
                _text);

            _autoUpdateService = new AutoUpdateService(_text);
            _aboutViewModel = new AboutWindowViewModel(_text, CheckForUpdatesAsync);
            var window = new MainWindow { DataContext = _viewModel, AboutViewModel = _aboutViewModel };
            desktop.MainWindow = window;
            _profileNotificationService = new ProfileNotificationService(window, _text);
            _viewModel.ProfileActivated += _profileNotificationService.Show;
            CreateTrayIcon(desktop, window);
            _ = _aboutViewModel.CheckForUpdatesCommand.ExecuteAsync(null);

            if (desktop.Args?.Contains("--minimized", StringComparer.OrdinalIgnoreCase) == true)
            {
                window.Opened += (_, _) => window.Hide();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        var openItem = new NativeMenuItem(_text["Open"]);
        openItem.Click += (_, _) => ShowWindow(window);
        var exitItem = new NativeMenuItem(_text["Exit"]);
        exitItem.Click += (_, _) => Exit(desktop, window);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AudioSwitcher/Assets/audioswitcher.ico"))),
            ToolTipText = "AudioSwitcher",
            Menu = new NativeMenu { Items = { openItem, new NativeMenuItemSeparator(), exitItem } },
        };
        _trayIcon.Clicked += (_, _) => ShowWindow(window);
        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
        _text.LanguageChanged += (_, _) =>
        {
            openItem.Header = _text["Open"];
            exitItem.Header = _text["Exit"];
        };
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
            _viewModel?.ReportBackgroundError(_text.Format("UpdatePreparingFailed", exception.Message));
        }

        TrayIcon.SetIcons(this, null);
        _trayIcon?.Dispose();
        _profileNotificationService?.Dispose();
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
                message => Dispatcher.UIThread.Post(() =>
                {
                    _viewModel?.ReportBackgroundStatus(message);
                    if (_aboutViewModel is not null) _aboutViewModel.UpdateStatus = message;
                }),
                _updateCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var message = _text.Format("UpdateCheckFailed", exception.Message);
                _viewModel?.ReportBackgroundError(message);
                if (_aboutViewModel is not null) _aboutViewModel.UpdateStatus = message;
            });
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
