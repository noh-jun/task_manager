using TaskManager.Core;
using TaskManager.Core.Navigation;
using TaskManager.Infra.Csv;
using TaskManager.Infra.HotKeys;
using TaskManager.Infra.Json;
using TaskManager.Infra.Settings;
using TaskManager.Model;

namespace TaskManager;

internal static class Program
{
    private static void Main(string[] args)
    {
        AppLog.Write(LogLevel.Lv2, "[Program] 앱 시작");

        // TODO: 테스트용 더미 로그 - 확인 후 삭제
        LogLevel[] dummyLevels = { LogLevel.Lv1, LogLevel.Lv2, LogLevel.Lv3, LogLevel.Lv4, LogLevel.Lv5, LogLevel.Lv6 };
        for (int i = 0; i < 600; i++)
        {
            LogLevel lv = dummyLevels[i % dummyLevels.Length];
            AppLog.Write(lv, $"[Dummy] {lv} 테스트 로그 {i + 1:D3}");
        }

        string baseDir            = AppContext.BaseDirectory;
        string defaultConfigDir   = Path.Combine(baseDir, "config");
        string appSettingsPath    = Path.Combine(baseDir, "app_settings.json");

        var appSettings      = new AppSettings();
        var appSettingsStore = new AppSettingsStore(appSettingsPath, defaultConfigDir);
        appSettingsStore.Load(appSettings);

        string configDir         = appSettings.ConfigDirPath;
        string hotKeyConfigPath  = Path.Combine(configDir, "hotkeys.json");
        string taskRegistryPath  = Path.Combine(configDir, "task_registry.csv");
        string packageStorePath  = Path.Combine(configDir, "packages.json");

        var hotKeyConfig = new HotKeyConfig();
        var hotKeyConfigStore = new HotKeyConfigStore(hotKeyConfigPath);
        hotKeyConfigStore.Load(hotKeyConfig);

        var csvTaskStore     = new CsvTaskStore(taskRegistryPath);
        var jsonPackageStore = new JsonPackageStore(packageStorePath);

        IViewFactory viewFactory = new ViewFactory(hotKeyConfig, hotKeyConfigStore, csvTaskStore, jsonPackageStore, appSettings, appSettingsStore);
        IViewNavigator viewNavigator = new ViewNavigator(viewFactory, hotKeyConfig);

        viewNavigator.Push(ViewId.MainMenu);
        viewNavigator.Run();

        AppLog.Write(LogLevel.Lv2, "[Program] 앱 종료");
    }
}