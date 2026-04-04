using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaskManager.Core;
using TaskManager.Model;

namespace TaskManager.Infra.Json;

public sealed class JsonPackageStore
{
    private sealed class PackageDto
    {
        public string       Name       { get; set; } = string.Empty;
        public List<string> TaskNames  { get; set; } = new List<string>();
        public bool         IsFavorite { get; set; } = false;
    }

    private readonly string _filePath;

    public JsonPackageStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public void Load(TaskPackageStore packageStore)
    {
        ArgumentNullException.ThrowIfNull(packageStore);

        AppLog.Write(LogLevel.Lv1, $"[JsonPackageStore] 로드 시작: {_filePath}");

        if (!File.Exists(_filePath))
        {
            AppLog.Write(LogLevel.Lv1, $"[JsonPackageStore] 파일 없음, 로드 건너뜀: {_filePath}");
            return;
        }

        string json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return;

        List<PackageDto>? dtos = JsonSerializer.Deserialize<List<PackageDto>>(json);
        if (dtos is null)
            return;

        int count = 0;
        foreach (PackageDto dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                continue;

            packageStore.Add(new TaskPackage
            {
                Name       = dto.Name,
                TaskNames  = new List<string>(dto.TaskNames),
                IsFavorite = dto.IsFavorite,
            });
            count++;
        }

        AppLog.Write(LogLevel.Lv1, $"[JsonPackageStore] 로드 완료: {count}개");
    }

    public void Save(TaskPackageStore packageStore)
    {
        ArgumentNullException.ThrowIfNull(packageStore);

        string? directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var dtos = new List<PackageDto>();
        foreach (TaskPackage package in packageStore.GetAll())
        {
            dtos.Add(new PackageDto
            {
                Name       = package.Name,
                TaskNames  = new List<string>(package.TaskNames),
                IsFavorite = package.IsFavorite,
            });
        }

        string json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
        AppLog.Write(LogLevel.Lv1, $"[JsonPackageStore] 저장 완료: {_filePath}");
    }
}
