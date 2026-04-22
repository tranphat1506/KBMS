using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using KBMS.Models;
using NCalc; 
using System.Text.RegularExpressions;
using KBMS.Reasoning.Rete;

namespace KBMS.Reasoning;

public class InferenceEngine
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, NCalc.Expression> _expressionCache = new();
    
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
        public Dictionary<string, object> DerivedFacts { get; set; } = new();
        public List<string> Steps { get; set; } = new();
        public List<DerivationTrace> Traces { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public Func<string, Concept?>? ConceptResolver { get; set; }
    public Func<string, Function?>? FunctionResolver { get; set; }
    public Func<string, Operator?>? OperatorResolver { get; set; }
    public Func<string, List<string>>? HierarchyResolver { get; set; }
    public Func<string, List<string>>? PartOfResolver { get; set; }
    public Func<string, Relation?>? RelationResolver { get; set; }

    public ReasoningResult FindClosure(Concept concept, Dictionary<string, object> initialFacts, List<string> targetVariables)
    {
        var result = new ReasoningResult();
        var knownFacts = new Dictionary<string, object>(initialFacts);
        int stepCount = 0;

        var startTime = DateTime.UtcNow;
        var timeoutMs = 5000;

        result.Steps.Add($"Step {stepCount++}: Initializing reasoning for '{concept.Name}'");

        var effectiveConcept = GetEffectiveConcept(concept);
        var network = new ReteNetwork();
        network.Logger = (msg) => {
            if (msg.StartsWith("Rule ")) result.Steps.Add(msg);
            else result.Steps.Add($"[Rete] {msg}");
        };
        var compiler = new ReteCompiler(this, network);
        compiler.Compile(effectiveConcept);

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
                    var goalVar = targetVariables.FirstOrDefault(v => !knownFacts.ContainsKey(v));
                    throw new Exception($"Circular dependency: {goalVar ?? "unknown"}");
                }
                visited.Add(stateKey);

                foreach (var fact in knownFacts)
                {
                    var existing = network.WorkingMemory.FirstOrDefault(f => f.Name.Equals(fact.Key, StringComparison.OrdinalIgnoreCase));
                    if (existing == null || !ValuesEqual(existing.Value, fact.Value))
                    {
                        network.AssertFact(fact.Key, fact.Value);
                    }
                }

                int countBefore = network.WorkingMemory.Count;
                while (network.FireNext()) { }
                if (network.WorkingMemory.Count > countBefore) factAddedThisTurn = true;

                foreach (var fact in network.WorkingMemory.ToList())
                {
                    bool isNew = !knownFacts.ContainsKey(fact.Name);
                    bool isDifferent = !isNew && !ValuesEqual(knownFacts[fact.Name], fact.Value);

                    if (isNew || isDifferent)
                    {
                        var variable = effectiveConcept.Variables.FirstOrDefault(v => v.Name.Equals(fact.Name, StringComparison.OrdinalIgnoreCase));
                        var castedVal = CastToVariableType(fact.Value, variable);

                        knownFacts[fact.Name] = castedVal;
                        result.DerivedFacts[fact.Name] = castedVal;
                        factAddedThisTurn = true;
                        
                        if (isDifferent) result.Steps.Add($"Step {stepCount++}: Updated [{fact.Name}] = {castedVal}");
                        else result.Steps.Add($"Step {stepCount++}: Derived [{fact.Name}] = {castedVal}");
                    }
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
                                var subFacts = new Dictionary<string, object>();
                                var prefix = variable.Name + ".";
                                foreach (var fact in knownFacts.ToList())
                                {
                                    if (fact.Key.StartsWith(prefix))
                                        subFacts[fact.Key.Substring(prefix.Length)] = fact.Value;
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
                                            network.AssertFact(fullKey, derived.Value);
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
                        result.ErrorMessage = $"Constraint violated: {constraint.Expression}";
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

        return result;
    }

    private bool ResolveGoal(string goal, Concept concept, Dictionary<string, object> facts, HashSet<string> stack, ReasoningResult result)
    {
        if (facts.ContainsKey(goal)) return true;
        if (stack.Contains(goal)) throw new Exception($"Circular dependency: {goal}");
        
        stack.Add(goal);
        try {
            var candidateRules = concept.ConceptRules.Where(r => r.Conclusion.Any(c => GetConcludedVariable(c) == goal)).ToList();
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
            } catch { return double.NaN; }
        };

        double[] quickTests = { 0, 1, 10, 100, -1, -10, -100 };
        foreach (var t in quickTests)
        {
            var fv = f(t);
            if (!double.IsNaN(fv) && Math.Abs(fv) < 1e-6) return t;
        }

        double lower = -1000, upper = 1000;
        bool found = false;
        double step = 100;
        for (double st = -1000; st <= 1000 && !found; st += step)
        {
            var f1 = f(st);
            var f2 = f(st + step);
            if (!double.IsNaN(f1) && !double.IsNaN(f2) && f1 * f2 <= 0) { lower = st; upper = st + step; found = true; }
        }

        if (!found) return double.NaN;
        try { return MathNet.Numerics.RootFinding.Brent.FindRoot(f, lower, upper, 1e-6, 100); }
        catch { return double.NaN; }
    }

    public object EvaluateFormula(string formula, Dictionary<string, object> parameters, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(formula)) return 0.0;
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
                        if (evalArgs.Length >= 1) bp["a"] = evalArgs[0];
                        if (evalArgs.Length >= 2) bp["b"] = evalArgs[1];
                        args.Result = EvaluateFormula(op.Body, bp, log);
                        return;
                    }
                }
                var func = FunctionResolver?.Invoke(name);
                if (func != null)
                {
                    var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                    var bp = new Dictionary<string, object>();
                    for (int i = 0; i < func.Parameters.Count && i < evalArgs.Length; i++) bp[func.Parameters[i].Name] = evalArgs[i];
                    args.Result = EvaluateFormula(func.Body, bp, log);
                }
            };
        }

        foreach (var p in parameters) {
            if (p.Value is decimal d) e.Parameters[p.Key] = (double)d;
            else if (p.Value is long l) e.Parameters[p.Key] = (double)l;
            else if (p.Value is int i) e.Parameters[p.Key] = (double)i;
            else e.Parameters[p.Key] = p.Value;
        }

        try {
            var res = e.Evaluate();
            return (res is int or long or double or float or decimal) ? Convert.ToDouble(res) : res;
        } catch {
            return 0.0;
        }
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

    private bool ValuesEqual(object? v1, object? v2)
    {
        if (v1 == null && v2 == null) return true;
        if (v1 == null || v2 == null) return false;
        if (IsNumeric(v1) && IsNumeric(v2)) return Math.Abs(Convert.ToDouble(v1) - Convert.ToDouble(v2)) < 1e-7;
        return v1.ToString() == v2.ToString();
    }
    private bool IsNumeric(object v) => v is int or long or double or decimal or float;

    public bool EvaluateConstraint(string expr, Dictionary<string, object> parameters)
    {
        var safe = _eqRegex.Replace(PreProcessOperators(expr), "==");
        safe = _powRegex.Replace(safe, "Pow($1, $2)");
        safe = _dotRegex.Replace(safe, "[$1]");
        
        var e = new NCalc.Expression(safe);
        foreach (var p in parameters) {
            if (p.Value is decimal d) e.Parameters[p.Key] = (double)d;
            else if (p.Value is long l) e.Parameters[p.Key] = (double)l;
            else if (p.Value is int i) e.Parameters[p.Key] = (double)i;
            else e.Parameters[p.Key] = p.Value;
        }

        e.EvaluateFunction += (name, args) => {
            var func = FunctionResolver?.Invoke(name);
            if (func != null) {
                var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                var bp = new Dictionary<string, object>();
                for (int i = 0; i < func.Parameters.Count && i < evalArgs.Length; i++) bp[func.Parameters[i].Name] = evalArgs[i];
                args.Result = EvaluateFormula(func.Body, bp);
            }
        };

        try {
            var res = e.Evaluate();
            if (res is bool b) return b;
            if (res is int or long or double or float or decimal) return Convert.ToDouble(res) != 0;
            return false;
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
        var cleaned = Regex.Replace(expression.Trim('\''), @"'[^']*'|""[^""]*""", " ");
        var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var funcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { 
            "Abs", "Acos", "Asin", "Atan", "Atan2", "Ceiling", "Cos", "Cosh", "Exp", "Floor", "Log", "Log10", 
            "Max", "Min", "Pow", "Round", "Sign", "Sin", "Sinh", "Sqrt", "Tan", "Tanh", "Truncate", "if", "and", "or", "not"
        };

        foreach (Match m in Regex.Matches(cleaned, @"\b[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*\b"))
        {
            var val = m.Value;
            if (funcs.Contains(val) || val.Equals("true") || val.Equals("false") || double.TryParse(val, out _)) continue;
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
        var t = conclusion.Trim(' ', '(', ')').Replace("SET ", "", StringComparison.OrdinalIgnoreCase);
        var idx = t.IndexOfAny(new[] { '=', ':' });
        bool isAssignment = idx > 0 && (idx == t.Length - 1 || (t[idx - 1] != '!' && t[idx - 1] != '<' && t[idx - 1] != '>'));
        if (isAssignment && idx < t.Length - 1 && t[idx+1] == '=') isAssignment = false;

        if (isAssignment) {
            var varName = t.Substring(0, idx).Trim();
            var valRaw = EvaluateFormula(t.Substring(idx + 1).Trim(), knownFacts);
            var castedVal = CastToVariableType(valRaw, concept.Variables.FirstOrDefault(v => v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase)));
            if (knownFacts.ContainsKey(varName) && ValuesEqual(knownFacts[varName], castedVal)) return false;
            knownFacts[varName] = castedVal;
            result.DerivedFacts[varName] = castedVal;
            if (stepNumber.HasValue) result.Steps.Add($"Step {stepNumber}: From Rule [{ruleKind}] => {varName} = {castedVal}");
            return true;
        }
        knownFacts[t] = true;
        result.DerivedFacts[t] = true;
        return true;
    }
}
