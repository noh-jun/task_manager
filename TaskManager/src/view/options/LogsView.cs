using System;
using System.Collections.Generic;
using TaskManager.Core;
using TaskManager.Core.Navigation;
using TaskManager.Model;

namespace TaskManager.View.Options;

public sealed class LogsView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig   _hotKeyConfig;
    private readonly LogViewState   _logViewState;
    private int _scrollOffset;

    private const int HeaderLines = 8;  // blank + breadcrumb + box(3) + blank + hints + blank
    private const int DetailLines = 3;
    private const int FooterLines = DetailLines + 3;  // separator + detail(3) + separator + status
    private const int PageStep    = 100;

    public LogsView(IViewNavigator viewNavigator, HotKeyConfig hotKeyConfig, LogViewState logViewState)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig  = hotKeyConfig  ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _logViewState  = logViewState  ?? throw new ArgumentNullException(nameof(logViewState));
    }

    public ViewId ViewId => ViewId.Logs;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        int pageSize = GetPageSize();
        int cursor   = _logViewState.CursorIndex;

        if (cursor < _scrollOffset)
            _scrollOffset = cursor;
        else if (cursor >= _scrollOffset + pageSize)
            _scrollOffset = cursor - pageSize + 1;

        InvalidateRequested?.Invoke();
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        List<string> snapshot = _logViewState.Snapshot;

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Options  ›  Logs");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │           Logs           │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        string reloadKey   = HotKeyHelper.ToDisplayText(HotKeyAction.LogReload,   _hotKeyConfig);
        string pageUpKey   = HotKeyHelper.ToDisplayText(HotKeyAction.LogPageUp,   _hotKeyConfig);
        string pageDownKey = HotKeyHelper.ToDisplayText(HotKeyAction.LogPageDown, _hotKeyConfig);
        string homeKey     = HotKeyHelper.ToDisplayText(HotKeyAction.LogHome,     _hotKeyConfig);
        string endKey      = HotKeyHelper.ToDisplayText(HotKeyAction.LogEnd,      _hotKeyConfig);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ↑↓ Move    {pageUpKey} 이전    {pageDownKey} 다음    {homeKey} 처음    {endKey} 끝    {reloadKey} Reload    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        int pageSize     = GetPageSize();
        int end          = Math.Min(_scrollOffset + pageSize, snapshot.Count);
        int maxWidth     = Math.Max(1, Console.WindowWidth - 15);

        // 로그 목록
        for (int i = _scrollOffset; i < end; i++)
        {
            string line    = snapshot[i];
            string display = line.Length > maxWidth ? line[..maxWidth] + "..." : line;

            if (i == _logViewState.CursorIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  {display}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  {display}");
                Console.ResetColor();
            }
        }

        // 로그가 pageSize 보다 적을 때 빈 줄로 채움
        for (int i = end - _scrollOffset; i < pageSize; i++)
            Console.WriteLine();

        // details 영역 (항상 3줄 고정)
        string detail = (snapshot.Count > 0 && _logViewState.CursorIndex >= 0 && _logViewState.CursorIndex < snapshot.Count)
            ? snapshot[_logViewState.CursorIndex]
            : string.Empty;

        string[] detailLines = SplitToLines(detail, maxWidth, DetailLines);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.White;
        foreach (string dl in detailLines)
            Console.WriteLine($"  {dl}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  {_logViewState.CursorIndex + 1} / {snapshot.Count} lines");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        List<string> snapshot = _logViewState.Snapshot;

        if (keyInfo.Key == ConsoleKey.UpArrow && keyInfo.Modifiers == 0)
        {
            if (_logViewState.CursorIndex > 0)
            {
                _logViewState.CursorIndex--;
                if (_logViewState.CursorIndex < _scrollOffset)
                    _scrollOffset = _logViewState.CursorIndex;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (keyInfo.Key == ConsoleKey.DownArrow && keyInfo.Modifiers == 0)
        {
            if (_logViewState.CursorIndex < snapshot.Count - 1)
            {
                _logViewState.CursorIndex++;
                int pageSize = GetPageSize();
                if (_logViewState.CursorIndex >= _scrollOffset + pageSize)
                    _scrollOffset = _logViewState.CursorIndex - pageSize + 1;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.LogPageUp))
        {
            if (_logViewState.CursorIndex > 0)
            {
                _logViewState.CursorIndex = Math.Max(0, _logViewState.CursorIndex - PageStep);
                if (_logViewState.CursorIndex < _scrollOffset)
                    _scrollOffset = _logViewState.CursorIndex;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.LogPageDown))
        {
            if (_logViewState.CursorIndex < snapshot.Count - 1)
            {
                _logViewState.CursorIndex = Math.Min(snapshot.Count - 1, _logViewState.CursorIndex + PageStep);
                int pageSize = GetPageSize();
                if (_logViewState.CursorIndex >= _scrollOffset + pageSize)
                    _scrollOffset = _logViewState.CursorIndex - pageSize + 1;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.LogHome))
        {
            if (snapshot.Count > 0)
            {
                _logViewState.CursorIndex = 0;
                _scrollOffset = 0;
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.LogEnd))
        {
            if (snapshot.Count > 0)
            {
                MoveToBottom();
                InvalidateRequested?.Invoke();
            }
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.LogReload))
        {
            _logViewState.UpdateSnapshot(AppLog.GetSnapshot());
            MoveToBottom();
            InvalidateRequested?.Invoke();
            return true;
        }

        return false;
    }

    private int GetPageSize()
    {
        return Math.Max(1, Console.WindowHeight - HeaderLines - FooterLines);
    }

    private void MoveToBottom()
    {
        List<string> snapshot = _logViewState.Snapshot;
        _logViewState.CursorIndex  = Math.Max(0, snapshot.Count - 1);
        _scrollOffset = Math.Max(0, snapshot.Count - GetPageSize());
    }

    private static string[] SplitToLines(string text, int lineWidth, int maxLines)
    {
        var lines = new string[maxLines];
        int pos   = 0;
        for (int i = 0; i < maxLines; i++)
        {
            if (pos >= text.Length)
                lines[i] = string.Empty;
            else if (pos + lineWidth >= text.Length)
            {
                lines[i] = text[pos..];
                pos       = text.Length;
            }
            else
            {
                lines[i] = text[pos..(pos + lineWidth)];
                pos      += lineWidth;
            }
        }
        return lines;
    }
}
