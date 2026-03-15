using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using KBMS.Models;
using NCalc; // We will need NCalc or a similar math evaluator for parsing formulas
using System.Text.RegularExpressions;

namespace KBMS.Reasoning;

/// <summary>
/// The core Reasoning Engine implementing COKB solving methodologies (FClosure)
/// </summary>
public class InferenceEngine
{
    public class DerivationTrace
    {
        public string TargetVariable { get; set; } = "";
        public object? Value { get; set; }
        public string Mechanism { get; set; } = ""; // Equation, Rule, Computation, SameVariable
        public string Source { get; set; } = "";    // Original expression or rule name
        public Dictionary<string, object> Inputs { get; set; } = new();
    }

    public class ReasoningResult
    {
        public bool Success { get; set; } = true;
        public Dictionary<string, object> DerivedFacts { get; set; } = new();
        public List<string> Steps { get; set; } = new();
        public List<DerivationTrace> Traces { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public Func<string, Concept?>? ConceptResolver { get; set; }

    // (RC7) Resolve custom functions and operators
    public Func<string, Function?>? FunctionResolver { get; set; }
    public Func<string, Operator?>? OperatorResolver { get; set; }

    // (RC8) Resolve hierarchies (IS-A)
    public Func<string, List<string>>? HierarchyResolver { get; set; }

    // (Phase 15) Resolve PART_OF components
    public Func<string, List<string>>? PartOfResolver { get; set; }

    // (Phase 16) Resolve Relations for CONSTRUCT_RELATIONS expansion
    public Func<string, Relation?>? RelationResolver { get; set; }

    public ReasoningResult FindClosure(Concept concept, Dictionary<string, object> initialFacts, List<string> targetVariables)
    {
        var result = new ReasoningResult();
        var knownFacts = new Dictionary<string, object>(initialFacts);
        var appliedRuleIds = new HashSet<Guid>();
        int stepCount = 0;

        result.Steps.Add($"Step {stepCount++}: Initializing GT (Known Facts) = {{ {string.Join(", ", initialFacts.Select(kv => $"{kv.Key}={kv.Value}"))} }}");
        if (targetVariables.Any()) result.Steps.Add($"Goal KL (Targets) = {{ {string.Join(", ", targetVariables)} }}");

        // (RC6.1) Resolve Hierarchy (IS-A)
        var effectiveConcept = GetEffectiveConcept(concept);
        
        result.Steps.Add($"Concept '{concept.Name}' ready. Stats: {effectiveConcept.Variables.Count} Vars, {effectiveConcept.SameVariables.Count} SameVars, {effectiveConcept.CompRels.Count} CompRels, {effectiveConcept.ConceptRules.Count} Rules, {effectiveConcept.Constraints.Count} Constraints, {effectiveConcept.Equations.Count} Equations");
        
        // (RC7/Early) Check initial constraints
        if (effectiveConcept.Constraints.Count > 0)
        {
            foreach (var constraint in effectiveConcept.Constraints)
            {
                var constraintLabel = string.IsNullOrEmpty(constraint.Name) ? constraint.Expression : $"{constraint.Name}: {constraint.Expression}";
                var locationLabel = (constraint.Line > 0) ? $" (at line {constraint.Line}, col {constraint.Column})" : "";

                try
                {
                    var needed = ExtractVariablesFromExpression(constraint.Expression);
                    if (needed.All(v => knownFacts.ContainsKey(v)))
                    {
                        var ok = EvaluateConstraint(constraint.Expression, knownFacts);
                        if (!ok)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"Input constraint violated: {constraintLabel}{locationLabel}.";
                            result.Steps.Add($"  ✗ Input constraint VIOLATED: {constraintLabel}{locationLabel}");
                            return result;
                        }
                    }
                }
                catch { }
            }
        }

        bool factAdded = true;
        int iteration = 0;
        while (factAdded && iteration < 50) // Limit iterations to prevent infinite loops
        {
            factAdded = false;
            iteration++;

            // (RC6.2) Recursive Closure for sub-concepts
            if (ConceptResolver != null)
            {
                foreach (var variable in effectiveConcept.Variables)
                {
                    if (IsConceptType(variable.Type))
                    {
                        var subConcept = ConceptResolver(variable.Type);
                        if (subConcept != null)
                        {
                            var subFacts = new Dictionary<string, object>();
                            var prefix = variable.Name + ".";
                            foreach (var fact in knownFacts)
                            {
                                if (fact.Key.StartsWith(prefix))
                                    subFacts[fact.Key.Substring(prefix.Length)] = fact.Value;
                            }

                            if (subFacts.Count > 0)
                            {
                                // result.Steps.Add($"  [Debug] Recursing into {variable.Name} ({variable.Type}) with {subFacts.Count} facts");
                                var subResult = FindClosure(subConcept, subFacts, new List<string>());
                                
                                foreach (var derived in subResult.DerivedFacts)
                                {
                                    var fullKey = prefix + derived.Key;
                                    if (!knownFacts.ContainsKey(fullKey))
                                    {
                                        knownFacts[fullKey] = derived.Value;
                                        result.DerivedFacts[fullKey] = derived.Value;
                                        factAdded = true;
                                        result.Steps.Add($"Step {stepCount++}: Deriving {fullKey} = {derived.Value} (from {variable.Type} closure)");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // (RC2) SameVariables propagation
            foreach (var sv in effectiveConcept.SameVariables)
            {
                if (knownFacts.ContainsKey(sv.Variable1) && !knownFacts.ContainsKey(sv.Variable2))
                {
                    var val = knownFacts[sv.Variable1];
                    knownFacts[sv.Variable2] = val;
                    result.DerivedFacts[sv.Variable2] = val;
                    result.Steps.Add($"Step {stepCount++}: From SameVariable {sv.Variable1} = {sv.Variable2} => {sv.Variable2} = {val}");
                    
                    // (Phase 17) Trace
                    result.Traces.Add(new DerivationTrace
                    {
                        TargetVariable = sv.Variable2,
                        Value = val,
                        Mechanism = "SameVariable",
                        Source = $"{sv.Variable1} = {sv.Variable2}",
                        Inputs = new Dictionary<string, object> { { sv.Variable1, val } }
                    });

                    factAdded = true;
                }
                else if (knownFacts.ContainsKey(sv.Variable2) && !knownFacts.ContainsKey(sv.Variable1))
                {
                    var val = knownFacts[sv.Variable2];
                    knownFacts[sv.Variable1] = val;
                    result.DerivedFacts[sv.Variable1] = val;
                    result.Steps.Add($"Step {stepCount++}: From SameVariable {sv.Variable1} = {sv.Variable2} => {sv.Variable1} = {val}");
                    
                    // (Phase 17) Trace
                    result.Traces.Add(new DerivationTrace
                    {
                        TargetVariable = sv.Variable1,
                        Value = val,
                        Mechanism = "SameVariable",
                        Source = $"{sv.Variable1} = {sv.Variable2}",
                        Inputs = new Dictionary<string, object> { { sv.Variable2, val } }
                    });

                    factAdded = true;
                }
            }

            // (RC3) Computation Relations (Rf)
            foreach (var rel in effectiveConcept.CompRels)
            {
                if (rel.ResultVariable != null && !knownFacts.ContainsKey(rel.ResultVariable))
                {
                    if (rel.InputVariables.All(v => knownFacts.ContainsKey(v)))
                    {
                        try
                        {
                            var value = EvaluateFormula(rel.Expression, knownFacts);
                            knownFacts[rel.ResultVariable] = value;
                            result.DerivedFacts[rel.ResultVariable] = value;
                            result.Steps.Add($"Step {stepCount++}: From Computation {{{string.Join(", ", rel.InputVariables)}}} => {rel.ResultVariable} = {value}");
                            
                            // (Phase 17) Trace
                            result.Traces.Add(new DerivationTrace
                            {
                                TargetVariable = rel.ResultVariable,
                                Value = value,
                                Mechanism = "Computation",
                                Source = rel.Expression,
                                Inputs = rel.InputVariables.ToDictionary(v => v, v => knownFacts[v])
                            });

                            factAdded = true;
                        }
                        catch (Exception ex)
                        {
                            result.Steps.Add($"Step {stepCount++}: Error in computation {rel.Expression}: {ex.Message}");
                        }
                    }
                }
            }

            // (RC4) Rules (Rr)
            foreach (var rule in effectiveConcept.ConceptRules)
            {
                if (appliedRuleIds.Contains(rule.Id)) continue;
                
                bool allHypothesisMet = true;
                foreach (var hyp in rule.Hypothesis)
                {
                    try
                    {
                        var met = EvaluateConstraint(hyp, knownFacts);
                        if (!met) { allHypothesisMet = false; break; }
                    }
                    catch { allHypothesisMet = false; break; } // If variables are missing, hypothesis not met
                }

                if (allHypothesisMet && rule.Hypothesis.Count > 0)
                {
                    appliedRuleIds.Add(rule.Id);
                    foreach (var conclusion in rule.Conclusion)
                    {
                        var eqIdx = conclusion.IndexOf('=');
                        if (eqIdx > 0 && conclusion[eqIdx - 1] != '!' && conclusion[eqIdx - 1] != '<' && conclusion[eqIdx - 1] != '>')
                        {
                            var varName = conclusion.Substring(0, eqIdx).Trim();
                            var exprStr = conclusion.Substring(eqIdx + 1).Trim();

                            if (!knownFacts.ContainsKey(varName))
                            {
                                try
                                {
                                    var val = EvaluateFormula(exprStr, knownFacts);
                                    knownFacts[varName] = val;
                                    result.DerivedFacts[varName] = val;
                                    result.Steps.Add($"Step {stepCount++}: From Rule [{rule.Kind}] IF {{{string.Join(", ", rule.Hypothesis)}}} => {varName} = {val}");
                                    
                                    // (Phase 17) Trace
                                    var formulaVars = ExtractVariablesFromExpression(exprStr);
                                    var hypothesisVars = rule.Hypothesis.SelectMany(h => ExtractVariablesFromExpression(h)).Distinct();
                                    var allInputs = formulaVars.Concat(hypothesisVars).Distinct();

                                    result.Traces.Add(new DerivationTrace
                                    {
                                        TargetVariable = varName,
                                        Value = val,
                                        Mechanism = "Rule",
                                        Source = $"IF {string.Join(" AND ", rule.Hypothesis)} THEN {conclusion}",
                                        Inputs = allInputs.Where(v => knownFacts.ContainsKey(v)).ToDictionary(v => v, v => knownFacts[v])
                                    });

                                    factAdded = true;
                                }
                                catch { } // Cannot evaluate yet, skip for now
                            }
                        }
                        else
                        {
                            // Flag conclusion (e.g., "isRight" or "isRight=1")
                            var flagName = conclusion.Trim();
                            if (!knownFacts.ContainsKey(flagName))
                            {
                                knownFacts[flagName] = 1.0; // Represent boolean true as 1.0
                                result.DerivedFacts[flagName] = 1.0;
                                result.Steps.Add($"Step {stepCount++}: From Rule [{rule.Kind}] IF {{{string.Join(", ", rule.Hypothesis)}}} => {flagName} = 1");
                                
                                // (Phase 17) Trace
                                var hypothesisVars = rule.Hypothesis.SelectMany(h => ExtractVariablesFromExpression(h)).Distinct();
                                result.Traces.Add(new DerivationTrace
                                {
                                    TargetVariable = flagName,
                                    Value = 1.0,
                                    Mechanism = "Rule",
                                    Source = $"IF {string.Join(" AND ", rule.Hypothesis)} THEN {conclusion}",
                                    Inputs = hypothesisVars.Where(v => knownFacts.ContainsKey(v)).ToDictionary(v => v, v => knownFacts[v])
                                });

                                factAdded = true;
                            }
                        }
                    }
                }
            }

            // (RC5) Equation Solving
            foreach (var eq in effectiveConcept.Equations)
            {
                var eqVars = ExtractVariablesFromExpression(eq.Expression);
                var unknownEqVars = eqVars.Where(v => !knownFacts.ContainsKey(v)).ToList();
                
                result.Steps.Add($"  - (DEBUG) Testing Equation '{eq.Expression}': Vars=[{string.Join(",", eqVars)}], Unknowns=[{string.Join(",", unknownEqVars)}]");

                if (unknownEqVars.Count == 1)
                {
                    string targetVar = unknownEqVars[0];
                    try
                    {
                        var root = Solve1DEquation(eq.Expression, targetVar, knownFacts, (msg) => result.Steps.Add(msg));
                        if (!double.IsNaN(root))
                        {
                            knownFacts[targetVar] = root;
                            result.DerivedFacts[targetVar] = root;
                            result.Steps.Add($"Step {stepCount++}: From Equation '{eq.Expression}' solved for {targetVar} => {root:F4}");
                            
                            // (Phase 17) Trace
                            result.Traces.Add(new DerivationTrace
                            {
                                TargetVariable = targetVar,
                                Value = root,
                                Mechanism = "Equation",
                                Source = eq.Expression,
                                Inputs = eqVars.Where(v => v != targetVar && knownFacts.ContainsKey(v))
                                               .ToDictionary(v => v, v => knownFacts[v])
                            });

                            factAdded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        var loc = (eq.Line > 0) ? $" (at line {eq.Line}, col {eq.Column})" : "";
                        result.Steps.Add($"  - (ERROR) Solving Equation '{eq.Expression}'{loc} for {targetVar} failed: {ex.Message}");
                    }
                }
                else if (unknownEqVars.Count == 2)
                {
                    // Look for a pair of equations that share the same two unknowns
                    foreach (var eq2 in effectiveConcept.Equations)
                    {
                        if (eq == eq2) continue; // Don't pair an equation with itself
                        var eq2Vars = ExtractVariablesFromExpression(eq2.Expression);
                        var unknowns2 = eq2Vars.Where(v => !knownFacts.ContainsKey(v)).ToHashSet();
                        
                        if (unknowns2.Count == 2 && unknowns2.SetEquals(unknownEqVars))
                        {
                            try
                            {
                                var unknownList = unknownEqVars.ToList();
                                var sol = Solve2DEquationSystem(eq.Expression, eq2.Expression, unknownList[0], unknownList[1], knownFacts);
                                if (sol != null)
                                {
                                    foreach (var kvp in sol)
                                    {
                                        if (!knownFacts.ContainsKey(kvp.Key)) // Only add if not already known
                                        {
                                            knownFacts[kvp.Key] = kvp.Value;
                                            result.DerivedFacts[kvp.Key] = kvp.Value;
                                            factAdded = true;
                                        }
                                    }
                                    if (factAdded) // Only log if new facts were added
                                        result.Steps.Add($"Step {stepCount++}: From Equation System {{'{eq.Expression}', '{eq2.Expression}'}} solved for {{{string.Join(", ", unknownList)}}} => {{{string.Join(", ", sol.Values.Select(v => v.ToString("F4")))}}}");
                                }
                            }
                            catch { } // Failed to solve system, skip
                        }
                    }
                }
            }

            // Early exit if all targets KL met
            if (targetVariables.All(v => knownFacts.ContainsKey(v)))
            {
                result.Success = true;
                result.Steps.Add("=> Stopping condition met: All target variables KL are found in GT.");
                break;
            }
        }

        // Final Constraints Check
        if (effectiveConcept.Constraints.Count > 0)
        {
            result.Steps.Add($"Step {stepCount++}: Validating {effectiveConcept.Constraints.Count} constraint(s)...");
            foreach (var constraint in effectiveConcept.Constraints)
            {
                var constraintLabel = string.IsNullOrEmpty(constraint.Name) ? constraint.Expression : $"{constraint.Name}: {constraint.Expression}";
                var locationLabel = (constraint.Line > 0) ? $" (at line {constraint.Line}, col {constraint.Column})" : "";

                try
                {
                    var ok = EvaluateConstraint(constraint.Expression, knownFacts);
                    if (ok) result.Steps.Add($"  ✓ Constraint satisfied: {constraintLabel}{locationLabel}");
                    else
                    {
                        result.Steps.Add($"  ✗ Constraint VIOLATED: {constraintLabel}{locationLabel}");
                        result.Success = false;
                        result.ErrorMessage = $"Constraint violated: {constraintLabel}{locationLabel}";
                    }
                }
                catch { result.Steps.Add($"  ? Constraint skipped (missing vars): {constraintLabel}{locationLabel}"); } // Cannot evaluate, assume not violated for now
            }
        }

        if (targetVariables.Count > 0 && !targetVariables.All(v => knownFacts.ContainsKey(v)))
        {
            result.Success = false;
            result.ErrorMessage = "Reasoning engine halted: FClosure(GT) exhausted without reaching Goal(KL).";
        }

        return result;
    }

    private Concept GetEffectiveConcept(Concept primary)
    {
        var allBaseObjects = new HashSet<string>(primary.BaseObjects);
        
        // (RC8) Add IS-A hierarchies
        var additionalBases = HierarchyResolver?.Invoke(primary.Name);
        if (additionalBases != null)
        {
            foreach (var b in additionalBases) allBaseObjects.Add(b);
        }

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

        // IS_A merging
        if (allBaseObjects.Count > 0)
        {
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
        }

        // (Phase 15) PART_OF expansion: auto-discover component sub-concepts
        var partOfChildren = PartOfResolver?.Invoke(primary.Name);
        if (partOfChildren != null && partOfChildren.Count > 0 && ConceptResolver != null)
        {
            foreach (var partConceptName in partOfChildren)
            {
                // Find variables of this type that already exist as concept-typed vars
                // E.g., if "Canh" PART_OF "TamGiac" and TamGiac has variables "a: Canh, b: Canh, c: Canh"
                // then the sub-concept expansion is already handled by RC6.2
                // But if there are NO variables of this type, we auto-register hidden vars
                var existingVarsOfType = effective.Variables
                    .Where(v => v.Type.Equals(partConceptName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (existingVarsOfType.Count == 0)
                {
                    // Auto-create a single implicit variable for this part
                    var implicitVarName = partConceptName.ToLower();
                    if (!effective.Variables.Any(v => v.Name.Equals(implicitVarName, StringComparison.OrdinalIgnoreCase)))
                    {
                        effective.Variables.Add(new Variable { Name = implicitVarName, Type = partConceptName });
                    }
                }
            }
        }

        // (Phase 16) CONSTRUCT_RELATIONS expansion: inject relation knowledge
        if (effective.ConstructRelations.Count > 0 && RelationResolver != null)
        {
            foreach (var cr in effective.ConstructRelations)
            {
                var rel = RelationResolver(cr.RelationName);
                if (rel != null)
                {
                    // Map parameters to arguments
                    var binding = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < Math.Min(rel.ParamNames.Count, cr.Arguments.Count); i++)
                    {
                        binding[rel.ParamNames[i]] = cr.Arguments[i];
                    }

                    // Inject equations with substitution
                    foreach (var eq in rel.Equations)
                    {
                        var substitutedExpr = SubstituteVariables(eq.Expression, binding);
                        effective.Equations.Add(new Equation { Expression = substitutedExpr });
                    }

                    // Inject rules with substitution
                    foreach (var rule in rel.Rules)
                    {
                        effective.ConceptRules.Add(new ConceptRule
                        {
                            Kind = rule.Kind,
                            Hypothesis = rule.Hypothesis.Select(h => SubstituteVariables(h, binding)).ToList(),
                            Conclusion = rule.Conclusion.Select(c => SubstituteVariables(c, binding)).ToList()
                        });
                    }
                }
            }
        }

        return effective;
    }

    private string SubstituteVariables(string expression, Dictionary<string, string> binding)
    {
        var result = expression;
        foreach (var kvp in binding)
        {
            // Use regex to replace whole word only, allowing for dot notation access
            string pattern = $@"\b{Regex.Escape(kvp.Key)}\b";
            result = Regex.Replace(result, pattern, kvp.Value);
        }
        return result;
    }

    private bool IsConceptType(string type) => !new[] { "DECIMAL", "INT", "FLOAT", "DOUBLE", "BOOLEAN", "STRING" }.Contains(type.ToUpper());

    private Dictionary<string, double>? Solve2DEquationSystem(string expr1, string expr2, string var1, string var2, Dictionary<string, object> parameters)
    {
        var e1 = SplitEquation(expr1);
        var e2 = SplitEquation(expr2);
        Func<double, double, double> f1 = (x, y) => { var p = new Dictionary<string, object>(parameters) { [var1] = x, [var2] = y }; return Convert.ToDouble(EvaluateFormula(e1.left, p)) - Convert.ToDouble(EvaluateFormula(e1.right, p)); };
        Func<double, double, double> f2 = (x, y) => { var p = new Dictionary<string, object>(parameters) { [var1] = x, [var2] = y }; return Convert.ToDouble(EvaluateFormula(e2.left, p)) - Convert.ToDouble(EvaluateFormula(e2.right, p)); };

        double[] guesses = { 10.0, 1.0, 50.0, 0.1, -1.0, -10.0 }; // Added negative guesses
        foreach (var gx in guesses)
        {
            foreach (var gy in guesses)
            {
                double x = gx, y = gy;
                for (int iter = 0; iter < 50; iter++)
                {
                    double v1 = f1(x, y), v2 = f2(x, y);
                    if (Math.Abs(v1) < 1e-6 && Math.Abs(v2) < 1e-6) return new Dictionary<string, double> { { var1, x }, { var2, y } };
                    double h = 1e-7;
                    double j11 = (f1(x + h, y) - v1) / h, j12 = (f1(x, y + h) - v1) / h;
                    double j21 = (f2(x + h, y) - v2) / h, j22 = (f2(x, y + h) - v2) / h;
                    double det = j11 * j22 - j12 * j21;
                    if (Math.Abs(det) < 1e-12) break; // Jacobian is singular
                    
                    // Newton-Raphson update
                    double dx = (v2 * j12 - v1 * j22) / det;
                    double dy = (v1 * j21 - v2 * j11) / det;

                    x -= dx; // Corrected sign for Newton-Raphson update
                    y -= dy; // Corrected sign for Newton-Raphson update

                    if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) break;
                }
            }
        }
        return null;
    }

    private (string left, string right) SplitEquation(string expr)
    {
        var idx = expr.IndexOf('=');
        if (idx > 0 && expr[idx - 1] != '!' && expr[idx - 1] != '<' && expr[idx - 1] != '>')
            return (expr.Substring(0, idx).Trim(), expr.Substring(idx + 1).Trim());
        return (expr, "0"); // Default to expr = 0 if no simple '=' found
    }

    private double Solve1DEquation(string expr, string target, Dictionary<string, object> parameters, Action<string>? log = null)
    {
        var s = SplitEquation(expr);
        Func<double, double> f = (x) => { var p = new Dictionary<string, object>(parameters) { [target] = x }; return Convert.ToDouble(EvaluateFormula(s.left, p, log)) - Convert.ToDouble(EvaluateFormula(s.right, p, log)); };
        
        double lower = -1000, upper = 1000; // Default search range
        
        // Try to find a bracket with a sign change
        if (f(0) * f(10000) < 0) { lower = 0; upper = 10000; }
        else if (f(-10000) * f(0) < 0) { lower = -10000; upper = 0; }
        else {
            bool found = false;
            // Scan a wider range with larger steps
            for (double st = -100000; st < 100000; st += 1000)
            {
                try
                {
                    if (f(st) * f(st + 1000) <= 0)
                    {
                        lower = st;
                        upper = st + 1000;
                        found = true;
                        break;
                    }
                }
                catch { /* Evaluation might fail for some values, continue scanning */ }
            }
            if (!found) throw new Exception("No root found in extended range for 1D equation.");
        }
        return MathNet.Numerics.RootFinding.Brent.FindRoot(f, lower, upper);
    }

    private object EvaluateFormula(string formula, Dictionary<string, object> parameters, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(formula)) 
        {
            log?.Invoke("(DEBUG) EvaluateFormula received empty formula.");
            return 0.0;
        }

        // Replace custom operators with internal function calls
        string processed = PreProcessOperators(formula);

        // Replace base^exp with Pow(base, exp) - handle parenthesized bases like (x-y)^2
        var powSafe = new Regex(@"(\([^()]+\)|[a-zA-Z0-9_\.\[\]]+)\^([a-zA-Z0-9_\.\[\]]+)").Replace(processed, "Pow($1, $2)");
        // Wrap dot-notation variables (e.g., p1.x) with brackets ([p1.x])
        var safe = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z0-9_\.]+)\b").Replace(powSafe, "[$1]");
        
        if (string.IsNullOrWhiteSpace(safe)) return 0.0;

        var e = new NCalc.Expression(safe);
        foreach (var p in parameters) e.Parameters[p.Key] = p.Value;
        
        // (RC7) Hook into NCalc's EvaluateFunction to support custom functions
        e.EvaluateFunction += (name, args) => {
            // Handle Custom Operators (syntactic sugar)
            if (name.StartsWith("_op_"))
            {
                var symbol = GetSymbolFromOpName(name.Substring(4));
                var op = OperatorResolver?.Invoke(symbol);
                if (op != null)
                {
                    log?.Invoke($"(DEBUG) Found Custom Operator '{symbol}' with Body: '{op.Body}'");
                    var bodyParams = new Dictionary<string, object>();
                    var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                    // Match by a, b (assuming binary)
                    if (evalArgs.Length >= 2)
                    {
                        bodyParams["a"] = evalArgs[0];
                        bodyParams["b"] = evalArgs[1];
                    }
                    args.Result = EvaluateFormula(op.Body, bodyParams, log);
                    return;
                }
            }

            var func = FunctionResolver?.Invoke(name);
            if (func != null)
            {
                log?.Invoke($"(DEBUG) Found Custom Function '{name}' with Body: '{func.Body}'");
                var bodyParams = new Dictionary<string, object>();
                var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                for (int i = 0; i < func.Parameters.Count && i < evalArgs.Length; i++)
                {
                    bodyParams[func.Parameters[i].Name] = evalArgs[i];
                }
                var funcRes = EvaluateFormula(func.Body, bodyParams, log);
                log?.Invoke($"(DEBUG) Evaluated Custom Function '{name}'({string.Join(",", evalArgs)}) => {funcRes}");
                args.Result = funcRes;
            }
            else
            {
                log?.Invoke($"(DEBUG) Custom Function '{name}' NOT FOUND.");
            }
        };

        var res = e.Evaluate();
        log?.Invoke($"(DEBUG) EvaluateFormula('{safe}') => {res} (Type: {res?.GetType().Name})");
        return (res is int or long or double or float or decimal) ? Convert.ToDouble(res) : res;
    }

    private bool EvaluateConstraint(string expr, Dictionary<string, object> parameters)
    {
        // Replace custom operators
        string processed = PreProcessOperators(expr);

        // Convert single = to == for NCalc (avoiding >=, <=, etc.)
        var safe = new Regex(@"(?<![><!= ])=(?!=)").Replace(processed, "==");
        // Replace base^exp with Pow(base, exp)
        safe = new Regex(@"(\([^()]+\)|[a-zA-Z0-9_\.\[\]]+)\^([a-zA-Z0-9_\.\[\]]+)").Replace(safe, "Pow($1, $2)");
        // Wrap dot-notation variables
        safe = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z0-9_\.]+)\b").Replace(safe, "[$1]");
        
        var e = new NCalc.Expression(safe);
        foreach (var p in parameters) e.Parameters[p.Key] = p.Value;

        // (RC7) Support custom functions in constraints
        e.EvaluateFunction += (name, args) => {
            var func = FunctionResolver?.Invoke(name);
            if (func != null)
            {
                var bodyParams = new Dictionary<string, object>();
                var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                for (int i = 0; i < func.Parameters.Count && i < evalArgs.Length; i++)
                {
                    bodyParams[func.Parameters[i].Name] = evalArgs[i];
                }
                args.Result = EvaluateFormula(func.Body, bodyParams);
            }
        };

        var res = e.Evaluate();
        if (res is bool b) return b;
        if (res is int or long or double or float or decimal) return Convert.ToDouble(res) != 0;
        return false;
    }

    private string PreProcessOperators(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return expression;

        // Simple implementation for binary operators: a OP b -> _op_NAME(a, b)
        var symbolToName = new Dictionary<string, string> {
            { "#", "hash" }, { "@", "at" }, { "$", "dollar" }, { "&", "amp" }, 
            { "|", "pipe" }, { "!", "bang" }, { "~", "tilde" }, { "?", "question" }, { ":", "colon" }
        };
        
        var processed = expression;

        foreach (var kvp in symbolToName)
        {
            var sym = kvp.Key;
            var name = kvp.Value;
            // Regex to find 'left sym right' where sym is the operator
            var pattern = $@"(\b[a-zA-Z0-9_\[\]]+\b)\s*\{sym}\s*(\b[a-zA-Z0-9_\[\]]+\b)";
            var replacement = $"_op_{name}($1, $2)";
            processed = Regex.Replace(processed, pattern, replacement);
        }

        return processed; 
    }

    private string GetSymbolFromOpName(string name)
    {
        var nameToSymbol = new Dictionary<string, string> {
            { "hash", "#" }, { "at", "@" }, { "dollar", "$" }, { "amp", "&" }, 
            { "pipe", "|" }, { "bang", "!" }, { "tilde", "~" }, { "question", "?" }, { "colon", ":" }
        };
        return nameToSymbol.TryGetValue(name, out var sym) ? sym : name;
    }

    private List<string> ExtractVariablesFromExpression(string expression)
    {
        // Regex to find potential variable names (alphanumeric, underscore, dot-notation)
        var matches = new Regex(@"\b[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*\b").Matches(expression);
        var vars = new HashSet<string>();
        // List of common NCalc functions to exclude
        var funcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { 
            "Abs", "Acos", "Asin", "Atan", "Atan2", "Ceiling", "Cos", "Cosh", "Exp", "Floor", "Log", "Log10", 
            "Max", "Min", "Pow", "Round", "Sign", "Sin", "Sinh", "Sqrt", "Tan", "Tanh", "Truncate",
            "Iif", "In", "Contains", "Replace", "Substring", "Length", "ToUpper", "ToLower", "Trim", "IsNullOrEmpty",
            "DateTime", "DateAdd", "DateDiff", "TimeSpan"
        };
        foreach (Match m in matches)
        {
            // Exclude NCalc functions and numeric literals
            if (funcs.Contains(m.Value) || double.TryParse(m.Value, out _))
                continue;

            // (RC7) Exclude Custom Functions
            if (FunctionResolver?.Invoke(m.Value) != null)
                continue;

            // Basic check: if name is followed by '(', it's likely a function (even if not yet resolved)
            // This is a safety measure for yet-to-be-loaded functions or built-ins not in our static list.
            int index = m.Index + m.Length;
            while (index < expression.Length && char.IsWhiteSpace(expression[index])) index++;
            if (index < expression.Length && expression[index] == '(')
                continue;

            vars.Add(m.Value);
        }
        // result.Steps.Add($"  - (DEBUG) Extracted vars from '{expression}': [{string.Join(",", vars)}]");
        return vars.ToList();
    }
}
