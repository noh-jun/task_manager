using System.Collections.Generic;

namespace TaskManager.Model;

public sealed class LogViewState
{
    public List<string> Snapshot    { get; private set; } = new();
    public int          CursorIndex { get; set; }

    public void UpdateSnapshot(List<string> snapshot)
    {
        Snapshot = snapshot;
    }
}
