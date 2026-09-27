using Avalonia.Input;

namespace AudioSwitcher.Services;

public static class HotkeyCaptureFormatter
{
    private const KeyModifiers SupportedModifiers =
        KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift | KeyModifiers.Meta;

    public static bool TryFormat(Key key, KeyModifiers modifiers, out string hotkey)
    {
        hotkey = string.Empty;
        var keyName = GetKeyName(key);
        modifiers &= SupportedModifiers;
        if (keyName is null || modifiers == KeyModifiers.None) return false;

        var parts = new List<string>(5);
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win");
        parts.Add(keyName);
        hotkey = string.Join('+', parts);
        return true;
    }

    public static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;

    private static string? GetKeyName(Key key)
    {
        if (key is >= Key.A and <= Key.Z) return key.ToString();
        if (key is >= Key.D0 and <= Key.D9) return ((char)('0' + key - Key.D0)).ToString();
        if (key is >= Key.F1 and <= Key.F24) return key.ToString();

        return key switch
        {
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Insert => "Insert",
            Key.Delete => "Delete",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            _ => null,
        };
    }
}
