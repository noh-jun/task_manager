using System;
using TaskManager.Core.Navigation;
using TaskManager.Infra.Json;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Packages;

public sealed class PackageListView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly TaskStore _taskStore;
    private readonly TaskPackageStore _packageStore;
    private readonly PackageEditSession _packageEditSession;
    private readonly JsonPackageStore _jsonPackageStore;
    private int _selectedIndex;
    private string _statusMessage;

    public PackageListView(
        IViewNavigator viewNavigator,
        HotKeyConfig hotKeyConfig,
        TaskStore taskStore,
        TaskPackageStore packageStore,
        PackageEditSession packageEditSession,
        JsonPackageStore jsonPackageStore)
    {
        _viewNavigator      = viewNavigator      ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig       = hotKeyConfig       ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _taskStore          = taskStore          ?? throw new ArgumentNullException(nameof(taskStore));
        _packageStore       = packageStore       ?? throw new ArgumentNullException(nameof(packageStore));
        _packageEditSession = packageEditSession ?? throw new ArgumentNullException(nameof(packageEditSession));
        _jsonPackageStore   = jsonPackageStore   ?? throw new ArgumentNullException(nameof(jsonPackageStore));
        _selectedIndex = 0;
        _statusMessage = string.Empty;
    }

    public ViewId ViewId => ViewId.PackageList;

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
        Console.WriteLine("  Main Menu  ›  Package");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │      Package List        │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        string addKey      = HotKeyHelper.ToDisplayText(HotKeyAction.PackageAdd,      _hotKeyConfig);
        string editKey     = HotKeyHelper.ToDisplayText(HotKeyAction.PackageEdit,     _hotKeyConfig);
        string deleteKey   = HotKeyHelper.ToDisplayText(HotKeyAction.PackageDelete,   _hotKeyConfig);
        string favoriteKey = HotKeyHelper.ToDisplayText(HotKeyAction.PackageFavorite, _hotKeyConfig);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ↑↓ Move    Enter Details    {editKey} Edit    {addKey} Add    {deleteKey} Delete    {favoriteKey} Favorite    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Package list
        if (_packageStore.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  No packages yet. Press Add key to create one.");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < _packageStore.Count; i++)
            {
                TaskPackage package   = _packageStore.GetAt(i);
                string      taskCount = $"{package.TaskNames.Count} task(s)";
                bool        hasWarn   = HasDanglingReferences(package);
                string      star      = package.IsFavorite ? "★ " : "  ";

                if (i == _selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.Write($"  ▶  {star}{package.Name.PadRight(22)} {taskCount}");
                    Console.ResetColor();
                    if (hasWarn)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(" ⚠");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
                else
                {
                    if (package.IsFavorite)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else
                        Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"     {star}{package.Name.PadRight(22)} {taskCount}");
                    Console.ResetColor();
                    if (hasWarn)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(" ⚠");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
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
        if (_packageStore.Count > 0)
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
            if (!string.IsNullOrEmpty(_statusMessage))
                Console.WriteLine($"  {_statusMessage}");
            else
            {
                string hintAddKey    = HotKeyHelper.ToDisplayText(HotKeyAction.PackageAdd,    _hotKeyConfig);
                string hintEditKey   = HotKeyHelper.ToDisplayText(HotKeyAction.PackageEdit,   _hotKeyConfig);
                string hintDeleteKey = HotKeyHelper.ToDisplayText(HotKeyAction.PackageDelete, _hotKeyConfig);
                Console.WriteLine($"  {hintAddKey}: Add    {hintEditKey}: Edit    {hintDeleteKey}: Delete");
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
            OpenPackageDetails();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.PackageAdd))
        {
            CreatePackage();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.PackageEdit))
        {
            EditSelectedPackage();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.PackageDelete))
        {
            DeleteSelectedPackage();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.PackageFavorite))
        {
            ToggleFavorite();
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

    private void OpenPackageDetails()
    {
        if (_packageStore.Count == 0)
        {
            _statusMessage = "No package to view.";
            InvalidateRequested?.Invoke();
            return;
        }

        _packageEditSession.BeginEdit(_selectedIndex);
        _viewNavigator.Push(ViewId.PackageDetails);
    }

    private void CreatePackage()
    {
        _packageEditSession.BeginCreate();
        _viewNavigator.Push(ViewId.PackageEditor);
    }

    private void EditSelectedPackage()
    {
        if (_packageStore.Count == 0)
        {
            _statusMessage = "No package to edit.";
            InvalidateRequested?.Invoke();
            return;
        }

        _packageEditSession.BeginEdit(_selectedIndex);
        _viewNavigator.Push(ViewId.PackageEditor);
    }

    private void DeleteSelectedPackage()
    {
        if (_packageStore.Count == 0)
        {
            _statusMessage = "No package to delete.";
            InvalidateRequested?.Invoke();
            return;
        }

        string deletedName = _packageStore.GetAt(_selectedIndex).Name;
        _packageStore.Delete(_selectedIndex);
        _jsonPackageStore.Save(_packageStore);
        ClampSelectedIndex();
        _statusMessage = $"Deleted: {deletedName}";
        InvalidateRequested?.Invoke();
    }

    private void ToggleFavorite()
    {
        if (_packageStore.Count == 0)
            return;

        TaskPackage package = _packageStore.GetAt(_selectedIndex);
        package.IsFavorite = !package.IsFavorite;
        _jsonPackageStore.Save(_packageStore);
        _statusMessage = package.IsFavorite
            ? $"★ '{package.Name}' added to favorites."
            : $"'{package.Name}' removed from favorites.";
        InvalidateRequested?.Invoke();
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

    private bool HasDanglingReferences(TaskPackage package)
    {
        foreach (string taskName in package.TaskNames)
        {
            if (!TaskExistsInStore(taskName))
                return true;
        }

        return false;
    }

    private List<string> GetMissingTaskNames(TaskPackage package)
    {
        var missing = new List<string>();
        foreach (string taskName in package.TaskNames)
        {
            if (!TaskExistsInStore(taskName))
                missing.Add(taskName);
        }

        return missing;
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
