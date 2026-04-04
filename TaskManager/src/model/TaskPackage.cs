using System.Collections.Generic;

namespace TaskManager.Model;

public sealed class TaskPackage
{
    public string      Name       { get; set; } = string.Empty;
    public List<string> TaskNames { get; set; } = new List<string>();
    public bool        IsFavorite { get; set; } = false;

    public TaskPackage Clone()
    {
        return new TaskPackage
        {
            Name       = Name,
            TaskNames  = new List<string>(TaskNames),
            IsFavorite = IsFavorite,
        };
    }
}
