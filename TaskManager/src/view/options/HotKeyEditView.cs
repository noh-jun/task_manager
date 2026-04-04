using System;
using System.Collections.Generic;
using TaskManager.Core.Navigation;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Options;

public sealed class HotKeyEditView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly HotKeyCaptureSession _captureSession;
    private readonly IReadOnlyList<HotKeyAction> _editableActions;

    private int _selectedIndex;
    private string _statusMessage;

    public HotKeyEditView(
        IViewNavigator viewNavigator,
        HotKeyConfig hotKeyConfig,
        HotKeyCaptureSession captureSession)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig = hotKeyConfig ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _captureSession = captureSession ?? throw new ArgumentNullException(nameof(captureSession));
        _editableActions = HotKeyHelper.GetEditableActions();

        _selectedIndex = 0;
        _statusMessage = "Select an action and press Enter to change key.";
    }

    public ViewId ViewId => ViewId.HotKeyEdit;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        if (_captureSession.LastResultMessage is not null)
        {
            _statusMessage = _captureSession.LastResultMessage;
            _captureSession.ClearLastResultMessage();
        }

        InvalidateRequested?.Invoke();
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Options  ›  HotKey Edit");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │       HotKey Edit        │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ Move    Enter Edit    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Editable actions list
        for (int i = 0; i < _editableActions.Count; i++)
        {
            HotKeyAction action = _editableActions[i];
            string actionName = HotKeyHelper.GetActionDisplayName(action).PadRight(14);
            string keyText = _hotKeyConfig.GetGesture(action).ToDetailString();

            if (i == _selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ▶  {actionName} {keyText.PadRight(14)} ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"     {actionName} {keyText}");
                Console.ResetColor();
            }
        }

        // Fixed keys
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("     ↑ Move Up    ↓ Move Down    Enter Select    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();

        // Status at bottom
        int currentRow = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 6);
        int paddingLines = windowHeight - currentRow - 5;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {_statusMessage}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
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
            if (_selectedIndex < _editableActions.Count - 1)
            {
                _selectedIndex++;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (HotKeyHelper.IsSelectKey(keyInfo))
        {
            HotKeyAction selectedAction = _editableActions[_selectedIndex];
            _captureSession.Begin(selectedAction);
            _viewNavigator.Push(ViewId.HotKeyCapture);
            return true;
        }

        return false;
    }
}
