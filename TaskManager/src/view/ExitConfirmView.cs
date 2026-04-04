using System;
using TaskManager.Core.Navigation;

namespace TaskManager.View;

public sealed class ExitConfirmView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private int _selectedIndex; // 0 = Yes, 1 = No

    public ExitConfirmView(IViewNavigator viewNavigator)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _selectedIndex = 1; // default: No
    }

    public ViewId ViewId => ViewId.ExitConfirm;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        _selectedIndex = 1;
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │   Exit Task Manager?     │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ Move    Enter Select    Alt+Enter Cancel");
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < 2; i++)
        {
            string label = i == 0 ? "Yes — quit the application" : "No  — go back";
            if (i == _selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ▶  {label.PadRight(28)} ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"     {label}");
                Console.ResetColor();
            }
        }
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key == ConsoleKey.UpArrow && keyInfo.Modifiers == 0)
        {
            if (_selectedIndex > 0)
            {
                _selectedIndex--;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.DownArrow && keyInfo.Modifiers == 0)
        {
            if (_selectedIndex < 1)
            {
                _selectedIndex++;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            if (_selectedIndex == 0)
                _viewNavigator.Exit();
            else
                _viewNavigator.Pop();
            return true;
        }

        return false;
    }
}
