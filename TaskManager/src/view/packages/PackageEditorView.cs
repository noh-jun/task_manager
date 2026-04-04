using System;
using System.Collections.Generic;
using TaskManager.Core.Navigation;
using TaskManager.Infra.Json;
using TaskManager.Model;

namespace TaskManager.View.Packages;

public sealed class PackageEditorView : IView
{
    // Row layout (dynamic):
    //   0               = Name field
    //   1 .. N          = Task[0] .. Task[N-1]        (N = _taskStore.Count)
    //   N+1 .. N+M      = DanglingTask[0] .. [M-1]    (M = dangling count)
    //   N+M+1           = Save button
    //   N+M+2           = Cancel button

    private readonly IViewNavigator _viewNavigator;
    private readonly TaskStore _taskStore;
    private readonly TaskPackageStore _packageStore;
    private readonly PackageEditSession _packageEditSession;
    private readonly JsonPackageStore _jsonPackageStore;

    private readonly TextInputState _nameInput = new();
    private readonly HashSet<string> _selectedTaskNames;
    private int _selectedRow;
    private string _statusMessage;

    private int NameRow   => 0;
    private int SaveRow   => TaskRowCount() + 1;
    private int CancelRow => TaskRowCount() + 2;
    private int LastRow   => TaskRowCount() + 2;

    private bool IsTaskRow(int row) => row >= 1 && row <= TaskRowCount();
    private int  TaskIndexOf(int row) => row - 1;

