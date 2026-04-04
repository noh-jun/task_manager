using System;
using TaskManager.Core.Navigation;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Tasks;

public sealed class TaskMenuView : IView
{
    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly TaskStore _taskStore;
    private readonly TaskEditSession _taskEditSession;
    private readonly string[] _menuItems;
    private int _selectedIndex;

    public TaskMenuView(IViewNavigator viewNavigator, HotKeyConfig hotKeyConfig, TaskStore taskStore, TaskEditSession taskEditSession)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig = hotKeyConfig ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _taskEditSession = taskEditSession ?? throw new ArgumentNullException(nameof(taskEditSession));
        _menuItems = new[]
        {
            "Task List",
            "Add Task",
        };
        _selectedIndex = 0;
    }

    public ViewId ViewId => ViewId.TaskMenu;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        Console.WriteLine("Task Menu");
        Console.WriteLine("=========");
        Console.WriteLine();

        for (int menuIndex = 0; menuIndex < _menuItems.Length; menuIndex++)
        {
            string prefix = menuIndex == _selectedIndex ? "> " : "  ";
            Console.WriteLine($"{prefix}{_menuItems[menuIndex]}");
        }

        Console.WriteLine();
        Console.WriteLine($"Task Count : {_taskStore.Count}");
        Console.WriteLine();

        Console.WriteLine("Controls");
        Console.WriteLine($"- Move Up   : {HotKeyHelper.ToDisplayText(HotKeyAction.MoveUp, _hotKeyConfig)}");
        Console.WriteLine($"- Move Down : {HotKeyHelper.ToDisplayText(HotKeyAction.MoveDown, _hotKeyConfig)}");
        Console.WriteLine("- Select    : Enter");
        Console.WriteLine("- Back      : Alt+Enter / Ctrl+Enter");
        Console.WriteLine("- Exit      : Ctrl+D");
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
            OpenSelectedMenu();
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

    private void OpenSelectedMenu()
    {
        switch (_selectedIndex)
        {
            case 0:
                _viewNavigator.Push(ViewId.TaskList);
            break;

            case 1:
                _taskEditSession.BeginCreate();
                _viewNavigator.Push(ViewId.TaskEditor);
            break;
        }
    }
}