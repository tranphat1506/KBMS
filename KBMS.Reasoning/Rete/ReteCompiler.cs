using System;
using System.Collections.Generic;
using System.Linq;
using KBMS.Models;

namespace KBMS.Reasoning.Rete;

/// <summary>
/// Compiles Concept models (Rules and Equations) into a Rete network.
/// </summary>
public class ReteCompiler
{
    private readonly InferenceEngine _engine;
    private readonly ReteNetwork _network;

    public ReteCompiler(InferenceEngine engine, ReteNetwork network)
    {
        _engine = engine;
        _network = network;
    }

    /// <summary>
    /// Compiles a concept's rules and equations into the Rete network.
    /// </summary>
    public void Compile(Concept concept)
    {
        // 1. Compile Rules
        foreach (var rule in concept.ConceptRules)
        {
            CompileRule(concept, rule);
        }

        // 2. Compile Equations (as potential rules)
        foreach (var eq in concept.Equations)
        {
            CompileEquation(concept, eq);
        }

        // 3. Compile Computation Relations
        foreach (var rel in concept.CompRels)
        {
            CompileComputation(concept, rel);
        }

        // 4. Compile SameVariables
        foreach (var sv in concept.SameVariables)
        {
            CompileSameVariable(concept, sv);
        }

        // 5. Compile Constraints (as potential rules/solvers)
        foreach (var constraint in concept.Constraints)
        {
            CompileConstraint(concept, constraint);
        }
    }

    private void CompileRule(Concept concept, ConceptRule rule)
    {
        var terminalAction = new Action<Token>(token => {
            // This is the action to perform when the rule hypothesis is met
            foreach (var conclusion in rule.Conclusion)
            {
                // We use a simplified context from the token's facts
                var facts = token.ToDictionary();
                _engine.ApplyConclusion(conclusion, concept, facts, new InferenceEngine.ReasoningResult(), rule.Kind);
                
                // Assert the new facts back into the network for further reasoning
                foreach (var fact in facts)
                {
                    _network.AssertFact(fact.Key, fact.Value);
                }
            }
        });

        var terminalNode = new TerminalNode(rule.Kind ?? "Unnamed Rule", (token) => _network.AddToAgenda(null!, token)); 
        // Note: TerminalNode adds to agenda. The actual execution happens in FireNext.
        // We override the activation to use our terminalAction.
        var customTerminal = new TerminalNode(rule.Kind ?? "Unnamed Rule", terminalAction);

        // Chain conditions
        ReteNode currentParent = _network.Root;
        
        // Simplicity: we assume each hypothesis is a single condition on one or more variables.
        // For each Variable mentioned in the hypothesis, we need an AlphaNode.
        
        // This is a naive implementation: treating the whole hypothesis list as a sequence of facts
        // In reality, Rete is more complex. For this MVP, let's treat variables as facts.
        
        var neededVars = rule.Hypothesis.SelectMany(h => _engine.ExtractVariablesFromExpression(h)).Distinct().ToList();
        
        if (!neededVars.Any()) return;

        // Build the chain
        ReteNode? lastNode = null;

        for (int i = 0; i < neededVars.Count; i++)
        {
            var alpha = _network.GetOrCreateAlphaNode(neededVars[i]);
            
            if (i == 0)
            {
                lastNode = alpha;
            }
            else
            {
                var beta = new BetaNode();
                beta.LeftParent = lastNode;
                beta.RightParent = alpha;
                
                // Link Beta to its parents
                lastNode!.AddChild(new LeftDistributor(beta));
                alpha.AddChild(new RightDistributor(beta));
                
                lastNode = beta;
            }
        }

        // Final condition check node (Special Alpha/Beta to verify hypothesis)
        var filterNode = new FilterNode(token => {
            var facts = token.ToDictionary();
            return rule.Hypothesis.All(h => {
                try { return _engine.EvaluateConstraint(h, facts); } catch { return false; }
            });
        });

        lastNode!.AddChild(filterNode);
        filterNode.AddChild(customTerminal);
    }

    private void CompileEquation(Concept concept, Equation eq)
    {
        var vars = _engine.ExtractVariablesFromExpression(eq.Expression);
        if (vars == null || vars.Count <= 1) return; // Cannot solve if 0 or 1 total vars

        // For an equation with N variables, it can be triggered when N-1 are known.
        // We can create N possible "paths" in the Rete network, one for each target variable.
        
        foreach (var target in vars)
        {
            var inputs = vars.Where(v => v != target).ToList();
            
            // Build a chain for knowledge of all 'inputs'
            ReteNode? lastNode = null;
            for (int i = 0; i < inputs.Count; i++)
            {
                var alpha = _network.GetOrCreateAlphaNode(inputs[i]);
                if (i == 0) lastNode = alpha;
                else
                {
                    var beta = new BetaNode();
                    beta.LeftParent = lastNode;
                    beta.RightParent = alpha;
                    lastNode!.AddChild(new LeftDistributor(beta));
                    alpha.AddChild(new RightDistributor(beta));
                    lastNode = beta;
                }
            }

            var terminalAction = new Action<Token>(token => {
                var facts = token.ToDictionary();
                if (!facts.ContainsKey(target))
                {
                    // Target is unknown, solve it using the engine's solver
                    try
                    {
                        var root = _engine.Solve1DEquation(eq.Expression, target, facts);
                        if (!double.IsNaN(root))
                        {
                            var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                            var castedVal = _engine.CastToVariableType(root, variable);
                            
                            // Success! Assert the result back into the network
                            _network.AssertFact(target, castedVal);
                        }
                    }
                    catch { /* Solver failed or not enough data */ }
                }
            });

            var terminal = new TerminalNode($"EqSolve:{eq.Expression}->{target}", terminalAction);
            lastNode?.AddChild(terminal);
        }
    }

