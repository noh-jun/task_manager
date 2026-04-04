using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TaskManager.Core;

namespace TaskManager.Model;

public sealed class PackageRunStore
{
    private readonly List<RunningTaskInfo> _runningTasks = new();

    public string? ActivePackageName      { get; private set; }
    public string? SelectedOutputTaskName { get; set; }
    public int     SelectedPackageIndex   { get; private set; } = -1;
    public bool    IsActive               => ActivePackageName is not null;

    public void SelectPackage(int index)
    {
        SelectedPackageIndex = index;
    }

    public IReadOnlyList<RunningTaskInfo> GetRunningTasks()
    {
        return _runningTasks;
    }

    public RunningTaskInfo? GetRunningInfo(string taskName)
    {
        foreach (RunningTaskInfo info in _runningTasks)
        {
            if (string.Equals(info.TaskName, taskName, StringComparison.OrdinalIgnoreCase))
                return info;
        }

        return null;
    }

    public List<TaskStartResult> Start(TaskPackage package, TaskStore taskStore)
    {
        var results = new List<TaskStartResult>();

        if (IsActive)
            return results;

        AppLog.Write(LogLevel.Lv5, $"[PackageRun] 패키지 시작 요청: {package.Name}");

        ActivePackageName = package.Name;
        _runningTasks.Clear();

        foreach (string taskName in package.TaskNames)
        {
            TaskItem? task = FindTask(taskStore, taskName);
            if (task is null)
            {
                AppLog.Write(LogLevel.Lv6, $"[PackageRun] 태스크 없음: {taskName}");
                results.Add(new TaskStartResult
                {
                    TaskName = taskName,
                    Success  = false,
                    Reason   = "task not found in task list",
                });
                continue;
            }

            string   command   = BuildCommand(task);
            string[] parts     = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string   fileName  = parts[0];
            string   arguments = parts.Length > 1 ? parts[1] : string.Empty;

            var psi = new ProcessStartInfo
            {
                FileName               = fileName,
                Arguments              = arguments,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };

            try
            {
                Process? process = Process.Start(psi);
                if (process is null)
                {
                    AppLog.Write(LogLevel.Lv6, $"[PackageRun] 프로세스 생성 실패: {taskName}");
                    results.Add(new TaskStartResult
                    {
                        TaskName = taskName,
                        Success  = false,
                        Reason   = "process returned null",
                    });
                    continue;
                }

                var info = new RunningTaskInfo
                {
                    TaskName = taskName,
                    Pid      = process.Id,
                    Process  = process,
                };

                process.OutputDataReceived += (_, e) => { if (e.Data is not null) info.AddLine(e.Data); };
                process.ErrorDataReceived  += (_, e) => { if (e.Data is not null) info.AddLine(e.Data); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _runningTasks.Add(info);

                AppLog.Write(LogLevel.Lv5, $"[PackageRun] 프로세스 시작 성공: {taskName} PID={process.Id}");
                results.Add(new TaskStartResult
                {
                    TaskName = taskName,
                    Success  = true,
                    Pid      = process.Id,
                });
            }
            catch (Exception ex)
            {
                AppLog.Write(LogLevel.Lv6, $"[PackageRun] 프로세스 시작 예외: {taskName} - {ex.Message}");
                results.Add(new TaskStartResult
                {
                    TaskName = taskName,
                    Success  = false,
                    Reason   = ex.Message,
                });
            }
        }

        return results;
    }

    public void Stop()
    {
        AppLog.Write(LogLevel.Lv5, $"[PackageRun] 패키지 종료 요청: {ActivePackageName}");

        foreach (RunningTaskInfo info in _runningTasks)
        {
            AppLog.Write(LogLevel.Lv5, $"[PackageRun] 프로세스 종료 요청: {info.TaskName} PID={info.Pid}");
            try { info.Process.Kill(entireProcessTree: true); }
            catch (Exception ex)
            {
                AppLog.Write(LogLevel.Lv6, $"[PackageRun] 프로세스 종료 실패: {info.TaskName} - {ex.Message}");
            }
        }

        _runningTasks.Clear();
        AppLog.Write(LogLevel.Lv5, $"[PackageRun] 패키지 종료 완료: {ActivePackageName}");
        ActivePackageName = null;
    }

    private static TaskItem? FindTask(TaskStore taskStore, string taskName)
    {
        for (int i = 0; i < taskStore.Count; i++)
        {
            if (string.Equals(taskStore.GetAt(i).Name, taskName, StringComparison.OrdinalIgnoreCase))
                return taskStore.GetAt(i);
        }

        return null;
    }

    private static string BuildCommand(TaskItem task)
    {
        var sb = new StringBuilder(task.Command);

        if (!string.IsNullOrWhiteSpace(task.Args))
        {
            var session = new ArgsEditSession();
            session.LoadFrom(task.Args);
            sb.Append(' ').Append(session.SerializeForExecution());
        }

        if (!string.IsNullOrWhiteSpace(task.ConfigPath))
            sb.Append(" --config ").Append(task.ConfigPath);

        return sb.ToString();
    }
}
