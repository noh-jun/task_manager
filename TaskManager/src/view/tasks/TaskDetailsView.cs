using System;
using TaskManager.Core.Navigation;
using TaskManager.Model;

namespace TaskManager.View.Tasks;

public sealed class TaskDetailsView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly TaskStore _taskStore;
    private readonly TaskEditSession _taskEditSession;

    private TaskItem _taskItem;

    public TaskDetailsView(IViewNavigator viewNavigator, TaskStore taskStore, TaskEditSession taskEditSession)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _taskEditSession = taskEditSession ?? throw new ArgumentNullException(nameof(taskEditSession));
        _taskItem = new TaskItem();
    }

    public ViewId ViewId => ViewId.TaskDetails;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        _taskItem = _taskStore.GetAt(_taskEditSession.EditingTaskIndex);
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Task  ›  Task Details");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │       Task Details        │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Fields
        RenderField("Name      ", _taskItem.Name);
        RenderField("Command   ", _taskItem.Command);
        RenderField("ConfigPath", _taskItem.ConfigPath);
        RenderArgsField("Args      ", _taskItem.Args);

        // Status at bottom
        int currentRow = Console.CursorTop;
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

    private static void RenderField(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"     {label}  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(value);
        Console.ResetColor();
    }

    private static void RenderArgsField(string label, string args)
    {
        // "     " (5) + label (10) + "  " (2) = 17 chars indent for continuation lines
        string indent = new string(' ', 5 + label.Length + 2);

        List<string> tokens = TokenizeArgs(args);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"     {label}  ");

        if (tokens.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Empty);
        }
        else
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i > 0)
                    Console.Write(indent);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(tokens[i]);
                if (i < tokens.Count - 1)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
            }
        }

        Console.ResetColor();
    }

    // Splits args string by whitespace, respecting single- and double-quoted spans.
    private static List<string> TokenizeArgs(string args)
    {
        var tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(args))
            return tokens;

        var current = new System.Text.StringBuilder();
        char quote = '\0';

        foreach (char c in args)
        {
            if (quote != '\0')
            {
                if (c == quote)
                    quote = '\0';
                else
                    current.Append(c);
            }
            else if (c == '"' || c == '\'')
            {
                quote = c;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
