using System.Text.Json;
using TaskManager.Core;
using TaskManager.Model;

namespace TaskManager.Infra.Settings;

public sealed class AppSettingsStore
{
    private sealed class AppSettingsDto
    {
        public string ConfigDirPath { get; set; } = string.Empty;
    }

    private readonly string _filePath;
    private readonly string _defaultConfigDirPath;

    public AppSettingsStore(string filePath, string defaultConfigDirPath)
    {
        _filePath             = filePath             ?? throw new ArgumentNullException(nameof(filePath));
        _defaultConfigDirPath = defaultConfigDirPath ?? throw new ArgumentNullException(nameof(defaultConfigDirPath));
    }

    public void Load(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        AppLog.Write(LogLevel.Lv1, $"[AppSettingsStore] 로드 시작: {_filePath}");

        if (!File.Exists(_filePath))
        {
            settings.ConfigDirPath = _defaultConfigDirPath;
            AppLog.Write(LogLevel.Lv1, $"[AppSettingsStore] 파일 없음, 기본값 사용: {_defaultConfigDirPath}");
            Save(settings);
            return;
        }

        string json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            settings.ConfigDirPath = _defaultConfigDirPath;
            Save(settings);
            return;
        }

        AppSettingsDto? dto = JsonSerializer.Deserialize<AppSettingsDto>(json);
        if (dto is null || string.IsNullOrWhiteSpace(dto.ConfigDirPath))
        {
            settings.ConfigDirPath = _defaultConfigDirPath;
            Save(settings);
            return;
        }

        settings.ConfigDirPath = dto.ConfigDirPath;
        AppLog.Write(LogLevel.Lv1, $"[AppSettingsStore] 로드 완료: ConfigDirPath={settings.ConfigDirPath}");
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var dto = new AppSettingsDto { ConfigDirPath = settings.ConfigDirPath };
        string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
        AppLog.Write(LogLevel.Lv1, $"[AppSettingsStore] 저장 완료: {_filePath}");
    }
}
