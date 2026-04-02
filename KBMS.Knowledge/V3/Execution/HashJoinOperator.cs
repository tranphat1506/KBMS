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
    private readonly string _leftAlias;
    private readonly string _rightAlias;
    
    // In-memory Hash Table mapping the join key (encoded as Base64 string for equality) 
    // to a list of matching tuples from the Left (Build) side.
    private readonly Dictionary<string, List<Tuple>> _hashTable = new();
    private bool _isBuilt = false;
    
    // State for the Probe phase
    private Tuple? _currentRightTuple = null;
    private List<Tuple>? _currentMatchedLeftTuples = null;
    private int _matchingLeftIndex = 0;

    public HashJoinOperator(IExecutionOperator leftBuild, IExecutionOperator rightProbe, int leftJoinKeyIndex, int rightJoinKeyIndex, string leftAlias = "L", string rightAlias = "R")
    {
        _leftBuild = leftBuild;
        _rightProbe = rightProbe;
        _leftJoinKeyIndex = leftJoinKeyIndex;
        _rightJoinKeyIndex = rightJoinKeyIndex;
        _leftAlias = leftAlias;
        _rightAlias = rightAlias;
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
            string rawValue = System.Text.Encoding.UTF8.GetString(leftTuple.Fields[_leftJoinKeyIndex]);
            System.IO.File.AppendAllText("/tmp/kbms_diag.log", $"[HASHJOIN] Building key: index={_leftJoinKeyIndex}, raw='{rawValue}', base64={key}\n");
            
            if (!_hashTable.ContainsKey(key))
            {
                _hashTable[key] = new List<Tuple>();
            }
            _hashTable[key].Add(leftTuple);
        }
        System.IO.File.AppendAllText("/tmp/kbms_diag.log", $"[HASHJOIN] Finish Build phase. Table entries: {_hashTable.Count}\n");

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
            string rawValue = System.Text.Encoding.UTF8.GetString(_currentRightTuple.Fields[_rightJoinKeyIndex]);
            System.IO.File.AppendAllText("/tmp/kbms_diag.log", $"[HASHJOIN] Probing key: index={_rightJoinKeyIndex}, raw='{rawValue}', base64={key}\n");

            if (_hashTable.TryGetValue(key, out var matchedTuples))
            {
                System.IO.File.AppendAllText("/tmp/kbms_diag.log", $"[HASHJOIN] Found {matchedTuples.Count} matches for {key}\n");
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
        
        bool hasV3Metadata = left.Fields.Count >= 2 && left.Fields[0].Length == 16;

        if (hasV3Metadata)
        {
            combined.AddGuid(Guid.NewGuid());
            
            var leftNames = left.GetString(1).Split('|').Select(n => $"{_leftAlias}.{n}");
            var rightNames = right.GetString(1).Split('|').Select(n => $"{_rightAlias}.{n}");
            
            combined.AddString(string.Join("|", leftNames.Concat(rightNames)));
            
            for (int i = 2; i < left.Fields.Count; i++) combined.Fields.Add(left.Fields[i]);
            for (int i = 2; i < right.Fields.Count; i++) combined.Fields.Add(right.Fields[i]);
        }
        else
        {
            // Generic Unit Test Tuples (just append everything)
            combined.Fields.AddRange(left.Fields);
            combined.Fields.AddRange(right.Fields);
        }
        
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
