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
    public class ReasoningResult
    {
        public bool Success { get; set; } = true;
        public Dictionary<string, object> DerivedFacts { get; set; } = new();
        public List<string> Steps { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public Func<string, Concept?>? ConceptResolver { get; set; }

    public ReasoningResult FindClosure(Concept concept, Dictionary<string, object> initialFacts, List<string> targets)
    {
        var result = new ReasoningResult();
        var knownFacts = new Dictionary<string, object>(initialFacts);
        var appliedRuleIds = new HashSet<Guid>();
        int stepCount = 0;

        result.Steps.Add($"Step {stepCount++}: Initializing GT (Known Facts) = {{ {string.Join(", ", initialFacts.Select(kv => $"{kv.Key}={kv.Value}"))} }}");
        if (targets.Any()) result.Steps.Add($"Goal KL (Targets) = {{ {string.Join(", ", targets)} }}");

        // (RC6.1) Resolve Hierarchy (IS-A)
        var effectiveConcept = GetEffectiveConcept(concept);
        
        result.Steps.Add($"Concept '{concept.Name}' ready. Stats: {effectiveConcept.Variables.Count} Vars, {effectiveConcept.SameVariables.Count} SameVars, {effectiveConcept.CompRels.Count} CompRels, {effectiveConcept.ConceptRules.Count} Rules, {effectiveConcept.Constraints.Count} Constraints, {effectiveConcept.Equations.Count} Equations");
        
        // (RC7/Early) Check initial constraints
        if (effectiveConcept.Constraints.Count > 0)
        {
            foreach (var constraint in effectiveConcept.Constraints)
            {
                try
                {
                    var needed = ExtractVariablesFromExpression(constraint.Expression);
                    if (needed.All(v => knownFacts.ContainsKey(v)))
                    {
                        var ok = EvaluateConstraint(constraint.Expression, knownFacts);
                        if (!ok)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"Input constraint violated: {constraint.Expression}.";
                            result.Steps.Add($"  ✗ Input constraint VIOLATED: {constraint.Expression}");
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
                    factAdded = true;
                }
                else if (knownFacts.ContainsKey(sv.Variable2) && !knownFacts.ContainsKey(sv.Variable1))
                {
                    var val = knownFacts[sv.Variable2];
                    knownFacts[sv.Variable1] = val;
                    result.DerivedFacts[sv.Variable1] = val;
                    result.Steps.Add($"Step {stepCount++}: From SameVariable {sv.Variable1} = {sv.Variable2} => {sv.Variable1} = {val}");
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
                                factAdded = true;
                            }
                        }
                    }
                }
            }

            // (RC5) Equation Solving
            foreach (var eq in effectiveConcept.Equations)
            {
                var unknownVars = eq.Variables.Where(v => !knownFacts.ContainsKey(v)).ToList();
                if (unknownVars.Count == 1)
                {
                    string targetVar = unknownVars[0];
                    try
                    {
                        var root = Solve1DEquation(eq.Expression, targetVar, knownFacts);
                        if (!double.IsNaN(root))
                        {
                            knownFacts[targetVar] = root;
                            result.DerivedFacts[targetVar] = root;
                            result.Steps.Add($"Step {stepCount++}: From Equation '{eq.Expression}' solved for {targetVar} => {root:F4}");
                            factAdded = true;
                        }
                    }
                    catch { } // Cannot solve yet, skip for now
                }
                else if (unknownVars.Count == 2)
                {
                    // Look for a pair of equations that share the same two unknowns
                    foreach (var eq2 in effectiveConcept.Equations)
                    {
                        if (eq == eq2) continue; // Don't pair an equation with itself
                        var unknowns2 = eq2.Variables.Where(v => !knownFacts.ContainsKey(v)).ToHashSet();
                        if (unknowns2.Count == 2 && unknowns2.SetEquals(unknownVars))
                        {
                            try
                            {
                                var sol = Solve2DEquationSystem(eq.Expression, eq2.Expression, unknownVars[0], unknownVars[1], knownFacts);
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
                                        result.Steps.Add($"Step {stepCount++}: From Equation System {{'{eq.Expression}', '{eq2.Expression}'}} solved for {{{string.Join(", ", unknownVars)}}} => {{{string.Join(", ", sol.Values.Select(v => v.ToString("F4")))}}}");
                                }
                            }
                            catch { } // Failed to solve system, skip
                        }
                    }
                }
            }

            // Early exit if all targets KL met
            if (targets.Any() && targets.All(t => knownFacts.ContainsKey(t)))
            {
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
                try
                {
                    var ok = EvaluateConstraint(constraint.Expression, knownFacts);
                    if (ok) result.Steps.Add($"  ✓ Constraint satisfied: {constraint.Expression}");
                    else
                    {
                        result.Steps.Add($"  ✗ Constraint VIOLATED: {constraint.Expression}");
                        result.Success = false;
                        result.ErrorMessage = $"Constraint violated: {constraint.Expression}";
                    }
                }
                catch { result.Steps.Add($"  ? Constraint skipped (missing vars): {constraint.Expression}"); } // Cannot evaluate, assume not violated for now
            }
        }

        if (targets.Any() && !targets.All(t => knownFacts.ContainsKey(t)))
        {
            result.Success = false;
            result.ErrorMessage = "Reasoning engine halted: FClosure(GT) exhausted without reaching Goal(KL).";
        }

        return result;
    }

    private Concept GetEffectiveConcept(Concept primary)
    {
        if (primary.BaseObjects.Count == 0) return primary;

        var effective = new Concept
        {
            Name = primary.Name,
            Variables = new List<Variable>(primary.Variables),
            Constraints = new List<KBMS.Models.Constraint>(primary.Constraints),
            CompRels = new List<ComputationRelation>(primary.CompRels),
            SameVariables = new List<SameVariable>(primary.SameVariables),
            ConceptRules = new List<ConceptRule>(primary.ConceptRules),
            Equations = new List<Equation>(primary.Equations)
        };

        foreach (var baseName in primary.BaseObjects)
        {
            var baseConcept = ConceptResolver?.Invoke(baseName);
            if (baseConcept != null)
            {
                var flattendBase = GetEffectiveConcept(baseConcept); // Recursively flatten base concepts
                // Merge base into effective (avoid duplicates for variables, add all others)
                effective.Variables.AddRange(flattendBase.Variables.Where(v => !effective.Variables.Any(ev => ev.Name == v.Name)));
                effective.Constraints.AddRange(flattendBase.Constraints);
                effective.CompRels.AddRange(flattendBase.CompRels);
                effective.SameVariables.AddRange(flattendBase.SameVariables);
                effective.ConceptRules.AddRange(flattendBase.ConceptRules);
                effective.Equations.AddRange(flattendBase.Equations);
            }
        }
        return effective;
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

    private double Solve1DEquation(string expr, string target, Dictionary<string, object> parameters)
    {
        var s = SplitEquation(expr);
        Func<double, double> f = (x) => { var p = new Dictionary<string, object>(parameters) { [target] = x }; return Convert.ToDouble(EvaluateFormula(s.left, p)) - Convert.ToDouble(EvaluateFormula(s.right, p)); };
        
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

    private object EvaluateFormula(string formula, Dictionary<string, object> parameters)
    {
        // Replace base^exp with Pow(base, exp) - handle parenthesized bases like (x-y)^2
        var powSafe = new Regex(@"(\([^()]+\)|[a-zA-Z0-9_\.\[\]]+)\^([a-zA-Z0-9_\.\[\]]+)").Replace(formula, "Pow($1, $2)");
        // Wrap dot-notation variables (e.g., p1.x) with brackets ([p1.x])
        var safe = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z0-9_\.]+)\b").Replace(powSafe, "[$1]");
        var e = new NCalc.Expression(safe);
        foreach (var p in parameters) e.Parameters[p.Key] = p.Value;
        var res = e.Evaluate();
        return (res is int or long or double or float or decimal) ? Convert.ToDouble(res) : res;
    }

    private bool EvaluateConstraint(string expr, Dictionary<string, object> parameters)
    {
        // Convert single = to == for NCalc (avoiding >=, <=, etc.)
        var safe = new Regex(@"(?<![><!= ])=(?!=)").Replace(expr, "==");
        // Replace base^exp with Pow(base, exp)
        safe = new Regex(@"(\([^()]+\)|[a-zA-Z0-9_\.\[\]]+)\^([a-zA-Z0-9_\.\[\]]+)").Replace(safe, "Pow($1, $2)");
        // Wrap dot-notation variables
        safe = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z0-9_\.]+)\b").Replace(safe, "[$1]");
        
        var e = new NCalc.Expression(safe);
        foreach (var p in parameters) e.Parameters[p.Key] = p.Value;
        var res = e.Evaluate();
        if (res is bool b) return b;
        if (res is int or long or double or float or decimal) return Convert.ToDouble(res) != 0;
        return false;
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
            if (!funcs.Contains(m.Value) && !double.TryParse(m.Value, out _))
            {
                vars.Add(m.Value);
            }
        }
        return vars.ToList();
    }
}
