using System;
using System.Collections.Generic;

namespace TaskManager.Model;

public sealed class ArgsEditSession
{
    public List<ArgEntry> Entries { get; } = new();

    public void LoadFrom(string argsString)
    {
        Entries.Clear();

        if (string.IsNullOrWhiteSpace(argsString))
            return;

        string[] tokens = argsString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            // Expected key-value format: --key={value}
            int eqBrace = token.IndexOf("={", StringComparison.Ordinal);
            if (eqBrace > 0 && token.EndsWith('}'))
            {
                string key   = token[..eqBrace];
                string value = token[(eqBrace + 2)..^1];   // strip "={" prefix and "}" suffix
                Entries.Add(new ArgEntry { Key = key, Value = value });
            }
            else
            {
                Entries.Add(new ArgEntry { Key = token, Value = null });
            }
        }
    }

    public string Serialize()
    {
        if (Entries.Count == 0)
            return string.Empty;

        return string.Join(" ", Entries.ConvertAll(e => e.Serialize()));
    }

    public string SerializeForExecution()
    {
        if (Entries.Count == 0)
            return string.Empty;

        return string.Join(" ", Entries.ConvertAll(e => e.SerializeForExecution()));
    }
}
