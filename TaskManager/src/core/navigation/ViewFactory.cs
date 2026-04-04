using System;
using TaskManager.Infra.Csv;
using TaskManager.Infra.HotKeys;
using TaskManager.Infra.Json;
using TaskManager.Model;
using TaskManager.View;
using TaskManager.View.Options;
using TaskManager.View.Packages;
using TaskManager.View.Run;
using TaskManager.View.Tasks;

namespace TaskManager.Core.Navigation;

public sealed class ViewFactory : IViewFactory
{
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly HotKeyConfigStore _hotKeyConfigStore;
    private readonly HotKeyCaptureSession _hotKeyCaptureSession;
    private readonly CsvTaskStore _csvTaskStore;
    private readonly TaskStore _taskStore;
    private readonly TaskEditSession _taskEditSession;
    private readonly JsonPackageStore  _jsonPackageStore;
    private readonly TaskPackageStore  _packageStore;
    private readonly PackageEditSession _packageEditSession;
    private readonly PackageRunStore   _packageRunStore;
    private readonly LogViewState      _logViewState;
    private readonly ArgsEditSession   _argsEditSession;

    public ViewFactory(HotKeyConfig hotKeyConfig, HotKeyConfigStore hotKeyConfigStore, CsvTaskStore csvTaskStore, JsonPackageStore jsonPackageStore)
    {
        _hotKeyConfig      = hotKeyConfig      ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _hotKeyConfigStore = hotKeyConfigStore ?? throw new ArgumentNullException(nameof(hotKeyConfigStore));
        _csvTaskStore      = csvTaskStore      ?? throw new ArgumentNullException(nameof(csvTaskStore));
        _jsonPackageStore  = jsonPackageStore  ?? throw new ArgumentNullException(nameof(jsonPackageStore));
        _hotKeyCaptureSession = new HotKeyCaptureSession();
        _taskStore           = new TaskStore();
        _taskEditSession     = new TaskEditSession();
        _argsEditSession     = new ArgsEditSession();
        _packageStore        = new TaskPackageStore();
        _packageEditSession  = new PackageEditSession();
        _packageRunStore     = new PackageRunStore();
        _logViewState        = new LogViewState();
        _csvTaskStore.Load(_taskStore);
        _jsonPackageStore.Load(_packageStore);
    }

    public IView Create(ViewId viewId, IViewNavigator viewNavigator)
    {
        return viewId switch
        {
            ViewId.ExitConfirm  => new ExitConfirmView(viewNavigator),
            ViewId.MainMenu     => new MainMenuView(viewNavigator, _hotKeyConfig, _packageStore, _packageRunStore, _taskStore),
            ViewId.OptionsMenu  => new OptionsMenuView(viewNavigator, _hotKeyConfig),
            ViewId.HotKeyEdit   => new HotKeyEditView(viewNavigator, _hotKeyConfig, _hotKeyCaptureSession),
            ViewId.HotKeyCapture => new HotKeyCaptureView(viewNavigator, _hotKeyConfig, _hotKeyConfigStore, _hotKeyCaptureSession),
            ViewId.KeyInputTest => new KeyInputTestView(viewNavigator),
            ViewId.Logs        => new LogsView(viewNavigator, _hotKeyConfig, _logViewState),

            ViewId.TaskList    => new TaskListView(viewNavigator, _hotKeyConfig, _taskStore, _taskEditSession, _csvTaskStore),
            ViewId.TaskDetails => new TaskDetailsView(viewNavigator, _taskStore, _taskEditSession),
            ViewId.TaskEditor  => new TaskEditorView(viewNavigator, _taskStore, _taskEditSession, _csvTaskStore, _argsEditSession),
            ViewId.ArgsEdit    => new ArgsEditView(viewNavigator, _hotKeyConfig, _argsEditSession),

            ViewId.PackageRunList => new PackageRunListView(viewNavigator, _hotKeyConfig, _packageStore, _packageRunStore),
            ViewId.PackageRun    => new PackageRunView(viewNavigator, _taskStore, _packageStore, _packageRunStore),
            ViewId.TaskOutput    => new TaskOutputView(viewNavigator, _packageRunStore),

            ViewId.PackageList    => new PackageListView(viewNavigator, _hotKeyConfig, _taskStore, _packageStore, _packageEditSession, _jsonPackageStore),
            ViewId.PackageDetails => new PackageDetailsView(viewNavigator, _taskStore, _packageStore, _packageEditSession),
            ViewId.PackageEditor  => new PackageEditorView(viewNavigator, _taskStore, _packageStore, _packageEditSession, _jsonPackageStore),

            _ => throw new ArgumentOutOfRangeException(nameof(viewId), viewId, "Unsupported view id."),
        };
    }
}