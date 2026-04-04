using System;
using TaskManager.Core.Navigation;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Options;

public sealed class OptionsMenuView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly string[] _menuItems;
    private int _selectedIndex;

    public OptionsMenuView(IViewNavigator viewNavigator, HotKeyConfig hotKeyConfig)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig = hotKeyConfig ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _menuItems = new[]
        {
            "Key Input Test",
            "HotKey Edit",
            "Logs",
        };
        _selectedIndex = 0;
    }

    public ViewId ViewId => ViewId.OptionsMenu;

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
        Console.WriteLine("  Main Menu  ›  Options");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │         Options          │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ↑↓ Move    Enter Select    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Menu list
        for (int i = 0; i < _menuItems.Length; i++)
        {
            if (i == _selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ▶  {_menuItems[i].PadRight(24)} ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"     {_menuItems[i]}");
                Console.ResetColor();
            }
        }

        // Details at bottom
        int currentRow = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 7);
        int paddingLines = windowHeight - currentRow - 6;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        (string en, string ko) = GetMenuDetail(_selectedIndex);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {en}");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  {ko}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    private static (string en, string ko) GetMenuDetail(int index)
    {
        return index switch
        {
            0 => ("Test keyboard input — see key names and modifier combinations.",
                  "키보드 입력을 테스트합니다 — 키 이름과 조합을 확인할 수 있습니다."),
            1 => ("Rebind hotkeys for task operations (Add, Edit, Delete).",
                  "태스크 작업(추가, 수정, 삭제)의 단축키를 재설정합니다."),
            2 => ("View in-memory application logs.",
                  "앱 실행 중 기록된 로그를 확인합니다."),
            _ => (string.Empty, string.Empty),
        };
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.MoveUp))
        {
            MoveUp();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.MoveDown))
        {
            MoveDown();
            return true;
        }

        if (HotKeyHelper.IsSelectKey(keyInfo))
        {
            OpenSelectedMenu();
            return true;
        }

        return false;
    }

    private void MoveUp()
    {
        if (_selectedIndex > 0)
        {
            _selectedIndex--;
            InvalidateRequested?.Invoke();
        }
    }

    private void MoveDown()
    {
        if (_selectedIndex < _menuItems.Length - 1)
        {
            _selectedIndex++;
            InvalidateRequested?.Invoke();
        }
    }

    private void OpenSelectedMenu()
    {
        switch (_selectedIndex)
        {
            case 0:
                _viewNavigator.Push(ViewId.KeyInputTest);
            break;

            case 1:
                _viewNavigator.Push(ViewId.HotKeyEdit);
            break;

            case 2:
                _viewNavigator.Push(ViewId.Logs);
            break;
        }
    }
}