using System;
using System.Collections.Generic;
using TaskManager.Core.Navigation;
using TaskManager.Model;

namespace TaskManager.View.Tasks;

public sealed class ArgsEditView : IView
{
    private enum EditState { Browse, EditKey, EditValue }

    private readonly IViewNavigator  _viewNavigator;
    private readonly HotKeyConfig    _hotKeyConfig;
    private readonly ArgsEditSession _argsEditSession;

    private readonly TextInputState _keyInput   = new();
    private readonly TextInputState _valueInput = new();

    private int       _selectedIndex;
    private EditState _editState;
    private string    _statusMessage;

    public ArgsEditView(
        IViewNavigator  viewNavigator,
        HotKeyConfig    hotKeyConfig,
        ArgsEditSession argsEditSession)
    {
        _viewNavigator   = viewNavigator   ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig    = hotKeyConfig    ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _argsEditSession = argsEditSession ?? throw new ArgumentNullException(nameof(argsEditSession));
        _selectedIndex   = 0;
        _editState       = EditState.Browse;
        _statusMessage   = string.Empty;
    }

    public ViewId ViewId => ViewId.ArgsEdit;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        ClampSelectedIndex();
        _editState = EditState.Browse;
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        List<ArgEntry> entries = _argsEditSession.Entries;

        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Task  ›  Edit Task  ›  Args");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │       Args Editor        │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        string addKey    = HotKeyHelper.ToDisplayText(HotKeyAction.ArgAdd,    _hotKeyConfig);
        string deleteKey = HotKeyHelper.ToDisplayText(HotKeyAction.ArgDelete, _hotKeyConfig);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        switch (_editState)
        {
            case EditState.Browse:
                Console.WriteLine($"  ↑↓ Move    Enter Edit    {addKey} Add    {deleteKey} Delete    Alt+Enter Done");
                break;
            case EditState.EditKey:
                Console.WriteLine("  Type Key    → Add/Edit Value    Enter Key-only    Alt+Enter Cancel");
                break;
            case EditState.EditValue:
                Console.WriteLine("  Type Value    ← Back to Key    Enter Confirm    Alt+Enter Cancel");
                break;
        }
        Console.ResetColor();
        Console.WriteLine();

        // Args list
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ── Args ───────────────────────────────────");
        Console.ResetColor();

        if (entries.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  (No args. Press {addKey} to add.)");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                RenderEntry(i, entries[i]);
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────────");
        Console.ResetColor();

        // Status
        int currentRow   = Console.CursorTop;
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
        return _editState switch
        {
            EditState.Browse     => HandleBrowse(keyInfo),
            EditState.EditKey    => HandleEditKey(keyInfo),
            EditState.EditValue  => HandleEditValue(keyInfo),
            _                    => false,
        };
    }

    // ── Render helpers ───────────────────────────────────────────────────────

