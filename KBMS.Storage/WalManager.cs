using System.IO;

namespace KBMS.Storage;

/// <summary>
/// WalManager V2 - Write-Ahead Log (True WAL / Knowledge Log File .klf)
///
/// All CUD (Create/Update/Delete) operations are journaled here BEFORE the
/// main Buffer Pool is promoted to disk. On Server crash/restart, the engine
/// reads this .klf to understand which transactions were committed and which
/// were not, enabling clean recovery.
///
/// Log Entry Format:
///   [timestamp] [TXN_ID] OPERATION:DETAIL
///
/// Example:
///   [2026-03-20 14:00:00.000] [a1b2] INSERT_OBJECT:3f7c...
///   [2026-03-20 14:00:01.500] [a1b2] COMMIT
/// </summary>
public class WalManager
{
    // Each KB has its own .klf WAL file
    private string WalPath(string kbPath) => Path.Combine(kbPath, "transactions.klf");

    // ==================== WRITE ====================

    /// <summary>
    /// Appends a log entry for a CUD operation into the WAL file (.klf).
    /// The txnId links related entries together. Pass Guid.Empty if no active transaction.
    /// </summary>
    public void WriteLog(string kbPath, string logEntry, Guid? txnId = null)
    {
        Directory.CreateDirectory(kbPath);
        var walPath = WalPath(kbPath);
        var txn = txnId.HasValue ? txnId.Value.ToString("N")[..8] : "auto";
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] [{txn}] {logEntry}\n";
        File.AppendAllText(walPath, line);
    }

    /// <summary>
    /// Writes a COMMIT marker. The recovery routine uses this to identify
    /// fully committed transactions that are safe to replay.
    /// </summary>
    public void Commit(string kbPath, Guid? txnId = null)
    {
        var txn = txnId.HasValue ? txnId.Value.ToString("N")[..8] : "auto";
        var walPath = WalPath(kbPath);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        File.AppendAllText(walPath, $"[{timestamp}] [{txn}] COMMIT\n");
    }

    /// <summary>
    /// Writes a ROLLBACK marker. Signals to the recovery routine that the
    /// transaction's entries should be discarded.
    /// </summary>
    public void WriteRollback(string kbPath, Guid? txnId = null)
    {
        var txn = txnId.HasValue ? txnId.Value.ToString("N")[..8] : "auto";
        var walPath = WalPath(kbPath);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        File.AppendAllText(walPath, $"[{timestamp}] [{txn}] ROLLBACK\n");
    }

    // ==================== RECOVERY ====================

    /// <summary>
    /// Returns all committed log entries for this KB. Called on Server startup
    /// to re-hydrate the Buffer Pool from the WAL if the .kdf is stale or empty.
    /// </summary>
    public List<WalEntry> Recover(string kbPath)
    {
        var walPath = WalPath(kbPath);
        if (!File.Exists(walPath))
            return new List<WalEntry>();

        var lines = File.ReadAllLines(walPath);
        var committed = new List<WalEntry>();
        var pending = new List<WalEntry>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var entry = ParseLine(line);
            if (entry == null) continue;

            if (entry.Operation == "COMMIT")
            {
                // Everything accumulated under this txn ID is committed
                committed.AddRange(pending.Where(e => e.TxnId == entry.TxnId));
                pending.RemoveAll(e => e.TxnId == entry.TxnId);
            }
            else if (entry.Operation == "ROLLBACK")
            {
                // Discard pending entries for this txn
                pending.RemoveAll(e => e.TxnId == entry.TxnId);
            }
            else
            {
                pending.Add(entry);
            }
        }

        // Any pending (uncommitted) entries are NOT replayed — they were lost in crash.
        return committed;
    }

    // ==================== MAINTENANCE ====================

    /// <summary>
    /// Truncates the WAL after a successful full checkpoint (all data flushed to .kdf).
    /// This clears out the log so it doesn't grow unbounded.
    /// </summary>
    public void Checkpoint(string kbPath)
    {
        var walPath = WalPath(kbPath);
        if (File.Exists(walPath))
            File.WriteAllText(walPath, string.Empty);
    }

    // ==================== PARSING ====================

    private static WalEntry? ParseLine(string line)
    {
        // Format: [timestamp] [txnId] OPERATION:detail
        try
        {
            var parts = line.Split(']', 3);
            if (parts.Length < 3) return null;

            var timestamp = parts[0].TrimStart('[');
            var txnId = parts[1].TrimStart(' ', '[');
            var rest = parts[2].Trim();
            var colonIdx = rest.IndexOf(':');
            var operation = colonIdx >= 0 ? rest[..colonIdx] : rest;
            var detail = colonIdx >= 0 ? rest[(colonIdx + 1)..] : string.Empty;

            return new WalEntry
            {
                Timestamp = timestamp,
                TxnId = txnId,
                Operation = operation,
                Detail = detail
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>Represents a single parsed WAL entry from the .klf log file.</summary>
public class WalEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string TxnId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
