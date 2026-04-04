using System;
using TaskManager.Core.Navigation;
using TaskManager.Model;

namespace TaskManager.View.Packages;

public sealed class PackageDetailsView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly TaskStore _taskStore;
    private readonly TaskPackageStore _packageStore;
    private readonly PackageEditSession _packageEditSession;

    private TaskPackage _package;

    public PackageDetailsView(
        IViewNavigator viewNavigator,
        TaskStore taskStore,
        TaskPackageStore packageStore,
        PackageEditSession packageEditSession)
    {
        _viewNavigator      = viewNavigator      ?? throw new ArgumentNullException(nameof(viewNavigator));
        _taskStore          = taskStore          ?? throw new ArgumentNullException(nameof(taskStore));
        _packageStore       = packageStore       ?? throw new ArgumentNullException(nameof(packageStore));
        _packageEditSession = packageEditSession ?? throw new ArgumentNullException(nameof(packageEditSession));
        _package            = new TaskPackage();
    }

    public ViewId ViewId => ViewId.PackageDetails;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        _package = _packageStore.GetAt(_packageEditSession.EditingPackageIndex);
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Package  ›  Package Details");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │     Package Details      │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Package name
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("     Name        ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(_package.Name);
        Console.ResetColor();
        Console.WriteLine();

        // Task list
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ── Tasks ───────────────────────────────");
        Console.ResetColor();

        if (_taskStore.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (No tasks available)");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < _taskStore.Count; i++)
            {
                TaskItem task     = _taskStore.GetAt(i);
                bool     included = _package.TaskNames.Contains(task.Name);
                string   checkbox = included ? "[●]" : "[ ]";

                if (included)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"     {checkbox} {task.Name}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"     {checkbox} {task.Name}");
                    Console.ResetColor();
                }
            }
        }

        // Status at bottom
        int currentRow   = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 6);
        int paddingLines = windowHeight - currentRow - 5;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.WriteLine("  Read-only. Alt+Enter to go back.");
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        return false;
    }
}
