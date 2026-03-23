using System;
using System.Collections.Generic;

namespace KBMS.Models;

/// <summary>
/// Structured error response from KBMS Server
/// </summary>
public class ErrorResponse
{
    public bool success => false;
    public string Type { get; set; } = string.Empty;      // ParserError, RuntimeError, AuthError, PermissionError, ExecutionError
    public string Message { get; set; } = string.Empty;
    public string? Query { get; set; }                     // Original query that failed
    public int? Line { get; set; }                         // Parser error line
    public int? Column { get; set; }                       // Parser error column
    public Dictionary<string, object?>? Details { get; set; }

    /// <summary>
    /// Create error response for parser exceptions (includes line/column information)
    /// </summary>
    public static ErrorResponse ParserErrorResponse(string message, string query, int? line = null, int? column = null)
    {
        return new ErrorResponse
        {
            Type = "ParserError",
            Message = message,
            Query = query,
            Line = line > 0 ? line : null,
            Column = column > 0 ? column : null
        };
    }

    /// <summary>
    /// Create error response for runtime exceptions
    /// </summary>
    public static ErrorResponse RuntimeErrorResponse(Exception ex, string query)
    {
        return new ErrorResponse
        {
            Type = "RuntimeError",
            Message = ex.Message,
            Query = query
        };
    }

    /// <summary>
    /// Create error response for authentication failures
    /// </summary>
    public static ErrorResponse AuthenticationErrorResponse(string message)
    {
        return new ErrorResponse
        {
            Type = "AuthError",
            Message = message
        };
    }

    /// <summary>
    /// Create error response for permission denied
    /// </summary>
    public static ErrorResponse PermissionErrorResponse(string action, string resource)
    {
        return new ErrorResponse
        {
            Type = "PermissionError",
            Message = $"Permission denied: {action} on {resource}",
            Details = new Dictionary<string, object?>
            {
                { "action", action },
                { "resource", resource }
            }
        };
    }

    /// <summary>
    /// Create error response for execution errors (e.g., from knowledge manager)
    /// </summary>
    public static ErrorResponse ExecutionErrorResponse(string message, string? query = null)
    {
        return new ErrorResponse
        {
            Type = "ExecutionError",
            Message = message,
            Query = query
        };
    }
}
