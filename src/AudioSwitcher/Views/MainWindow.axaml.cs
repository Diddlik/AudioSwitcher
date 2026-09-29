using AudioSwitcher.Services;
using AudioSwitcher.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AudioSwitcher.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;
    public AboutWindowViewModel? AboutViewModel { get; init; }

    private async void OnAboutClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (AboutViewModel is not null)
            await new AboutWindow { DataContext = AboutViewModel }.ShowDialog(this);
    }

    public static FuncValueConverter<string?, string[]> HotkeyPartsConverter { get; } =
        new(value => (value ?? string.Empty).Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        SizeChanged += OnSizeChanged;
    }

    public void CloseForExit()
    {
        _allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs eventArgs)
    {
        if (_allowClose) return;
        eventArgs.Cancel = true;
        Hide();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.Source is Visual visual && visual.FindAncestorOfType<Button>() is not null) return;
        if (!eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (eventArgs.ClickCount == 2) ToggleMaximized();
        else BeginMoveDrag(eventArgs);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs eventArgs) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs eventArgs) => ToggleMaximized();

    private void OnCaptionCloseClick(object? sender, RoutedEventArgs eventArgs) => Close();

    private void ToggleMaximized() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnSizeChanged(object? sender, SizeChangedEventArgs eventArgs)
    {
        var compact = eventArgs.NewSize.Width < 960;
        Sidebar.Width = compact ? 232 : 264;
        RefreshLabel.IsVisible = !compact;
        var horizontalPadding = compact ? 24 : 32;
        var margin = new Thickness(horizontalPadding, 28, horizontalPadding, 32);
        ProfileContent.Margin = margin;
        QuickContent.Margin = margin;
        SettingsContent.Margin = margin;
        var contentWidth = Math.Max(0, Math.Min(680, eventArgs.NewSize.Width - Sidebar.Width - 2 * horizontalPadding - 3));
        ProfileContent.Width = contentWidth;
        QuickContent.Width = contentWidth;
        SettingsContent.Width = contentWidth;
    }

    private void OnProfilePointerReleased(object? sender, PointerReleasedEventArgs eventArgs)
    {
        if (eventArgs.InitialPressMouseButton == MouseButton.Left &&
            eventArgs.Source is Visual visual && visual.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not null &&
            DataContext is MainWindowViewModel model)
            model.CurrentPage = MainPage.Profile;
    }

    private void OnProfileListKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Enter || DataContext is not MainWindowViewModel model || !model.HasSelectedProfile) return;
        model.CurrentPage = MainPage.Profile;
        ProfileName.Focus();
        eventArgs.Handled = true;
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (sender is not Button button) return;
        if (eventArgs.Key is Key.Tab or Key.Escape) return;

        if (eventArgs.Key is Key.Back ||
            eventArgs.Key is Key.Delete && eventArgs.KeyModifiers == KeyModifiers.None)
        {
            button.SetCurrentValue(ContentControl.ContentProperty, string.Empty);
            eventArgs.Handled = true;
            return;
        }

        if (HotkeyCaptureFormatter.IsModifierKey(eventArgs.Key))
        {
            eventArgs.Handled = true;
            return;
        }

        if (HotkeyCaptureFormatter.TryFormat(eventArgs.Key, eventArgs.KeyModifiers, out var hotkey))
        {
            button.SetCurrentValue(ContentControl.ContentProperty, hotkey);
        }
        else if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ReportBackgroundError(viewModel.Text["ShortcutCombinationRequired"]);
        }

        eventArgs.Handled = true;
    }
}
