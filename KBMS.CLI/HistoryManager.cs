using System;
using System.Collections.Generic;
using System.Linq;

namespace KBMS.CLI;

public class HistoryManager
{
    private readonly List<string> _history = new();
    private readonly int _maxSize;

    public HistoryManager(int maxSize = 100)
    {
        _maxSize = maxSize;
    }

    public List<string> GetHistory() => new(_history);

    public void AddCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        
        // Privacy filter
        if (IsSensitive(command)) return;

        // Remove if already exists (bring to front)
        _history.RemoveAll(c => c.Equals(command, StringComparison.OrdinalIgnoreCase));

        _history.Add(command);

        if (_history.Count > _maxSize)
        {
            _history.RemoveAt(0);
        }
    }

    private bool IsSensitive(string command)
    {
        var upper = command.ToUpper().Trim();
        
        // Traditional sensitive keywords
        if (upper.StartsWith("LOGIN")) return true;
        if (upper.StartsWith("CREATE USER")) return true;
        
        // Combined keywords or substrings
        if (upper.Contains("PASSWORD")) return true;
        
        return false;
    }
}
