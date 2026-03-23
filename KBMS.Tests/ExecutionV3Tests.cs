using System;
using System.Collections.Generic;
using Xunit;
using KBMS.Storage.V3;
using KBMS.Knowledge.V3.Execution;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Tests;

public class ExecutionV3Tests
{
    private class MockScanOperator : IExecutionOperator
    {
        private readonly List<Tuple> _tuples;
        private int _index;

        public MockScanOperator(List<Tuple> tuples)
        {
            _tuples = tuples;
        }

        public void Init() => _index = 0;
        public Tuple? Next() => _index < _tuples.Count ? _tuples[_index++] : null;
        public void Close() { }
        public void Dispose() { }
    }

    [Fact]
    public void FilterOperator_FiltersCorrectly()
    {
        var tuples = new List<Tuple>
        {
            CreateTuple(1, "Alice"),
            CreateTuple(2, "Bob"),
            CreateTuple(3, "Charlie")
        };

        var scan = new MockScanOperator(tuples);
        var filter = new FilterOperator(scan, t => t.GetInt(0) > 1); // Keep Id > 1

        filter.Init();
        var t1 = filter.Next();
        Assert.NotNull(t1);
        Assert.Equal(2, t1.GetInt(0));
        Assert.Equal("Bob", t1.GetString(1));

        var t2 = filter.Next();
        Assert.NotNull(t2);
        Assert.Equal(3, t2.GetInt(0));
        Assert.Equal("Charlie", t2.GetString(1));

        Assert.Null(filter.Next());
    }

    [Fact]
    public void HashJoinOperator_JoinsCorrectly()
    {
        var leftTuples = new List<Tuple>
        {
            CreateTuple(1, "DeptA"),
            CreateTuple(2, "DeptB")
        };

        var rightTuples = new List<Tuple>
        {
            CreateTuple(101, 1), // belongs to DeptA
            CreateTuple(102, 1), // belongs to DeptA
            CreateTuple(103, 3)  // No match
        };

        var leftScan = new MockScanOperator(leftTuples);
        var rightScan = new MockScanOperator(rightTuples);

        var join = new HashJoinOperator(
            leftBuild: leftScan,
            rightProbe: rightScan,
            leftJoinKeyIndex: 0, // left tuple index 0 is deptId (1, 2)
            rightJoinKeyIndex: 1 // right tuple index 1 is deptId (1, 1, 3)
        );

        join.Init();

        // Expect: 
        // 1, "DeptA", 101, 1
        // 1, "DeptA", 102, 1
        
        var j1 = join.Next();
        Assert.NotNull(j1);
        Assert.Equal(1, j1.GetInt(0));
        Assert.Equal("DeptA", j1.GetString(1));
        Assert.Equal(101, j1.GetInt(2));
        Assert.Equal(1, j1.GetInt(3));

        var j2 = join.Next();
        Assert.NotNull(j2);
        Assert.Equal(1, j2.GetInt(0));
        Assert.Equal("DeptA", j2.GetString(1));
        Assert.Equal(102, j2.GetInt(2));
        Assert.Equal(1, j2.GetInt(3));

        Assert.Null(join.Next());
    }

    private Tuple CreateTuple(int id, string name)
    {
        var tuple = new Tuple();
        tuple.AddInt(id);
        tuple.AddString(name);
        return tuple;
    }

    private Tuple CreateTuple(int id, int refId)
    {
        var tuple = new Tuple();
        tuple.AddInt(id);
        tuple.AddInt(refId);
        return tuple;
    }
}
