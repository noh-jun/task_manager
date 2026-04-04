namespace TaskManager.Core.Navigation;

public interface IViewNavigator
{
    void Push(ViewId viewId);
    void Pop();
    void Exit();
    void RequestRender();
    void Run();
}