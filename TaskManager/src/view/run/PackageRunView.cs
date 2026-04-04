using System;
using System.Collections.Generic;
using TaskManager.Core.Navigation;
using TaskManager.Model;

namespace TaskManager.View.Run;

public sealed class PackageRunView : IView
{
    private readonly IViewNavigator   _viewNavigator;
    private readonly TaskStore        _taskStore;
    private readonly TaskPackageStore _packageStore;
    private readonly PackageRunStore  _packageRunStore;

    private string _statusMessage = string.Empty;
    private List<TaskStartResult> _startResults = new();
    private int _selectedTaskIndex;

    public PackageRunView(
        IViewNavigator   viewNavigator,
        TaskStore        taskStore,
        TaskPackageStore packageStore,
        PackageRunStore  packageRunStore)
    {
        _viewNavigator   = viewNavigator   ?? throw new ArgumentNullException(nameof(viewNavigator));
        _taskStore       = taskStore       ?? throw new ArgumentNullException(nameof(taskStore));
        _packageStore    = packageStore    ?? throw new ArgumentNullException(nameof(packageStore));
        _packageRunStore = packageRunStore ?? throw new ArgumentNullException(nameof(packageRunStore));
    }

    public ViewId ViewId => ViewId.PackageRun;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        _statusMessage = string.Empty;
        _startResults.Clear();
        _selectedTaskIndex = 0;
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        TaskPackage package = _packageStore.GetAt(_packageRunStore.SelectedPackageIndex);
        bool        isThisPackageActive = string.Equals(
            _packageRunStore.ActivePackageName, package.Name,
            StringComparison.OrdinalIgnoreCase);

        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Main Menu  ›  Run  ›  {package.Name}");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine($"  │  {package.Name.PadRight(24)}│");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        if (isThisPackageActive)
            Console.WriteLine("  ↑↓ Move    Enter View Output    S Stop    Alt+Enter Back    Ctrl+D Exit");
        else if (_packageRunStore.IsActive)
            Console.WriteLine("  (Another package is running)    Alt+Enter Back    Ctrl+D Exit");
        else
            Console.WriteLine("  R Run    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Task list with run state
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ── Tasks ───────────────────────────────");
        Console.ResetColor();

        if (package.TaskNames.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (No tasks in this package)");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < package.TaskNames.Count; i++)
            {
                string taskName = package.TaskNames[i];
                RunningTaskInfo? info = isThisPackageActive
                    ? _packageRunStore.GetRunningInfo(taskName)
                    : null;

                bool taskExists   = TaskExistsInStore(taskName);
                bool isSelected   = isThisPackageActive && i == _selectedTaskIndex;
                string cursor     = isSelected ? "▶" : " ";

                if (info is not null)
                {
                    Console.ForegroundColor = isSelected ? ConsoleColor.Cyan : ConsoleColor.Green;
                    Console.WriteLine($"  {cursor}  ●  {taskName.PadRight(22)} PID: {info.Pid}");
                    Console.ResetColor();
                }
                else if (!taskExists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  {cursor}  ⚠  {taskName.PadRight(22)} (task not found)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  {cursor}  ○  {taskName.PadRight(22)} (not running)");
                    Console.ResetColor();
                }
            }
        }

        // Info at bottom
        int infoLines    = _startResults.Count > 0 ? _startResults.Count + 2 : 3;
        int currentRow   = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + infoLines + 2);
        int paddingLines = windowHeight - currentRow - infoLines - 1;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");

        if (_startResults.Count > 0)
        {
            foreach (TaskStartResult result in _startResults)
            {
                if (result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ● {result.TaskName.PadRight(22)} PID: {result.Pid}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ {result.TaskName.PadRight(22)} {result.Reason}");
                }
                Console.ResetColor();
            }
        }
        else if (!string.IsNullOrEmpty(_statusMessage))
        {
            ConsoleColor statusColor = _statusMessage.Contains("already")
                ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.ForegroundColor = statusColor;
            Console.WriteLine($"  {_statusMessage}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  R: Run    S: Stop");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Modifiers != 0)
            return false;

        TaskPackage package = _packageStore.GetAt(_packageRunStore.SelectedPackageIndex);
        bool isThisPackageActive = string.Equals(
            _packageRunStore.ActivePackageName, package.Name,
            StringComparison.OrdinalIgnoreCase);

        if (keyInfo.Key == ConsoleKey.UpArrow && isThisPackageActive)
        {
            if (_selectedTaskIndex > 0)
            {
                _selectedTaskIndex--;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.DownArrow && isThisPackageActive)
        {
            if (_selectedTaskIndex < package.TaskNames.Count - 1)
            {
                _selectedTaskIndex++;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Enter && isThisPackageActive)
        {
            OpenTaskOutput(package);
            return true;
        }

        if (keyInfo.Key == ConsoleKey.R)
        {
            RunPackage();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.S)
        {
            StopPackage();
            return true;
        }

        return false;
    }

    private void OpenTaskOutput(TaskPackage package)
    {
        if (package.TaskNames.Count == 0)
            return;

        string taskName = package.TaskNames[_selectedTaskIndex];
        if (_packageRunStore.GetRunningInfo(taskName) is null)
        {
            _statusMessage = $"{taskName} is not running.";
            InvalidateRequested?.Invoke();
            return;
        }

        _packageRunStore.SelectedOutputTaskName = taskName;
        _viewNavigator.Push(ViewId.TaskOutput);
    }

    private void RunPackage()
    {
        if (_packageRunStore.IsActive)
        {
            _statusMessage = $"Already running: {_packageRunStore.ActivePackageName}";
            InvalidateRequested?.Invoke();
            return;
        }

        TaskPackage package = _packageStore.GetAt(_packageRunStore.SelectedPackageIndex);
        _startResults = _packageRunStore.Start(package, _taskStore);
        _statusMessage = string.Empty;
        InvalidateRequested?.Invoke();
    }

    private void StopPackage()
    {
        if (!_packageRunStore.IsActive)
        {
            _statusMessage = "No package is running.";
            InvalidateRequested?.Invoke();
            return;
        }

        string name = _packageRunStore.ActivePackageName!;
        _packageRunStore.Stop();
        _startResults.Clear();
        _statusMessage = $"Stopped: {name}";
        InvalidateRequested?.Invoke();
    }

    private bool TaskExistsInStore(string taskName)
    {
        for (int i = 0; i < _taskStore.Count; i++)
        {
            if (string.Equals(_taskStore.GetAt(i).Name, taskName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