    // Returns (taskName, isReal) for every displayed task row.
    private List<(string Name, bool IsReal)> BuildTaskRows()
    {
        var rows = new List<(string Name, bool IsReal)>();

        for (int i = 0; i < _taskStore.Count; i++)
            rows.Add((_taskStore.GetAt(i).Name, true));

        foreach (string name in _selectedTaskNames)
        {
            bool found = false;
            for (int i = 0; i < _taskStore.Count; i++)
            {
                if (string.Equals(_taskStore.GetAt(i).Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                rows.Add((name, false));
        }

        return rows;
    }

    private int TaskRowCount() => BuildTaskRows().Count;

    public PackageEditorView(
        IViewNavigator viewNavigator,
        TaskStore taskStore,
        TaskPackageStore packageStore,
        PackageEditSession packageEditSession,
        JsonPackageStore jsonPackageStore)
    {
        _viewNavigator      = viewNavigator      ?? throw new ArgumentNullException(nameof(viewNavigator));
        _taskStore          = taskStore          ?? throw new ArgumentNullException(nameof(taskStore));
        _packageStore       = packageStore       ?? throw new ArgumentNullException(nameof(packageStore));
        _packageEditSession = packageEditSession ?? throw new ArgumentNullException(nameof(packageEditSession));
        _jsonPackageStore   = jsonPackageStore   ?? throw new ArgumentNullException(nameof(jsonPackageStore));
        _selectedTaskNames  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _selectedRow        = NameRow;
        _statusMessage      = string.Empty;
    }

    public ViewId ViewId => ViewId.PackageEditor;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        _selectedTaskNames.Clear();

        if (_packageEditSession.IsCreateMode)
        {
            _nameInput.SetText(string.Empty);
            _statusMessage = "Fill in the name, select tasks, then Save.";
        }
        else
        {
            TaskPackage existing = _packageStore.GetAt(_packageEditSession.EditingPackageIndex);
            _nameInput.SetText(existing.Name);
            foreach (string taskName in existing.TaskNames)
                _selectedTaskNames.Add(taskName);
            _statusMessage = "Edit name or task selection, then Save.";
        }

        _selectedRow = NameRow;
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        bool   isCreate   = _packageEditSession.IsCreateMode;
        string modeLabel  = isCreate ? "Create Package" : "Edit Package";
        string breadcrumb = isCreate
            ? "  Main Menu  ›  Package  ›  Create Package"
            : "  Main Menu  ›  Package  ›  Edit Package";

        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(breadcrumb);
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine($"  │  {modeLabel.PadRight(24)}│");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ Move    ←→ Cursor    Space Toggle    Type/BS/Del Edit Name    Enter Confirm    Alt+Enter Back");
        Console.ResetColor();
        Console.WriteLine();

        // Name field
        RenderNameField();
        Console.WriteLine();

        // Task selection
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ── Tasks ───────────────────────────────");
        Console.ResetColor();

        List<(string Name, bool IsReal)> taskRows = BuildTaskRows();

        if (taskRows.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (No tasks available)");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < taskRows.Count; i++)
            {
                (string name, bool isReal) = taskRows[i];
                int    row      = i + 1;
                bool   checked_ = _selectedTaskNames.Contains(name);
                string checkbox = checked_ ? "[●]" : "[ ]";

                if (!isReal)
                {
                    // Dangling task: always checked, shown with warning
                    string label = $"  ▶  {checkbox} {name.PadRight(20)} ⚠ missing";
                    if (_selectedRow == row)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(label);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"     {checkbox} {name.PadRight(20)} ⚠ missing");
                        Console.ResetColor();
                    }
                }
                else if (_selectedRow == row)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = checked_ ? ConsoleColor.Cyan : ConsoleColor.DarkGray;
                    Console.WriteLine($"  ▶  {checkbox} {name} ");
                    Console.ResetColor();
                }
                else if (checked_)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"     {checkbox} {name}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"     {checkbox} {name}");
                    Console.ResetColor();
                }
            }
        }

        Console.WriteLine();

        // Buttons
        RenderButton(SaveRow,   "Save  ");
        RenderButton(CancelRow, "Cancel");

        // Status at bottom
        int currentRow   = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 6);
        int paddingLines = windowHeight - currentRow - 5;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        ConsoleColor statusColor = _statusMessage.Contains("required") || _statusMessage.Contains("exists")
            ? ConsoleColor.Red
            : ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ForegroundColor = statusColor;
        Console.WriteLine($"  {_statusMessage}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key == ConsoleKey.UpArrow && keyInfo.Modifiers == 0)
        {
            MoveUp();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.DownArrow && keyInfo.Modifiers == 0)
        {
            MoveDown();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Spacebar && keyInfo.Modifiers == 0)
        {
            ToggleTaskSelection();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            return HandleEnter();
        }

        if (_selectedRow == NameRow)
        {
            if (keyInfo.Key == ConsoleKey.LeftArrow && keyInfo.Modifiers == 0)
            {
                _nameInput.MoveLeft();
                InvalidateRequested?.Invoke();
                return true;
            }

            if (keyInfo.Key == ConsoleKey.RightArrow && keyInfo.Modifiers == 0)
            {
                _nameInput.MoveRight();
                InvalidateRequested?.Invoke();
                return true;
            }

            if (keyInfo.Key == ConsoleKey.Home && keyInfo.Modifiers == 0)
            {
                _nameInput.MoveToStart();
                InvalidateRequested?.Invoke();
                return true;
            }

            if (keyInfo.Key == ConsoleKey.End && keyInfo.Modifiers == 0)
            {
                _nameInput.MoveToEnd();
                InvalidateRequested?.Invoke();
                return true;
            }

            if (keyInfo.Key == ConsoleKey.Backspace && keyInfo.Modifiers == 0)
            {
                _nameInput.DeleteBefore();
                InvalidateRequested?.Invoke();
                return true;
            }

            if (keyInfo.Key == ConsoleKey.Delete && keyInfo.Modifiers == 0)
            {
                _nameInput.DeleteAt();
                InvalidateRequested?.Invoke();
                return true;
            }

            if (keyInfo.Modifiers == 0 && !char.IsControl(keyInfo.KeyChar))
            {
                _nameInput.Insert(keyInfo.KeyChar);
                InvalidateRequested?.Invoke();
                return true;
            }
        }

        return false;
    }

    private void RenderNameField()
    {
        if (_selectedRow == NameRow)
        {
            string display = _nameInput.ToDisplayText();
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ▶  {"Name".PadRight(10)}  {display}{"".PadRight(Math.Max(0, 20 - _nameInput.Text.Length))}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"     {"Name".PadRight(10)}  ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(_nameInput.Text);
            Console.ResetColor();
        }
    }

    private void RenderButton(int row, string text)
    {
        if (_selectedRow == row)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ▶  [ {text} ]  ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"     [ {text} ]");
            Console.ResetColor();
        }
    }

    private void MoveUp()
    {
        if (_selectedRow > NameRow)
        {
            _selectedRow--;
            InvalidateRequested?.Invoke();
        }
    }

    private void MoveDown()
    {
        if (_selectedRow < LastRow)
        {
            _selectedRow++;
            InvalidateRequested?.Invoke();
        }
    }

    private void ToggleTaskSelection()
    {
        if (!IsTaskRow(_selectedRow))
            return;

        List<(string Name, bool IsReal)> taskRows = BuildTaskRows();
        string taskName = taskRows[TaskIndexOf(_selectedRow)].Name;

        if (_selectedTaskNames.Contains(taskName))
            _selectedTaskNames.Remove(taskName);
        else
            _selectedTaskNames.Add(taskName);

        InvalidateRequested?.Invoke();
    }

    private bool HandleEnter()
    {
        if (_selectedRow == SaveRow)
        {
            SavePackage();
            return true;
        }

        if (_selectedRow == CancelRow)
        {
            CancelEdit();
            return true;
        }

        return false;
    }

    private void SavePackage()
    {
        if (string.IsNullOrWhiteSpace(_nameInput.Text))
        {
            _statusMessage = "Name is required.";
            InvalidateRequested?.Invoke();
            return;
        }

        int? excludeIndex = _packageEditSession.IsCreateMode
            ? null
            : _packageEditSession.EditingPackageIndex;

        if (_packageStore.IsDuplicateName(_nameInput.Text, excludeIndex))
        {
            _statusMessage = "Name already exists.";
            InvalidateRequested?.Invoke();
            return;
        }

        var package = new TaskPackage
        {
            Name      = _nameInput.Text,
            TaskNames = new List<string>(_selectedTaskNames),
        };

        if (_packageEditSession.IsCreateMode)
        {
            _packageStore.Add(package);
        }
        else
        {
            _packageStore.Update(_packageEditSession.EditingPackageIndex, package);
        }

        _jsonPackageStore.Save(_packageStore);
        _packageEditSession.Clear();
        _viewNavigator.Pop();
    }

    private void CancelEdit()
    {
        _packageEditSession.Clear();
        _viewNavigator.Pop();
    }
}
