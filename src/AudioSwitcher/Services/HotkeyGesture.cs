using System.Globalization;

namespace AudioSwitcher.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
    NoRepeat = 0x4000,
}

public readonly record struct HotkeyGesture(HotkeyModifiers Modifiers, uint VirtualKey)
{
    public static bool TryParse(string? text, out HotkeyGesture gesture, out string error,
        LocalizationService? localization = null)
    {
        localization ??= new LocalizationService();
        gesture = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = localization["ShortcutMissing"];
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var modifiers = HotkeyModifiers.NoRepeat;
        uint key = 0;

        foreach (var rawPart in parts)
        {
            var part = rawPart.ToUpperInvariant();
            switch (part)
            {
                case "CTRL" or "CONTROL" or "STRG":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "ALT":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "SHIFT" or "UMSCHALT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "WIN" or "WINDOWS":
                    modifiers |= HotkeyModifiers.Windows;
                    break;
                default:
                    if (key != 0 || !TryParseKey(part, out key))
                    {
                        error = localization.Format("InvalidKey", rawPart);
                        return false;
                    }

                    break;
            }
        }

        if (key == 0)
        {
            error = localization["ShortcutKeyRequired"];
            return false;
        }

        if ((modifiers & ~HotkeyModifiers.NoRepeat) == HotkeyModifiers.None)
        {
            error = localization["ShortcutModifierRequired"];
            return false;
        }

        gesture = new HotkeyGesture(modifiers, key);
        return true;
    }

    private static bool TryParseKey(string value, out uint key)
    {
        key = 0;
        if (value.Length == 1 && char.IsAsciiLetterOrDigit(value[0]))
        {
            key = char.ToUpper(value[0], CultureInfo.InvariantCulture);
            return true;
        }

        if (value.StartsWith('F') && int.TryParse(value.AsSpan(1), out var functionKey) && functionKey is >= 1 and <= 24)
        {
            key = (uint)(0x70 + functionKey - 1);
            return true;
        }

        key = value switch
        {
            "SPACE" or "LEERTASTE" => 0x20,
            "TAB" => 0x09,
            "HOME" or "POS1" => 0x24,
            "END" or "ENDE" => 0x23,
            "PAGEUP" or "BILDHOCH" => 0x21,
            "PAGEDOWN" or "BILDRUNTER" => 0x22,
            "INSERT" or "EINFG" => 0x2D,
            "DELETE" or "ENTF" => 0x2E,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            _ => 0,
        };
        return key != 0;
    }
}
