namespace TaskManager.Model;

public sealed class TaskItem
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string ConfigPath { get; set; } = string.Empty;
    public string Args { get; set; } = string.Empty;

    public TaskItem Clone()
    {
        return new TaskItem
        {
            Name = Name,
            Command = Command,
            ConfigPath = ConfigPath,
            Args = Args,
        };
    }
}