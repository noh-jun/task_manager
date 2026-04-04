using System.Text.Json;
using TaskManager.Core;
using TaskManager.Model;

namespace TaskManager.Infra.HotKeys;

public sealed class HotKeyConfigStore
{
    private sealed class GestureDto
    {
        public int    Key       { get; set; }
        public string KeyChar   { get; set; } = string.Empty;
        public int    Modifiers { get; set; }
    }

    private sealed class HotKeyConfigDto
    {
        public Dictionary<string, GestureDto> KeyMap { get; set; } = new();
    }

    private readonly string _filePath;

    public HotKeyConfigStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public void Load(HotKeyConfig hotKeyConfig)
    {
        if (hotKeyConfig is null)
            throw new ArgumentNullException(nameof(hotKeyConfig));

        AppLog.Write(LogLevel.Lv1, $"[HotKeyConfigStore] 로드 시작: {_filePath}");

        if (!File.Exists(_filePath))
        {
            AppLog.Write(LogLevel.Lv1, $"[HotKeyConfigStore] 파일 없음, 기본값으로 신규 생성: {_filePath}");
            Save(hotKeyConfig);
            return;
        }

        string jsonText = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Save(hotKeyConfig);
            return;
        }

        HotKeyConfigDto? dto = JsonSerializer.Deserialize<HotKeyConfigDto>(jsonText);
        if (dto is null)
        {
            Save(hotKeyConfig);
            return;
        }

        foreach (KeyValuePair<string, GestureDto> pair in dto.KeyMap)
        {
            if (!Enum.TryParse(pair.Key, ignoreCase: true, out HotKeyAction action))
                continue;

            char keyChar = pair.Value.KeyChar.Length == 1 ? pair.Value.KeyChar[0] : '\0';
            var gesture  = new HotKeyGesture(
                (ConsoleKey)pair.Value.Key,
                (ConsoleModifiers)pair.Value.Modifiers,
                keyChar);

            hotKeyConfig.SetKeyWithoutValidation(action, gesture);
        }

        AppLog.Write(LogLevel.Lv1, $"[HotKeyConfigStore] 로드 완료: {_filePath}");
    }

    public void Save(HotKeyConfig hotKeyConfig)
    {
        if (hotKeyConfig is null)
            throw new ArgumentNullException(nameof(hotKeyConfig));

        string? directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

        IReadOnlyDictionary<HotKeyAction, HotKeyGesture> exportedKeyMap = hotKeyConfig.Export();

        var configDto = new HotKeyConfigDto();
        foreach (KeyValuePair<HotKeyAction, HotKeyGesture> pair in exportedKeyMap)
        {
            configDto.KeyMap[pair.Key.ToString()] = new GestureDto
            {
                Key       = (int)pair.Value.Key,
                KeyChar   = pair.Value.KeyChar == '\0' ? string.Empty : pair.Value.KeyChar.ToString(),
                Modifiers = (int)pair.Value.Modifiers,
            };
        }

        string jsonText = JsonSerializer.Serialize(configDto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, jsonText);
        AppLog.Write(LogLevel.Lv1, $"[HotKeyConfigStore] 저장 완료: {_filePath}");
    }
}
