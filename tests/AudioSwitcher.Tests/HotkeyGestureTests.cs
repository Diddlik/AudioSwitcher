using AudioSwitcher.Services;

namespace AudioSwitcher.Tests;

public sealed class HotkeyGestureTests
{
    [Theory]
    [InlineData("Ctrl+Alt+1", HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.NoRepeat, 0x31)]
    [InlineData("Strg + Shift + F12", HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.NoRepeat, 0x7B)]
    [InlineData("Win+Space", HotkeyModifiers.Windows | HotkeyModifiers.NoRepeat, 0x20)]
    public void TryParse_ValidShortcut_ReturnsGesture(string text, HotkeyModifiers modifiers, uint key)
    {
        var result = HotkeyGesture.TryParse(text, out var gesture, out var error);

        Assert.True(result, error);
        Assert.Equal(modifiers, gesture.Modifiers);
        Assert.Equal(key, gesture.VirtualKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+Alt+A+B")]
    [InlineData("Ctrl+F25")]
    public void TryParse_InvalidShortcut_ReturnsError(string text)
    {
        Assert.False(HotkeyGesture.TryParse(text, out _, out var error));
        Assert.NotEmpty(error);
    }
}
