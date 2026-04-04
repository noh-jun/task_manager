using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TaskManager.Core;
using TaskManager.Model;

namespace TaskManager.Infra.Csv;

public sealed class CsvTaskStore
{
    private const string Header = "Name,Command,ConfigPath,Args";

    private readonly string _filePath;

    public CsvTaskStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public void Load(TaskStore taskStore)
    {
        ArgumentNullException.ThrowIfNull(taskStore);

        AppLog.Write(LogLevel.Lv1, $"[CsvTaskStore] 로드 시작: {_filePath}");

        if (!File.Exists(_filePath))
        {
            AppLog.Write(LogLevel.Lv1, $"[CsvTaskStore] 파일 없음, 로드 건너뜀: {_filePath}");
            return;
        }

        string[] lines = File.ReadAllLines(_filePath, Encoding.UTF8);
        if (lines.Length == 0)
            return;

        if (!string.Equals(lines[0].Trim(), Header, StringComparison.Ordinal))
            return;

        int count = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!TryParseLine(line, out string name, out string command, out string configPath, out string args))
                continue;

            taskStore.Add(new TaskItem
            {
                Name = name,
                Command = command,
                ConfigPath = configPath,
                Args = args,
            });
            count++;
        }

        AppLog.Write(LogLevel.Lv1, $"[CsvTaskStore] 로드 완료: {count}개");
    }

    public void Save(TaskStore taskStore)
    {
        ArgumentNullException.ThrowIfNull(taskStore);

        string? directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (TaskItem task in taskStore.GetAll())
        {
            sb.AppendLine(
                $"{QuoteField(task.Name)},{QuoteField(task.Command)},{QuoteField(task.ConfigPath)},{QuoteField(task.Args)}");
        }

        File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
        AppLog.Write(LogLevel.Lv1, $"[CsvTaskStore] 저장 완료: {_filePath}");
    }

    private static string QuoteField(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static bool TryParseLine(string line, out string name, out string command, out string configPath, out string args)
    {
        name = command = configPath = args = string.Empty;

        List<string> fields = ParseCsvLine(line);
        if (fields.Count != 4)
            return false;

        name = fields[0];
        command = fields[1];
        configPath = fields[2];
        args = fields[3];
        return true;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    current.Append(c);
                    i++;
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                    i++;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                    i++;
                }
                else
                {
                    current.Append(c);
                    i++;
                }
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
