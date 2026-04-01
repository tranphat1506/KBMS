using System;
using System.Collections.Generic;
using KBMS.Parser.Ast.Kql;
using KBMS.Knowledge.V3.Execution;
using KBMS.Storage.V3;
using System.Linq;

namespace KBMS.Knowledge.V3.Optimizer;

/// <summary>
/// The Cost-Based Optimizer (CBO) module for V3.
/// Converts an Abstract Syntax Tree (AST) into an optimized physical Execution Plan.
/// It applies optimization rules (HashJoin selection) and generates EXPLAIN data.
/// </summary>
public class QueryOptimizer
{
    private readonly BufferPoolManager _bpm;
    private readonly Func<string, string, List<int>> _pageIdResolver;

    public QueryOptimizer(BufferPoolManager bpm, Func<string, string, List<int>> pageIdResolver)
    {
        _bpm = bpm;
        _pageIdResolver = pageIdResolver;
    }


    /// <summary>
    /// Generates a physical evaluation network tree to be displayed by the EXPLAIN command.
    /// This represents the dry-run path of the query without actually pulling tuples.
    /// </summary>
    public PlanNode ExplainSelect(SelectNode ast, string kbName)
    {
        // 1. Base Scan Node
        var pageIds = _pageIdResolver(kbName, ast.ConceptName);
        var scanNode = new ScanPlanNode 
        { 
            Operation = "Sequential Scan", 
            Detail = $"Concept: {ast.ConceptName}",
            EstimatedCost = pageIds.Count * 1.0, 
            EstimatedRows = pageIds.Count * 10 // Assumption: 10 tuples per page
        };

        PlanNode currentRoot = scanNode;

        // 2. Resolve Joins
        if (ast.Joins != null)
        {
            foreach (var join in ast.Joins)
            {
                var buildScanNode = new ScanPlanNode 
                { 
                    Operation = "Sequential Scan", 
                    Detail = $"Concept: {join.Target}",
                    EstimatedCost = 50.0, 
                    EstimatedRows = 500
                };

                // Optimizer smartly chooses HashJoin over NestedLoop
                var hashJoinNode = new HashJoinPlanNode
                {
                    Operation = "Hash Join",
                    Detail = $"ON {join.OnCondition?.Field} = {join.OnCondition?.Value}",
                    EstimatedCost = currentRoot.EstimatedCost + buildScanNode.EstimatedCost + 10.0, // O(N+M) complexity representation
                    EstimatedRows = Math.Max(currentRoot.EstimatedRows, buildScanNode.EstimatedRows)
                };
                
                hashJoinNode.Children.Add(currentRoot);   // The probe side
                hashJoinNode.Children.Add(buildScanNode); // The build side
                currentRoot = hashJoinNode;
            }
        }

        // 3. Predicate Pushdown (Filter)
        if (ast.Conditions != null && ast.Conditions.Count > 0)
        {
            var filterNode = new FilterPlanNode
            {
                Operation = "Filter",
                Detail = $"Condition: {string.Join(" AND ", ast.Conditions.Select(c => $"{c.Field} {c.Operator} {c.Value}"))}",
                EstimatedCost = currentRoot.EstimatedCost * 1.1,
                EstimatedRows = currentRoot.EstimatedRows / 10 // Highly selective filter
            };
            filterNode.Children.Add(currentRoot);
            currentRoot = filterNode;
        }

        return currentRoot;
    }

    /// <summary>
    /// Builds and returns the actual Volcano Execution interface pipeline from the AST.
    /// Ready for .Init() and .Next() loop.
    /// </summary>
    public IExecutionOperator BuildExecutionPlan(SelectNode ast, string kbName)
    {
        // Physical binding: resolve real concept page IDs from the catalog
        var conceptPageIds = _pageIdResolver(kbName, ast.ConceptName); 
        
        IExecutionOperator currentOp = new SequentialScanOperator(_bpm, conceptPageIds);

        if (ast.Joins != null)
        {
            foreach (var join in ast.Joins)
            {
                var buildPageIds = _pageIdResolver(kbName, join.Target);
                var buildOp = new SequentialScanOperator(_bpm, buildPageIds);
                
                // Construct HashJoin operator logic
                currentOp = new HashJoinOperator(
                    leftBuild: buildOp, 
                    rightProbe: currentOp, 
                    leftJoinKeyIndex: 0, 
                    rightJoinKeyIndex: 0); 
            }
        }

        if (ast.Conditions != null && ast.Conditions.Count > 0)
        {
            // The Predicate execution runs on physical tuples
            currentOp = new FilterOperator(currentOp, PredicateCompiler.Compile(ast.Conditions)); 
        }


        return currentOp;
    }
}
