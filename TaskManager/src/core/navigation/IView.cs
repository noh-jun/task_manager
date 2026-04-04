using System;

namespace TaskManager.Core.Navigation;

public interface IView
{
    ViewId ViewId { get; }
    event Action? InvalidateRequested;

    void OnEnter();
    void OnLeave();
    void Render();
    bool HandleKey(ConsoleKeyInfo keyInfo);
}