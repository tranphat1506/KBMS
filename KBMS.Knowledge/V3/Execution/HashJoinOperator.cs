using System;
using System.Collections.Generic;
using KBMS.Storage.V3;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Knowledge.V3.Execution;

/// <summary>
/// Volcano pattern operator that performs an Equi-Join using the Hash Join algorithm.
/// Reduces O(N * M) complex Nested Loop join to O(N + M) amortized time.
/// </summary>
public class HashJoinOperator : IExecutionOperator
{
    private readonly IExecutionOperator _leftBuild;
    private readonly IExecutionOperator _rightProbe;
    private readonly int _leftJoinKeyIndex;
    private readonly int _rightJoinKeyIndex;

    // In-memory Hash Table mapping the join key (encoded as Base64 string for equality) 
    // to a list of matching tuples from the Left (Build) side.
    private readonly Dictionary<string, List<Tuple>> _hashTable = new();

    // State for the Probe phase
    private Tuple? _currentRightTuple = null;
    private List<Tuple>? _currentMatchedLeftTuples = null;
    private int _matchingLeftIndex = 0;

    public HashJoinOperator(
        IExecutionOperator leftBuild, 
        IExecutionOperator rightProbe, 
        int leftJoinKeyIndex, 
        int rightJoinKeyIndex)
    {
        _leftBuild = leftBuild;
        _rightProbe = rightProbe;
        _leftJoinKeyIndex = leftJoinKeyIndex;
        _rightJoinKeyIndex = rightJoinKeyIndex;
    }

    public void Init()
    {
        _leftBuild.Init();
        _rightProbe.Init();
        _hashTable.Clear();

        // 1. Build Phase: Consume left child completely to build hash table in RAM
        Tuple? leftTuple;
        while ((leftTuple = _leftBuild.Next()) != null)
        {
            // Encode the matched field parameter into a string for O(1) Dictionary lookups
            string key = Convert.ToBase64String(leftTuple.Fields[_leftJoinKeyIndex]);
            
            if (!_hashTable.ContainsKey(key))
            {
                _hashTable[key] = new List<Tuple>();
            }
            _hashTable[key].Add(leftTuple);
        }

        _leftBuild.Close(); // Done with build phase to free resources
        _currentRightTuple = null;
        _currentMatchedLeftTuples = null;
        _matchingLeftIndex = 0;
    }

    public Tuple? Next()
    {
        // 2. Probe Phase: Pull right child continuously and probe the hash table
        while (true)
        {
            // If we are currently outputting a cross product of matching tuples
            if (_currentMatchedLeftTuples != null && _matchingLeftIndex < _currentMatchedLeftTuples.Count)
            {
                var leftTuple = _currentMatchedLeftTuples[_matchingLeftIndex++];
                return CombineTuples(leftTuple, _currentRightTuple!);
            }

            // Fetch the next tuple from the right side
            _currentRightTuple = _rightProbe.Next();
            if (_currentRightTuple == null) return null; // EOF

            string key = Convert.ToBase64String(_currentRightTuple.Fields[_rightJoinKeyIndex]);
            if (_hashTable.TryGetValue(key, out var matchedTuples))
            {
                _currentMatchedLeftTuples = matchedTuples;
                _matchingLeftIndex = 0;
            }
            else
            {
                _currentMatchedLeftTuples = null; // No match, skip right tuple
            }
        }
    }

    private Tuple CombineTuples(Tuple left, Tuple right)
    {
        var combined = new Tuple();
        combined.Fields.AddRange(left.Fields);
        combined.Fields.AddRange(right.Fields);
        return combined;
    }

    public void Close()
    {
        _hashTable.Clear();
        _leftBuild.Close();
        _rightProbe.Close();
    }

    public void Dispose()
    {
        Close();
    }
}
