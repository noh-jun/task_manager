namespace TaskManager.Model;

public sealed class ArgEntry
{
    public string  Key   { get; set; } = string.Empty;
    public string? Value { get; set; } = null;   // null = key-only

    public bool IsKeyOnly => Value is null;

    // Serializes to: "--key" or "--key={value}"  (storage / display format)
    public string Serialize() =>
        Value is null ? Key : $"{Key}={{{Value}}}";

    // Serializes to: "--key" or "--key value"  (process execution format)
    public string SerializeForExecution() =>
        Value is null ? Key : $"{Key} {Value}";

    public ArgEntry Clone() => new ArgEntry { Key = Key, Value = Value };
}
