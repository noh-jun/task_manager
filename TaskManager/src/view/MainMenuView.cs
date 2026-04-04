using System;
using System.Collections.Generic;
using TaskManager.Core.Navigation;
using TaskManager.Model;

namespace TaskManager.View;

public sealed class MainMenuView : IView
{
    private enum MenuItemKind { Fixed, FavoritePackage }

    private sealed class MenuItem
    {
        public MenuItemKind Kind       { get; init; }
        public string       Label      { get; init; } = string.Empty;
        public int          FixedIndex { get; init; } = -1;   // index in _fixedItems
        public int          PkgIndex   { get; init; } = -1;   // index in packageStore
    }

    private static readonly string[] _fixedItems = { "Run", "Package", "Task List", "Options", "Exit" };

    private readonly IViewNavigator    _viewNavigator;
    private readonly HotKeyConfig      _hotKeyConfig;
    private readonly TaskPackageStore  _packageStore;
    private readonly PackageRunStore   _packageRunStore;
    private readonly TaskStore         _taskStore;

    private int    _selectedIndex;
    private string _statusMessage = string.Empty;

    public MainMenuView(
        IViewNavigator   viewNavigator,
        HotKeyConfig     hotKeyConfig,
        TaskPackageStore packageStore,
        PackageRunStore  packageRunStore,
        TaskStore        taskStore)
    {
        _viewNavigator   = viewNavigator   ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig    = hotKeyConfig    ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _packageStore    = packageStore    ?? throw new ArgumentNullException(nameof(packageStore));
        _packageRunStore = packageRunStore ?? throw new ArgumentNullException(nameof(packageRunStore));
        _taskStore       = taskStore       ?? throw new ArgumentNullException(nameof(taskStore));
        _selectedIndex = 0;
    }

    public ViewId ViewId => ViewId.MainMenu;

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
        List<MenuItem> items = BuildMenuItems();

        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │       Task Manager       │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑↓ Move    Enter Select    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Menu list
        for (int i = 0; i < items.Count; i++)
        {
            MenuItem item = items[i];
            bool selected = (i == _selectedIndex);

            if (item.Kind == MenuItemKind.FavoritePackage)
            {
                TaskPackage pkg      = _packageStore.GetAt(item.PkgIndex);
                bool        running  = IsRunning(pkg.Name);
                string      runTag   = running ? " (run)" : string.Empty;
                string      line     = $"     ★ {pkg.Name}{runTag}";

                if (selected)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = running ? ConsoleColor.Green : ConsoleColor.Cyan;
                    Console.WriteLine(line.PadRight(32));
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = running ? ConsoleColor.Green : ConsoleColor.Yellow;
                    Console.WriteLine(line);
                    Console.ResetColor();
                }
            }
            else
            {
                string label = _fixedItems[item.FixedIndex];
                if (selected)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  ▶  {label.PadRight(24)} ");
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

        // Details at bottom
        int currentRow    = Console.CursorTop;
        int windowHeight  = Math.Max(Console.WindowHeight, currentRow + 7);
        int paddingLines  = windowHeight - currentRow - 6;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {_statusMessage}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
        }
        else
        {
            MenuItem sel = items[_selectedIndex];
            if (sel.Kind == MenuItemKind.FavoritePackage)
            {
                TaskPackage pkg     = _packageStore.GetAt(sel.PkgIndex);
                bool        running = IsRunning(pkg.Name);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  ★ {pkg.Name}  ·  {pkg.TaskNames.Count} task(s)");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(running ? "  Enter: Stop" : "  Enter: Run");
            }
            else
            {
                (string en, string ko) = GetMenuDetail(sel.FixedIndex);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  {en}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  {ko}");
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
            MoveUp();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.DownArrow && keyInfo.Modifiers == 0)
        {
            MoveDown();
            return true;
        }

        if (HotKeyHelper.IsSelectKey(keyInfo))
        {
            OpenSelected();
            return true;
        }

        return false;
    }

    private void MoveUp()
    {
        if (_selectedIndex > 0)
        {
            _selectedIndex--;
            _statusMessage = string.Empty;
            InvalidateRequested?.Invoke();
        }
    }

