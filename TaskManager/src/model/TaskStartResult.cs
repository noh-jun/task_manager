namespace TaskManager.Model;

public sealed class TaskStartResult
{
    public string TaskName { get; set; } = string.Empty;
    public bool   Success  { get; set; }
    public int    Pid      { get; set; }
    public string Reason   { get; set; } = string.Empty;
}
