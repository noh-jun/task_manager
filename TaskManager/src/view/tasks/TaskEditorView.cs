using System;
using TaskManager.Core.Navigation;
using TaskManager.Infra.Csv;
using TaskManager.Model;

namespace TaskManager.View.Tasks;

public sealed class TaskEditorView : IView
{
    private enum EditorRow
    {
        Name = 0,
        Command = 1,
        ConfigPath = 2,
        Args = 3,
        Save = 4,
        Cancel = 5,
    }

    private readonly IViewNavigator  _viewNavigator;
    private readonly TaskStore       _taskStore;
    private readonly TaskEditSession _taskEditSession;
    private readonly CsvTaskStore    _csvTaskStore;
    private readonly ArgsEditSession _argsEditSession;

    private readonly TextInputState _nameInput       = new();
    private readonly TextInputState _commandInput    = new();
    private readonly TextInputState _configPathInput = new();

    private EditorRow _selectedRow;
    private string    _statusMessage;
    private bool      _hasInitialized;

    public TaskEditorView(
        IViewNavigator  viewNavigator,
        TaskStore       taskStore,
        TaskEditSession taskEditSession,
        CsvTaskStore    csvTaskStore,
        ArgsEditSession argsEditSession)
    {
        _viewNavigator   = viewNavigator   ?? throw new ArgumentNullException(nameof(viewNavigator));
        _taskStore       = taskStore       ?? throw new ArgumentNullException(nameof(taskStore));
        _taskEditSession = taskEditSession ?? throw new ArgumentNullException(nameof(taskEditSession));
        _csvTaskStore    = csvTaskStore    ?? throw new ArgumentNullException(nameof(csvTaskStore));
        _argsEditSession = argsEditSession ?? throw new ArgumentNullException(nameof(argsEditSession));
        _selectedRow     = EditorRow.Name;
        _statusMessage   = string.Empty;
        _hasInitialized  = false;
    }

    public ViewId ViewId => ViewId.TaskEditor;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        if (_hasInitialized)
        {
            // Returning from ArgsEditView — fields are unchanged, args already updated in session
            return;
        }

        _hasInitialized = true;

        if (_taskEditSession.IsCopyMode)
        {
            TaskItem src = _taskEditSession.CopySource!;
            _nameInput.SetText(src.Name);
            _commandInput.SetText(src.Command);
            _configPathInput.SetText(src.ConfigPath);
            _argsEditSession.LoadFrom(src.Args);
            _statusMessage = "Copied. Change Name (and other fields), then Save.";
        }
        else if (_taskEditSession.IsCreateMode)
        {
            _nameInput.SetText(string.Empty);
            _commandInput.SetText(string.Empty);
            _configPathInput.SetText(string.Empty);
            _argsEditSession.LoadFrom(string.Empty);
            _statusMessage = "Fill in the fields and select Save.";
        }
        else
        {
            TaskItem task = _taskStore.GetAt(_taskEditSession.EditingTaskIndex);
            _nameInput.SetText(task.Name);
            _commandInput.SetText(task.Command);
            _configPathInput.SetText(task.ConfigPath);
            _argsEditSession.LoadFrom(task.Args);
            _statusMessage = "Edit fields and select Save.";
        }

        _selectedRow = EditorRow.Name;
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        string modeLabel = _taskEditSession.IsCopyMode  ? "Copy Task"
                         : _taskEditSession.IsCreateMode ? "Create Task"
                         : "Edit Task";
        string breadcrumb = $"  Main Menu  ›  Task  ›  {modeLabel}";

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
        Console.WriteLine("  ↑↓ Move    ←→ Cursor    Home/End    Type Input    BS/Del Delete    Enter Confirm    Alt+Enter Back");
        Console.ResetColor();
        Console.WriteLine();