    private void RenderEntry(int index, ArgEntry entry)
    {
        bool selected = (index == _selectedIndex);

        if (selected && _editState == EditState.EditKey)
        {
            string keyDisplay   = _keyInput.ToDisplayText();
            string valueDisplay = entry.IsKeyOnly ? string.Empty : $"  ={{{entry.Value}}}";
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.Write($"  ▶  [{keyDisplay.PadRight(22)}]");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(valueDisplay);
            Console.ResetColor();
        }
        else if (selected && _editState == EditState.EditValue)
        {
            string valueDisplay = _valueInput.ToDisplayText();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  ▶  {_keyInput.Text.PadRight(24)}");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.Write($"  =[{valueDisplay.PadRight(20)}]");
            Console.ResetColor();
            Console.WriteLine();
        }
        else if (selected)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            string valueDisplay = entry.IsKeyOnly ? string.Empty : $"  ={{{entry.Value}}}";
            Console.Write($"  ▶  {entry.Key.PadRight(24)}{valueDisplay}");
            Console.ResetColor();
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            string valueDisplay = entry.IsKeyOnly ? string.Empty : $"  ={{{entry.Value}}}";
            Console.WriteLine($"     {entry.Key.PadRight(24)}{valueDisplay}");
            Console.ResetColor();
        }
    }

    // ── Key handlers ─────────────────────────────────────────────────────────

    private bool HandleBrowse(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key == ConsoleKey.UpArrow && keyInfo.Modifiers == 0)
        {
            if (_selectedIndex > 0)
            {
                _selectedIndex--;
                _statusMessage = string.Empty;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.DownArrow && keyInfo.Modifiers == 0)
        {
            if (_selectedIndex < _argsEditSession.Entries.Count - 1)
            {
                _selectedIndex++;
                _statusMessage = string.Empty;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            if (_argsEditSession.Entries.Count == 0)
                return true;

            BeginEditEntry(_selectedIndex);
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.ArgAdd))
        {
            AddEntry();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.ArgDelete))
        {
            DeleteEntry();
            return true;
        }

        return false;
    }

    private bool HandleEditKey(ConsoleKeyInfo keyInfo)
    {
        // → : switch to value editing
        if (keyInfo.Key == ConsoleKey.RightArrow && keyInfo.Modifiers == 0)
        {
            ArgEntry entry = _argsEditSession.Entries[_selectedIndex];
            entry.Key = _keyInput.Text;
            if (entry.Value is null)
                entry.Value = string.Empty;
            _valueInput.SetText(entry.Value);
            _editState     = EditState.EditValue;
            _statusMessage = "Editing value.  ← Back to key    Enter Confirm";
            InvalidateRequested?.Invoke();
            return true;
        }

        // Enter : confirm as key-only
        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            ArgEntry entry = _argsEditSession.Entries[_selectedIndex];
            entry.Key   = _keyInput.Text;
            entry.Value = null;
            _editState     = EditState.Browse;
            _statusMessage = string.Empty;
            InvalidateRequested?.Invoke();
            return true;
        }

        return HandleTextInput(_keyInput, keyInfo);
    }

    private bool HandleEditValue(ConsoleKeyInfo keyInfo)
    {
        // ← : switch back to key editing
        if (keyInfo.Key == ConsoleKey.LeftArrow && keyInfo.Modifiers == 0)
        {
            ArgEntry entry = _argsEditSession.Entries[_selectedIndex];
            entry.Value    = string.IsNullOrEmpty(_valueInput.Text) ? null : _valueInput.Text;
            _keyInput.SetText(entry.Key);
            _editState     = EditState.EditKey;
            _statusMessage = "Editing key.  → Add/Edit value    Enter Key-only";
            InvalidateRequested?.Invoke();
            return true;
        }

        // Enter : confirm
        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            ArgEntry entry = _argsEditSession.Entries[_selectedIndex];
            entry.Value    = string.IsNullOrEmpty(_valueInput.Text) ? null : _valueInput.Text;
            _editState     = EditState.Browse;
            _statusMessage = string.Empty;
            InvalidateRequested?.Invoke();
            return true;
        }

        return HandleTextInput(_valueInput, keyInfo);
    }

    private bool HandleTextInput(TextInputState input, ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key == ConsoleKey.LeftArrow && keyInfo.Modifiers == 0)
        {
            input.MoveLeft();
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.RightArrow && keyInfo.Modifiers == 0)
        {
            input.MoveRight();
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Home && keyInfo.Modifiers == 0)
        {
            input.MoveToStart();
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.End && keyInfo.Modifiers == 0)
        {
            input.MoveToEnd();
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Backspace && keyInfo.Modifiers == 0)
        {
            input.DeleteBefore();
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Delete && keyInfo.Modifiers == 0)
        {
            input.DeleteAt();
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Modifiers == 0 && !char.IsControl(keyInfo.KeyChar))
        {
            input.Insert(keyInfo.KeyChar);
            InvalidateRequested?.Invoke();
            return true;
        }

        return false;
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    private void BeginEditEntry(int index)
    {
        ArgEntry entry = _argsEditSession.Entries[index];
        _keyInput.SetText(entry.Key);
        _valueInput.SetText(entry.Value ?? string.Empty);
        _editState     = EditState.EditKey;
        _statusMessage = "Editing key.  → Add/Edit value    Enter Key-only";
        InvalidateRequested?.Invoke();
    }

    private void AddEntry()
    {
        var entry = new ArgEntry { Key = string.Empty, Value = null };
        _argsEditSession.Entries.Add(entry);
        _selectedIndex = _argsEditSession.Entries.Count - 1;
        BeginEditEntry(_selectedIndex);
    }

    private void DeleteEntry()
    {
        if (_argsEditSession.Entries.Count == 0)
            return;

        string deleted = _argsEditSession.Entries[_selectedIndex].Serialize();
        _argsEditSession.Entries.RemoveAt(_selectedIndex);
        ClampSelectedIndex();
        _statusMessage = $"Deleted: {deleted}";
        InvalidateRequested?.Invoke();
    }

    private void ClampSelectedIndex()
    {
        int count = _argsEditSession.Entries.Count;
        if (count == 0) { _selectedIndex = 0; return; }
        if (_selectedIndex >= count) _selectedIndex = count - 1;
        if (_selectedIndex < 0)      _selectedIndex = 0;
    }
}
