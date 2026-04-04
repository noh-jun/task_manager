using System;
using System.Collections.Generic;

namespace TaskManager.Model;

public sealed class TaskPackageStore
{
    private readonly List<TaskPackage> _packages;

    public TaskPackageStore()
    {
        _packages = new List<TaskPackage>();
    }

    public int Count => _packages.Count;

    public IReadOnlyList<TaskPackage> GetAll()
    {
        return _packages;
    }

    public TaskPackage GetAt(int index)
    {
        if (index < 0 || index >= _packages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Package index is out of range.");
        }

        return _packages[index];
    }

    public void Add(TaskPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        _packages.Add(package.Clone());
    }

    public void Update(int index, TaskPackage package)
    {
        if (index < 0 || index >= _packages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Package index is out of range.");
        }

        ArgumentNullException.ThrowIfNull(package);
        _packages[index] = package.Clone();
    }

    public void Delete(int index)
    {
        if (index < 0 || index >= _packages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Package index is out of range.");
        }

        _packages.RemoveAt(index);
    }

    public bool IsDuplicateName(string name, int? excludeIndex = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        for (int i = 0; i < _packages.Count; i++)
        {
            if (excludeIndex.HasValue && excludeIndex.Value == i)
            {
                continue;
            }

            if (string.Equals(_packages[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