    private void MoveDown()
    {
        List<MenuItem> items = BuildMenuItems();
        if (_selectedIndex < items.Count - 1)
        {
            _selectedIndex++;
            _statusMessage = string.Empty;
            InvalidateRequested?.Invoke();
        }
    }

    private void OpenSelected()
    {
        List<MenuItem> items = BuildMenuItems();
        MenuItem       item  = items[_selectedIndex];

        if (item.Kind == MenuItemKind.FavoritePackage)
        {
            ToggleFavoritePackage(item.PkgIndex);
            return;
        }

        switch (item.FixedIndex)
        {
            case 0: _viewNavigator.Push(ViewId.PackageRunList); break;
            case 1: _viewNavigator.Push(ViewId.PackageList);    break;
            case 2: _viewNavigator.Push(ViewId.TaskList);       break;
            case 3: _viewNavigator.Push(ViewId.OptionsMenu);    break;
            case 4: _viewNavigator.Push(ViewId.ExitConfirm);    break;
        }
    }

    private void ToggleFavoritePackage(int pkgIndex)
    {
        TaskPackage pkg = _packageStore.GetAt(pkgIndex);

        if (IsRunning(pkg.Name))
        {
            _packageRunStore.Stop();
            _statusMessage = $"Stopped: {pkg.Name}";
            InvalidateRequested?.Invoke();
            return;
        }

        List<TaskStartResult> results = _packageRunStore.Start(pkg, _taskStore);

        bool anyFailed = false;
        var  failures  = new List<string>();
        foreach (TaskStartResult r in results)
        {
            if (!r.Success)
            {
                anyFailed = true;
                failures.Add($"{r.TaskName} ({r.Reason})");
            }
        }

        if (anyFailed)
        {
            _statusMessage = $"Start failed: {string.Join(", ", failures)}";
        }
        else if (results.Count == 0)
        {
            _statusMessage = $"No tasks in package '{pkg.Name}'.";
        }
        else
        {
            _statusMessage = string.Empty;
        }

        InvalidateRequested?.Invoke();
    }

    private bool IsRunning(string packageName)
    {
        return _packageRunStore.IsActive
               && string.Equals(_packageRunStore.ActivePackageName, packageName, StringComparison.OrdinalIgnoreCase);
    }

    private void ClampSelectedIndex()
    {
        List<MenuItem> items = BuildMenuItems();
        if (_selectedIndex >= items.Count)
            _selectedIndex = items.Count - 1;
        if (_selectedIndex < 0)
            _selectedIndex = 0;
    }

    private List<MenuItem> BuildMenuItems()
    {
        var items = new List<MenuItem>();

        // "Run" fixed item
        items.Add(new MenuItem { Kind = MenuItemKind.Fixed, FixedIndex = 0, Label = _fixedItems[0] });

        // Favorite packages after "Run"
        for (int i = 0; i < _packageStore.Count; i++)
        {
            if (_packageStore.GetAt(i).IsFavorite)
            {
                items.Add(new MenuItem { Kind = MenuItemKind.FavoritePackage, PkgIndex = i });
            }
        }

        // Remaining fixed items: Package, Task List, Options, Exit
        for (int fi = 1; fi < _fixedItems.Length; fi++)
        {
            items.Add(new MenuItem { Kind = MenuItemKind.Fixed, FixedIndex = fi, Label = _fixedItems[fi] });
        }

        return items;
    }

    private static (string en, string ko) GetMenuDetail(int fixedIndex)
    {
        return fixedIndex switch
        {
            0 => ("Run a package — select and launch a group of tasks.",
                  "패키지를 실행합니다 — 태스크 그룹을 선택하여 실행합니다."),
            1 => ("Manage packages — group tasks into named packages.",
                  "패키지를 관리합니다 — 태스크를 패키지로 묶어 관리합니다."),
            2 => ("Manage your task list — add, edit, and delete tasks.",
                  "태스크 목록을 관리합니다 — 추가, 수정, 삭제가 가능합니다."),
            3 => ("Customize keyboard shortcuts for task operations.",
                  "태스크 작업에 사용되는 단축키를 설정합니다."),
            4 => ("Exit the application.",
                  "앱을 종료합니다."),
            _ => (string.Empty, string.Empty),
        };
    }
}
