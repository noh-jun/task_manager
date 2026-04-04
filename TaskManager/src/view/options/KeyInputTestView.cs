using System;
using TaskManager.Core.Navigation;

namespace TaskManager.View.Options;

public sealed class KeyInputTestView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private ConsoleKeyInfo? _lastKey;

    public KeyInputTestView(IViewNavigator viewNavigator)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _lastKey = null;
    }

    public ViewId ViewId => ViewId.KeyInputTest;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Options  ›  Key Input Test");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │      Key Input Test      │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Instruction
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("  Press any key to see its details.");
        Console.WriteLine("  (Global keys are intercepted by the navigator.)");
        Console.ResetColor();
        Console.WriteLine();

        // Last key display
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();

        if (_lastKey is null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Waiting for key input...");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Key       ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(_lastKey.Value.Key.ToString());

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Char      ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"'{FormatKeyChar(_lastKey.Value.KeyChar)}'");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Modifiers ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(FormatModifiers(_lastKey.Value.Modifiers));
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        _lastKey = keyInfo;
        InvalidateRequested?.Invoke();
        return true;
    }

    private static string FormatKeyChar(char keyChar)
    {
        if (keyChar == '\0')
        {
            return "\\0";
        }

        if (keyChar == '\r')
        {
            return "\\r";
        }

        if (keyChar == '\n')
        {
            return "\\n";
        }

        if (keyChar == '\t')
        {
            return "\\t";
        }

        if (char.IsControl(keyChar))
        {
            return $"0x{(int)keyChar:X2}";
        }

        return keyChar.ToString();
    }

    private static string FormatModifiers(ConsoleModifiers consoleModifiers)
    {
        return consoleModifiers == 0
            ? "None"
            : consoleModifiers.ToString();
    }
}