using System;
using TaskManager.Core.Navigation;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Run;

public sealed class PackageRunListView : IView
{
    private readonly IViewNavigator   _viewNavigator;
    private readonly HotKeyConfig     _hotKeyConfig;
    private readonly TaskPackageStore _packageStore;
    private readonly PackageRunStore  _packageRunStore;
    private int _selectedIndex;

    public PackageRunListView(
        IViewNavigator   viewNavigator,
        HotKeyConfig     hotKeyConfig,
        TaskPackageStore packageStore,
        PackageRunStore  packageRunStore)
    {
        _viewNavigator   = viewNavigator   ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig    = hotKeyConfig    ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _packageStore    = packageStore    ?? throw new ArgumentNullException(nameof(packageStore));
        _packageRunStore = packageRunStore ?? throw new ArgumentNullException(nameof(packageRunStore));
        _selectedIndex   = 0;
    }

    public ViewId ViewId => ViewId.PackageRunList;

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
        Console.WriteLine("  Main Menu  ›  Run");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │        Run Package       │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ Move    Enter Select    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Package list
        if (_packageStore.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  No packages available.");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < _packageStore.Count; i++)
            {
                TaskPackage package   = _packageStore.GetAt(i);
                bool        isRunning = string.Equals(
                    _packageRunStore.ActivePackageName, package.Name,
                    StringComparison.OrdinalIgnoreCase);
                string taskCount = $"{package.TaskNames.Count} task(s)";

                if (i == _selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = isRunning ? ConsoleColor.Green : ConsoleColor.Cyan;
                    Console.Write($"  ▶  {package.Name.PadRight(24)} {taskCount} ");
                    Console.ResetColor();
                    if (isRunning)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" ●");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = isRunning ? ConsoleColor.Green : ConsoleColor.Gray;
                    Console.Write($"     {package.Name.PadRight(24)} {taskCount}");
                    if (isRunning)
                        Console.Write(" ●");
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }
        }

        // Details at bottom
        int currentRow   = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 7);
        int paddingLines = windowHeight - currentRow - 6;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        if (_packageRunStore.IsActive)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  ● Running: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(_packageRunStore.ActivePackageName);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  ·  {_packageRunStore.GetRunningTasks().Count} process(es)");
        }
        else if (_packageStore.Count > 0)
        {
            TaskPackage selected = _packageStore.GetAt(_selectedIndex);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {selected.Name}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  ·  {selected.TaskNames.Count} task(s)");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  No packages.");
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
            OpenRunView();
            return true;
        }

        return false;
    }

    private void MoveUp()
    {
        if (_packageStore.Count == 0)
            return;

        if (_selectedIndex > 0)
        {
            _selectedIndex--;
            InvalidateRequested?.Invoke();
        }
    }

    private void MoveDown()
    {
        if (_packageStore.Count == 0)
            return;

        if (_selectedIndex < _packageStore.Count - 1)
        {
            _selectedIndex++;
            InvalidateRequested?.Invoke();
        }
    }

    private void OpenRunView()
    {
        if (_packageStore.Count == 0)
            return;

        _packageRunStore.SelectPackage(_selectedIndex);
        _viewNavigator.Push(ViewId.PackageRun);
    }

    private void ClampSelectedIndex()
    {
        if (_packageStore.Count == 0)
        {
            _selectedIndex = 0;
            return;
        }

        if (_selectedIndex >= _packageStore.Count)
            _selectedIndex = _packageStore.Count - 1;

        if (_selectedIndex < 0)
            _selectedIndex = 0;
    }
}
