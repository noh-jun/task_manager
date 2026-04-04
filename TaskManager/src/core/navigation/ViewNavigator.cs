using System;
using System.Collections.Generic;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.Core.Navigation;

public sealed class ViewNavigator : IViewNavigator
{
    private readonly IViewFactory _viewFactory;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly Stack<IView> _viewStack;
    private bool _isExitRequested;
    private bool _isRenderRequested;

    public ViewNavigator(IViewFactory viewFactory, HotKeyConfig hotKeyConfig)
    {
        _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        _hotKeyConfig = hotKeyConfig ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _viewStack = new Stack<IView>();
        _isExitRequested = false;
        _isRenderRequested = true;
    }

    public void Push(ViewId viewId)
    {
        IView? currentView = GetCurrentView();
        if (currentView is not null)
        {
            UnbindView(currentView);
            currentView.OnLeave();
        }

        IView nextView = _viewFactory.Create(viewId, this);
        _viewStack.Push(nextView);

        BindView(nextView);
        nextView.OnEnter();
        RequestRender();
    }

    public void Pop()
    {
        if (_viewStack.Count <= 1)
        {
            RequestRender();
            return;
        }

        IView currentView = _viewStack.Peek();
        UnbindView(currentView);
        currentView.OnLeave();
        _viewStack.Pop();

        IView previousView = _viewStack.Peek();
        BindView(previousView);
        previousView.OnEnter();
        RequestRender();
    }

    public void Exit()
    {
        _isExitRequested = true;
    }

    public void RequestRender()
    {
        _isRenderRequested = true;
    }

    public void Run()
    {
        while (!_isExitRequested && _viewStack.Count > 0)
        {
            if (_isRenderRequested)
            {
                RenderActiveView();
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

            if (HandleGlobalKey(keyInfo))
            {
                continue;
            }

            IView? currentView = GetCurrentView();
            if (currentView is null)
            {
                continue;
            }

            bool isHandled = currentView.HandleKey(keyInfo);
            if (isHandled)
            {
                RequestRender();
            }
        }
    }

    private IView? GetCurrentView()
    {
        if (_viewStack.Count == 0)
        {
            return null;
        }

        return _viewStack.Peek();
    }

    private void BindView(IView view)
    {
        view.InvalidateRequested += OnViewInvalidateRequested;
    }

    private void UnbindView(IView view)
    {
        view.InvalidateRequested -= OnViewInvalidateRequested;
    }

    private void OnViewInvalidateRequested()
    {
        RequestRender();
    }

    private void RenderActiveView()
    {
        IView? currentView = GetCurrentView();
        if (currentView is null)
        {
            return;
        }

        Console.Clear();
        currentView.Render();
        _isRenderRequested = false;
    }

    private bool HandleGlobalKey(ConsoleKeyInfo keyInfo)
    {
        if (HotKeyHelper.IsExitKey(keyInfo))
        {
            Push(ViewId.ExitConfirm);
            return true;
        }

        if (HotKeyHelper.IsBackKey(keyInfo))
        {
            Pop();
            return true;
        }

        return false;
    }

}