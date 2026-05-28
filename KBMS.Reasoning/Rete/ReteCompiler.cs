using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        // (RC16) Compile ConstructRelations
        foreach (var cr in concept.ConstructRelations)
        {
            CompileConstructRelation(concept, cr);
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
        // Compile multi-concept and single-concept rules
        if (rule.IsMultiConcept && rule.ScopeConcepts.Count > 1)
        {
            CompileMultiConceptRule(concept, rule);
            return;
        }

        // Separate Alpha (single-variable) conditions from Beta/Filter (multi-variable) conditions
        var alphaConditions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var filterConditions = new List<string>();

        foreach (var cond in rule.Hypothesis)
        {
            var vars = _engine.ExtractVariablesFromExpression(cond);
            if (vars.Count == 1)
            {
                var v = vars.First();
                if (!alphaConditions.ContainsKey(v)) alphaConditions[v] = new List<string>();
                alphaConditions[v].Add(cond);
            }
            else
            {
                filterConditions.Add(cond);
            }
        }

        var neededVars = _engine.ExtractVariablesFromExpression(string.Join(" and ", rule.Hypothesis)).Distinct().ToList();
        var ruleName = !string.IsNullOrEmpty(rule.Name) ? rule.Name : (rule.Kind ?? "R" + (concept.ConceptRules.IndexOf(rule) + 1));

        ReteNode lastNode = _network.Root;

        if (neededVars.Any())
        {
            for (int i = 0; i < neededVars.Count; i++)
            {
                var varName = neededVars[i];
                Func<Token, bool>? alphaCondition = null;
                string? combinedCondExpr = null;
                
                if (alphaConditions.TryGetValue(varName, out var conds))
                {
                    combinedCondExpr = string.Join(" and ", conds);
                    alphaCondition = ExpressionCompiler.CompileCondition(combinedCondExpr, _engine);
                }

                var alpha = _network.GetOrCreateAlphaNode(varName, combinedCondExpr, alphaCondition);

                if (i == 0)
                {
                    lastNode = alpha;
                }
                else
                {
                    var beta = new BetaNode();
                    beta.LeftParent = lastNode;
                    beta.RightParent = alpha;

                    lastNode.AddChild(new LeftDistributor(beta));
                    alpha.AddChild(new RightDistributor(beta));

                    lastNode = beta;
                }
            }
        }

        // ruleName already declared above
        
        // Final condition check node (only multi-variable conditions)
        var combinedFilterExpr = string.Join(" and ", filterConditions);
        var compiledFilter = filterConditions.Count == 0 ? (token => true) : ExpressionCompiler.CompileCondition(combinedFilterExpr, _engine);

        var filterNode = new FilterNode(token => compiledFilter(token));

        lastNode!.AddChild(filterNode);

        var terminalAction = new Action<Token, InferenceSession>((token, session) => {
            // Build fact context for conclusion execution
            // Start with facts that triggered the rule (from Token)
            var facts = token.ToDictionary();

            // Supplement with ANY other facts currently in the network's working memory
            // This is essential if the conclusion uses variables not present in the hypothesis chain
            foreach (var f in session.WorkingMemory)
            {
                if (!facts.ContainsKey(f.Name)) facts[f.Name] = f.Value;
            }
            foreach (var kv in session.ExternalFacts)
            {
                if (!facts.ContainsKey(kv.Key)) facts[kv.Key] = kv.Value;
            }

            try
            {
                var res = new InferenceEngine.ReasoningResult();
                var outputFacts = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var concl in rule.Conclusion)
                {
                    _engine.ApplyConclusion(concl, concept, facts, res, ruleName);
                    foreach (var derived in res.DerivedFacts)
                    {
                        outputFacts[derived.Key] = derived.Value;
                        _network.AssertFact(derived.Key, derived.Value, session);
                        session.Logger?.Invoke($"Rule {ruleName} resolved {derived.Key}");
                    }
                }
                
                if (outputFacts.Count > 0)
                {
                    var step = new ReasoningStep
                    {
                        RuleName = ruleName,
                        StepCost = rule.Cost,
                        InputFacts = token.Facts.ToDictionary(f => f.Name, f => f.Value, StringComparer.OrdinalIgnoreCase),
                        OutputFacts = outputFacts,
                        Timestamp = DateTime.UtcNow,
                        Logic = $"IF {string.Join(" AND ", rule.Hypothesis)} THEN {string.Join(", ", rule.Conclusion)}",
                        UsedVariables = neededVars
                    };
                    token.AuditTrail.Add(step);
                    session.AuditTrail.Add(step);
                    token.GeneratedVariables.AddRange(outputFacts.Keys);
                    foreach (var k in outputFacts.Keys) session.GeneratedVariables.Add(k);
                }
            }
            catch (Exception ex)
            {
                session.Logger?.Invoke($"Rule {ruleName} failed: {ex.Message}");
                // If it's a math error we want to propagate, we might need a way to signal the engine
                throw; 
            }
        });
        var terminal = new TerminalNode(ruleName, terminalAction, rule.Cost, rule.Priority);
        filterNode.AddChild(terminal);
    }

    /// <summary>
    /// Compiles a multi-concept rule that spans multiple concepts with join conditions
    /// </summary>
    private void CompileMultiConceptRule(Concept concept, ConceptRule rule)
    {
        // For multi-concept rules, we need to:
        // 1. Extract variables from hypothesis with alias prefixes
        // 2. Build Alpha nodes for aliased variables
        // 3. Add join condition evaluation in the filter
        // 4. Execute conclusions with proper variable binding

        var ruleName = !string.IsNullOrEmpty(rule.Name) ? rule.Name : (rule.Kind ?? $"MultiRule_{rule.Id}");

        // Build alias map: alias -> concept name
        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sc in rule.ScopeConcepts)
        {
            var alias = sc.Alias ?? sc.ConceptName;
            aliasMap[alias] = sc.ConceptName;
        }

        // Separate Alpha (single-variable) conditions from Beta/Filter (multi-variable) conditions
        var alphaConditions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var filterConditions = new List<string>();

        foreach (var cond in rule.Hypothesis)
        {
            var vars = _engine.ExtractVariablesFromExpression(cond);
            if (vars.Count == 1)
            {
                var v = vars.First();
                if (!alphaConditions.ContainsKey(v)) alphaConditions[v] = new List<string>();
                alphaConditions[v].Add(cond);
            }
            else
            {
                filterConditions.Add(cond);
            }
        }

        // For multi-concept rules, needed vars are ALL scope concepts!
        var neededVars = rule.ScopeConcepts.Select(sc => sc.Alias ?? sc.ConceptName).Distinct().ToList();

        if (!neededVars.Any()) return;

        // Build the chain of Alpha/Beta nodes
        ReteNode? lastNode = null;
        var currentScopeVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remainingFilterConditions = new List<string>(filterConditions);
        var remainingJoinConditions = rule.JoinConditions != null ? new List<KBMS.Models.ConceptRuleJoinCondition>(rule.JoinConditions) : new List<KBMS.Models.ConceptRuleJoinCondition>();

        for (int i = 0; i < neededVars.Count; i++)
        {
            var varName = neededVars[i];
            currentScopeVars.Add(varName);

            Func<Token, bool>? alphaCondition = null;
            string? combinedCondExpr = null;
            
            if (alphaConditions.TryGetValue(varName, out var conds))
            {
                combinedCondExpr = string.Join(" and ", conds);
                alphaCondition = ExpressionCompiler.CompileCondition(combinedCondExpr, _engine);
            }

            var alpha = _network.GetOrCreateAlphaNode(varName, combinedCondExpr, alphaCondition);

            if (i == 0)
            {
                lastNode = alpha;
            }
            else
            {
                var beta = new BetaNode();
                beta.LeftParent = lastNode;
                beta.RightParent = alpha;

                // Push down applicable conditions to BetaNode
                var betaFilterConds = new List<string>();
                foreach (var fc in remainingFilterConditions.ToList())
                {
                    var varsInFc = _engine.ExtractVariablesFromExpression(fc);
                    if (varsInFc.All(v => currentScopeVars.Contains(v)))
                    {
                        betaFilterConds.Add(fc);
                        remainingFilterConditions.Remove(fc);
                    }
                }

                var betaJoinConds = new List<KBMS.Models.ConceptRuleJoinCondition>();
                foreach (var jc in remainingJoinConditions.ToList())
                {
                    string leftRoot = jc.LeftField.Split('.')[0];
                    string rightRoot = jc.RightField.Split('.')[0];
                    if (currentScopeVars.Contains(leftRoot) && currentScopeVars.Contains(rightRoot))
                    {
                        betaJoinConds.Add(jc);
                        remainingJoinConditions.Remove(jc);
                    }
                }

                if (betaFilterConds.Any() || betaJoinConds.Any())
                {
                    var betaFilterCompiled = betaFilterConds.Any() ? ExpressionCompiler.CompileCondition(string.Join(" and ", betaFilterConds), _engine) : (t => true);
                    beta.JoinConditionEvaluator = (left, right) => {
                        var allFacts = left.Facts.Concat(right.Facts).ToList();
                        var token = new Token(allFacts);
                        
                        if (!betaFilterCompiled(token)) return false;
                        var facts = token.ToDictionary();

                        foreach (var jc in betaJoinConds)
                        {
                            try
                            {
                                var leftVal = EvaluateFieldValue(jc.LeftField, facts);
                                var rightVal = EvaluateFieldValue(jc.RightField, facts);
                                if (leftVal == null || rightVal == null) return false;
                                
                                // Evaluate operator
                                if (!EvaluateJoinOperator(leftVal, jc.Operator, rightVal)) return false;
                            }
                            catch { return false; }
                        }
                        return true;
                    };
                }
                if (_engine.ExternalDataSource != null && betaJoinConds.Any())
                {
                    // To fetch Right side (varName) based on Left side (lastNode)
                    var rightJoinConds = betaJoinConds.Where(jc => jc.RightField.StartsWith(varName + ".")).ToList();
                    var leftJoinConds = betaJoinConds.Where(jc => jc.LeftField.StartsWith(varName + ".")).ToList();

                    // Swap them so the Right concept is always the RightField for the search
                    var normalizedConds = new List<KBMS.Models.ConceptRuleJoinCondition>();
                    foreach (var jc in rightJoinConds) normalizedConds.Add(jc);
                    foreach (var jc in leftJoinConds)
                    {
                        string revOp = jc.Operator;
                        if (revOp == ">") revOp = "<";
                        else if (revOp == "<") revOp = ">";
                        else if (revOp == ">=") revOp = "<=";
                        else if (revOp == "<=") revOp = ">=";
                        
                        normalizedConds.Add(new KBMS.Models.ConceptRuleJoinCondition { LeftField = jc.RightField, Operator = revOp, RightField = jc.LeftField });
                    }

                    var rightConceptName = rule.ScopeConcepts?.FirstOrDefault(sc => (sc.Alias ?? sc.ConceptName).Equals(varName, StringComparison.OrdinalIgnoreCase))?.ConceptName ?? varName;

                    beta.RightDataSource = leftToken => {
                        var factsList = _engine.ExternalDataSource(rightConceptName, normalizedConds, leftToken);
                        if (factsList == null) return Enumerable.Empty<Token>();

                        return factsList.Select(f => {
                            var facts = f.Select(kv => new Fact($"{varName}.{kv.Key}", kv.Value)).ToList();
                            var rightToken = new Token(facts);
                            if (alphaCondition != null && !alphaCondition(rightToken)) return null;
                            return rightToken;
                        }).Where(t => t != null)!;
                    };

                    // For the Left side (lastNode), we also need a LeftDataSource to fetch Left when Right is inserted.
                    // This is harder since Left could be a complex tree. For our simple multi-concept rule (A JOIN B), 
                    // lastNode is an AlphaNode for `leftVarName`.
                    if (i == 1) // Only support simple 2-concept join for LeftDataSource
                    {
                        var leftVarName = neededVars[0]; // the previous concept alias
                        var leftConceptName = rule.ScopeConcepts?.FirstOrDefault(sc => (sc.Alias ?? sc.ConceptName).Equals(leftVarName, StringComparison.OrdinalIgnoreCase))?.ConceptName ?? leftVarName;
                        Func<Token, bool>? leftAlphaCond = null;
                        if (alphaConditions.TryGetValue(leftVarName, out var leftConds))
                        {
                            leftAlphaCond = ExpressionCompiler.CompileCondition(string.Join(" and ", leftConds), _engine);
                        }

                        // normalizedConds is already Left = RightField, Operator, Right = LeftField
                        var leftNormalizedConds = new List<KBMS.Models.ConceptRuleJoinCondition>();
                        foreach (var jc in rightJoinConds)
                        {
                            string revOp = jc.Operator;
                            if (revOp == ">") revOp = "<";
                            else if (revOp == "<") revOp = ">";
                            else if (revOp == ">=") revOp = "<=";
                            else if (revOp == "<=") revOp = ">=";
                            
                            leftNormalizedConds.Add(new KBMS.Models.ConceptRuleJoinCondition { LeftField = jc.RightField, Operator = revOp, RightField = jc.LeftField });
                        }
                        foreach (var jc in leftJoinConds) leftNormalizedConds.Add(jc);

                        beta.LeftDataSource = rightToken => {
                            var factsList = _engine.ExternalDataSource(leftConceptName, leftNormalizedConds, rightToken);
                            if (factsList == null) return Enumerable.Empty<Token>();

                            return factsList.Select(f => {
                                var facts = f.Select(kv => new Fact($"{leftVarName}.{kv.Key}", kv.Value)).ToList();
                                var leftTokenData = new Token(facts);
                                if (leftAlphaCond != null && !leftAlphaCond(leftTokenData)) return null;
                                return leftTokenData;
                            }).Where(t => t != null)!;
                        };
                    }
                }

                lastNode!.AddChild(new LeftDistributor(beta));
                alpha.AddChild(new RightDistributor(beta));

                lastNode = beta;
            }
        }

        // Filter node: checks hypothesis AND join conditions
        var compiledRemainingFilter = remainingFilterConditions.Any() ? ExpressionCompiler.CompileCondition(string.Join(" and ", remainingFilterConditions), _engine) : (t => true);

        var filterNode = new FilterNode(token => {
            var facts = token.ToDictionary();

            // Check remaining multi-variable hypothesis conditions
            if (!compiledRemainingFilter(token))
                return false;

            // Check join conditions (if any)
            foreach (var jc in remainingJoinConditions)
            {
                try
                {
                    var leftVal = EvaluateFieldValue(jc.LeftField, facts);
                    var rightVal = EvaluateFieldValue(jc.RightField, facts);

                    if (leftVal == null || rightVal == null) return false;

                    // Evaluate operator
                    if (!EvaluateJoinOperator(leftVal, jc.Operator, rightVal)) return false;
                }
                catch
                {
                    return false;
                }
            }

            return true;
        });

        lastNode!.AddChild(filterNode);

        // Terminal node: execute conclusions
        var terminalAction = new Action<Token, InferenceSession>((token, session) => {
            var facts = token.ToDictionary();

            // Supplement with working memory
            foreach (var f in session.WorkingMemory)
            {
                if (!facts.ContainsKey(f.Name)) facts[f.Name] = f.Value;
            }

            var res = new InferenceEngine.ReasoningResult();
            var outputFacts = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var concl in rule.Conclusion)
            {
                _engine.ApplyConclusion(concl, concept, facts, res, ruleName);
                foreach (var derived in res.DerivedFacts)
                {
                    outputFacts[derived.Key] = derived.Value;
                    _network.AssertFact(derived.Key, derived.Value, session);
                    session.Logger?.Invoke($"Multi-Concept Rule {ruleName} resolved {derived.Key}");
                }
            }
            
            if (outputFacts.Count > 0)
            {
                // Also assert internal IDs so the engine can track which external objects were used
                foreach (var kvp in facts)
                {
                    if (kvp.Key.EndsWith("__internal_id", StringComparison.OrdinalIgnoreCase) || 
                        kvp.Key.EndsWith("__internal_concept", StringComparison.OrdinalIgnoreCase))
                    {
                        _network.AssertFact(kvp.Key, kvp.Value, session);
                    }
                }

                Console.WriteLine($"[DEBUG] Rule {ruleName} fired! Derived facts: {string.Join(", ", outputFacts.Select(kv => kv.Key + "=" + kv.Value))}");
                
                var step = new ReasoningStep
                {
                    RuleName = ruleName,
                    StepCost = rule.Cost,
                    InputFacts = token.Facts.ToDictionary(f => f.Name, f => f.Value, StringComparer.OrdinalIgnoreCase),
                    OutputFacts = outputFacts,
                    Timestamp = DateTime.UtcNow,
                    Logic = $"IF {string.Join(" AND ", rule.Hypothesis)} THEN {string.Join(", ", rule.Conclusion)}",
                    UsedVariables = rule.Hypothesis.SelectMany(h => _engine.ExtractVariablesFromExpression(h)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                };
                
                token.AuditTrail.Add(step);
                session.AuditTrail.Add(step);
                
                token.GeneratedVariables.AddRange(outputFacts.Keys);
                foreach (var k in outputFacts.Keys)
                {
                    session.GeneratedVariables.Add(k);
                }
            }
        });

        var terminal = new TerminalNode(ruleName, terminalAction, rule.Cost, rule.Priority);
        filterNode.AddChild(terminal);
    }

    /// <summary>
    /// Evaluates a field value, supporting dot notation (e.g., "p.age")
    /// </summary>
    private object? EvaluateFieldValue(string field, Dictionary<string, object> facts)
    {
        // Direct field lookup
        if (facts.TryGetValue(field, out var directVal))
            return directVal;

        // Try without prefix (for backward compatibility)
        var lastDot = field.LastIndexOf('.');
        if (lastDot > 0)
        {
            var shortName = field.Substring(lastDot + 1);
            if (facts.TryGetValue(shortName, out var shortVal))
                return shortVal;
        }

        return null;
    }

    private bool ValuesEqual(object? v1, object? v2)
    {
        if (v1 == null && v2 == null) return true;
        if (v1 == null || v2 == null) return false;

        // Handle numeric equality
        if (IsNumeric(v1) && IsNumeric(v2))
        {
            try
            {
                return Math.Abs(Convert.ToDouble(v1) - Convert.ToDouble(v2)) < 1e-9;
            }
            catch { return false; }
        }

        return v1.Equals(v2) || v1.ToString() == v2.ToString();
    }

    private static bool EvaluateJoinOperator(object? leftVal, string op, object? rightVal)
    {
        if (leftVal == null || rightVal == null) return false;
        
        // If they are both numeric
        if (double.TryParse(leftVal.ToString(), out double lNum) && double.TryParse(rightVal.ToString(), out double rNum))
        {
            switch (op)
            {
                case "=": return Math.Abs(lNum - rNum) < 0.00001;
                case "!=": return Math.Abs(lNum - rNum) >= 0.00001;
                case ">": return lNum > rNum;
                case "<": return lNum < rNum;
                case ">=": return lNum >= rNum;
                case "<=": return lNum <= rNum;
            }
        }
        
        // String comparison
        int cmp = string.Compare(leftVal.ToString(), rightVal.ToString(), StringComparison.OrdinalIgnoreCase);
        switch (op)
        {
            case "=": return cmp == 0;
            case "!=": return cmp != 0;
            case ">": return cmp > 0;
            case "<": return cmp < 0;
            case ">=": return cmp >= 0;
            case "<=": return cmp <= 0;
        }
        
        return false;
    }

    private bool IsNumeric(object v) => v is int or long or double or decimal or float;

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

            var terminalAction = new Action<Token, InferenceSession>((token, session) => {
                var facts = token.ToDictionary();
                // Supplement with working memory and external facts
                foreach (var f in session.WorkingMemory)
                    if (!facts.ContainsKey(f.Name)) facts[f.Name] = f.Value;
                foreach (var kv in session.ExternalFacts)
                    if (!facts.ContainsKey(kv.Key)) facts[kv.Key] = kv.Value;

                // Target might be known, but if this equation implies a DIFFERENT value, we should update it
                try
                {
                    bool isKnown = session.WorkingMemory.Any(f => f.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                    object? existingVal = isKnown ? session.WorkingMemory.First(f => f.Name.Equals(target, StringComparison.OrdinalIgnoreCase)).Value : null;

                    if (isKnown)
                    {
                        var s = _engine.GetType().GetMethod("SplitEquation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(_engine, new object[] { eq.Expression }) as dynamic;
                        if (s != null)
                        {
                            string leftExpr = s.left;
                            string rightExpr = s.right;
                            try {
                                var leftResult = _engine.EvaluateFormula(leftExpr, facts);
                                var rightResult = _engine.EvaluateFormula(rightExpr, facts);
                                if (_engine.ValuesEqual(leftResult, rightResult))
                                {
                                    return; // Equation already satisfied, no need to solve and risk finding a spurious root
                                }
                            } catch { }
                        }
                    }

                    var root = _engine.Solve1DEquation(eq.Expression, target, facts);
                    if (!double.IsNaN(root))
                    {
                        var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                        var castedVal = _engine.CastToVariableType(root, variable);
                        
                        // Check if it's already known and matches
                        bool isKnownTarget = session.WorkingMemory.Any(f => f.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                        object? existingTargetVal = isKnownTarget ? session.WorkingMemory.First(f => f.Name.Equals(target, StringComparison.OrdinalIgnoreCase)).Value : null;

                        if (!isKnownTarget || !_engine.ValuesEqual(existingTargetVal, castedVal))
                        {
                            // Let's explicitly remove the old fact if it exists
                            if (isKnownTarget)
                            {
                                var oldFact = session.WorkingMemory.First(f => f.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                                _network.RetractFact(oldFact.Name, session);
                            }
                            
                            _network.AssertFact(target, castedVal, session);
                        }
                    }
                }
                catch { /* Solver failed or not enough data */ }
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

        var terminalAction = new Action<Token, InferenceSession>((token, session) => {
            var facts = token.ToDictionary();
            bool isKnownGlobally = session.WorkingMemory.Any(f => f.Name.Equals(rel.ResultVariable, StringComparison.OrdinalIgnoreCase));
            if (!facts.ContainsKey(rel.ResultVariable) && !isKnownGlobally)
            {
                try
                {
                    var resValue = _engine.EvaluateFormula(rel.Expression, facts);
                    var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(rel.ResultVariable, StringComparison.OrdinalIgnoreCase));
                    var castedVal = _engine.CastToVariableType(resValue, variable);
                    _network.AssertFact(rel.ResultVariable, castedVal, session);
                }
                catch { }
            }
        });

        var terminal = new TerminalNode($"Comp:{rel.Expression}", terminalAction);
        lastNode?.AddChild(terminal);
    }

    private void CompileConstructRelation(Concept concept, ConstructRelation cr)
    {
        var rel = _engine.RelationResolver?.Invoke(cr.RelationName);
        if (rel == null) return;

        foreach (var eq in rel.Equations)
        {
            // Map relation equation variables to concept context
            // e.g. total.r = c1.r + c2.r
            // If Arguments = [r1, r2, this], then c1->r1, c2->r2, total->this
            var mappedExpr = eq.Expression;
            for (int i = 0; i < rel.ParamNames.Count && i < cr.Arguments.Count; i++)
            {
                var param = rel.ParamNames[i];
                var arg = cr.Arguments[i];
                
                if (arg.Equals("this", StringComparison.OrdinalIgnoreCase))
                {
                    // total.r -> r (if it's a direct property)
                    // Wait! The test has total_r mapped to this.r.
                    // If we map total.r to r, it might not match total_r.
                    // But SameVariables will bridge it!
                    mappedExpr = Regex.Replace(mappedExpr, @"\b" + Regex.Escape(param) + @"\.", "this.");
                }
                else
                {
                    // c1.r -> r1.r
                    mappedExpr = Regex.Replace(mappedExpr, @"\b" + Regex.Escape(param) + @"\.", arg + ".");
                }
            }

            // Now we have a mapped equation, e.g., this.r = r1.r + r2.r
            // Compile it as a normal equation in this concept
            CompileEquation(concept, new Equation { Expression = mappedExpr });
        }
    }

    private void CompileSameVariable(Concept concept, SameVariable sv)
    {
        // Direction 1: v1 -> v2
        var a1 = _network.GetOrCreateAlphaNode(sv.Variable1);
        var t1 = new TerminalNode($"SameVar:{sv.Variable1}->{sv.Variable2}", (token, session) => {
            var val = token.GetValue(sv.Variable1);
            if (val != null) _network.AssertFact(sv.Variable2, val, session);
        });
        a1.AddChild(t1);

        // Direction 2: v2 -> v1
        var a2 = _network.GetOrCreateAlphaNode(sv.Variable2);
        var t2 = new TerminalNode($"SameVar:{sv.Variable2}->{sv.Variable1}", (token, session) => {
            var val = token.GetValue(sv.Variable2);
            if (val != null) _network.AssertFact(sv.Variable1, val, session);
        });
        a2.AddChild(t2);
    }

    private void CompileConstraint(Concept concept, Constraint constraint)
    {
        var vars = _engine.ExtractVariablesFromExpression(constraint.Expression);
        if (vars == null || vars.Count == 0) return;

        bool isEquation = constraint.Expression.Contains("=") && 
                         !constraint.Expression.Contains(">") && 
                         !constraint.Expression.Contains("<") && 
                         !constraint.Expression.Contains("!");

        if (isEquation)
        {
            // Treat as equation root-finding
            foreach (var target in vars)
            {
                var inputs = vars.Where(v => v != target).ToList();
                ReteNode? lastNode = null;
                
                if (inputs.Count > 0)
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

                var terminalAction = new Action<Token, InferenceSession>((token, session) => {
                    var facts = token.ToDictionary();
                    if (!facts.ContainsKey(target))
                    {
                        try
                        {
                            var root = _engine.Solve1DEquation(constraint.Expression, target, facts);
                            if (!double.IsNaN(root))
                            {
                                var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                                var castedVal = _engine.CastToVariableType(root, variable);
                                _network.AssertFact(target, castedVal, session);
                            }
                        }
                        catch { }
                    }
                });

                var terminal = new TerminalNode($"ConstraintSolve:{constraint.Expression}->{target}", terminalAction);
                lastNode.AddChild(terminal);
            }
        }
        else
        {
            // Inequality constraint: treat as a FilterNode validation
            ReteNode? lastNode = null;
            for (int i = 0; i < vars.Count; i++)
            {
                var alpha = _network.GetOrCreateAlphaNode(vars[i]);
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

            if (lastNode != null)
            {
                var filter = new FilterNode(token => {
                    var facts = token.ToDictionary();
                    try {
                        return _engine.EvaluateConstraint(constraint.Expression, facts);
                    } catch { return true; } // Ignore errors in validation during propagation
                });
                lastNode.AddChild(filter);
                
                // Add a terminal node just to log/trace the constraint check
                var terminal = new TerminalNode($"ConstraintCheck:{constraint.Expression}", (token, session) => {
                    session.Logger?.Invoke($"Constraint satisfied: {constraint.Expression}");
                });
                filter.AddChild(terminal);
            }
        }
    }
}

// Helper nodes to bridge Alpha/Beta correctly
internal class LeftDistributor : ReteNode {
    private readonly BetaNode _beta;
    public LeftDistributor(BetaNode beta) => _beta = beta;
    public override void ReceiveToken(Token token, ReteNode? sender, InferenceSession session) => _beta.ReceiveLeft(token, session);
    public override void RetractFact(Fact fact, ReteNode? sender, InferenceSession session) => _beta.RetractFact(fact, this, session);
}

internal class RightDistributor : ReteNode {
    private readonly BetaNode _beta;
    public RightDistributor(BetaNode beta) => _beta = beta;
    public override void ReceiveToken(Token token, ReteNode? sender, InferenceSession session) => _beta.ReceiveRight(token, session);
    public override void RetractFact(Fact fact, ReteNode? sender, InferenceSession session) => _beta.RetractFact(fact, this, session);
}

internal class FilterNode : ReteNode {
    private readonly Func<Token, bool> _predicate;
    public FilterNode(Func<Token, bool> predicate) => _predicate = predicate;
    public override void ReceiveToken(Token token, ReteNode? sender, InferenceSession session) {
        if (_predicate(token)) Propagate(token, session);
    }
    public override void RetractFact(Fact fact, ReteNode? sender, InferenceSession session) => PropagateRetract(fact, session);
}
