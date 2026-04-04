namespace TaskManager.Model;

public sealed class HotKeyCaptureSession
{
    public HotKeyAction TargetAction { get; private set; }
    public string? LastResultMessage { get; private set; }

    public void Begin(HotKeyAction action)
    {
        TargetAction = action;
        LastResultMessage = null;
    }

    public void SetLastResultMessage(string message)
    {
        LastResultMessage = message;
    }

    public void ClearLastResultMessage()
    {
        LastResultMessage = null;
    }
}
