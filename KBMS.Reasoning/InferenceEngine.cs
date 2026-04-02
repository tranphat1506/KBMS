using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using KBMS.Models;
using NCalc; // We will need NCalc or a similar math evaluator for parsing formulas
using System.Text.RegularExpressions;
using KBMS.Reasoning.Rete;

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
        int stepCount = 0;

        result.Steps.Add($"Step {stepCount++}: Initializing reasoning for '{concept.Name}'");

        // (RC6) Identify inherited knowledge (tri-knowledge: C, H, R)
        var effectiveConcept = GetEffectiveConcept(concept);

        // Initialize Rete Network once for this concept
        var network = new ReteNetwork();
        network.Logger = (msg) => result.Steps.Add($"[Rete] {msg}");
        var compiler = new ReteCompiler(this, network);
        compiler.Compile(effectiveConcept);

        int iteration = 0;
        try
        {
            while (iteration < 2000)
            {
                bool factAddedThisTurn = false;

                // 1. Assert Current Known Facts into Rete
                foreach (var fact in knownFacts)
                {
                    network.AssertFact(fact.Key, fact.Value);
                }

                // 2. Fire Rete Rules/Equations
                while (network.FireNext())
                {
                    // FireNext triggers actions that update network.WorkingMemory
                }

                // 3. Sync Rete Memory -> Engine Memory
                foreach (var fact in network.WorkingMemory.ToList())
                {
                    if (!knownFacts.ContainsKey(fact.Name))
                    {
                        knownFacts[fact.Name] = fact.Value;
                        result.DerivedFacts[fact.Name] = fact.Value;
                        factAddedThisTurn = true;
                    }
                }

                // 4. Recursive Closure for Nested Concepts (RC6.2)
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
                                    
                                    // Merge steps for debugging
                                    foreach(var step in subResult.Steps) 
                                        result.Steps.Add($"  [{variable.Name}] {step}");

                                    foreach (var derived in subResult.DerivedFacts)
                                    {
                                        var fullKey = prefix + derived.Key;
                                        if (!knownFacts.ContainsKey(fullKey))
                                        {
                                            // Assert back into parent Rete network
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

        // Final Constraints Check (RC7)
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
                        result.ErrorMessage = $"Constraint violated: {constraint.Expression}";
                        result.Steps.Add($"  ✗ Constraint VIOLATED: {constraint.Expression}");
                    }
                }
            }
            catch { }
        }

        if (targetVariables.Count > 0 && !targetVariables.All(v => knownFacts.ContainsKey(v)))
        {
            result.Success = false;
            result.ErrorMessage = "Target variables not reached.";
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
        Func<double, double, double> f1 = (x, y) => { 
            var p = new Dictionary<string, object>(parameters) { [var1] = x, [var2] = y }; 
            var resLeft = EvaluateFormula(e1.left, p);
            var left = resLeft != null ? Convert.ToDouble(resLeft) : 0.0;
            var resRight = EvaluateFormula(e1.right, p);
            var right = resRight != null ? Convert.ToDouble(resRight) : 0.0;
            return left - right;
        };
        Func<double, double, double> f2 = (x, y) => { 
            var p = new Dictionary<string, object>(parameters) { [var1] = x, [var2] = y }; 
            var resLeft = EvaluateFormula(e2.left, p);
            var left = resLeft != null ? Convert.ToDouble(resLeft) : 0.0;
            var resRight = EvaluateFormula(e2.right, p);
            var right = resRight != null ? Convert.ToDouble(resRight) : 0.0;
            return left - right;
        };

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

    public double Solve1DEquation(string expr, string target, Dictionary<string, object> parameters, Action<string>? log = null)
    {
        var s = SplitEquation(expr);
        Func<double, double> f = (x) => { 
            var p = new Dictionary<string, object>(parameters) { [target] = x }; 
            var res = EvaluateFormula(s.left, p, log);
            var left = res != null ? Convert.ToDouble(res) : 0.0;
            var resRight = EvaluateFormula(s.right, p, log);
            var right = resRight != null ? Convert.ToDouble(resRight) : 0.0;
            return left - right;
        };
        
        double lower = -1000, upper = 1000; // Default search range
        
        // Try to find a bracket with a sign change
        if (f(0) * f(10000) < 0) { lower = 0; upper = 10000; }
        else if (f(-10000) * f(0) < 0) { lower = -10000; upper = 0; }
        else 
        {
            bool found = false;
            // Scan a wider range with adaptive steps
            double[] scanRanges = { 100, 1000, 10000, 100000, 1000000 };
            foreach (var range in scanRanges)
            {
                for (double st = -range; st < range; st += range / 100)
                {
                    try
                    {
                        if (f(st) * f(st + (range / 100)) <= 0)
                        {
                            lower = st;
                            upper = st + (range / 100);
                            found = true;
                            break;
                        }
                    }
                    catch { }
                }
                if (found) break;
            }

            if (!found) throw new Exception($"No root found in extended range for 1D equation: {expr}");
        }
        return MathNet.Numerics.RootFinding.Brent.FindRoot(f, lower, upper, 1e-8);
    }

    private double Factorial(double n)
    {
        if (n < 0) return double.NaN;
        if (n > 170) return double.PositiveInfinity; // Limit for double
        return MathNet.Numerics.SpecialFunctions.Factorial((int)n);
    }

    public object EvaluateFormula(string formula, Dictionary<string, object> parameters, Action<string>? log = null)
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
            // Handle Factorial
            if (name.Equals("Factorial", StringComparison.OrdinalIgnoreCase))
            {
                var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                if (evalArgs.Length >= 1)
                {
                    double n = Convert.ToDouble(evalArgs[0]);
                    args.Result = Factorial(n);
                    return;
                }
            }

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
        return res;
    }

    public object CastToVariableType(object? val, KBMS.Models.Variable? variable)
    {
        if (val == null || variable == null) return val ?? 0.0;

        var type = variable.Type.ToUpper();
        try
        {
            if (type is "INT" or "INTEGER" or "LONG")
            {
                return Convert.ToInt64(val);
            }
            if (type is "DECIMAL" or "MONEY" or "NUMBER")
            {
                // Use rounding to clean up floating point noise from 'double' calculations
                // If scale is specified, use it. Otherwise use 10 as a sensible default.
                decimal dec;
                if (val is double d)
                {
                    // Round to 12 digits first to eliminate noise like .99999999999
                    dec = Convert.ToDecimal(Math.Round(d, 12));
                }
                else
                {
                    dec = Convert.ToDecimal(val);
                }

                if (variable.Scale.HasValue)
                {
                    dec = Math.Round(dec, variable.Scale.Value);
                }
                else
                {
                    // Default cleaning of decimal noise
                    dec = Math.Round(dec, 10);
                }
                return dec;
            }
            if (type is "FLOAT" or "DOUBLE")
            {
                return Convert.ToDouble(val);
            }
            if (type is "BOOL" or "BOOLEAN")
            {
                if (val is bool b) return b;
                if (val is string s) return bool.TryParse(s, out var bres) ? bres : s.Equals("1") || s.Equals("true", StringComparison.OrdinalIgnoreCase);
                return Convert.ToDouble(val) != 0;
            }
            if (type is "STRING" or "VARCHAR" or "TEXT")
            {
                var s = val?.ToString() ?? "";
                if (s.StartsWith("'") && s.EndsWith("'") && s.Length >= 2)
                    s = s.Substring(1, s.Length - 2).Replace("''", "'");
                return s;
            }
        }
        catch { }
        return val;
    }

    public bool EvaluateConstraint(string expr, Dictionary<string, object> parameters)
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
            // Handle Factorial
            if (name.Equals("Factorial", StringComparison.OrdinalIgnoreCase))
            {
                var evalArgs = args.EvaluateParameters(System.Threading.CancellationToken.None);
                if (evalArgs.Length >= 1)
                {
                    double n = Convert.ToDouble(evalArgs[0]);
                    args.Result = Factorial(n);
                    return;
                }
            }

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

    public List<string> ExtractVariablesFromExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return new List<string>();

        // Strip string literals to avoid matching variable names inside quotes
        var cleaned = Regex.Replace(expression, @"'[^']*'", " ");
        cleaned = Regex.Replace(cleaned, @"""[^""]*""", " ");

        var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var funcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { 
            "Abs", "Acos", "Asin", "Atan", "Atan2", "Ceiling", "Cos", "Cosh", "Exp", "Floor", "Log", "Log10", 
            "Max", "Min", "Pow", "Round", "Sign", "Sin", "Sinh", "Sqrt", "Tan", "Tanh", "Truncate",
            "Factorial", "Iif", "In", "Contains", "Replace", "Substring", "Length", "ToUpper", "ToLower", "Trim", "IsNullOrEmpty",
            "if", "and", "or", "not"
        };

        var matches = Regex.Matches(cleaned, @"\b[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*\b");

        foreach (Match m in matches)
        {
            var val = m.Value;
            if (funcs.Contains(val)) continue;
            if (val.Equals("true", StringComparison.OrdinalIgnoreCase) || val.Equals("false", StringComparison.OrdinalIgnoreCase) || val.Equals("null", StringComparison.OrdinalIgnoreCase))
                continue;

            if (FunctionResolver?.Invoke(val) != null) continue;

            int peekIdx = m.Index + m.Length;
            while (peekIdx < cleaned.Length && char.IsWhiteSpace(cleaned[peekIdx])) peekIdx++;
            if (peekIdx < cleaned.Length && cleaned[peekIdx] == '(') continue;

            if (double.TryParse(val, out _)) continue;

            vars.Add(val);
        }
        return vars.ToList();
    }

    // --- Helper methods for expression parsing and evaluation ---

    private string? GetConcludedVariable(string conclusion)
    {
        var trimmed = conclusion.Trim();
        while (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
            trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();

        if (trimmed.StartsWith("SET ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(4).Trim();

        var eqIdx = trimmed.IndexOfAny(new[] { '=', ':' });
        // Ensure it's not a comparison operator like ==, <=, >=, !=
        if (eqIdx > 0 && (eqIdx == trimmed.Length - 1 || (trimmed[eqIdx - 1] != '!' && trimmed[eqIdx - 1] != '<' && trimmed[eqIdx - 1] != '>')))
        {
            // Check if NEXT char is NOT =
            if (eqIdx < trimmed.Length - 1 && trimmed[eqIdx + 1] == '=') return null;

            return trimmed.Substring(0, eqIdx).Trim();
        }
        return null;
    }

    public bool ApplyConclusion(string conclusion, Concept concept, Dictionary<string, object> knownFacts, ReasoningResult result, string ruleKind, int? stepNumber = null)
    {
        var trimmed = conclusion.Trim();
        while (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
            trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();

        if (trimmed.StartsWith("SET ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(4).Trim();

        var eqIdx = trimmed.IndexOfAny(new[] { '=', ':' });
        bool isAssignment = eqIdx > 0 && (eqIdx == trimmed.Length - 1 || (trimmed[eqIdx - 1] != '!' && trimmed[eqIdx - 1] != '<' && trimmed[eqIdx - 1] != '>'));
        if (isAssignment && eqIdx < trimmed.Length - 1 && trimmed[eqIdx+1] == '=') isAssignment = false; // == is comparison

        if (isAssignment)
        {
            var varName = trimmed.Substring(0, eqIdx).Trim();
            var exprStr = trimmed.Substring(eqIdx + 1).Trim();

            try
            {
                var valRaw = EvaluateFormula(exprStr, knownFacts);
                var variable = concept.Variables.FirstOrDefault(v => v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                var castedVal = CastToVariableType(valRaw, variable);

                knownFacts[varName] = castedVal;
                result.DerivedFacts[varName] = castedVal;
                
                if (stepNumber.HasValue)
                    result.Steps.Add($"Step {stepNumber}: From Rule [{ruleKind}] => {varName} = {castedVal}");
                
                return true;
            }
            catch (Exception ex)
            {
                result.Steps.Add($"  ! Failed to evaluate conclusion [{conclusion}]: {ex.Message}");
                return false;
            }
        }
        else
        {
            // Boolean flag
            var flagName = trimmed;
            knownFacts[flagName] = true;
            result.DerivedFacts[flagName] = true;
            if (stepNumber.HasValue)
                result.Steps.Add($"Step {stepNumber}: From Rule [{ruleKind}] => {flagName} = true");
            return true;
        }
    }
}
