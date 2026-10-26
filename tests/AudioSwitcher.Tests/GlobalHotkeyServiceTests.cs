using System.Runtime.InteropServices;
using AudioSwitcher.Services;

namespace AudioSwitcher.Tests;

public sealed class GlobalHotkeyServiceTests
{
    private const int BlockerId = 0xA5A5;
    private const uint ControlAltShift = 0x0001 | 0x0002 | 0x0004;
    private const uint F23 = 0x86;

    [Fact]
    public void Register_ShortcutInUse_ReportsItAndKeepsOtherShortcuts()
    {
        // Simulates another application that already owns Ctrl+Alt+Shift+F23.
        Assert.True(RegisterHotKey(IntPtr.Zero, BlockerId, ControlAltShift, F23));
        try
        {
            using var service = new GlobalHotkeyService(new LocalizationService());

            var registered = service.Register(
                new Dictionary<string, string> { ["taken"] = "Ctrl+Alt+Shift+F23", ["free"] = "Ctrl+Alt+Shift+F22" },
                out var error,
                out var unavailable);

            Assert.False(registered);
            Assert.Contains("Ctrl+Alt+Shift+F23", error);
            Assert.Equal(["taken"], unavailable);
        }
        finally
        {
            UnregisterHotKey(IntPtr.Zero, BlockerId);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
}