    private void CompileComputation(Concept concept, ComputationRelation rel)
    {
        if (rel.ResultVariable == null || rel.InputVariables == null || !rel.InputVariables.Any()) return;

        ReteNode? lastNode = null;
        for (int i = 0; i < rel.InputVariables.Count; i++)
        {
            var alpha = _network.GetOrCreateAlphaNode(rel.InputVariables[i]);
            if (i == 0) lastNode = alpha;
            else
            {
                var beta = new BetaNode();
                beta.LeftParent = lastNode;
                beta.RightParent = alpha;
                lastNode!.AddChild(new LeftDistributor(beta));
                alpha.AddChild(new RightDistributor(beta));
                lastNode = beta;
            }
        }

        var terminalAction = new Action<Token>(token => {
            var facts = token.ToDictionary();
            if (!facts.ContainsKey(rel.ResultVariable))
            {
                try
                {
                    var resValue = _engine.EvaluateFormula(rel.Expression, facts);
                    var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(rel.ResultVariable, StringComparison.OrdinalIgnoreCase));
                    var castedVal = _engine.CastToVariableType(resValue, variable);
                    _network.AssertFact(rel.ResultVariable, castedVal);
                }
                catch { }
            }
        });

        var terminal = new TerminalNode($"Comp:{rel.Expression}", terminalAction);
        lastNode?.AddChild(terminal);
    }

    private void CompileSameVariable(Concept concept, SameVariable sv)
    {
        // Direction 1: v1 -> v2
        var a1 = _network.GetOrCreateAlphaNode(sv.Variable1);
        var t1 = new TerminalNode($"SameVar:{sv.Variable1}->{sv.Variable2}", token => {
            var val = token.GetValue(sv.Variable1);
            if (val != null) _network.AssertFact(sv.Variable2, val);
        });
        a1.AddChild(t1);

        // Direction 2: v2 -> v1
        var a2 = _network.GetOrCreateAlphaNode(sv.Variable2);
        var t2 = new TerminalNode($"SameVar:{sv.Variable2}->{sv.Variable1}", token => {
            var val = token.GetValue(sv.Variable2);
            if (val != null) _network.AssertFact(sv.Variable1, val);
        });
        a2.AddChild(t2);
    }

    private void CompileConstraint(Concept concept, Constraint constraint)
    {
        var vars = _engine.ExtractVariablesFromExpression(constraint.Expression);
        if (vars == null || vars.Count == 0) return;

        // If it's a condition (all vars known), it could be a filter.
        // But COKB often treats constraints as equations if 1 var is missing.
        if (vars.Count >= 1)
        {
            foreach (var target in vars)
            {
                var inputs = vars.Where(v => v != target).ToList();
                ReteNode? lastNode = null;
                
                if (inputs.Count == 0)
                {
                    // Constant constraint? handle as entry check
                }
                else
                {
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        var alpha = _network.GetOrCreateAlphaNode(inputs[i]);
                        if (i == 0) lastNode = alpha;
                        else
                        {
                            var beta = new BetaNode();
                            beta.LeftParent = lastNode;
                            beta.RightParent = alpha;
                            lastNode!.AddChild(new LeftDistributor(beta));
                            alpha.AddChild(new RightDistributor(beta));
                            lastNode = beta;
                        }
                    }
                }

                if (lastNode == null) continue;

                var terminalAction = new Action<Token>(token => {
                    var facts = token.ToDictionary();
                    if (!facts.ContainsKey(target))
                    {
                        try
                        {
                            // Treat as equation root-finding
                            var root = _engine.Solve1DEquation(constraint.Expression, target, facts);
                            if (!double.IsNaN(root))
                            {
                                var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                                var castedVal = _engine.CastToVariableType(root, variable);
                                _network.AssertFact(target, castedVal);
                            }
                        }
                        catch { }
                    }
                });

                var terminal = new TerminalNode($"ConstraintSolve:{constraint.Expression}->{target}", terminalAction);
                lastNode.AddChild(terminal);
            }
        }
    }
}

// Helper nodes to bridge Alpha/Beta correctly
internal class LeftDistributor : ReteNode {
    private readonly BetaNode _beta;
    public LeftDistributor(BetaNode beta) => _beta = beta;
    public override void ReceiveToken(Token token, ReteNode? sender) => _beta.ReceiveLeft(token);
}

internal class RightDistributor : ReteNode {
    private readonly BetaNode _beta;
    public RightDistributor(BetaNode beta) => _beta = beta;
    public override void ReceiveToken(Token token, ReteNode? sender) => _beta.ReceiveRight(token);
}

internal class FilterNode : ReteNode {
    private readonly Func<Token, bool> _predicate;
    public FilterNode(Func<Token, bool> predicate) => _predicate = predicate;
    public override void ReceiveToken(Token token, ReteNode? sender) {
        if (_predicate(token)) Propagate(token);
    }
}
