using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AudioSwitcher.Views;

public partial class AboutWindow : Window
{
    public AboutWindow() => InitializeComponent();

    private void OnCloseClick(object? sender, RoutedEventArgs eventArgs) => Close();
}
