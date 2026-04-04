using System;
using System.Collections.Generic;
using TaskManager.Core.Navigation;
using TaskManager.Model;

namespace TaskManager.View.Run;

public sealed class TaskOutputView : IView
{
    private readonly IViewNavigator  _viewNavigator;
    private readonly PackageRunStore _packageRunStore;

    private List<string> _snapshot    = new();
    private int          _scrollOffset;

    public TaskOutputView(IViewNavigator viewNavigator, PackageRunStore packageRunStore)
    {
        _viewNavigator   = viewNavigator   ?? throw new ArgumentNullException(nameof(viewNavigator));
        _packageRunStore = packageRunStore ?? throw new ArgumentNullException(nameof(packageRunStore));
    }

    public ViewId ViewId => ViewId.TaskOutput;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        TakeSnapshot();
        _scrollOffset = Math.Max(0, _snapshot.Count - GetPageSize());
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        string taskName    = _packageRunStore.SelectedOutputTaskName ?? "(unknown)";
        string packageName = _packageRunStore.ActivePackageName      ?? "(unknown)";

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Main Menu  ›  Run  ›  {packageName}  ›  {taskName}");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine($"  │  {taskName.PadRight(24)}│");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ Scroll    R Refresh    Alt+Enter Back");
        Console.ResetColor();
        Console.WriteLine();

        int pageSize = GetPageSize();
        int total    = _snapshot.Count;
        int start    = Math.Clamp(_scrollOffset, 0, Math.Max(0, total - pageSize));

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ── Output ({total} lines) ─────────────────");
        Console.ResetColor();

        if (total == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (No output yet)");
            Console.ResetColor();
        }
        else
        {
            int end = Math.Min(start + pageSize, total);
            for (int i = start; i < end; i++)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  {_snapshot[i]}");
                Console.ResetColor();
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key == ConsoleKey.UpArrow && keyInfo.Modifiers == 0)
        {
            if (_scrollOffset > 0)
            {
                _scrollOffset--;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.DownArrow && keyInfo.Modifiers == 0)
        {
            int pageSize = GetPageSize();
            if (_scrollOffset < _snapshot.Count - pageSize)
            {
                _scrollOffset++;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.R && keyInfo.Modifiers == 0)
        {
            TakeSnapshot();
            _scrollOffset = Math.Max(0, _snapshot.Count - GetPageSize());
            InvalidateRequested?.Invoke();
            return true;
        }

        return false;
    }

    private void TakeSnapshot()
    {
        string? taskName = _packageRunStore.SelectedOutputTaskName;
        if (taskName is null)
        {
            _snapshot = new List<string>();
            return;
        }

        RunningTaskInfo? info = _packageRunStore.GetRunningInfo(taskName);
        _snapshot = info is not null ? info.GetSnapshot() : new List<string>();
    }

    private int GetPageSize() => Math.Max(5, Console.WindowHeight - 12);
}
