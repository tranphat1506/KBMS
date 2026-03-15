using System;
using System.Collections.Generic;
using KBMS.Parser;

namespace KBMS.Network;

/// <summary>
/// Structured error response from KBMS Server
/// </summary>
public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;      // ParserError, RuntimeError, AuthError, PermissionError, ExecutionError
    public string Message { get; set; } = string.Empty;
    public string? Query { get; set; }                     // Original query that failed
    public int? Line { get; set; }                         // Parser error line
    public int? Column { get; set; }                       // Parser error column
    public Dictionary<string, object?>? Details { get; set; }

    /// <summary>
    /// Create error response for parser exceptions (includes line/column information)
    /// </summary>
    public static ErrorResponse ParserErrorResponse(ParserException ex, string query)
    {
        return new ErrorResponse
        {
            Type = "ParserError",
            Message = ex.Message,
            Query = query,
            Line = ex.Line > 0 ? ex.Line : null,
            Column = ex.Column > 0 ? ex.Column : null
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
