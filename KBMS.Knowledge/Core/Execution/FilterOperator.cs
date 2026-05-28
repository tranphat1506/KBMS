using System;
using KBMS.Storage.Core;
using Tuple = KBMS.Storage.Core.Tuple;

namespace KBMS.Knowledge.Core.Execution;

/// <summary>
/// Volcano pattern operator that filters tuples from its child operator 
/// based on a provided predicate function (representing WHERE and ON clauses).
/// </summary>
public class FilterOperator : IExecutionOperator
{
    private readonly IExecutionOperator _child;
    private readonly Func<Tuple, bool> _predicate;

    public FilterOperator(IExecutionOperator child, Func<Tuple, bool> predicate)
    {
        _child = child;
        _predicate = predicate;
    }

    public void Init()
    {
        _child.Init();
    }

    public Tuple? Next()
    {
        while (true)
        {
            var tuple = _child.Next();
            if (tuple == null) return null; // EOF
            
            if (_predicate(tuple))
            {
                return tuple; // Record satisfies condition
            }
        }
    }

    public void Close()
    {
        _child.Close();
    }

    public void Dispose()
    {
        _child.Dispose();
    }
}
