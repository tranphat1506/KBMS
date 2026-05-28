using System;
using KBMS.Storage.Core;

namespace KBMS.Knowledge.Core.Execution;

/// <summary>
/// The core interface for the Volcano Execution Model.
/// All physical plan nodes implement this Iterator pattern to process tuples sequentially.
/// This prevents memory overflow (Out Of Memory errors) when dealing with massive Knowledge Bases.
/// </summary>
public interface IExecutionOperator : IDisposable
{
    /// <summary>
    /// Initializes or resets the operator. 
    /// Scans open files, joins might build hash tables here.
    /// </summary>
    void Init();

    /// <summary>
    /// Fetches the next tuple in the data stream.
    /// </summary>
    /// <returns>The next Tuple, or null if no more tuples exist (EOF).</returns>
    KBMS.Storage.Core.Tuple? Next();

    /// <summary>
    /// Closes the operator, releasing any Buffer Pool pins or file handles.
    /// </summary>
    void Close();
}
