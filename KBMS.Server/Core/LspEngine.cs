using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KBMS.Parser;
using KBMS.Storage.Core;

namespace KBMS.Server.Core;

public enum LspContext
{
    Global,
    ExpectCreateTarget,
    InConceptDefinition,
    InConceptVariables,
    ExpectDataType,
    InRuleDefinition,
    InRuleScope,
    InRuleIf,
    InRuleThen,
    ExpectConceptName,
    ExpectVariableName,
    AfterInsert,
    AfterInsertInto,
    AfterInsertValues,
    AfterSelect,
    AfterFrom,
    AfterWhere,
    AfterOrderBy,
    AfterGroupBy,
    AfterUpdate,
    AfterSet,
    AfterDelete,
    AfterDrop,
    AfterUse,
    AfterShow,
    AfterGrant,
    AfterRevoke,
    AfterFind,
    AfterFindConcept,
    AfterFindReturn,
    AfterFindWhere,
}

public class LspEngine
{
    private readonly ConceptCatalog _conceptCatalog;
    private readonly KbCatalog? _kbCatalog;

    public LspEngine(ConceptCatalog conceptCatalog, KbCatalog? kbCatalog = null)
    {
        _conceptCatalog = conceptCatalog;
        _kbCatalog = kbCatalog;
    }

    /// <summary>
    /// Validates the full document and returns syntax errors
    /// </summary>
    public object GetDiagnostics(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return new { valid = true, errors = Array.Empty<object>() };

        try
        {
            var parser = new KBMS.Parser.Parser(code) { StrictSemicolon = true };
            parser.ParseAll();
            return new { valid = true, errors = Array.Empty<object>() };
        }
        catch (ParserException ex)
        {
            var res = ex.Response;
            return new
            {
                valid = false,
                errors = new[]
                {
                    new { line = res.Line, column = res.Column, message = res.Message, length = 1 }
                }
            };
        }
        catch (Exception ex)
        {
            return new
            {
                valid = false,
                errors = new[]
                {
                    new { line = 1, column = 1, message = ex.Message, length = 1 }
                }
            };
        }
    }

