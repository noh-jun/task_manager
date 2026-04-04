namespace TaskManager.Model;

public sealed class PackageEditSession
{
    public bool IsCreateMode { get; private set; } = true;
    public int EditingPackageIndex { get; private set; } = -1;

    public void BeginCreate()
    {
        IsCreateMode = true;
        EditingPackageIndex = -1;
    }

    public void BeginEdit(int packageIndex)
    {
        IsCreateMode = false;
        EditingPackageIndex = packageIndex;
    }

    public void Clear()
    {
        IsCreateMode = true;
        EditingPackageIndex = -1;
    }
}
