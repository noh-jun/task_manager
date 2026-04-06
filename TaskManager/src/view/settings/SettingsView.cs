using System;
using TaskManager.Core.Navigation;
using TaskManager.Infra.Settings;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Settings;

public sealed class SettingsView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly AppSettings _appSettings;
    private readonly AppSettingsStore _appSettingsStore;
    private readonly DirectoryPickerSession _directoryPickerSession;

    private readonly string[] _menuItems;
    private int _selectedIndex;

    public SettingsView(
        IViewNavigator viewNavigator,
        HotKeyConfig hotKeyConfig,
        AppSettings appSettings,
        AppSettingsStore appSettingsStore,
        DirectoryPickerSession directoryPickerSession)
    {
        _viewNavigator          = viewNavigator          ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig           = hotKeyConfig           ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _appSettings            = appSettings            ?? throw new ArgumentNullException(nameof(appSettings));
        _appSettingsStore       = appSettingsStore       ?? throw new ArgumentNullException(nameof(appSettingsStore));
        _directoryPickerSession = directoryPickerSession ?? throw new ArgumentNullException(nameof(directoryPickerSession));
        _menuItems = new[]
        {
            "Config Dir Path",
        };
        _selectedIndex = 0;
    }

    public ViewId ViewId => ViewId.Settings;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        if (_directoryPickerSession.Confirmed)
        {
            _appSettings.ConfigDirPath = _directoryPickerSession.ResultPath;
            _appSettingsStore.Save(_appSettings);
            _directoryPickerSession.Confirmed = false;
        }
    }

    public void OnLeave() { }

    public void Render()
    {
        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Options  ›  Settings");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │         Settings         │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ↑↓ Move    Enter Edit    Alt+Enter Back    Ctrl+D Exit");
        Console.ResetColor();
        Console.WriteLine();

        // Settings list
        for (int i = 0; i < _menuItems.Length; i++)
        {
            string value = GetSettingValue(i);

            if (i == _selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ▶  {_menuItems[i].PadRight(20)}  {value} ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"     {_menuItems[i].PadRight(20)}  ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(value);
                Console.ResetColor();
            }
        }

        // Details at bottom
        int currentRow   = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 7);
        int paddingLines = windowHeight - currentRow - 6;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        (string en, string ko) = GetMenuDetail(_selectedIndex);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {en}");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  {ko}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.MoveUp))
        {
            MoveUp();
            return true;
        }

        if (HotKeyHelper.IsActionKey(keyInfo, _hotKeyConfig, HotKeyAction.MoveDown))
        {
            MoveDown();
            return true;
        }

        if (HotKeyHelper.IsSelectKey(keyInfo))
        {
            OpenEditor();
            return true;
        }

        return false;
    }

    private void MoveUp()
    {
        if (_selectedIndex > 0)
        {
            _selectedIndex--;
            InvalidateRequested?.Invoke();
        }
    }

    private void MoveDown()
    {
        if (_selectedIndex < _menuItems.Length - 1)
        {
            _selectedIndex++;
            InvalidateRequested?.Invoke();
        }
    }

    private void OpenEditor()
    {
        switch (_selectedIndex)
        {
            case 0:
                _directoryPickerSession.Begin(_appSettings.ConfigDirPath);
                _viewNavigator.Push(ViewId.DirectoryPicker);
                break;
        }
    }

    private string GetSettingValue(int index)
    {
        return index switch
        {
            0 => _appSettings.ConfigDirPath,
            _ => string.Empty,
        };
    }

    private static (string en, string ko) GetMenuDetail(int index)
    {
        return index switch
        {
            0 => ("Directory where config files (hotkeys.json, packages.json, task_registry.csv) are stored.",
                  "설정 파일(hotkeys.json, packages.json, task_registry.csv)이 저장되는 디렉토리 경로입니다."),
            _ => (string.Empty, string.Empty),
        };
    }
}