    /// <summary>
    /// Provides context-aware auto-completion using keyword-chain analysis.
    /// Reads the last 1-3 tokens before the cursor to determine exactly what
    /// should come next (e.g. INSERT → INTO, CREATE → CONCEPT/RULE/...).
    /// </summary>
    public object GetCompletions(string code, int line, int column, string? currentKb = null)
    {
        try
        {
            // ── 1. Extract text before cursor ──────────────────────────────
            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string currentLineText = line <= lines.Length ? lines[line - 1] : "";
            int colIndex = Math.Min(column - 1, currentLineText.Length);
            string lineBeforeCursor = currentLineText.Substring(0, colIndex).TrimStart();

            // All significant uppercase tokens on this line before cursor
            var tokensBefore = lineBeforeCursor
                .Split(new[] { ' ', '\t', '(', ')', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToUpperInvariant())
                .ToList();

            string lastToken  = tokensBefore.Count >= 1 ? tokensBefore[^1] : "";
            string prevToken  = tokensBefore.Count >= 2 ? tokensBefore[^2] : "";
            string prev2Token = tokensBefore.Count >= 3 ? tokensBefore[^3] : "";

            // Current prefix (partial word the user is still typing)
            string prefix = "";
            int lastSep = lineBeforeCursor.LastIndexOfAny(new[] { ' ', '\t', '(', ')', ',', ';', '.' });
            if (lastSep >= 0 && lastSep < lineBeforeCursor.Length - 1)
                prefix = lineBeforeCursor.Substring(lastSep + 1);
            else if (lastSep < 0)
                prefix = lineBeforeCursor;

            // ── 2. Keyword-chain context detection ────────────────────────
            LspContext context = DetectKeywordContext(lastToken, prevToken, prev2Token, lineBeforeCursor, code);

            // ── 3. Extract concept scope from full code ───────────────────
            string? scopeConcept = null;
            var symbolMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ExtractScopeFromCode(code, symbolMap, ref scopeConcept);

            // ── 4. Dot accessor → suggest variables of prefixed concept ───
            if (lineBeforeCursor.TrimEnd().EndsWith("."))
            {
                context = LspContext.ExpectVariableName;
                // extract alias before dot
                var parts = lineBeforeCursor.TrimEnd().Split(new[] { ' ', '\t', '(', ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries);
                string dotPart = parts.LastOrDefault() ?? "";
                string varPrefix = dotPart.TrimEnd('.');
                if (!string.IsNullOrEmpty(varPrefix) &&
                    symbolMap.TryGetValue(varPrefix, out string? mappedConcept))
                    scopeConcept = mappedConcept;
                prefix = "";
            }

            // ── 5. Colon → data type ──────────────────────────────────────
            if (lineBeforeCursor.TrimEnd().EndsWith(":"))
                context = LspContext.ExpectDataType;

            // ── 6. Generate & filter ──────────────────────────────────────
            var suggestions = GenerateSuggestions(context, currentKb, scopeConcept);

            var filtered = suggestions
                .Where(s => string.IsNullOrEmpty(prefix) ||
                            s.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Kind == "Keyword" ? 0 : s.Kind == "Concept" ? 1 : 2)
                .ThenBy(s => s.Label)
                .ToList();

            return new { completions = filtered };
        }
        catch
        {
            return new { completions = Array.Empty<object>() };
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Keyword-chain → LspContext
    // Reads last 1-3 tokens on the current line to pick the best context.
    // ──────────────────────────────────────────────────────────────────────────
    private static LspContext DetectKeywordContext(
        string last, string prev, string prev2, string lineText, string fullCode)
    {
        // CREATE chain
        if (last == "CREATE")  return LspContext.ExpectCreateTarget;
        if (prev == "CREATE")
        {
            return last switch
            {
                "CONCEPT"   => LspContext.InConceptDefinition,
                "RULE"      => LspContext.InRuleDefinition,
                "KNOWLEDGE" => LspContext.ExpectCreateTarget,
                _           => LspContext.ExpectCreateTarget
            };
        }
        if (last == "BASE" && prev == "KNOWLEDGE" && prev2 == "CREATE")
            return LspContext.Global; // will add KB name next

        // CONCEPT body
        if (last is "VARIABLES" or "ATTRIBUTES" or "HIERARCHIES" or "CONSTRAINTS")
            return LspContext.InConceptDefinition;

        // INSERT → INTO → <concept>
        if (last == "INSERT")                               return LspContext.AfterInsert;
        if (last == "INTO" && prev == "INSERT")            return LspContext.AfterInsertInto;
        if (last == "VALUES")                              return LspContext.AfterInsertValues;

        // SELECT → <cols/star> → FROM → <concept> → WHERE → <vars>
        if (last == "SELECT")                              return LspContext.AfterSelect;
        if (last == "FROM")                                return LspContext.AfterFrom;

        // WHERE / AND / OR  (works for both SELECT and FIND)
        if (last is "WHERE" or "AND" or "OR" or "NOT")    return LspContext.AfterWhere;

        // ORDER BY / GROUP BY
        if (last == "ORDER")                               return LspContext.AfterWhere; // next word = BY
        if (last == "BY" && prev == "ORDER")              return LspContext.AfterOrderBy;
        if (last == "GROUP")                               return LspContext.AfterWhere;
        if (last == "BY" && prev == "GROUP")              return LspContext.AfterGroupBy;

        // HAVING
        if (last == "HAVING")                              return LspContext.AfterWhere;

        // UPDATE → <concept> → SET → <vars>
        if (last == "UPDATE")                              return LspContext.AfterUpdate;
        if (last == "SET")                                 return LspContext.AfterSet;

        // DELETE → FROM
        if (last == "DELETE")                              return LspContext.AfterDelete;

        // DROP → <type>
        if (last == "DROP")                                return LspContext.AfterDrop;

        // USE → <kb>
        if (last == "USE")                                 return LspContext.AfterUse;

        // SHOW → <target>
        if (last == "SHOW")                                return LspContext.AfterShow;

        // GRANT / REVOKE
        if (last == "GRANT")                               return LspContext.AfterGrant;
        if (last == "REVOKE")                              return LspContext.AfterRevoke;
        if (last == "ON" && (prev == "GRANT" || prev == "REVOKE")) return LspContext.ExpectConceptName;

        // FIND → <concept> → WITH → <vars> → RETURN → <vars>
        if (last == "FIND")                                return LspContext.AfterFind;
        if (last == "WITH")                                return LspContext.AfterWhere;
        if (last == "RETURN")                              return LspContext.AfterFindReturn;
        if (prev == "FIND" || prev2 == "FIND")             return LspContext.AfterFindConcept;
        // FIND ConceptName WITH → variables
        if (last == "WITH" || (prev == "WITH" && last != "RETURN")) return LspContext.AfterFindWhere;

        // RULE body
        if (last == "SCOPE")                               return LspContext.InRuleScope;
        if (last == "IF")                                  return LspContext.InRuleIf;
        if (last == "THEN")                                return LspContext.InRuleThen;

        return LspContext.Global;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scan full code for FROM/JOIN/FIND/UPDATE/INTO to build alias→concept map
    // ──────────────────────────────────────────────────────────────────────────
    private static void ExtractScopeFromCode(
        string code, Dictionary<string, string> symbolMap, ref string? scopeConcept)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "JOIN", "WHERE", "ON", "SET", "INNER", "LEFT", "RIGHT", "OUTER", "IF", "THEN" };

        var listRx = new Regex(@"(?:SCOPE|FROM|JOIN|UPDATE|INTO|FIND)\s+([\s\S]*?)(?:IF|WHERE|SET|WITH|;|$)", RegexOptions.IgnoreCase);
        foreach (Match m in listRx.Matches(code))
        {
            string listStr = m.Groups[1].Value;
            var parts = listStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var tokens = p.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 1)
                {
                    string concept = tokens[0];
                    if (skip.Contains(concept)) continue;
                    string alias = tokens.Length >= 2 ? tokens[1] : concept;
                    if (alias.Equals("AS", StringComparison.OrdinalIgnoreCase) && tokens.Length >= 3)
                        alias = tokens[2];
                    if (skip.Contains(alias)) alias = concept;
                    symbolMap[alias] = concept;
                    symbolMap[concept] = concept;
                    scopeConcept ??= concept;
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Suggestion sets per context
    // ──────────────────────────────────────────────────────────────────────────
    private List<CompletionItem> GenerateSuggestions(LspContext ctx, string? kb, string? scopeConcept)
    {
        var s = new List<CompletionItem>();

        switch (ctx)
        {
            // ── Global: top-level statements ─────────────────────────────
            case LspContext.Global:
                AddKeywords(s, "CREATE", "DROP", "FIND", "INSERT", "USE", "SHOW",
                               "EXPORT", "IMPORT", "SELECT", "UPDATE", "DELETE",
                               "GRANT", "REVOKE", "BEGIN", "COMMIT", "ROLLBACK", "MAINTAIN");
                break;

            // ── CREATE target ─────────────────────────────────────────────
            case LspContext.ExpectCreateTarget:
                AddKeywords(s, "CONCEPT", "RULE", "RELATION", "FUNCTION",
                               "OPERATOR", "TRIGGER", "KNOWLEDGE BASE");
                break;

            // ── CONCEPT body ──────────────────────────────────────────────
            case LspContext.InConceptDefinition:
                AddKeywords(s, "VARIABLES", "ATTRIBUTES", "HIERARCHIES",
                               "CONSTRAINTS", "RULES", "DESCRIPTION");
                break;

            // ── Data types ────────────────────────────────────────────────
            case LspContext.ExpectDataType:
                foreach (var t in new[] { "STRING","INT","DECIMAL","BOOLEAN","DATETIME","OBJECT","TEXT","BIGINT","FLOAT" })
                    s.Add(new CompletionItem(t, "Type", $"{t} data type"));
                break;

            // ── INSERT ────────────────────────────────────────────────────
            case LspContext.AfterInsert:
                AddKeywords(s, "INTO");
                break;

            case LspContext.AfterInsertInto:
                AddConceptNames(s, kb);
                break;

            case LspContext.AfterInsertValues:
                AddKeywords(s, "VALUES", "SET");
                break;

            // ── SELECT ────────────────────────────────────────────────────
            case LspContext.AfterSelect:
                AddKeywords(s, "*", "FROM", "DISTINCT");
                AddConceptVariables(s, kb, scopeConcept);
                break;

            case LspContext.AfterFrom:
                AddConceptNames(s, kb);
                break;

            // ── WHERE / AND / OR ──────────────────────────────────────────
            case LspContext.AfterWhere:
            case LspContext.AfterFindWhere:
            case LspContext.ExpectVariableName:
                AddConceptVariables(s, kb, scopeConcept);
                AddKeywords(s, "AND", "OR", "NOT", "IS", "IN", "LIKE", "BETWEEN", "NULL");
                foreach (var fn in KBMS.Models.BuiltInFunctions.MathFunctions)
                    s.Add(new CompletionItem(fn, "Function", "Math function"));
                foreach (var fn in KBMS.Models.BuiltInFunctions.AggregateFunctions)
                    s.Add(new CompletionItem(fn, "Function", "Aggregate function"));
                foreach (var fn in new[] { "IS_STUCK", "HAS_FIRED", "IS_DEDUCED", "TOTAL_COST" })
                    s.Add(new CompletionItem(fn, "Function", "Inference status function"));
                foreach (var macro in KBMS.Models.BuiltInFunctions.SystemMacros)
                    s.Add(new CompletionItem(macro, "Macro", "System macro"));
                AddModelConstants(s, kb);
                break;

            // ── ORDER BY / GROUP BY ───────────────────────────────────────
            case LspContext.AfterOrderBy:
            case LspContext.AfterGroupBy:
                AddConceptVariables(s, kb, scopeConcept);
                AddKeywords(s, "ASC", "DESC");
                break;

            // ── UPDATE / SET ──────────────────────────────────────────────
            case LspContext.AfterUpdate:
                AddConceptNames(s, kb);
                break;

            case LspContext.AfterSet:
                AddConceptVariables(s, kb, scopeConcept);
                break;

            // ── DELETE ────────────────────────────────────────────────────
            case LspContext.AfterDelete:
                AddKeywords(s, "FROM");
                break;

            // ── DROP ──────────────────────────────────────────────────────
            case LspContext.AfterDrop:
                AddKeywords(s, "CONCEPT", "RULE", "RELATION", "FUNCTION",
                               "OPERATOR", "TRIGGER", "INDEX", "KNOWLEDGE BASE");
                break;

            // ── USE ───────────────────────────────────────────────────────
            case LspContext.AfterUse:
                AddKbNames(s);
                break;

            // ── SHOW ──────────────────────────────────────────────────────
            case LspContext.AfterShow:
                AddKeywords(s, "CONCEPTS", "RULES", "KNOWLEDGE BASES", "RELATIONS",
                               "FUNCTIONS", "OPERATORS", "HIERARCHIES", "USERS",
                               "SESSIONS", "PRIVILEGES", "TRIGGERS", "INDEXES");
                break;

            // ── GRANT / REVOKE ────────────────────────────────────────────
            case LspContext.AfterGrant:
                AddKeywords(s, "READ", "WRITE", "ADMIN", "ALL", "ON");
                break;

            case LspContext.AfterRevoke:
                AddKeywords(s, "ON", "ALL");
                AddConceptNames(s, kb);
                break;

            // ── FIND ──────────────────────────────────────────────────────
            case LspContext.AfterFind:
            case LspContext.ExpectConceptName:
                AddConceptNames(s, kb);
                break;
                
            case LspContext.AfterFindConcept:
                AddKeywords(s, "WITH", "RETURN");
                break;
                
            case LspContext.AfterFindReturn:
                AddKeywords(s, "*");
                AddConceptVariables(s, kb, scopeConcept);
                foreach (var fn in new[] { "AUDIT_TRAIL", "AUDIT_LOG", "MISSING_FACTS", "GENERATED_VARIABLES", "EXPLAIN_TREE" })
                    s.Add(new CompletionItem(fn, "Function", "Explainability function"));
                break;

            // ── RULE body ─────────────────────────────────────────────────
            case LspContext.InRuleDefinition:
                AddKeywords(s, "SCOPE", "IF", "PRIORITY");
                break;

            case LspContext.InRuleScope:
                AddConceptNames(s, kb);
                break;

            case LspContext.InRuleIf:
                AddConceptVariables(s, kb, scopeConcept);
                AddKeywords(s, "AND", "OR", "NOT", "THEN", "IS", "IN", "NULL");
                foreach (var fn in KBMS.Models.BuiltInFunctions.MathFunctions)
                    s.Add(new CompletionItem(fn, "Function", "Math function"));
                AddModelConstants(s, kb);
                break;

            case LspContext.InRuleThen:
                AddKeywords(s, "SET", "DO", "AND");
                AddConceptVariables(s, kb, scopeConcept);
                break;

            default:
                AddKeywords(s, "CREATE", "DROP", "FIND", "INSERT", "SELECT",
                               "UPDATE", "DELETE", "SHOW", "USE", "GRANT", "REVOKE");
                break;
        }

        return s;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void AddKeywords(List<CompletionItem> list, params string[] keywords)
    {
        foreach (var kw in keywords)
            list.Add(new CompletionItem(kw, "Keyword", "Keyword"));
    }

    private void AddConceptNames(List<CompletionItem> list, string? kb)
    {
        if (string.IsNullOrEmpty(kb)) return;
        try
        {
            foreach (var c in _conceptCatalog.ListConcepts(kb))
                list.Add(new CompletionItem(c.Name, "Concept", $"Concept in {kb}"));
        }
        catch { }
    }

    private void AddKbNames(List<CompletionItem> list)
    {
        if (_kbCatalog == null) return;
        try
        {
            foreach (var kb in _kbCatalog.ListKbs())
                list.Add(new CompletionItem(kb.Name, "Database", "Knowledge Base"));
        }
        catch { }
    }

    private void AddConceptVariables(List<CompletionItem> list, string? kb, string? conceptName)
    {
        if (string.IsNullOrEmpty(kb) || string.IsNullOrEmpty(conceptName)) return;
        try
        {
            var vars = GetEffectiveVariables(kb, conceptName);
            foreach (var v in vars)
                list.Add(new CompletionItem(v.Name, "Variable", $"{v.Type} variable"));
        }
        catch { }
    }

    /// <summary>
    /// Recursively collects all variables a concept has, including those inherited
    /// from BASE_OBJECTS (parent concepts). Deduplication by name.
    /// </summary>
    private List<KBMS.Models.Variable> GetEffectiveVariables(string kb, string conceptName, int depth = 0)
    {
        if (depth > 8) return new(); // guard against circular references
        var concept = _conceptCatalog.ListConcepts(kb)
            .FirstOrDefault(c => c.Name.Equals(conceptName, StringComparison.OrdinalIgnoreCase));
        if (concept == null) return new();

        var result = new List<KBMS.Models.Variable>(concept.Variables);

        foreach (var baseObj in concept.BaseObjects)
        {
            var inherited = GetEffectiveVariables(kb, baseObj, depth + 1);
            foreach (var iv in inherited)
            {
                if (!result.Any(r => r.Name.Equals(iv.Name, StringComparison.OrdinalIgnoreCase)))
                    result.Add(iv);
            }
        }
        return result;
    }

    private void AddModelConstants(List<CompletionItem> list, string? kbName)
    {
        if (string.IsNullOrEmpty(kbName) || _kbCatalog == null) return;
        try
        {
            var kb = _kbCatalog.LoadKb(kbName);
            if (kb?.Constants != null)
            {
                foreach (var c in kb.Constants)
                    list.Add(new CompletionItem(c.Name, "Constant", $"Type: {c.Type}, Value: {c.Value}"));
            }
        }
        catch { }
    }

    private class CompletionItem
    {
        public string Label  { get; }
        public string Kind   { get; }
        public string Detail { get; }

        public CompletionItem(string label, string kind, string detail)
        {
            Label  = label;
            Kind   = kind;
            Detail = detail;
        }
    }
}
