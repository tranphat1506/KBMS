using System.IO;

namespace KBMS.Storage;

public class WalManager
{
    public void WriteLog(string kbPath, string logEntry)
    {
        var walPath = Path.Combine(kbPath, "wal.log");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] {logEntry}\n";
        File.AppendAllText(walPath, line);
    }

    public void Commit(string kbPath)
    {
        var walPath = Path.Combine(kbPath, "wal.log");
        File.AppendAllText(walPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] COMMIT\n");
    }

    public List<string> Recover(string kbPath)
    {
        var walPath = Path.Combine(kbPath, "wal.log");
        if (!File.Exists(walPath))
            return new List<string>();

        return File.ReadAllLines(walPath).ToList();
    }
}
