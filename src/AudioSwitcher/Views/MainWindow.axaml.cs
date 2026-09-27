using Avalonia.Controls;
using Avalonia.Controls.Primitives;

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
}
