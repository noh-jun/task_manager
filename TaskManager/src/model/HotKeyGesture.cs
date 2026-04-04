using System;

namespace TaskManager.Model;

public readonly struct HotKeyGesture : IEquatable<HotKeyGesture>
{
    public HotKeyGesture(ConsoleKey key, ConsoleModifiers modifiers, char keyChar = '\0')
    {
        Key       = key;
        Modifiers = modifiers;
        KeyChar   = keyChar;
    }

    public ConsoleKey       Key       { get; }
    public ConsoleModifiers Modifiers { get; }
    public char             KeyChar   { get; }

    public bool Equals(HotKeyGesture other)
    {
        return Key == other.Key && KeyChar == other.KeyChar && Modifiers == other.Modifiers;
    }

    public override bool Equals(object? obj)
    {
        return obj is HotKeyGesture other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, KeyChar, Modifiers);
    }

    public override string ToString()
    {
        string keyPart = Key != 0 ? Key.ToString() : KeyChar.ToString();

        if (Modifiers == 0)
            return keyPart;

        return $"{FormatModifiers(Modifiers)}+{keyPart}";
    }

    // (Modifiers+)Key={Char} 형식 - HotKeyEditView 표기용
    public string ToDetailString()
    {
        string keyPart  = Key != 0 ? Key.ToString() : string.Empty;
        string charPart = (KeyChar != '\0' && !char.IsControl(KeyChar)) ? $"={{{KeyChar}}}" : string.Empty;
        string full     = string.IsNullOrEmpty(keyPart) ? KeyChar.ToString() : keyPart + charPart;

        if (Modifiers == 0)
            return full;

        return $"{FormatModifiers(Modifiers)}+{full}";
    }

    public string FormatKeyChar()
    {
        if (KeyChar == '\0') return "None";
        if (char.IsControl(KeyChar)) return $"0x{(int)KeyChar:X2}";
        return KeyChar.ToString();
    }

    public string FormatModifiersString()
    {
        return Modifiers == 0 ? "None" : FormatModifiers(Modifiers);
    }

    public static HotKeyGesture FromConsoleKeyInfo(ConsoleKeyInfo keyInfo)
    {
        return new HotKeyGesture(keyInfo.Key, keyInfo.Modifiers, keyInfo.KeyChar);
    }

    public static bool TryParse(string text, out HotKeyGesture gesture)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            gesture = default;
            return false;
        }

        string[] parts   = text.Split('+');
        string   keyPart = parts[^1];

        ConsoleModifiers modifiers = 0;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl":  modifiers |= ConsoleModifiers.Control; break;
                case "alt":   modifiers |= ConsoleModifiers.Alt;     break;
                case "shift": modifiers |= ConsoleModifiers.Shift;   break;
                default:
                    gesture = default;
                    return false;
            }
        }

        if (Enum.TryParse(keyPart, ignoreCase: true, out ConsoleKey key))
        {
            gesture = new HotKeyGesture(key, modifiers);
            return true;
        }

        if (keyPart.Length == 1)
        {
            gesture = new HotKeyGesture(0, modifiers, keyPart[0]);
            return true;
        }

        gesture = default;
        return false;
    }

    private static string FormatModifiers(ConsoleModifiers modifiers)
    {
        string result = string.Empty;

        if ((modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
            result = "Ctrl";

        if ((modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt)
            result = string.IsNullOrEmpty(result) ? "Alt" : $"{result}+Alt";

        if ((modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
            result = string.IsNullOrEmpty(result) ? "Shift" : $"{result}+Shift";

        return result;
    }
}
