using System.Collections.Generic;
using System.Diagnostics;

namespace TaskManager.Model;

public sealed class RunningTaskInfo
{
    private const int MaxLines = 5000;

    private readonly Queue<string> _outputBuffer = new();
    private readonly object        _lock          = new();

    public string  TaskName { get; set; } = string.Empty;
    public int     Pid      { get; set; }
    public Process Process  { get; set; } = null!;

    public void AddLine(string line)
    {
        lock (_lock)
        {
            _outputBuffer.Enqueue(line);
            if (_outputBuffer.Count > MaxLines)
                _outputBuffer.Dequeue();
        }
    }

    public List<string> GetSnapshot()
    {
        lock (_lock)
            return new List<string>(_outputBuffer);
    }
}
