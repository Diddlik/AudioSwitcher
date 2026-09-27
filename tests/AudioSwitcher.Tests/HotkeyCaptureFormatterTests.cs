using AudioSwitcher.Services;
using Avalonia.Input;

namespace AudioSwitcher.Tests;

public sealed class HotkeyCaptureFormatterTests
{
    [Theory]
    [InlineData(Key.D1, KeyModifiers.Control | KeyModifiers.Alt, "Ctrl+Alt+1")]
    [InlineData(Key.F12, KeyModifiers.Control | KeyModifiers.Shift, "Ctrl+Shift+F12")]
    [InlineData(Key.Space, KeyModifiers.Meta, "Win+Space")]
    [InlineData(Key.PageDown, KeyModifiers.Alt, "Alt+PageDown")]
    public void TryFormat_SupportedCombination_ReturnsCanonicalText(
        Key key, KeyModifiers modifiers, string expected)
    {
        Assert.True(HotkeyCaptureFormatter.TryFormat(key, modifiers, out var hotkey));
        Assert.Equal(expected, hotkey);
    }

    [Theory]
    [InlineData(Key.A, KeyModifiers.None)]
    [InlineData(Key.Escape, KeyModifiers.Control)]
    [InlineData(Key.LeftCtrl, KeyModifiers.Control)]
    public void TryFormat_IncompleteOrUnsupportedCombination_ReturnsFalse(Key key, KeyModifiers modifiers)
    {
        Assert.False(HotkeyCaptureFormatter.TryFormat(key, modifiers, out var hotkey));
        Assert.Empty(hotkey);
    }
}
