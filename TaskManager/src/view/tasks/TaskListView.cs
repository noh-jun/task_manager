using System;
using TaskManager.Core.Navigation;
using TaskManager.Infra.Csv;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Tasks;

public sealed class TaskListView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly TaskStore _taskStore;
    private readonly TaskEditSession _taskEditSession;
    private readonly CsvTaskStore _csvTaskStore;
    private int _selectedIndex;
    private string _statusMessage;

    public TaskListView(
        IViewNavigator viewNavigator,
        HotKeyConfig hotKeyConfig,
        TaskStore taskStore,
        TaskEditSession taskEditSession,
        CsvTaskStore csvTaskStore)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig = hotKeyConfig ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _taskEditSession = taskEditSession ?? throw new ArgumentNullException(nameof(taskEditSession));
        _csvTaskStore = csvTaskStore ?? throw new ArgumentNullException(nameof(csvTaskStore));
        _selectedIndex = 0;
        _statusMessage = string.Empty;
    }

    public ViewId ViewId => ViewId.TaskList;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        ClampSelectedIndex();
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Task");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │        Task List         │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        string editKey   = HotKeyHelper.ToDisplayText(HotKeyAction.TaskEdit,   _hotKeyConfig);
        string addKey    = HotKeyHelper.ToDisplayText(HotKeyAction.TaskAdd,    _hotKeyConfig);
        string deleteKey = HotKeyHelper.ToDisplayText(HotKeyAction.TaskDelete, _hotKeyConfig);
        string copyKey   = HotKeyHelper.ToDisplayText(HotKeyAction.TaskCopy,   _hotKeyConfig);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ↑↓ Move    Enter Details    {editKey} Edit    {addKey} Add    {deleteKey} Delete    {copyKey} Copy    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Task list
        if (_taskStore.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  No tasks yet. Press Add key to create one.");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < _taskStore.Count; i++)
            {
                TaskItem task = _taskStore.GetAt(i);

                if (i == _selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  ▶  {task.Name} ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"     {task.Name}");
                    Console.ResetColor();
                }
            }
        }

        // Details at bottom
        int currentRow = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 7);
        int paddingLines = windowHeight - currentRow - 6;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        if (_taskStore.Count > 0)
        {
            TaskItem selected = _taskStore.GetAt(_selectedIndex);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {selected.Name}");
            if (!string.IsNullOrEmpty(selected.Command))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  ·  ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(selected.Command);
                if (!string.IsNullOrEmpty(selected.Args))
                    Console.Write($"  {selected.Args}");
            }
            Console.WriteLine();
            if (!string.IsNullOrEmpty(selected.ConfigPath))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {selected.ConfigPath}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                Console.WriteLine($"  {_statusMessage}");
            }
            else
            {
                string hintEditKey   = HotKeyHelper.ToDisplayText(HotKeyAction.TaskEdit,   _hotKeyConfig);
                string hintAddKey    = HotKeyHelper.ToDisplayText(HotKeyAction.TaskAdd,    _hotKeyConfig);
                string hintDeleteKey = HotKeyHelper.ToDisplayText(HotKeyAction.TaskDelete, _hotKeyConfig);
                Console.WriteLine($"  Enter: Details    {hintEditKey}: Edit    {hintAddKey}: Add    {hintDeleteKey}: Delete");
            }
        }
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
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

        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            OpenTaskDetails();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.TaskEdit))
        {
            EditSelectedTask();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.TaskAdd))
        {
            CreateTask();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.TaskDelete))
        {
            DeleteSelectedTask();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.TaskCopy))
        {
            CopySelectedTask();
            return true;
        }

        return false;
    }

    private void MoveUp()
    {
        if (_taskStore.Count == 0)
        {
            return;
        }

        if (_selectedIndex > 0)
        {
            _selectedIndex--;
            InvalidateRequested?.Invoke();
        }
    }

    private void MoveDown()
    {
        if (_taskStore.Count == 0)
        {
            return;
        }

        if (_selectedIndex < _taskStore.Count - 1)
        {
            _selectedIndex++;
            InvalidateRequested?.Invoke();
        }
    }

    private void OpenTaskDetails()
    {
        if (_taskStore.Count == 0)
        {
            _statusMessage = "No task to view.";
            InvalidateRequested?.Invoke();
            return;
        }

        _taskEditSession.BeginEdit(_selectedIndex);
        _viewNavigator.Push(ViewId.TaskDetails);
    }

    private void EditSelectedTask()
    {
        if (_taskStore.Count == 0)
        {
            _statusMessage = "No task to edit.";
            InvalidateRequested?.Invoke();
            return;
        }

        _taskEditSession.BeginEdit(_selectedIndex);
        _viewNavigator.Push(ViewId.TaskEditor);
    }

    private void CreateTask()
    {
        _taskEditSession.BeginCreate();
        _viewNavigator.Push(ViewId.TaskEditor);
    }

    private void CopySelectedTask()
    {
        if (_taskStore.Count == 0)
        {
            _statusMessage = "No task to copy.";
            InvalidateRequested?.Invoke();
            return;
        }

        _taskEditSession.BeginCopy(_taskStore.GetAt(_selectedIndex));
        _viewNavigator.Push(ViewId.TaskEditor);
    }

    private void DeleteSelectedTask()
    {
        if (_taskStore.Count == 0)
        {
            _statusMessage = "No task to delete.";
            InvalidateRequested?.Invoke();
            return;
        }

        string deletedTaskName = _taskStore.GetAt(_selectedIndex).Name;
        _taskStore.Delete(_selectedIndex);
        _csvTaskStore.Save(_taskStore);
        ClampSelectedIndex();
        _statusMessage = $"Deleted: {deletedTaskName}";
        InvalidateRequested?.Invoke();
    }

    private void ClampSelectedIndex()
    {
        if (_taskStore.Count == 0)
        {
            _selectedIndex = 0;
            return;
        }

        if (_selectedIndex >= _taskStore.Count)
        {
            _selectedIndex = _taskStore.Count - 1;
        }

        if (_selectedIndex < 0)
        {
            _selectedIndex = 0;
        }
    }
}