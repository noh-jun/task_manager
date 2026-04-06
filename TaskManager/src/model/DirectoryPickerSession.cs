namespace TaskManager.Model;

public sealed class DirectoryPickerSession
{
    public string InitialPath { get; set; } = string.Empty;
    public string ResultPath  { get; set; } = string.Empty;
    public bool   Confirmed   { get; set; } = false;

    public void Begin(string initialPath)
    {
        InitialPath = initialPath ?? string.Empty;
        ResultPath  = string.Empty;
        Confirmed   = false;
    }

    public void Confirm(string resultPath)
    {
        ResultPath = resultPath ?? string.Empty;
        Confirmed  = true;
    }
}
