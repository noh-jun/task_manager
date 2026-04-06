using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskManager.Core.Navigation;
using TaskManager.Infra.Settings;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Settings;

public sealed class DirectoryPickerView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly AppSettings _appSettings;
    private readonly AppSettingsStore _appSettingsStore;
    private readonly DirectoryPickerSession _session;

    private readonly List<char> _input = new();
    private int _cursorPos;
    private bool _showHidden;
    private List<string> _completions = new();
    private string _errorMessage = string.Empty;

    public DirectoryPickerView(
        IViewNavigator viewNavigator,
        AppSettings appSettings,
        AppSettingsStore appSettingsStore,
        DirectoryPickerSession session)
    {
        _viewNavigator    = viewNavigator    ?? throw new ArgumentNullException(nameof(viewNavigator));
        _appSettings      = appSettings      ?? throw new ArgumentNullException(nameof(appSettings));
        _appSettingsStore = appSettingsStore ?? throw new ArgumentNullException(nameof(appSettingsStore));
        _session          = session          ?? throw new ArgumentNullException(nameof(session));
    }

    public ViewId ViewId => ViewId.DirectoryPicker;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        _input.Clear();
        _input.AddRange(_session.InitialPath);
        _cursorPos    = _input.Count;
        _showHidden   = false;
        _errorMessage = string.Empty;
        RefreshCompletions();
    }

    public void OnLeave() { }

    public void Render()
    {
        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Options  ›  Settings  ›  Config Dir Path");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │    Directory Picker      │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hint bar
        string hiddenLabel = _showHidden ? "ON " : "OFF";
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ←→ Cursor    Backspace/Del Delete    Tab Complete    Ctrl+H Hidden [{hiddenLabel}]    Enter Confirm");
        Console.ResetColor();
        Console.WriteLine();

        // Input line
        string inputStr = new string(_input.ToArray());
        string before   = inputStr[.._cursorPos];
        string atCursor = _cursorPos < inputStr.Length ? inputStr[_cursorPos].ToString() : " ";
        string after    = _cursorPos < inputStr.Length ? inputStr[(_cursorPos + 1)..] : string.Empty;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  > ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(before);
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.White;
        Console.Write(atCursor);
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(after);
        Console.ResetColor();
        Console.WriteLine();

        // Completion list
        if (_completions.Count > 0)
        {
            const int maxDisplay = 8;
            int displayCount = Math.Min(_completions.Count, maxDisplay);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ── Suggestions ──────────────────────");
            for (int i = 0; i < displayCount; i++)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"     {_completions[i]}");
            }
            if (_completions.Count > maxDisplay)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"     ... {_completions.Count - maxDisplay} more");
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ─────────────────────────────────────");
            Console.ResetColor();
        }

        // Bottom bar
        int currentRow   = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 5);
        int paddingLines = windowHeight - currentRow - 4;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗  {_errorMessage}");
        }
        else
        {
            bool exists = Directory.Exists(inputStr);
            Console.ForegroundColor = exists ? ConsoleColor.Green : ConsoleColor.DarkGray;
            Console.WriteLine(exists ? $"  ✓  {inputStr}" : "  —  Directory not found");
        }
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        _errorMessage = string.Empty;

        if (keyInfo.Key == ConsoleKey.LeftArrow && keyInfo.Modifiers == 0)
        {
            if (_cursorPos > 0)
            {
                _cursorPos--;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.RightArrow && keyInfo.Modifiers == 0)
        {
            if (_cursorPos < _input.Count)
            {
                _cursorPos++;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Home)
        {
            _cursorPos = 0;
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.End)
        {
            _cursorPos = _input.Count;
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Backspace && keyInfo.Modifiers == 0)
        {
            if (_cursorPos > 0)
            {
                _input.RemoveAt(_cursorPos - 1);
                _cursorPos--;
                RefreshCompletions();
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Delete && keyInfo.Modifiers == 0)
        {
            if (_cursorPos < _input.Count)
            {
                _input.RemoveAt(_cursorPos);
                RefreshCompletions();
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Tab && keyInfo.Modifiers == 0)
        {
            if (_completions.Count == 1)
            {
                _input.Clear();
                _input.AddRange(_completions[0]);
                _cursorPos = _input.Count;
                RefreshCompletions();
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.H && keyInfo.Modifiers == ConsoleModifiers.Control)
        {
            _showHidden = !_showHidden;
            RefreshCompletions();
            InvalidateRequested?.Invoke();
            return true;
        }

        if (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == 0)
        {
            string path = new string(_input.ToArray());
            if (!Directory.Exists(path))
            {
                _errorMessage = "Directory not found.";
                InvalidateRequested?.Invoke();
                return true;
            }
            _session.Confirm(Path.GetFullPath(path));
            _viewNavigator.Pop();
            return true;
        }

        if (!char.IsControl(keyInfo.KeyChar))
        {
            _input.Insert(_cursorPos, keyInfo.KeyChar);
            _cursorPos++;
            RefreshCompletions();
            InvalidateRequested?.Invoke();
            return true;
        }

        return false;
    }

    private void RefreshCompletions()
    {
        string input = new string(_input.ToArray());

        if (input.Length == 0)
        {
            _completions = new List<string>();
            return;
        }

        string dirPart;
        string prefix;

        if (input.EndsWith(Path.DirectorySeparatorChar) || input.EndsWith('/'))
        {
            dirPart = input;
            prefix  = string.Empty;
        }
        else
        {
            dirPart = Path.GetDirectoryName(input) ?? string.Empty;
            prefix  = Path.GetFileName(input) ?? string.Empty;
        }

        if (!Directory.Exists(dirPart))
        {
            _completions = new List<string>();
            return;
        }

        try
        {
            _completions = Directory.GetDirectories(dirPart)
                .Select(d => Path.GetFileName(d) ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name))
                .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Where(name => _showHidden || !name.StartsWith("."))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => Path.Combine(dirPart, name) + Path.DirectorySeparatorChar)
                .ToList();
        }
        catch
        {
            _completions = new List<string>();
        }
    }
}
