using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using KBMS.Models;
using NCalc; 
using System.Text.RegularExpressions;
using KBMS.Reasoning.Rete;

namespace KBMS.Reasoning;

public class InferenceEngine
{
    private readonly ConcurrentDictionary<string, NCalc.Expression> _expressionCache = new();
    
    // Cache compiled Rete networks keyed by concept state signature
    private readonly ConcurrentDictionary<string, ReteNetwork> _networkCache = new(StringComparer.OrdinalIgnoreCase);
    
    private static readonly Regex _powRegex = new(@"(\([^()]+\)|[a-zA-Z0-9_\.\[\]]+)\^([a-zA-Z0-9_\.\[\]]+)", RegexOptions.Compiled);
    private static readonly Regex _dotRegex = new(@"\b([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)\b", RegexOptions.Compiled);
    private static readonly Regex _eqRegex = new(@"(?<![><!= ])=(?!=)", RegexOptions.Compiled);

    public class DerivationTrace
    {
        public string TargetVariable { get; set; } = "";
        public object? Value { get; set; }
        public string Mechanism { get; set; } = ""; 
        public string Source { get; set; } = "";    
        public Dictionary<string, object> Inputs { get; set; } = new();
    }

    public class ReasoningResult
    {
        public bool Success { get; set; } = true;
        public Dictionary<string, object> DerivedFacts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Steps { get; set; } = new();
        public List<DerivationTrace> Traces { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public IReadOnlyList<KBMS.Reasoning.Rete.Fact> WorkingMemory { get; set; } = new List<KBMS.Reasoning.Rete.Fact>();
        
        /// <summary>
        /// Variables that were needed for inference but were missing.
        /// </summary>
        public HashSet<string> MissingFacts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public List<KBMS.Reasoning.Rete.ReasoningStep> AuditTrail { get; set; } = new();
        public HashSet<string> GeneratedVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public Func<string, Concept?>? ConceptResolver { get; set; }
    public Func<string, Function?>? FunctionResolver { get; set; }
    public Func<string, Operator?>? OperatorResolver { get; set; }
    public Func<string, List<string>>? HierarchyResolver { get; set; }
    public Func<string, List<string>>? PartOfResolver { get; set; }
    public Func<string, Relation?>? RelationResolver { get; set; }
    
    /// <summary>
    /// External Data Source hook for Lazy Loading in Rete Network.
    /// Parameters: conceptName, joinConditions, leftToken
    /// Returns: A list of matching facts (as Dictionary) from external storage.
    /// </summary>
    public Func<string, List<KBMS.Models.ConceptRuleJoinCondition>, Token, IEnumerable<Dictionary<string, object>>>? ExternalDataSource { get; set; }

    /// <summary>
    /// Invalidates the Rete network cache for the given concept. Call after rules are updated.
    /// </summary>
    public void InvalidateCache(string conceptName) => _networkCache.TryRemove(conceptName, out _);
    public void ClearCache() => _networkCache.Clear();

    public ReasoningResult FindClosure(Concept concept, Dictionary<string, object> initialFacts, List<string> targetVariables)
    {
        var result = new ReasoningResult();
        var knownFacts = new Dictionary<string, object>(initialFacts);
        int stepCount = 0;

        var startTime = DateTime.UtcNow;
        var timeoutMs = 5000;

        result.Steps.Add($"Step {stepCount++}: Initializing reasoning for '{concept.Name}'");

        var resolvedConcept = ConceptResolver?.Invoke(concept.Name) ?? concept;
        var effectiveConcept = GetEffectiveConcept(resolvedConcept);

        // --- Cache key: concept name + actual content of rules/equations/relations ---
        // Using counts alone is insufficient: changing an equation expression while keeping
        // the same count would cause a stale cache hit with the wrong compiled network.
        var eqSignature  = string.Join(";", effectiveConcept.Equations.Select(e => e.Expression ?? "").OrderBy(s => s));
        var ruleSignature = string.Join(";", effectiveConcept.ConceptRules.Select(r => $"{r.Kind}:{string.Join(",", r.Hypothesis)}:{string.Join(",", r.Conclusion)}").OrderBy(s => s));
        var relSignature  = string.Join(";", effectiveConcept.ConstructRelations.Select(r => $"{r.RelationName}({string.Join(",", r.Arguments)})").OrderBy(s => s));
        var cacheKey = $"{effectiveConcept.Name}|eqs={eqSignature}|rules={ruleSignature}|rels={relSignature}";

        // Get or compile the network (compiled once, reused across calls)
        var network = _networkCache.GetOrAdd(cacheKey, _ =>
        {
            var newNetwork = new ReteNetwork();
            newNetwork.ContextConcept = effectiveConcept;

            if (RelationResolver != null)
            {
                foreach (var cr in effectiveConcept.ConstructRelations)
                {
                    var relation = RelationResolver(cr.RelationName);
                    if (relation != null)
                    {
                        var paramToArg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < relation.ParamNames.Count && i < cr.Arguments.Count; i++)
                        {
                            paramToArg[relation.ParamNames[i]] = cr.Arguments[i];
                        }

                        foreach (var eq in relation.Equations)
                        {
                            string mappedExpr = eq.Expression;
                            foreach (var kvp in paramToArg)
                                mappedExpr = Regex.Replace(mappedExpr, $@"\b{Regex.Escape(kvp.Key)}\b", kvp.Value);
                            effectiveConcept.Equations.Add(new Equation { Expression = mappedExpr });
                        }

                        foreach (var rule in relation.Rules)
                        {
                            var newRule = new ConceptRule { Id = Guid.NewGuid(), Kind = rule.Kind, Scope = rule.Scope, Priority = rule.Priority };
                            foreach (var h in rule.Hypothesis)
                            {
                                string mappedExpr = h;
                                foreach (var kvp in paramToArg)
                                    mappedExpr = Regex.Replace(mappedExpr, $@"\b{Regex.Escape(kvp.Key)}\b", kvp.Value);
                                newRule.Hypothesis.Add(mappedExpr);
                            }
                            foreach (var c in rule.Conclusion)
                            {
                                string mappedExpr = c;
                                foreach (var kvp in paramToArg)
                                    mappedExpr = Regex.Replace(mappedExpr, $@"\b{Regex.Escape(kvp.Key)}\b", kvp.Value);
                                newRule.Conclusion.Add(mappedExpr);
                            }
                            if (rule.IsMultiConcept)
                            {
                                foreach (var sc in rule.ScopeConcepts)
                                {
                                    var newSc = new ConceptRuleScopeConcept { ConceptName = sc.ConceptName, Alias = sc.Alias, Position = sc.Position };
                                    if (!string.IsNullOrEmpty(newSc.Alias) && paramToArg.TryGetValue(newSc.Alias, out var mappedAlias))
                                        newSc.Alias = mappedAlias;
                                    newRule.ScopeConcepts.Add(newSc);
                                }
                                foreach (var jc in rule.JoinConditions)
                                {
                                    var newJc = new ConceptRuleJoinCondition { Operator = jc.Operator, LeftField = jc.LeftField, RightField = jc.RightField };
                                    foreach (var kvp in paramToArg)
                                    {
                                        newJc.LeftField = Regex.Replace(newJc.LeftField, $@"\b{Regex.Escape(kvp.Key)}\b", kvp.Value);
                                        newJc.RightField = Regex.Replace(newJc.RightField, $@"\b{Regex.Escape(kvp.Key)}\b", kvp.Value);
                                    }
                                    newRule.JoinConditions.Add(newJc);
                                }
                            }
                            effectiveConcept.ConceptRules.Add(newRule);
                        }
                    }
                }
            }

            var compiler = new ReteCompiler(this, newNetwork);
            compiler.Compile(effectiveConcept);
            return newNetwork;
        });

        // Create a fresh session for this individual reasoning run
        var session = new InferenceSession();
        session.Logger = (msg) => {
            if (msg.StartsWith("Rule ")) result.Steps.Add(msg);
            else result.Steps.Add($"[Rete] {msg}");
        };

        foreach (var fact in knownFacts)
        {
            session.ExternalFacts[fact.Key] = fact.Value;
        }

        int iteration = 0;
        var visited = new HashSet<string>();
        try
        {
            while (iteration < 2000)
            {
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Reasoning timeout after {timeoutMs}ms (iteration {iteration})";
                    return result;
                }

                bool factAddedThisTurn = false;
                var stateKey = string.Join("|", knownFacts.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}"));
                if (iteration > 0 && visited.Contains(stateKey)) {
                    // If all target variables are already satisfied, we reached stability and can stop.
                    if (targetVariables.Count > 0 && targetVariables.All(v => knownFacts.ContainsKey(v))) break;
                    
                    // Otherwise, it's an unresolved circular loop
                    result.Success = false;
                    result.ErrorMessage = $"Circular dependency or reasoning loop detected without progress. StateKey: {stateKey}";
                    return result;
                }
                visited.Add(stateKey);

                // Collect all aliases that refer to this concept from its rules
                var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var r in effectiveConcept.ConceptRules) {
                    if (r.ScopeConcepts != null && r.ScopeConcepts.Any()) {
                        foreach (var sc in r.ScopeConcepts) {
                            if (sc != null && !string.IsNullOrEmpty(sc.ConceptName) && sc.ConceptName.Equals(effectiveConcept.Name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(sc.Alias))
                                aliases.Add(sc.Alias!);
                        }
                    }
                    else {
                        // Fallback: extract aliases from hypothesis/conclusion patterns like "p.variable"
                        var allExprs = r.Hypothesis.Concat(r.Conclusion);
                        foreach (var expr in allExprs) {
                            var matches = Regex.Matches(expr, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\.[a-zA-Z_][a-zA-Z0-9_]*\b");
                            foreach (Match m in matches) {
                                var prefix = m.Groups[1].Value;
                                // If prefix is NOT a known concept name, and NOT a variable name, it's likely an alias for the current concept
                                if (!prefix.Equals(effectiveConcept.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Check if prefix is a sub-object (variable)
                                    bool isVariable = effectiveConcept.Variables.Any(v => v.Name.Equals(prefix, StringComparison.OrdinalIgnoreCase) || v.Name.StartsWith(prefix + "."));
                                    if (!isVariable)
                                        aliases.Add(prefix);
                                }
                            }
                        }
                    }
                }
                
                // Add the concept name itself as an alias
                if (!aliases.Contains(effectiveConcept.Name, StringComparer.OrdinalIgnoreCase))
                    aliases.Add(effectiveConcept.Name);

                foreach (var fact in knownFacts.ToList())
                {
                    // Skip asserting facts that are target goals (or their aliased versions), as we want to re-derive them
                    string rawName = fact.Key;
                    var di = fact.Key.IndexOf('.');
                    if (di > 0)
                    {
                        var prefix = fact.Key.Substring(0, di);
                        if (aliases.Contains(prefix, StringComparer.OrdinalIgnoreCase))
                            rawName = fact.Key.Substring(di + 1);
                    }

                    if (targetVariables.Contains(rawName, StringComparer.OrdinalIgnoreCase)) continue;
                    if (fact.Value is Dictionary<string, object>) continue; // Skip expanding object assertions

                    // Assert the raw fact
                    network.AssertFact(fact.Key, fact.Value, session);
                    
                    // If fact is not already prefixed, assert it with all relevant aliases
                    if (!fact.Key.Contains('.'))
                    {
                        foreach (var alias in aliases)
                        {
                            var aliasedKey = $"{alias}.{fact.Key}";
                            if (!knownFacts.ContainsKey(aliasedKey))
                            {
                                network.AssertFact(aliasedKey, fact.Value, session);
                            }
                        }
                    }
                    else
                    {
                        // If fact IS prefixed (e.g., "Patient.sys"), and prefix is one of our aliases,
                        // also assert the raw version if not present.
                        var dotIdx = fact.Key.IndexOf('.');
                        var prefix = fact.Key.Substring(0, dotIdx);
                        var rawKey = fact.Key.Substring(dotIdx + 1);
                        
                        if (aliases.Contains(prefix, StringComparer.OrdinalIgnoreCase))
                        {
                            if (!knownFacts.ContainsKey(rawKey))
                            {
                                network.AssertFact(rawKey, fact.Value, session);
                            }
                            
                            // Also assert with other aliases
                            foreach (var otherAlias in aliases)
                            {
                                if (otherAlias.Equals(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                                var otherAliasedKey = $"{otherAlias}.{rawKey}";
                                if (!knownFacts.ContainsKey(otherAliasedKey))
                                {
                                    network.AssertFact(otherAliasedKey, fact.Value, session);
                                }
                            }
                        }
                    }
                }

                // For multi-concept rules, AlphaNodes expect a full object assertion under the alias
                foreach (var alias in aliases)
                {
                    var aliasPrefix = alias + ".";
                    var conceptDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var f in session.WorkingMemory)
                    {
                        if (f.Name.StartsWith(aliasPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var key = f.Name.Substring(aliasPrefix.Length);
                            conceptDict[key] = f.Value;
                        }
                    }
                    if (conceptDict.Count > 0)
                    {
                        network.AssertFact(alias, conceptDict, session);
                    }
                }

                int countBefore = session.WorkingMemory.Count;
                int maxFireLimit = 5000;
                int fireCount = 0;
                try
                {
                    while (network.FireNext(session)) {
                        fireCount++;
                        if (fireCount > maxFireLimit) throw new Exception("Rete network propagation exceeded maximum iterations (infinite loop detected).");
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    return result;
                }
                if (session.WorkingMemory.Count > countBefore) factAddedThisTurn = true;

                foreach (var fact in session.WorkingMemory.ToList())
                {
                    // Check if fact name has an alias we should strip
                    string rawName = fact.Name;
                    var dotIdx = fact.Name.IndexOf('.');
                    if (dotIdx > 0)
                    {
                        var prefix = fact.Name.Substring(0, dotIdx);
                        if (aliases.Contains(prefix, StringComparer.OrdinalIgnoreCase))
                        {
                            rawName = fact.Name.Substring(dotIdx + 1);
                        }
                    }

                    void UpdateFact(string name, object value) {
                        if (value is Dictionary<string, object>) return;
                        
                        bool isNew = !knownFacts.ContainsKey(name);
                        bool isDifferent = !isNew && !ValuesEqual(knownFacts[name], value);

                        if (isNew || isDifferent)
                        {
                            var variable = effectiveConcept.Variables.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                            var castedVal = CastToVariableType(value, variable);

                            knownFacts[name] = castedVal;
                            session.ExternalFacts[name] = castedVal;
                            result.DerivedFacts[name] = castedVal;
                            factAddedThisTurn = true;
                            
                            if (isDifferent) result.Steps.Add($"Step {stepCount++}: Updated [{name}] = {castedVal}");
                            else result.Steps.Add($"Step {stepCount++}: Derived [{name}] = {castedVal}");
                        }
                    }

                    UpdateFact(fact.Name, fact.Value);
                    if (rawName != fact.Name) UpdateFact(rawName, fact.Value);
                }

                if (ConceptResolver != null)
                {
                    foreach (var variable in effectiveConcept.Variables.ToList())
                    {
                        if (IsConceptType(variable.Type))
                        {
                            var subConcept = ConceptResolver(variable.Type);
                            if (subConcept != null)
                            {
                                var subFacts = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                var prefix = variable.Name + ".";
                                var conceptPrefix = subConcept.Name + ".";
                                foreach (var fact in knownFacts.ToList())
                                {
                                    if (fact.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var stripped = fact.Key.Substring(prefix.Length);
                                        // Also strip inner concept-name prefix e.g. "Resistor.status" -> "status"
                                        if (stripped.StartsWith(conceptPrefix, StringComparison.OrdinalIgnoreCase))
                                            stripped = stripped.Substring(conceptPrefix.Length);
                                        subFacts[stripped] = fact.Value;
                                    }
                                }

                                if (subFacts.Count > 0)
                                {
                                    var subResult = FindClosure(subConcept, subFacts, new List<string>());
                                    foreach(var step in subResult.Steps) result.Steps.Add($"  [{variable.Name}] {step}");

                                    foreach (var derived in subResult.DerivedFacts)
                                    {
                                        var fullKey = prefix + derived.Key;
                                        if (!knownFacts.ContainsKey(fullKey) || !ValuesEqual(knownFacts[fullKey], derived.Value))
                                        {
                                            knownFacts[fullKey] = derived.Value;
                                            session.ExternalFacts[fullKey] = derived.Value;
                                            result.DerivedFacts[fullKey] = derived.Value;
                                            network.AssertFact(fullKey, derived.Value, session);
                                            factAddedThisTurn = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!factAddedThisTurn) break;
                iteration++;
            }
        }
        catch (Exception ex)
        {
            result.Steps.Add($"[FATAL-ERROR] {ex.Message}");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }

        if (targetVariables.Count > 0 && !targetVariables.All(v => knownFacts.ContainsKey(v)))
        {
            var missing = targetVariables.Where(v => !knownFacts.ContainsKey(v)).ToList();
            foreach (var goal in missing)
            {
                try {
                    ResolveGoal(goal, effectiveConcept, knownFacts, new HashSet<string>(StringComparer.OrdinalIgnoreCase), result);
                } catch { }
            }
        }

        foreach (var constraint in effectiveConcept.Constraints)
        {
            try
            {
                var needed = ExtractVariablesFromExpression(constraint.Expression);
                if (needed.All(v => knownFacts.ContainsKey(v)))
                {
                    if (!EvaluateConstraint(constraint.Expression, knownFacts))
                    {
                        result.Success = false;
                        var meta = "";
                        if (!string.IsNullOrEmpty(constraint.Name)) meta += $"{constraint.Name} ";
                        if (constraint.Line > 0) meta += $"(line {constraint.Line}, col {constraint.Column}) ";
                        result.ErrorMessage = $"Constraint violated: {meta}{constraint.Expression}";
                        return result;
                    }
                }
            }
            catch { }
        }

        if (targetVariables.Count > 0 && !targetVariables.All(v => knownFacts.ContainsKey(v)))
        {
            result.Success = false;
            var missing = targetVariables.Where(v => !knownFacts.ContainsKey(v));
            result.ErrorMessage = $"Could not resolve goals: {string.Join(", ", missing)}";
        }

        result.WorkingMemory = session.WorkingMemory.ToList();
        result.AuditTrail.AddRange(session.AuditTrail);
        foreach (var v in session.GeneratedVariables) result.GeneratedVariables.Add(v);

        Console.WriteLine($"[DEBUG FindClosure] Returning AuditTrail with {result.AuditTrail.Count} items.");
        return result;
    }

    private bool ResolveGoal(string goal, Concept concept, Dictionary<string, object> facts, HashSet<string> stack, ReasoningResult result)
    {
        if (facts.ContainsKey(goal)) return true;
        if (stack.Contains(goal)) throw new Exception($"Circular dependency: {goal}");
        
        stack.Add(goal);
        try {
            var candidateRules = concept.ConceptRules.Where(r => r.Conclusion.Any(c => GetConcludedVariable(c) == goal)).ToList();
            
            if (candidateRules.Count == 0)
            {
                // No rules can conclude this goal, so it's a true missing fact (input needed)
                result.MissingFacts.Add(goal);
                return false;
            }
            
            foreach (var rule in candidateRules)
            {
                bool hypothesisMet = true;
                foreach (var h in rule.Hypothesis)
                {
                    var needed = ExtractVariablesFromExpression(h);
                    foreach (var v in needed)
                    {
                        if (!ResolveGoal(v, concept, facts, stack, result)) { hypothesisMet = false; break; }
                    }
                    if (!hypothesisMet) break;
                    if (!EvaluateConstraint(h, facts)) { hypothesisMet = false; break; }
                }

                if (hypothesisMet)
                {
                    foreach (var conc in rule.Conclusion)
                    {
                        if (ApplyConclusion(conc, concept, facts, result, rule.Kind))
                        {
                            if (facts.ContainsKey(goal)) return true;
                        }
                    }
                }
            }
            
            // If we tried all rules and still didn't resolve it, it means the rules couldn't fire 
            // (likely due to other missing facts which were already added to MissingFacts during the recursion).
        } finally { stack.Remove(goal); }
        return false;
    }

    private bool IsConceptType(string type) => !new[] { "DECIMAL", "INT", "INTEGER", "FLOAT", "DOUBLE", "NUMBER", "MONEY", "BOOLEAN", "BOOL", "STRING", "VARCHAR" }.Contains(type.ToUpper());

    private Concept GetEffectiveConcept(Concept primary)
    {
        var allBaseObjects = new HashSet<string>(primary.BaseObjects);
        var additionalBases = HierarchyResolver?.Invoke(primary.Name);
        if (additionalBases != null) { foreach (var b in additionalBases) allBaseObjects.Add(b); }

        var effective = new Concept
        {
            Name = primary.Name,
            Variables = new List<Variable>(primary.Variables),
            Constraints = new List<KBMS.Models.Constraint>(primary.Constraints),
            CompRels = new List<ComputationRelation>(primary.CompRels),
            SameVariables = new List<SameVariable>(primary.SameVariables),
            ConceptRules = new List<ConceptRule>(primary.ConceptRules),
            Equations = new List<Equation>(primary.Equations),
            ConstructRelations = new List<ConstructRelation>(primary.ConstructRelations)
        };

        // 1. INHERITANCE (Base Objects)
        foreach (var baseName in allBaseObjects)
        {
            var baseConcept = ConceptResolver?.Invoke(baseName);
            if (baseConcept != null)
            {
                var flattendBase = GetEffectiveConcept(baseConcept);
                effective.Variables.AddRange(flattendBase.Variables.Where(v => !effective.Variables.Any(ev => ev.Name == v.Name)));
                effective.Constraints.AddRange(flattendBase.Constraints);
                effective.CompRels.AddRange(flattendBase.CompRels);
                effective.SameVariables.AddRange(flattendBase.SameVariables);
                effective.ConceptRules.AddRange(flattendBase.ConceptRules);
                effective.Equations.AddRange(flattendBase.Equations);
                effective.ConstructRelations.AddRange(flattendBase.ConstructRelations);
            }
        }

        // 2. COMPOSITION (Sub-objects Instantiation)
        var compositeVariables = effective.Variables.ToList();
        foreach (var v in compositeVariables)
        {
            if (IsConceptType(v.Type))
            {
                var subConceptName = v.Type;
                if (v.IsReference && !string.IsNullOrEmpty(v.ReferenceConceptName))
                    subConceptName = v.ReferenceConceptName;

                var subConcept = ConceptResolver?.Invoke(subConceptName);
                if (subConcept != null)
                {
                    var flattenedSub = GetEffectiveConcept(subConcept);
                    
                    // Prefix sub-variables
                    foreach (var subV in flattenedSub.Variables)
                    {
                        var newVName = $"{v.Name}.{subV.Name}";
                        if (!effective.Variables.Any(ev => ev.Name == newVName))
                        {
                            effective.Variables.Add(new Variable { Name = newVName, Type = subV.Type, Length = subV.Length, Scale = subV.Scale, IsReference = subV.IsReference, ReferenceConceptName = subV.ReferenceConceptName });
                        }
                    }
                    
                    // Prefix constraints
                    foreach (var c in flattenedSub.Constraints)
                    {
                        var mappedExpr = c.Expression;
                        foreach (var subV in flattenedSub.Variables)
                            mappedExpr = System.Text.RegularExpressions.Regex.Replace(mappedExpr, $@"\b{System.Text.RegularExpressions.Regex.Escape(subV.Name)}\b", $"{v.Name}.{subV.Name}");
                        effective.Constraints.Add(new KBMS.Models.Constraint { Name = $"{v.Name}_{c.Name}", Expression = mappedExpr, Line = c.Line, Column = c.Column });
                    }

                    // Prefix equations
                    foreach (var eq in flattenedSub.Equations)
                    {
                        var mappedExpr = eq.Expression;
                        foreach (var subV in flattenedSub.Variables)
                            mappedExpr = System.Text.RegularExpressions.Regex.Replace(mappedExpr, $@"\b{System.Text.RegularExpressions.Regex.Escape(subV.Name)}\b", $"{v.Name}.{subV.Name}");
                        effective.Equations.Add(new Equation { Expression = mappedExpr, Line = eq.Line, Column = eq.Column });
                    }

                    // Prefix rules
                    foreach (var cr in flattenedSub.ConceptRules)
                    {
                        var newRule = new ConceptRule { Id = Guid.NewGuid(), Kind = cr.Kind, Scope = cr.Scope, Priority = cr.Priority };
                        foreach (var h in cr.Hypothesis)
                        {
                            var mapped = h;
                            foreach (var subV in flattenedSub.Variables)
                                mapped = System.Text.RegularExpressions.Regex.Replace(mapped, $@"\b{System.Text.RegularExpressions.Regex.Escape(subV.Name)}\b", $"{v.Name}.{subV.Name}");
                            newRule.Hypothesis.Add(mapped);
                        }
                        foreach (var conc in cr.Conclusion)
                        {
                            var mapped = conc;
                            foreach (var subV in flattenedSub.Variables)
                                mapped = System.Text.RegularExpressions.Regex.Replace(mapped, $@"\b{System.Text.RegularExpressions.Regex.Escape(subV.Name)}\b", $"{v.Name}.{subV.Name}");
                            newRule.Conclusion.Add(mapped);
                        }
                        if (cr.IsMultiConcept)
                        {
                            foreach (var sc in cr.ScopeConcepts)
                            {
                                var newSc = new ConceptRuleScopeConcept { ConceptName = sc.ConceptName, Alias = sc.Alias, Position = sc.Position };
                                newRule.ScopeConcepts.Add(newSc);
                            }
                            foreach (var jc in cr.JoinConditions)
                            {
                                var newJc = new ConceptRuleJoinCondition { Operator = jc.Operator, LeftField = jc.LeftField, RightField = jc.RightField };
                                foreach (var subV in flattenedSub.Variables)
                                {
                                    newJc.LeftField = System.Text.RegularExpressions.Regex.Replace(newJc.LeftField, $@"\b{System.Text.RegularExpressions.Regex.Escape(subV.Name)}\b", $"{v.Name}.{subV.Name}");
                                    newJc.RightField = System.Text.RegularExpressions.Regex.Replace(newJc.RightField, $@"\b{System.Text.RegularExpressions.Regex.Escape(subV.Name)}\b", $"{v.Name}.{subV.Name}");
                                }
                                newRule.JoinConditions.Add(newJc);
                            }
                        }
                        effective.ConceptRules.Add(newRule);
                    }
                }
            }
        }

        return effective;
    }

    public double Solve1DEquation(string expr, string target, Dictionary<string, object> parameters, Action<string>? log = null)
    {
        var s = SplitEquation(expr);
        Func<double, double> f = (x) => {
            var p = new Dictionary<string, object>(parameters) { [target] = x };
            try {
                var leftResult = EvaluateFormula(s.left, p);
                var left = Convert.ToDouble(leftResult);
                var rightResult = EvaluateFormula(s.right, p);
                var right = Convert.ToDouble(rightResult);
                double result = left - right;
                return result;
            } catch { 
                return double.NaN; 
            }
        };

        double[] quickTests = { 0, 1, 10, 100, 1000, 10000, -1, -10, -100, -1000, -10000 };
        foreach (var t in quickTests)
        {
            var fv = f(t);
            if (!double.IsNaN(fv) && Math.Abs(fv) < 1e-6) return t;
        }

        double lower = -1000000000, upper = 1000000000;
        bool found = false;
        double step = 10000000;
        Console.WriteLine($"[Solve1DEquation] Start loop for {target} with expr: {expr}");
        for (double st = -1000000000; st <= 1000000000 && !found; st += step)
        {
            var f1 = f(st);
            var f2 = f(st + step);
            if (!double.IsNaN(f1) && !double.IsNaN(f2) && f1 * f2 <= 0) { lower = st; upper = st + step; found = true; }
        }
        Console.WriteLine($"[Solve1DEquation] End loop for {target}, found={found}");

        if (!found) return double.NaN;
        try { 
            Console.WriteLine($"[Solve1DEquation] FindRoot start for {target}");
            var r = MathNet.Numerics.RootFinding.Brent.FindRoot(f, lower, upper, 1e-6, 100); 
            Console.WriteLine($"[Solve1DEquation] FindRoot end for {target}, root={r}");
            return r;
        }
        catch { return double.NaN; }
    }

    public double[] EvaluateFormulaSIMD(string formula, Dictionary<string, double[]> bulkParameters, int length)
    {
        var parts = formula.Split(new[] { '+', '-', '*', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new NotSupportedException($"SIMD evaluation currently supports simple binary operations. Found: {formula}");

        string leftStr = parts[0].Trim();
        string rightStr = parts[1].Trim();
        char op = formula.First(c => c == '+' || c == '-' || c == '*' || c == '/');

        double[] result = new double[length];
        
        double[] leftArr = bulkParameters.TryGetValue(leftStr, out var l) ? l : System.Linq.Enumerable.Repeat(double.Parse(leftStr), length).ToArray();
        double[] rightArr = bulkParameters.TryGetValue(rightStr, out var r) ? r : System.Linq.Enumerable.Repeat(double.Parse(rightStr), length).ToArray();

        int vectorSize = System.Numerics.Vector<double>.Count;
        int i = 0;
        
        for (; i <= length - vectorSize; i += vectorSize)
        {
            var vLeft = new System.Numerics.Vector<double>(leftArr, i);
            var vRight = new System.Numerics.Vector<double>(rightArr, i);
            System.Numerics.Vector<double> vRes;
            
            switch (op)
            {
                case '+': vRes = System.Numerics.Vector.Add(vLeft, vRight); break;
                case '-': vRes = System.Numerics.Vector.Subtract(vLeft, vRight); break;
                case '*': vRes = System.Numerics.Vector.Multiply(vLeft, vRight); break;
                case '/': vRes = System.Numerics.Vector.Divide(vLeft, vRight); break;
                default: throw new InvalidOperationException();
            }
            vRes.CopyTo(result, i);
        }

        for (; i < length; i++)
        {
            switch (op)
            {
                case '+': result[i] = leftArr[i] + rightArr[i]; break;
                case '-': result[i] = leftArr[i] - rightArr[i]; break;
                case '*': result[i] = leftArr[i] * rightArr[i]; break;
                case '/': result[i] = leftArr[i] / rightArr[i]; break;
            }
        }
        
        return result;
    }

    public object EvaluateFormula(string formula, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrWhiteSpace(formula)) return 0.0;
        var trimmed = formula.Trim();
        if (trimmed.StartsWith("'") && trimmed.EndsWith("'") && trimmed.Length >= 2)
            return trimmed.Substring(1, trimmed.Length - 2);

        string cacheKey = formula.Trim('\'').Trim();

        if (!_expressionCache.TryGetValue(cacheKey, out var e))
        {
            string processed = PreProcessOperators(cacheKey);
            // Fix case sensitivity for common math functions for NCalc
            processed = Regex.Replace(processed, @"\bsqrt\b", "Sqrt", RegexOptions.IgnoreCase);
            processed = Regex.Replace(processed, @"\bsin\b", "Sin", RegexOptions.IgnoreCase);
            processed = Regex.Replace(processed, @"\bcos\b", "Cos", RegexOptions.IgnoreCase);
            processed = Regex.Replace(processed, @"\btan\b", "Tan", RegexOptions.IgnoreCase);
            processed = Regex.Replace(processed, @"\babs\b", "Abs", RegexOptions.IgnoreCase);
            processed = Regex.Replace(processed, @"\bpow\b", "Pow", RegexOptions.IgnoreCase);
            processed = Regex.Replace(processed, @"\blog\b", "Log", RegexOptions.IgnoreCase);
            
            var powSafe = _powRegex.Replace(processed, "Pow($1, $2)");
            var safe = _dotRegex.Replace(powSafe, "[$1]");
            
            // Convert single-quoted strings to double-quoted for NCalc compatibility
            if (!safe.Contains("\"") && safe.Contains("'"))
            {
                safe = Regex.Replace(safe, @"'([^']*)'", "\"$1\"");
            }
            
            e = new NCalc.Expression(safe);
            _expressionCache[cacheKey] = e;

            e.EvaluateFunction += (name, args) => {
                if (name.Equals("Factorial", StringComparison.OrdinalIgnoreCase))
                {
                    var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                    if (evalArgs.Length >= 1) { args.Result = MathNet.Numerics.SpecialFunctions.Factorial((int)Convert.ToDouble(evalArgs[0])); return; }
                }
                if (name.StartsWith("_op_"))
                {
                    var op = OperatorResolver?.Invoke(GetSymbolFromOpName(name.Substring(4)));
                    if (op != null) {
                        var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                        var bp = new Dictionary<string, object>();
                        if (evalArgs.Length >= 1) bp["a"] = evalArgs[0]!;
                        if (evalArgs.Length >= 2) bp["b"] = evalArgs[1]!;
                        args.Result = EvaluateFormula(op.Body, bp);
                        return;
                    }
                }
                var func = FunctionResolver?.Invoke(name);
                if (func != null)
                {
                    var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                    var bp = new Dictionary<string, object>();
                    for (int i = 0; i < func.Parameters.Count && i < evalArgs.Length; i++) bp[func.Parameters[i].Name] = evalArgs[i]!;
                    args.Result = EvaluateFormula(func.Body, bp);
                }
            };
        }
        
        e.Parameters.Clear();
        foreach (var p in parameters) {
            var val = p.Value;
            if (val is System.Text.Json.JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Number: val = je.GetDouble(); break;
                    case System.Text.Json.JsonValueKind.True: val = true; break;
                    case System.Text.Json.JsonValueKind.False: val = false; break;
                    case System.Text.Json.JsonValueKind.String: val = je.GetString()!; break;
                    case System.Text.Json.JsonValueKind.Null: val = null!; break;
                }
            }

            if (val is decimal dec) e.Parameters[p.Key] = (double)dec;
            else if (val is long l) e.Parameters[p.Key] = (double)l;
            else if (val is int i) e.Parameters[p.Key] = (double)i;
            else e.Parameters[p.Key] = val;
        }

        var res = e.Evaluate();
        if (res is double d && (double.IsInfinity(d) || double.IsNaN(d)))
            throw new Exception("Mathematical error: infinity produced.");
        return (res is int or long or double or float or decimal) ? Convert.ToDouble(res) : (res ?? DBNull.Value);
    }

    public object CastToVariableType(object? val, KBMS.Models.Variable? variable)
    {
        if (val == null || variable == null) return val ?? 0.0;
        var type = variable.Type.ToUpper();
        try {
            if (type is "INT" or "INTEGER" or "LONG") return Convert.ToInt64(val);
            if (type is "DECIMAL" or "MONEY" or "NUMBER") {
                decimal dec = (val is double d) ? Convert.ToDecimal(Math.Round(d, 12)) : Convert.ToDecimal(val);
                return variable.Scale.HasValue ? Math.Round(dec, variable.Scale.Value) : Math.Round(dec, 10);
            }
            if (type is "FLOAT" or "DOUBLE") return Convert.ToDouble(val);
            if (type is "BOOL" or "BOOLEAN") {
                if (val is bool b) return b;
                return Convert.ToDouble(val) != 0;
            }
            if (type is "STRING" or "VARCHAR" or "TEXT") return val.ToString()?.Trim('\'') ?? "";
        } catch { }
        return val;
    }

    public bool ValuesEqual(object? v1, object? v2)
    {
        if (v1 == null && v2 == null) return true;
        if (v1 == null || v2 == null) return false;

        object? val1 = v1;
        if (val1 is System.Text.Json.JsonElement je1) {
            if (je1.ValueKind == System.Text.Json.JsonValueKind.True) val1 = true;
            else if (je1.ValueKind == System.Text.Json.JsonValueKind.False) val1 = false;
            else if (je1.ValueKind == System.Text.Json.JsonValueKind.Number) val1 = je1.GetDouble();
            else val1 = je1.ToString();
        }

        object? val2 = v2;
        if (val2 is System.Text.Json.JsonElement je2) {
            if (je2.ValueKind == System.Text.Json.JsonValueKind.True) val2 = true;
            else if (je2.ValueKind == System.Text.Json.JsonValueKind.False) val2 = false;
            else if (je2.ValueKind == System.Text.Json.JsonValueKind.Number) val2 = je2.GetDouble();
            else val2 = je2.ToString();
        }

        if (val1 is bool b1 && val2 is bool b2) return b1 == b2;
        if (IsNumeric(val1) && IsNumeric(val2)) return Math.Abs(Convert.ToDouble(val1) - Convert.ToDouble(val2)) < 1e-5;
        
        return val1!.ToString()!.Equals(val2!.ToString()!, StringComparison.OrdinalIgnoreCase);
    }
    private bool IsNumeric(object v) => v is int or long or double or decimal or float;

    public bool EvaluateConstraint(string expr, Dictionary<string, object> parameters)
    {
        var safe = _eqRegex.Replace(PreProcessOperators(expr), "==");
        // Convert single-quoted string literals to double-quoted for NCalc compatibility
        safe = Regex.Replace(safe, @"'([^']*)'", "\"$1\"");
        safe = _powRegex.Replace(safe, "Pow($1, $2)");
        safe = _dotRegex.Replace(safe, "[$1]");
        
        var e = new NCalc.Expression(safe);
        foreach (var p in parameters) {
            var val = p.Value;
            if (val is System.Text.Json.JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Number: val = je.GetDouble(); break;
                    case System.Text.Json.JsonValueKind.True: val = true; break;
                    case System.Text.Json.JsonValueKind.False: val = false; break;
                    case System.Text.Json.JsonValueKind.String: val = je.GetString()!; break;
                    case System.Text.Json.JsonValueKind.Null: val = null!; break;
                }
            }
            if (val is decimal dec) e.Parameters[p.Key] = (double)dec;
            else if (val is long l) e.Parameters[p.Key] = (double)l;
            else if (val is int i) e.Parameters[p.Key] = (double)i;
            else e.Parameters[p.Key] = val;
        }

        e.EvaluateFunction += (name, args) => {
            var func = FunctionResolver?.Invoke(name);
            if (func != null) {
                var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                var bp = new Dictionary<string, object>();
                for (int i = 0; i < func.Parameters.Count && i < evalArgs.Length; i++) bp[func.Parameters[i].Name] = evalArgs[i]!;
                args.Result = EvaluateFormula(func.Body, bp);
            }
        };

        try {
            var res = e.Evaluate();
            var b = (res is bool val) ? val : (res is int or long or double or float or decimal) ? Convert.ToDouble(res) != 0 : false;
            return b;
        } catch { return false; }
    }

    private string PreProcessOperators(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return expression;
        var symbolToName = new Dictionary<string, string> {
            { "#", "hash" }, { "@", "at" }, { "$", "dollar" }, { "&", "amp" }, 
            { "|", "pipe" }, { "!", "bang" }, { "~", "tilde" }, { "?", "question" }, { ":", "colon" }
        };
        var processed = expression;
        foreach (var kvp in symbolToName)
        {
            if (!expression.Contains(kvp.Key)) continue;
            var pattern = $@"(\b[a-zA-Z0-9_\[\]]+\b)\s*\{Regex.Escape(kvp.Key)}\s*(\b[a-zA-Z0-9_\[\]]+\b)";
            processed = Regex.Replace(processed, pattern, $"_op_{kvp.Value}($1, $2)");
        }
        return processed; 
    }

    private string GetSymbolFromOpName(string name) {
        var m = new Dictionary<string, string> { { "hash", "#" }, { "at", "@" }, { "dollar", "$" }, { "amp", "&" }, { "pipe", "|" }, { "bang", "!" }, { "tilde", "~" }, { "question", "?" }, { "colon", ":" } };
        return m.TryGetValue(name, out var sym) ? sym : name;
    }

    public List<string> ExtractVariablesFromExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return new List<string>();
        // Strip string literals (single or double quoted) before extracting variable names
        var cleaned = Regex.Replace(expression.Trim(), @"'[^']*'|""[^""]*""", " ");
        var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var funcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in KBMS.Models.BuiltInFunctions.MathFunctions) funcs.Add(f);
        foreach (var f in KBMS.Models.BuiltInFunctions.LogicalFunctions) funcs.Add(f);

        foreach (Match m in Regex.Matches(cleaned, @"\b[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*\b"))
        {
            var val = m.Value;
            if (funcs.Contains(val) || val.Equals("true", StringComparison.OrdinalIgnoreCase) || val.Equals("false", StringComparison.OrdinalIgnoreCase) || double.TryParse(val, out _)) continue;
            int peekIdx = m.Index + m.Length;
            while (peekIdx < cleaned.Length && char.IsWhiteSpace(cleaned[peekIdx])) peekIdx++;
            if (peekIdx < cleaned.Length && cleaned[peekIdx] == '(') continue;
            vars.Add(val);
        }
        return vars.ToList();
    }

    private (string left, string right) SplitEquation(string expr)
    {
        var c = expr.Trim('\'').Trim();
        var idx = c.IndexOf('=');
        if (idx > 0 && c[idx - 1] != '!' && c[idx - 1] != '<' && c[idx - 1] != '>')
            return (c.Substring(0, idx).Trim(), c.Substring(idx + 1).Trim());
        return (c, "0");
    }

    private string? GetConcludedVariable(string conclusion)
    {
        var t = conclusion.Trim(' ', '(', ')').Replace("SET ", "", StringComparison.OrdinalIgnoreCase);
        var idx = t.IndexOfAny(new[] { '=', ':' });
        if (idx > 0 && (idx == t.Length - 1 || (t[idx - 1] != '!' && t[idx - 1] != '<' && t[idx - 1] != '>')))
            return (idx < t.Length - 1 && t[idx + 1] == '=') ? null : t.Substring(0, idx).Trim();
        return null;
    }

    public bool ApplyConclusion(string conclusion, Concept concept, Dictionary<string, object> knownFacts, ReasoningResult result, string ruleKind, int? stepNumber = null)
    {
        // Trim only leading/trailing spaces, then remove SET keyword
        // Do NOT Trim('(', ')') as that breaks function calls like GravityForce(...)
        var t = conclusion.Trim();
        // Remove optional outer parentheses only if the whole string is wrapped
        if (t.StartsWith("(") && t.EndsWith(")")) {
            // Verify it's a wrapping paren, not part of expression
            int depth = 0;
            bool isWrapper = true;
            for (int ci = 0; ci < t.Length - 1; ci++) {
                if (t[ci] == '(') depth++;
                else if (t[ci] == ')') depth--;
                if (depth == 0 && ci < t.Length - 1) { isWrapper = false; break; }
            }
            if (isWrapper) t = t.Substring(1, t.Length - 2).Trim();
        }
        t = Regex.Replace(t, @"^SET\s+", "", RegexOptions.IgnoreCase);
        var idx = t.IndexOfAny(new[] { '=', ':' });
        bool isAssignment = idx > 0 && (idx == t.Length - 1 || (t[idx - 1] != '!' && t[idx - 1] != '<' && t[idx - 1] != '>'));
        if (isAssignment && idx < t.Length - 1 && t[idx+1] == '=') isAssignment = false;

        if (isAssignment) {
            var varName = t.Substring(0, idx).Trim();
            var formula = t.Substring(idx + 1).Trim();
            var valRaw = EvaluateFormula(formula, knownFacts);
            var castedVal = CastToVariableType(valRaw, concept.Variables.FirstOrDefault(v => v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase) || varName.EndsWith("." + v.Name, StringComparison.OrdinalIgnoreCase)));
            
            // Extract plain name if aliased
            string plainName = varName;
            int dotIdx = varName.IndexOf('.');
            if (dotIdx > 0) plainName = varName.Substring(dotIdx + 1);

            if (knownFacts.ContainsKey(plainName) && ValuesEqual(knownFacts[plainName], castedVal)) return false;
            
            knownFacts[plainName] = castedVal;
            result.DerivedFacts[plainName] = castedVal;
            
            if (plainName != varName) {
                knownFacts[varName] = castedVal;
                result.DerivedFacts[varName] = castedVal;
            }

            if (concept != null && !plainName.Contains('.'))
            {
                var aliasedName = $"{concept.Name}.{plainName}";
                if (knownFacts.ContainsKey(aliasedName)) knownFacts[aliasedName] = castedVal;
                result.DerivedFacts[aliasedName] = castedVal;
            }
            if (stepNumber.HasValue) result.Steps.Add($"Step {stepNumber}: From Rule [{ruleKind}] => {plainName} = {castedVal}");
            
            // Add to AuditTrail so backward chaining is explainable
            result.AuditTrail.Add(new KBMS.Reasoning.Rete.ReasoningStep
            {
                RuleName = ruleKind, // For backward chaining, ruleKind is used as the rule name
                StepCost = 1, // Default cost
                InputFacts = new Dictionary<string, object>(),
                OutputFacts = new Dictionary<string, object> { [plainName] = castedVal! }
            });
            Console.WriteLine($"[DEBUG ApplyConclusion] Added to AuditTrail. Current count: {result.AuditTrail.Count}");
            result.GeneratedVariables.Add(plainName);
            if (plainName != varName) result.GeneratedVariables.Add(varName);

            return true;
        }
        knownFacts[t] = true;
        result.DerivedFacts[t] = true;
        return true;
    }
    public static KBMS.Reasoning.Rete.ExplanationNode BuildExplanationTree(string goal, Dictionary<string, object> finalFacts, List<KBMS.Reasoning.Rete.ReasoningStep> auditTrail, IEnumerable<string> generatedVariables)
    {
        object? val = null;
        if (!finalFacts.TryGetValue(goal, out val))
        {
            // Attempt 1: Check if the exact aliased name exists in the AuditTrail (as an Input or Output)
            var stepWithExactAlias = auditTrail.LastOrDefault(s => s.InputFacts.ContainsKey(goal) || s.OutputFacts.ContainsKey(goal));
            if (stepWithExactAlias != null)
            {
                if (stepWithExactAlias.OutputFacts.TryGetValue(goal, out var outVal)) val = outVal;
                else if (stepWithExactAlias.InputFacts.TryGetValue(goal, out var inVal)) val = inVal;
            }

            // Attempt 2: Fallback to stripping the alias and checking the current object's final facts
            if (val == null)
            {
                var dotIdx = goal.IndexOf('.');
                if (dotIdx > 0)
                {
                    var unaliased = goal.Substring(dotIdx + 1);
                    finalFacts.TryGetValue(unaliased, out val);
                }
            }
        }

        var node = new KBMS.Reasoning.Rete.ExplanationNode
        {
            Goal = goal,
            Value = val
        };

        // If the variable wasn't generated by AI, it's a base fact (provided by user/system)
        if (!generatedVariables.Contains(goal, StringComparer.OrdinalIgnoreCase))
        {
            node.IsBaseFact = true;
            return node;
        }

        // Find the rule that generated this goal (most recent one first)
        var generatingStep = auditTrail.LastOrDefault(s => s.OutputFacts.ContainsKey(goal) || s.OutputFacts.Keys.Any(k => k.Equals(goal, StringComparison.OrdinalIgnoreCase)));
        
        if (generatingStep != null)
        {
            node.DerivedBy = generatingStep.RuleName;
            node.Logic = generatingStep.Logic;
            node.StepCost = generatingStep.StepCost;
            node.Dependencies = new List<KBMS.Reasoning.Rete.ExplanationNode>();
            
            var relevantInputs = generatingStep.InputFacts
                .Where(kv => generatingStep.UsedVariables == null || generatingStep.UsedVariables.Count == 0 || generatingStep.UsedVariables.Contains(kv.Key, StringComparer.OrdinalIgnoreCase) || generatingStep.UsedVariables.Contains(kv.Key.Split('.').Last(), StringComparer.OrdinalIgnoreCase));
                
            foreach (var input in relevantInputs)
            {
                // Recursively build tree for each dependency
                node.Dependencies.Add(BuildExplanationTree(input.Key, finalFacts, auditTrail, generatedVariables));
            }
        }
        else
        {
            // Fallback if we couldn't find the exact step (e.g., due to backward chaining missing input facts tracking)
            // Wait, backward chaining steps DO track InputFacts if we update ApplyConclusion to track them.
            // But currently ApplyConclusion passes new Dictionary<string, object>() for InputFacts.
            // Let's just mark it as generated but with unknown dependencies if step is missing or has no inputs.
        }

        return node;
    }
}