        // Fields
        RenderField(EditorRow.Name,       "Name      ", _nameInput);
        RenderField(EditorRow.Command,    "Command   ", _commandInput);
        RenderField(EditorRow.ConfigPath, "ConfigPath", _configPathInput);
        RenderArgsField();
        Console.WriteLine();

        // Buttons
        RenderButton(EditorRow.Save,   "Save  ");
        RenderButton(EditorRow.Cancel, "Cancel");

        // Status at bottom
        int currentRow = Console.CursorTop;
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

        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            if (_selectedRow == EditorRow.Args)
            {
                _viewNavigator.Push(ViewId.ArgsEdit);
                return true;
            }
            return HandleEnter();
        }

        TextInputState? input = GetActiveInput();
        if (input is null)
            return false;

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

    private TextInputState? GetActiveInput()
    {
        return _selectedRow switch
        {
            EditorRow.Name       => _nameInput,
            EditorRow.Command    => _commandInput,
            EditorRow.ConfigPath => _configPathInput,
            _                    => null,
        };
    }

    private void RenderArgsField()
    {
        int    count   = _argsEditSession.Entries.Count;
        string summary = count == 0 ? "(none)" : $"{count} arg(s)  — Enter to edit";

        if (_selectedRow == EditorRow.Args)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ▶  {"Args      "}  {summary}{"".PadRight(Math.Max(0, 20 - summary.Length))}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"     {"Args      "}  ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(summary);
            Console.ResetColor();
        }
    }

    private void RenderField(EditorRow editorRow, string label, TextInputState input)
    {
        if (_selectedRow == editorRow)
        {
            string display = input.ToDisplayText();
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ▶  {label}  {display}{"".PadRight(Math.Max(0, 20 - input.Text.Length))}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"     {label}  ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(input.Text);
            Console.ResetColor();
        }
    }

    private void RenderButton(EditorRow editorRow, string text)
    {
        if (_selectedRow == editorRow)
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
        if (_selectedRow > EditorRow.Name)
        {
            _selectedRow--;
            InvalidateRequested?.Invoke();
        }
    }

    private void MoveDown()
    {
        if (_selectedRow < EditorRow.Cancel)
        {
            _selectedRow++;
            InvalidateRequested?.Invoke();
        }
    }

    private bool HandleEnter()
    {
        if (_selectedRow == EditorRow.Save)
        {
            SaveTask();
            return true;
        }

        if (_selectedRow == EditorRow.Cancel)
        {
            CancelEdit();
            return true;
        }

        return false;
    }

    private void SaveTask()
    {
        string name       = _nameInput.Text;
        string command    = _commandInput.Text;
        string configPath = _configPathInput.Text;
        string args       = _argsEditSession.Serialize();

        if (string.IsNullOrWhiteSpace(name))
        {
            _statusMessage = "Name is required.";
            InvalidateRequested?.Invoke();
            return;
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            _statusMessage = "Command is required.";
            InvalidateRequested?.Invoke();
            return;
        }

        int? excludeTaskIndex = _taskEditSession.IsCreateMode
            ? null
            : _taskEditSession.EditingTaskIndex;

        if (_taskStore.IsDuplicateName(name, excludeTaskIndex))
        {
            _statusMessage = "Name already exists.";
            InvalidateRequested?.Invoke();
            return;
        }

        var draftTaskItem = new TaskItem
        {
            Name       = name,
            Command    = command,
            ConfigPath = configPath,
            Args       = args,
        };

        if (_taskEditSession.IsCreateMode)
        {
            _taskStore.Add(draftTaskItem);
            _statusMessage = $"Created: {name}";
        }
        else
        {
            _taskStore.Update(_taskEditSession.EditingTaskIndex, draftTaskItem);
            _statusMessage = $"Updated: {name}";
        }

        _csvTaskStore.Save(_taskStore);
        _taskEditSession.Clear();
        _viewNavigator.Pop();
    }

    private void CancelEdit()
    {
        _taskEditSession.Clear();
        _viewNavigator.Pop();
    }
}
