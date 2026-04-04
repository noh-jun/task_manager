namespace TaskManager.Model;

public sealed class TaskEditSession
{
    public bool     IsCreateMode     { get; private set; } = true;
    public bool     IsCopyMode       { get; private set; } = false;
    public int      EditingTaskIndex { get; private set; } = -1;
    public TaskItem? CopySource      { get; private set; } = null;

    public void BeginCreate()
    {
        IsCreateMode     = true;
        IsCopyMode       = false;
        EditingTaskIndex = -1;
        CopySource       = null;
    }

    public void BeginEdit(int taskIndex)
    {
        IsCreateMode     = false;
        IsCopyMode       = false;
        EditingTaskIndex = taskIndex;
        CopySource       = null;
    }

    public void BeginCopy(TaskItem source)
    {
        IsCreateMode     = true;
        IsCopyMode       = true;
        EditingTaskIndex = -1;
        CopySource       = source.Clone();
    }

    public void Clear()
    {
        IsCreateMode     = true;
        IsCopyMode       = false;
        EditingTaskIndex = -1;
        CopySource       = null;
    }
}
