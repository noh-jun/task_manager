namespace TaskManager.Core.Navigation;

public interface IViewFactory
{
    IView Create(ViewId viewId, IViewNavigator viewNavigator);
}