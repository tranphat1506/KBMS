using System;

namespace KBMS.Models.V3;

/// <summary>
/// Core phases of the KBMS V3 engine. Used to tag errors with their origin point.
/// </summary>
public enum ErrorStage
{
    PREPROCESS, // Audit Logs, Authentication, Roles & Privileges
    PARSER,     // Lexical tokens, Abstract Syntax Tree generation
    OPTIMIZER,  // Join ordering, Predicate pushdown, Plan generation
    EXECUTION,  // Volcano Iterators (Next), Type checking at runtime
    STORAGE     // Buffer Pool, Disk I/O, Page allocation, B+ Tree logic
}

/// <summary>
/// A centralized Exception class for the KBMS engine that provides high-resolution 
/// context on exactly where and why a query failed.
/// Integrates deeply with the V3 Pipeline.
/// </summary>
public class KBMSException : Exception
{
    public ErrorStage Stage { get; }
    public string SqlSnippet { get; }
    public int Line { get; }
    public int Column { get; }

    public string ErrorMessage { get; }

    public KBMSException(
        ErrorStage stage, 
        string message, 
        string sqlSnippet = "", 
        int line = -1, 
        int column = -1, 
        Exception? innerException = null) 
        : base(message, innerException)
    {
        ErrorMessage = message;
        Stage = stage;
        SqlSnippet = sqlSnippet;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Formats the exception into a structured JSON payload for the KBMS CLI/Client.
    /// This prevents exposing bare text logs and gives the frontend UI structural data.
    /// </summary>
    public string ToClientResponse()
    {
        var errorObj = new
        {
            status = "ERROR",
            stage = Stage.ToString(),
            message = ErrorMessage,
            context = new 
            {
                line = Line >= 0 ? (int?)Line : null,
                column = Column >= 0 ? (int?)Column : null,
                snippet = string.IsNullOrEmpty(SqlSnippet) ? null : SqlSnippet
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(errorObj, 
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
