using AudioSwitcher.Services;
using AudioSwitcher.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace AudioSwitcher.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
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

    private void OnHotkeyGotFocus(object? sender, GotFocusEventArgs eventArgs)
    {
        if (sender is TextBox textBox) textBox.SelectAll();
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (sender is not TextBox textBox) return;

        if (eventArgs.Key is Key.Back ||
            eventArgs.Key is Key.Delete && eventArgs.KeyModifiers == KeyModifiers.None)
        {
            textBox.Text = string.Empty;
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
            textBox.Text = hotkey;
        }
        else if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ReportBackgroundError("Shortcut benötigt Ctrl, Alt, Shift oder Win plus eine Taste.");
        }

        eventArgs.Handled = true;
    }
}
