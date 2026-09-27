using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AudioSwitcher.Services;

public sealed class ProfileNotificationService : IDisposable
{
    private const double OverlayWidth = 400;
    private const double OverlayHeight = 72;
    private const int ExtendedWindowStyle = -20;
    private const long TransparentWindowStyle = 0x00000020L;
    private const long ToolWindowStyle = 0x00000080L;
    private const long NoActivateWindowStyle = 0x08000000L;
    private const uint PlaySoundAsync = 0x0001;
    private const uint PlaySoundNoDefault = 0x0002;
    private const uint PlaySoundAlias = 0x00010000;
    private readonly Window _screenSource;
    private readonly DispatcherTimer _closeTimer;
    private readonly List<Window> _windows = [];

    public ProfileNotificationService(Window screenSource)
    {
        _screenSource = screenSource;
        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.6) };
        _closeTimer.Tick += (_, _) => CloseAll();
    }

    public void Show(string profileName)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Show(profileName));
            return;
        }

        try
        {
            CloseAll();
            foreach (var screen in _screenSource.Screens.All)
            {
                var window = CreateWindow(profileName, screen);
                window.Opened += (_, _) => MakeNonActivating(window);
                window.Show();
                _windows.Add(window);
            }

            PlaySound("SystemAsterisk", IntPtr.Zero, PlaySoundAlias | PlaySoundAsync | PlaySoundNoDefault);
            _closeTimer.Start();
        }
        catch
        {
            CloseAll();
        }
    }

    public void Dispose() => CloseAll();

    private static Window CreateWindow(string profileName, Screen screen) => new()
    {
        Width = OverlayWidth,
        Height = OverlayHeight,
        Position = CalculatePosition(screen),
        WindowStartupLocation = WindowStartupLocation.Manual,
        SystemDecorations = SystemDecorations.None,
        ShowInTaskbar = false,
        ShowActivated = false,
        CanResize = false,
        Topmost = true,
        IsHitTestVisible = false,
        Background = Brushes.Transparent,
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 24, 24, 27)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24, 16),
            Child = new TextBlock
            {
                Text = $"✓  Profil „{profileName}“ aktiviert",
                Foreground = Brushes.White,
                FontSize = 19,
                FontWeight = FontWeight.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        },
    };

    private static PixelPoint CalculatePosition(Screen screen)
    {
        var width = (int)Math.Round(OverlayWidth * screen.Scaling);
        var height = (int)Math.Round(OverlayHeight * screen.Scaling);
        var bottomMargin = (int)Math.Round(64 * screen.Scaling);
        return new PixelPoint(
            screen.Bounds.X + (screen.Bounds.Width - width) / 2,
            screen.Bounds.Bottom - height - bottomMargin);
    }

    private static void MakeNonActivating(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero) return;

        var style = GetExtendedWindowStyle(handle);
        SetExtendedWindowStyle(
            handle,
            style | TransparentWindowStyle | ToolWindowStyle | NoActivateWindowStyle);
    }

    private void CloseAll()
    {
        _closeTimer.Stop();
        foreach (var window in _windows) window.Close();
        _windows.Clear();
    }

    private static long GetExtendedWindowStyle(IntPtr window) => IntPtr.Size == 8
        ? GetWindowLongPtr(window, ExtendedWindowStyle).ToInt64()
        : GetWindowLong(window, ExtendedWindowStyle);

    private static void SetExtendedWindowStyle(IntPtr window, long style)
    {
        if (IntPtr.Size == 8) SetWindowLongPtr(window, ExtendedWindowStyle, new IntPtr(style));
        else SetWindowLong(window, ExtendedWindowStyle, unchecked((int)style));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr window, int index, int value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(string sound, IntPtr module, uint flags);
}
