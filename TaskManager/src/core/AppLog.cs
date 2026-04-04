using System;
using System.Collections.Generic;

namespace TaskManager.Core;

public enum LogLevel
{
    Lv1 = 1,
    Lv2 = 2,
    Lv3 = 3,
    Lv4 = 4,
    Lv5 = 5,
    Lv6 = 6,
}

public static class AppLog
{
    public const int MaxLines = 10_000;

    private static readonly object _lock = new();
    private static readonly Queue<string> _lines = new();

    public static LogLevel CurrentLevel { get; set; } = LogLevel.Lv1;

    public static void Write(LogLevel level, string message)
    {
        if (level < CurrentLevel)
            return;

        string entry = $"[{DateTime.Now:HH:mm:ss.fff}][{level}] {message}";
        lock (_lock)
        {
            if (_lines.Count >= MaxLines)
                _lines.Dequeue();
            _lines.Enqueue(entry);
        }
    }

    public static List<string> GetSnapshot()
    {
        lock (_lock)
        {
            return new List<string>(_lines);
        }
    }

    public static int Count
    {
        get { lock (_lock) return _lines.Count; }
    }
}
