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
    private ProfileControlServer? _controlServer;
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
            _aboutViewModel = new AboutWindowViewModel(_text, CheckForUpdatesAsync, InstallUpdate);
            _viewModel.Updates = _aboutViewModel;
            var window = new MainWindow { DataContext = _viewModel, AboutViewModel = _aboutViewModel };
            desktop.MainWindow = window;
            _profileNotificationService = new ProfileNotificationService(window, _text);
            _viewModel.ProfileActivated += _profileNotificationService.Show;
            CreateTrayIcon(desktop, window);
            var viewModel = _viewModel;
            _controlServer = new ProfileControlServer(
                request => Dispatcher.UIThread.InvokeAsync(() => viewModel.HandleControlRequest(request)).GetTask());
            _controlServer.Start();
            _viewModel.ActivateStartupProfile();
            _ = _aboutViewModel.CheckForUpdatesCommand.ExecuteAsync(null);

            if (desktop.Args?.Contains("--minimized", StringComparer.OrdinalIgnoreCase) == true)
            {
                // Avalonia raises Opened on every Show, so hide only on the first one.
                void HideOnFirstOpen(object? sender, EventArgs eventArgs)
                {
                    window.Opened -= HideOnFirstOpen;
                    window.Hide();
                }

                window.Opened += HideOnFirstOpen;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AudioSwitcher/Assets/audioswitcher.ico"))),
            ToolTipText = "AudioSwitcher",
        };
        _trayIcon.Clicked += (_, _) => ShowWindow(window);
        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
        UpdateTrayMenu(desktop, window);
        _viewModel!.StateChanged += () => UpdateTrayMenu(desktop, window);
    }

    private void UpdateTrayMenu(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        if (_trayIcon is null || _viewModel is not { } viewModel) return;

        var state = viewModel.HandleControlRequest(new ControlRequest("state", null));
        var menu = new NativeMenu();
        foreach (var profile in state.Profiles)
        {
            var item = new NativeMenuItem(profile.Name)
            {
                ToggleType = NativeMenuItemToggleType.Radio,
                IsChecked = profile.Id == state.ActiveProfileId,
                IsEnabled = profile.Available,
            };
            item.Click += (_, _) => viewModel.HandleControlRequest(new ControlRequest("activate", profile.Id));
            menu.Items.Add(item);
        }

        var quickSwitchItem = new NativeMenuItem(_text["QuickSwitch"])
        {
            IsEnabled = viewModel.ToggleProfileA is not null && viewModel.ToggleProfileB is not null,
        };
        quickSwitchItem.Click += (_, _) => viewModel.HandleControlRequest(new ControlRequest("toggle", null));
        var openItem = new NativeMenuItem(_text["Open"]);
        openItem.Click += (_, _) => ShowWindow(window);
        var exitItem = new NativeMenuItem(_text["Exit"]);
        exitItem.Click += (_, _) => Exit(desktop, window);

        if (menu.Items.Count > 0) menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(quickSwitchItem);
        menu.Items.Add(openItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);
        _trayIcon.Menu = menu;
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
        _trayIcon = null;
        _controlServer?.Dispose();
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
                    if (_aboutViewModel is not null)
                    {
                        _aboutViewModel.UpdateStatus = message;
                        _aboutViewModel.IsUpdateReady = _autoUpdateService.HasPendingUpdate;
                    }
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

    private void InstallUpdate()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not MainWindow window) return;
        try
        {
            if (_autoUpdateService?.SchedulePendingUpdate(restart: true) == true)
            {
                _aboutViewModel!.IsUpdateReady = false;
                Exit(desktop, window);
            }
        }
        catch (Exception exception)
        {
            var message = _text.Format("UpdatePreparingFailed", exception.Message);
            _viewModel?.ReportBackgroundError(message);
            if (_aboutViewModel is not null) _aboutViewModel.UpdateStatus = message;
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
