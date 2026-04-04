using System;
using System.Collections.Generic;

namespace TaskManager.Model;

public sealed class TaskStore
{
    private readonly List<TaskItem> _taskItems;

    public TaskStore()
    {
        _taskItems = new List<TaskItem>();
    }

    public int Count => _taskItems.Count;

    public IReadOnlyList<TaskItem> GetAll()
    {
        return _taskItems;
    }

    public TaskItem GetAt(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= _taskItems.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(taskIndex), taskIndex, "Task index is out of range.");
        }

        return _taskItems[taskIndex];
    }

    public void Add(TaskItem taskItem)
    {
        if (taskItem is null)
        {
            throw new ArgumentNullException(nameof(taskItem));
        }

        _taskItems.Add(taskItem.Clone());
    }

    public void Update(int taskIndex, TaskItem taskItem)
    {
        if (taskIndex < 0 || taskIndex >= _taskItems.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(taskIndex), taskIndex, "Task index is out of range.");
        }

        if (taskItem is null)
        {
            throw new ArgumentNullException(nameof(taskItem));
        }

        _taskItems[taskIndex] = taskItem.Clone();
    }

    public void Delete(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= _taskItems.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(taskIndex), taskIndex, "Task index is out of range.");
        }

        _taskItems.RemoveAt(taskIndex);
    }

    public bool IsDuplicateName(string taskName, int? excludeTaskIndex = null)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            return false;
        }

        for (int taskIndex = 0; taskIndex < _taskItems.Count; taskIndex++)
        {
            if (excludeTaskIndex.HasValue && excludeTaskIndex.Value == taskIndex)
            {
                continue;
            }

            if (string.Equals(
                    _taskItems[taskIndex].Name,
                    taskName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}