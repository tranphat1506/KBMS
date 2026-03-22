using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KBMS.Models;
using KBMS.Network;

namespace KBMS.CLI;

/// <summary>
/// Parser and formatter for server responses - MySQL-like plain text output
/// </summary>
public static class ResponseParser
{
    private static List<string>? _streamingColumns;
    private static Dictionary<string, int>? _streamingWidths;
    private static int _streamingCount;
    private static string? _streamingMode;
    /// <summary>
    /// Display a result message with plain text table formatting (MySQL-like)
    /// </summary>
    public static void DisplayResult(Message message, string? currentCommand = null)
    {
        if (message.Content.StartsWith("LOGIN_SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            // Special handling for login success
            var resultParts = message.Content.Split(':');
            Console.WriteLine($"Logged in as {resultParts[1]} ({resultParts[2]})");
            return;
        }

        if (message.Content == "LOGOUT_SUCCESS")
        {
            Console.WriteLine("Logged out successfully.");
            return;
        }

        // Handle USE command specially
        if (currentCommand != null && currentCommand.StartsWith("USE", StringComparison.OrdinalIgnoreCase))
        {
            var parts = currentCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                Console.WriteLine($"Database changed: {parts[1]}");
            }
            return;
        }

        // Try to parse as JSON and display as table
        try
        {
            switch (message.Type)
            {
                case MessageType.METADATA:
                    DisplayMetadata(message.Content);
                    break;
                case MessageType.ROW:
                    DisplayRow(message.Content);
                    break;
                case MessageType.FETCH_DONE:
                    DisplayFetchDone(message.Content);
                    break;
                default:
                    var jsonDoc = JsonDocument.Parse(message.Content);
                    DisplayJsonAsTable(jsonDoc);
                    break;
            }
        }
        catch (JsonException)
        {
            // Not valid JSON, display as-is (plain text message)
            Console.WriteLine(message.Content);
        }
    }

    private static void DisplayMetadata(string content)
    {
        try
        {
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            _streamingColumns = root.GetProperty("Columns").EnumerateArray().Select(e => e.GetString() ?? "").ToList();
            _streamingWidths = new Dictionary<string, int>();
            _streamingCount = 0;
            _streamingMode = root.TryGetProperty("ConceptName", out var cn) ? cn.GetString() : null;

            bool hasWidths = root.TryGetProperty("Widths", out var widthsProp);

            foreach (var col in _streamingColumns)
            {
                if (hasWidths && widthsProp.TryGetProperty(col, out var wProp) && wProp.ValueKind == JsonValueKind.Number)
                {
                    _streamingWidths[col] = Math.Max(col.Length, wProp.GetInt32());
                }
                else
                {
                    _streamingWidths[col] = Math.Max(col.Length, 15); // Default width 15
                }
            }

            if (!IsVerticalMode(_streamingMode))
            {
                var headerParts = _streamingColumns.Select(c => Pad(c, _streamingWidths[c]));
                Console.WriteLine("\n| " + string.Join(" | ", headerParts) + " |");
                Console.WriteLine("|" + string.Join("+", _streamingColumns.Select(c => new string('-', _streamingWidths[c] + 2))) + "|");
            }
        }
        catch { Console.WriteLine("Error parsing metadata"); }
    }

    private static void DisplayRow(string content)
    {
        if (_streamingColumns == null || _streamingWidths == null) return;

        try
        {
            var doc = JsonDocument.Parse(content);
            var values = doc.RootElement;
            _streamingCount++;

            if (IsVerticalMode(_streamingMode))
            {
                Console.WriteLine($"*************************** {_streamingCount}. row ***************************");
                int maxColLen = _streamingColumns.Max(c => c.Length) + 1;
                var padSpaces = new string(' ', maxColLen + 1);
                foreach (var col in _streamingColumns)
                {
                    var valStr = values.TryGetProperty(col, out var val) ? GetDisplayString(val) : "NULL";
                    valStr = valStr.Replace("\n", "\n" + padSpaces);
                    Console.WriteLine($"{PadLeft(col + ":", maxColLen)} {valStr}");
                }
                return;
            }

            // Normal table mode with multi-line cell support
            var colLines = _streamingColumns.Select(c => 
            {
                string valStr = values.TryGetProperty(c, out var val) ? GetDisplayString(val) : "NULL";
                return valStr.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            }).ToList();

            int maxLines = colLines.Count > 0 ? colLines.Max(lines => lines.Length) : 0;
            if (maxLines == 0) maxLines = 1;

            for (int i = 0; i < maxLines; i++)
            {
                var rowParts = new List<string>();
                for (int j = 0; j < _streamingColumns.Count; j++)
                {
                    string cellLine = i < colLines[j].Length ? colLines[j][i] : "";
                    rowParts.Add(Pad(cellLine, _streamingWidths[_streamingColumns[j]]));
                }
                Console.WriteLine("| " + string.Join(" | ", rowParts) + " |");
            }
            
            // Draw a bottom border for each row so multi-line rows are clearly separated
            Console.WriteLine("|" + string.Join("+", _streamingColumns.Select(c => new string('-', _streamingWidths[c] + 2))) + "|");
        }
        catch { /* Skip malformed rows */ }
    }

    private static void DisplayFetchDone(string content)
    {
        try
        {
            var doc = JsonDocument.Parse(content);
            var executionTime = doc.RootElement.TryGetProperty("executionTime", out var et) ? et.GetDouble().ToString("F2") : "0.00";
            
            Console.WriteLine($"{_streamingCount} row(s) in set ({executionTime} sec)");
            
            // Reset for next possible stream
            _streamingColumns = null;
            _streamingWidths = null;
            _streamingMode = null;
        }
        catch { }
    }

    private static bool IsVerticalMode(string? mode)
    {
        if (string.IsNullOrEmpty(mode)) return false;
        return mode.StartsWith("Describe_", StringComparison.OrdinalIgnoreCase) || 
               mode.StartsWith("Explain_", StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Display JSON data as MySQL-like table
    /// </summary>
    private static void DisplayJsonAsTable(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        // Get execution time if available
        var executionTime = "0.00";
        if (root.TryGetProperty("executionTime", out var etProp))
        {
            executionTime = etProp.ValueKind == JsonValueKind.Number
                ? etProp.GetDouble().ToString("F2")
                : "0.00";
        }

        // Check if it's an error response
        if (root.TryGetProperty("error", out var errorProp))
        {
            DisplayErrorPlain(errorProp.GetString() ?? "Unknown error");
            return;
        }
        // Check for success message
        if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
        {
            if (root.TryGetProperty("message", out var messageProp))
            {
                Console.WriteLine(messageProp.GetString());
                return;
            }

            // Display count message if available
            if (root.TryGetProperty("count", out var countProp))
            {
                var count = countProp.GetInt32();
                if (count == 0)
                {
                    Console.WriteLine($"Empty set ({executionTime} sec)");
                    return;
                }
            }
        }

        // Handle different data types
        if (root.TryGetProperty("objects", out var objectsProp))
        {
            DisplayObjectsAsTable(objectsProp, executionTime);
        }
        else if (root.TryGetProperty("knowledgeBases", out var kbsProp))
        {
            DisplaySimpleListAsTable(kbsProp, "Database", executionTime);
        }
        else if (root.TryGetProperty("concepts", out var conceptsProp))
        {
            DisplayConceptsAsTable(conceptsProp, executionTime);
        }
        else if (root.TryGetProperty("concept", out var conceptProp))
        {
            DisplayConceptDetail(conceptProp, executionTime);
        }
        else if (root.TryGetProperty("rules", out var rulesProp))
        {
            DisplayRulesAsTable(rulesProp, executionTime);
        }
        else if (root.TryGetProperty("relations", out var relationsProp))
        {
            DisplayRelationsAsTable(relationsProp, executionTime);
        }
        else if (root.TryGetProperty("operators", out var operatorsProp))
        {
            DisplayOperatorsAsTable(operatorsProp, executionTime);
        }
        else if (root.TryGetProperty("functions", out var functionsProp))
        {
            DisplayFunctionsAsTable(functionsProp, executionTime);
        }
        else if (root.TryGetProperty("hierarchies", out var hierarchiesProp))
        {
            DisplayHierarchiesAsTable(hierarchiesProp, executionTime);
        }
        else if (root.TryGetProperty("users", out var usersProp))
        {
            DisplayUsersAsTable(usersProp, executionTime);
        }
        else if (root.TryGetProperty("privileges", out var privilegesProp))
        {
            DisplayPrivilegesAsTable(privilegesProp, executionTime);
        }
        else if (root.TryGetProperty("groups", out var groupsProp))
        {
            DisplayGroupsAsTable(groupsProp, executionTime);
        }
        else if (root.TryGetProperty("aggregates", out var aggregatesProp))
        {
            DisplayAggregatesAsTable(aggregatesProp, executionTime);
        }
        else if (root.TryGetProperty("Success", out _) && root.TryGetProperty("DerivedFacts", out var derivedFactsProp))
        {
            // Handling Reasoning Engine Result (SOLVE)
            DisplaySolveResult(root, executionTime);
        }
        else
        {
            // Fallback: display JSON as indented
            var options = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(root, options));
        }
    }

    private static void DisplayObjectsAsTable(JsonElement objects, string executionTime = "0.00")
    {
        if (objects.ValueKind == JsonValueKind.Null || !objects.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var objectArray = objects.EnumerateArray().ToList();
        
        // Find all dynamic column names from the "Values" dictionary across all objects
        var columns = new HashSet<string>();
        foreach (var obj in objectArray)
        {
            if (obj.TryGetProperty("Values", out var valuesProp) && valuesProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in valuesProp.EnumerateObject())
                {
                    columns.Add(prop.Name);
                }
            }
        }
        var columnList = columns.ToList();

        if (columnList.Count == 0)
        {
            // Fallback to old behavior if no "Values" found (e.g., grouped queries that don't return standard Objects)
            columnList = objectArray[0].EnumerateObject()
                .Select(p => p.Name)
                .Where(c => !string.Equals(c, "Id", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(c, "KbId", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (columnList.Count == 0)
            {
                Console.WriteLine($"Empty set ({executionTime} sec)");
                return;
            }
            
            var widthsFallback = new Dictionary<string, int>();
            foreach (var col in columnList)
            {
                var maxWidth = col.Length;
                foreach (var obj in objectArray)
                {
                    if (obj.TryGetProperty(col, out var value))
                    {
                        var str = GetDisplayString(value);
                        maxWidth = Math.Max(maxWidth, str.Length);
                    }
                }
                widthsFallback[col] = maxWidth;
            }

            var headerPartsFallback = columnList.Select(c => Pad(c, widthsFallback[c]));
            Console.WriteLine("| " + string.Join(" | ", headerPartsFallback) + " |");
            Console.WriteLine("|" + string.Join("+", columnList.Select(c => new string('-', widthsFallback[c] + 2))) + "|");

            foreach (var obj in objectArray)
            {
                var rowParts = columnList.Select(c =>
                {
                    if (obj.TryGetProperty(c, out var value))
                        return Pad(GetDisplayString(value), widthsFallback[c]);
                    return Pad("NULL", widthsFallback[c]);
                });
                Console.WriteLine("| " + string.Join(" | ", rowParts) + " |");
            }

            Console.WriteLine($"{objectArray.Count} row(s) in set ({executionTime} sec)");
            return;
        }

        // Calculate column widths for "Values" extraction
        var widths = new Dictionary<string, int>();
        foreach (var col in columnList)
        {
            var maxWidth = col.Length;
            foreach (var obj in objectArray)
            {
                if (obj.TryGetProperty("Values", out var valuesProp) && valuesProp.TryGetProperty(col, out var value))
                {
                    var str = GetDisplayString(value);
                    maxWidth = Math.Max(maxWidth, str.Length);
                }
            }
            widths[col] = maxWidth;
        }

        // Print header
        var headerParts = columnList.Select(c => Pad(c, widths[c]));
        Console.WriteLine("| " + string.Join(" | ", headerParts) + " |");
        Console.WriteLine("|" + string.Join("+", columnList.Select(c => new string('-', widths[c] + 2))) + "|");

        // Print rows
        foreach (var obj in objectArray)
        {
            var rowParts = columnList.Select(c =>
            {
                if (obj.TryGetProperty("Values", out var valuesProp) && valuesProp.TryGetProperty(c, out var value))
                {
                    return Pad(GetDisplayString(value), widths[c]);
                }
                return Pad("NULL", widths[c]);
            });
            Console.WriteLine("| " + string.Join(" | ", rowParts) + " |");
        }

        Console.WriteLine($"{objectArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static void DisplaySimpleListAsTable(JsonElement items, string itemName, string executionTime = "0.00")
    {
        if (items.ValueKind == JsonValueKind.Null || !items.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var names = items.EnumerateArray()
            .Select(e => e.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : e.GetString() ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var maxWidth = names.Count > 0 ? Math.Max(itemName.Length, names.Max(n => n.Length)) : itemName.Length;

        // Print header
        Console.WriteLine($"| {Pad(itemName, maxWidth)} |");
        Console.WriteLine($"|{new string('-', maxWidth + 2)}|");

        // Print rows
        foreach (var name in names)
        {
            Console.WriteLine($"| {Pad(name ?? "", maxWidth)} |");
        }

        Console.WriteLine($"{names.Count} row(s) in set ({executionTime} sec)");
    }

    private static void DisplayConceptsAsTable(JsonElement concepts, string executionTime = "0.00")
    {
        if (concepts.ValueKind == JsonValueKind.Null || !concepts.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var conceptArray = concepts.EnumerateArray().ToList();
        var nameWidth = "Name".Length;
        var variablesWidth = "Variables".Length;
        var aliasesWidth = "Aliases".Length;
        var basesWidth = "BaseObjects".Length;

        foreach (var c in conceptArray)
        {
            if (c.TryGetProperty("Name", out var name))
                nameWidth = Math.Max(nameWidth, (name.GetString() ?? "").Length);
            
            var vars = GetVariablesString(c);
            variablesWidth = Math.Max(variablesWidth, vars.Length);
            
            var aliases = GetStringArrayProperty(c, "Aliases");
            aliasesWidth = Math.Max(aliasesWidth, aliases.Length);
            
            var bases = GetStringArrayProperty(c, "BaseObjects");
            basesWidth = Math.Max(basesWidth, bases.Length);
        }

        Console.WriteLine($"| {Pad("Name", nameWidth)} | {Pad("Variables", variablesWidth)} | {Pad("Aliases", aliasesWidth)} | {Pad("BaseObjects", basesWidth)} |");
        var separator = "+" + new string('-', nameWidth + 2) + "+" +
                      new string('-', variablesWidth + 2) + "+" +
                      new string('-', aliasesWidth + 2) + "+" +
                      new string('-', basesWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var c in conceptArray)
        {
            var name = c.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var vars = GetVariablesString(c);
            var aliases = GetStringArrayProperty(c, "Aliases");
            var bases = GetStringArrayProperty(c, "BaseObjects");
            
            Console.WriteLine($"| {Pad(name, nameWidth)} | {Pad(vars, variablesWidth)} | {Pad(aliases, aliasesWidth)} | {Pad(bases, basesWidth)} |");
        }

        Console.WriteLine($"{conceptArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static string GetStringArrayProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var arrayProp) && arrayProp.ValueKind == JsonValueKind.Array)
        {
            var items = arrayProp.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s));
            return string.Join(", ", items);
        }
        return "";
    }

    private static string GetVariablesString(JsonElement concept)
    {
        if (concept.TryGetProperty("Variables", out var varsProp) && varsProp.ValueKind == JsonValueKind.Array)
        {
            var items = varsProp.EnumerateArray().Select(v => v.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "").Where(s => !string.IsNullOrEmpty(s));
            return string.Join(", ", items);
        }
        return "";
    }

    private static void DisplayConceptDetail(JsonElement concept, string executionTime = "0.00")
    {
        if (concept.ValueKind == JsonValueKind.Null)
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        Console.WriteLine("Field          | Type");
        Console.WriteLine("---------------+---------------");

        if (concept.TryGetProperty("Variables", out var variables))
        {
            foreach (var v in variables.EnumerateArray())
            {
                var name = v.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                var type = v.TryGetProperty("Type", out var t) ? t.GetString() ?? "" : "";
                Console.WriteLine($"{Pad(name, 14)} | {type}");
            }
        }

        Console.WriteLine($"1 row in set ({executionTime} sec)");
    }

    private static void DisplayRulesAsTable(JsonElement rules, string executionTime = "0.00")
    {
        if (rules.ValueKind == JsonValueKind.Null || !rules.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var ruleArray = rules.EnumerateArray().ToList();
        var nameWidth = "Name".Length;
        var typeWidth = "Type".Length;
        var scopeWidth = "Scope".Length;
        var ifWidth = "If".Length;
        var thenWidth = "Then".Length;
        var costWidth = "Cost".Length;

        foreach (var r in ruleArray)
        {
            if (r.TryGetProperty("Name", out var name))
                nameWidth = Math.Max(nameWidth, (name.GetString() ?? "").Length);
            if (r.TryGetProperty("RuleType", out var type))
                typeWidth = Math.Max(typeWidth, (type.GetString() ?? "").Length);
            if (r.TryGetProperty("Scope", out var scope))
                scopeWidth = Math.Max(scopeWidth, (scope.GetString() ?? "").Length);
            
            var ifStr = GetExpressionListString(r, "Hypothesis");
            ifWidth = Math.Max(ifWidth, ifStr.Length);
            
            var thenStr = GetExpressionListString(r, "Conclusion");
            thenWidth = Math.Max(thenWidth, thenStr.Length);
            
            if (r.TryGetProperty("Cost", out var cost))
                costWidth = Math.Max(costWidth, cost.GetRawText().Length);
        }

        Console.WriteLine($"| {Pad("Name", nameWidth)} | {Pad("Type", typeWidth)} | {Pad("Scope", scopeWidth)} | {Pad("If", ifWidth)} | {Pad("Then", thenWidth)} | {Pad("Cost", costWidth)} |");
        var separator = "+" + new string('-', nameWidth + 2) + "+" +
                      new string('-', typeWidth + 2) + "+" +
                      new string('-', scopeWidth + 2) + "+" +
                      new string('-', ifWidth + 2) + "+" +
                      new string('-', thenWidth + 2) + "+" +
                      new string('-', costWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var r in ruleArray)
        {
            var name = r.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var type = r.TryGetProperty("RuleType", out var typeProp) ? typeProp.GetString() ?? "" : "";
            var scope = r.TryGetProperty("Scope", out var scopeProp) ? scopeProp.GetString() ?? "" : "";
            var ifStr = GetExpressionListString(r, "Hypothesis");
            var thenStr = GetExpressionListString(r, "Conclusion");
            var cost = r.TryGetProperty("Cost", out var costProp) ? costProp.GetRawText() : "";
            
            Console.WriteLine($"| {Pad(name, nameWidth)} | {Pad(type, typeWidth)} | {Pad(scope, scopeWidth)} | {Pad(ifStr, ifWidth)} | {Pad(thenStr, thenWidth)} | {Pad(cost, costWidth)} |");
        }

        Console.WriteLine($"{ruleArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static string GetExpressionListString(JsonElement rule, string propertyName)
    {
        if (rule.TryGetProperty(propertyName, out var exprArray) && exprArray.ValueKind == JsonValueKind.Array)
        {
            var items = exprArray.EnumerateArray().Select(e => e.TryGetProperty("Content", out var c) ? c.GetString() ?? "" : "").Where(s => !string.IsNullOrEmpty(s));
            return string.Join(", ", items);
        }
        return "";
    }

    private static void DisplayFunctionsAsTable(JsonElement functions, string executionTime = "0.00")
    {
        if (functions.ValueKind == JsonValueKind.Null || !functions.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var funcArray = functions.EnumerateArray().ToList();
        var nameWidth = "Name".Length;
        var returnTypeWidth = "Returns".Length;
        var paramsWidth = "Params".Length;
        var bodyWidth = "Body".Length;

        foreach (var f in funcArray)
        {
            if (f.TryGetProperty("Name", out var name))
                nameWidth = Math.Max(nameWidth, (name.GetString() ?? "").Length);
            if (f.TryGetProperty("ReturnType", out var ret))
                returnTypeWidth = Math.Max(returnTypeWidth, (ret.GetString() ?? "").Length);
                
            var pStr = GetFunctionParamsString(f);
            paramsWidth = Math.Max(paramsWidth, pStr.Length);
            
            if (f.TryGetProperty("Body", out var body))
                bodyWidth = Math.Max(bodyWidth, (body.GetString() ?? "").Length);
        }

        Console.WriteLine($"| {Pad("Name", nameWidth)} | {Pad("Params", paramsWidth)} | {Pad("Returns", returnTypeWidth)} | {Pad("Body", bodyWidth)} |");
        var separator = "+" + new string('-', nameWidth + 2) + "+" +
                      new string('-', paramsWidth + 2) + "+" +
                      new string('-', returnTypeWidth + 2) + "+" +
                      new string('-', bodyWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var f in funcArray)
        {
            var name = f.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var pStr = GetFunctionParamsString(f);
            var ret = f.TryGetProperty("ReturnType", out var retProp) ? retProp.GetString() ?? "" : "";
            var body = f.TryGetProperty("Body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            
            Console.WriteLine($"| {Pad(name, nameWidth)} | {Pad(pStr, paramsWidth)} | {Pad(ret, returnTypeWidth)} | {Pad(body, bodyWidth)} |");
        }

        Console.WriteLine($"{funcArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static string GetFunctionParamsString(JsonElement func)
    {
        if (func.TryGetProperty("Parameters", out var paramArray) && paramArray.ValueKind == JsonValueKind.Array)
        {
            var items = paramArray.EnumerateArray()
                .Select(p => 
                {
                    var type = p.TryGetProperty("Type", out var tProp) ? tProp.GetString() ?? "" : "";
                    var name = p.TryGetProperty("Name", out var nProp) ? nProp.GetString() ?? "" : "";
                    return $"{type} {name}".Trim();
                })
                .Where(s => !string.IsNullOrEmpty(s));
            return string.Join(", ", items);
        }
        return "";
    }

    private static void DisplayRelationsAsTable(JsonElement relations, string executionTime = "0.00")
    {
        if (relations.ValueKind == JsonValueKind.Null || !relations.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var relArray = relations.EnumerateArray().ToList();
        var nameWidth = "Name".Length;
        var domainWidth = "Domain".Length;
        var rangeWidth = "Range".Length;
        var propertiesWidth = "Properties".Length;

        foreach (var r in relArray)
        {
            if (r.TryGetProperty("Name", out var name))
                nameWidth = Math.Max(nameWidth, (name.GetString() ?? "").Length);
            if (r.TryGetProperty("Domain", out var domain))
                domainWidth = Math.Max(domainWidth, (domain.GetString() ?? "").Length);
            if (r.TryGetProperty("Range", out var range))
                rangeWidth = Math.Max(rangeWidth, (range.GetString() ?? "").Length);
                
            var props = GetStringArrayProperty(r, "Properties");
            propertiesWidth = Math.Max(propertiesWidth, props.Length);
        }

        Console.WriteLine($"| {Pad("Name", nameWidth)} | {Pad("Domain", domainWidth)} | {Pad("Range", rangeWidth)} | {Pad("Properties", propertiesWidth)} |");
        var separator = "+" + new string('-', nameWidth + 2) + "+" +
                      new string('-', domainWidth + 2) + "+" +
                      new string('-', rangeWidth + 2) + "+" +
                      new string('-', propertiesWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var r in relArray)
        {
            var name = r.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var domain = r.TryGetProperty("Domain", out var domainProp) ? domainProp.GetString() ?? "" : "";
            var range = r.TryGetProperty("Range", out var rangeProp) ? rangeProp.GetString() ?? "" : "";
            var props = GetStringArrayProperty(r, "Properties");
            
            Console.WriteLine($"| {Pad(name, nameWidth)} | {Pad(domain, domainWidth)} | {Pad(range, rangeWidth)} | {Pad(props, propertiesWidth)} |");
        }

        Console.WriteLine($"{relArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static void DisplayOperatorsAsTable(JsonElement operators, string executionTime = "0.00")
    {
        if (operators.ValueKind == JsonValueKind.Null || !operators.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var opArray = operators.EnumerateArray().ToList();
        var symbolWidth = "Symbol".Length;
        var paramsWidth = "Params".Length;
        var returnsWidth = "Returns".Length;
        var propertiesWidth = "Properties".Length;

        foreach (var o in opArray)
        {
            if (o.TryGetProperty("Symbol", out var symbol))
                symbolWidth = Math.Max(symbolWidth, (symbol.GetString() ?? "").Length);
                
            var paramTypes = GetStringArrayProperty(o, "ParamTypes");
            paramsWidth = Math.Max(paramsWidth, paramTypes.Length);
            
            if (o.TryGetProperty("ReturnType", out var ret))
                returnsWidth = Math.Max(returnsWidth, (ret.GetString() ?? "").Length);
                
            var props = GetStringArrayProperty(o, "Properties");
            propertiesWidth = Math.Max(propertiesWidth, props.Length);
        }

        Console.WriteLine($"| {Pad("Symbol", symbolWidth)} | {Pad("Params", paramsWidth)} | {Pad("Returns", returnsWidth)} | {Pad("Properties", propertiesWidth)} |");
        var separator = "+" + new string('-', symbolWidth + 2) + "+" +
                      new string('-', paramsWidth + 2) + "+" +
                      new string('-', returnsWidth + 2) + "+" +
                      new string('-', propertiesWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var o in opArray)
        {
            var symbol = o.TryGetProperty("Symbol", out var symbolProp) ? symbolProp.GetString() ?? "" : "";
            var paramTypes = GetStringArrayProperty(o, "ParamTypes");
            var ret = o.TryGetProperty("ReturnType", out var retProp) ? retProp.GetString() ?? "" : "";
            var props = GetStringArrayProperty(o, "Properties");
            
            Console.WriteLine($"| {Pad(symbol, symbolWidth)} | {Pad(paramTypes, paramsWidth)} | {Pad(ret, returnsWidth)} | {Pad(props, propertiesWidth)} |");
        }

        Console.WriteLine($"{opArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static void DisplayHierarchiesAsTable(JsonElement hierarchies, string executionTime = "0.00")
    {
        if (hierarchies.ValueKind == JsonValueKind.Null || !hierarchies.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var hArray = hierarchies.EnumerateArray().ToList();
        var parentWidth = "Parent".Length;
        var childWidth = "Child".Length;
        var typeWidth = "Type".Length;

        foreach (var h in hArray)
        {
            if (h.TryGetProperty("Parent", out var parent))
                parentWidth = Math.Max(parentWidth, (parent.GetString() ?? "").Length);
            if (h.TryGetProperty("Child", out var child))
                childWidth = Math.Max(childWidth, (child.GetString() ?? "").Length);
            if (h.TryGetProperty("HierarchyType", out var type))
                typeWidth = Math.Max(typeWidth, (type.GetString() ?? "").Length);
        }

        Console.WriteLine($"| {Pad("Parent", parentWidth)} | {Pad("Child", childWidth)} | {Pad("Type", typeWidth)} |");
        var separator = "+" + new string('-', parentWidth + 2) + "+" +
                      new string('-', childWidth + 2) + "+" +
                      new string('-', typeWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var h in hArray)
        {
            var parent = h.TryGetProperty("Parent", out var parentProp) ? parentProp.GetString() ?? "" : "";
            var child = h.TryGetProperty("Child", out var childProp) ? childProp.GetString() ?? "" : "";
            var type = h.TryGetProperty("HierarchyType", out var typeProp) ? typeProp.GetString() ?? "" : "";
            Console.WriteLine($"| {Pad(parent, parentWidth)} | {Pad(child, childWidth)} | {Pad(type, typeWidth)} |");
        }

        Console.WriteLine($"{hArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static void DisplayUsersAsTable(JsonElement users, string executionTime = "0.00")
    {   
        if (users.ValueKind == JsonValueKind.Null || !users.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var userArray = users.EnumerateArray().ToList();
        var usernameWidth = "Username".Length;
        var roleWidth = "Role".Length;

        foreach (var u in userArray)
        {
            if (u.TryGetProperty("Username", out var username))
                usernameWidth = Math.Max(usernameWidth, (username.GetString() ?? "").Length);
            if (u.TryGetProperty("Role", out var role))
                roleWidth = Math.Max(roleWidth, (role.GetString() ?? "").Length);
        }

        Console.WriteLine($"| {Pad("Username", usernameWidth)} | {Pad("Role", roleWidth)} |");
        var separator = "+" + new string('-', usernameWidth + 2) + "+" +
                      new string('-', roleWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var u in userArray)
        {
            var username = u.TryGetProperty("Username", out var usernameProp) ? usernameProp.GetString() ?? "" : "";
            var role = u.TryGetProperty("Role", out var roleProp) ? roleProp.GetString() ?? "" : "";
            Console.WriteLine($"| {Pad(username, usernameWidth)} | {Pad(role, roleWidth)} |");
        }

        Console.WriteLine($"{userArray.Count} row(s) in set ({executionTime} sec)");
    }

    private static void DisplayPrivilegesAsTable(JsonElement privileges, string executionTime = "0.00")
    {
        if (privileges.ValueKind == JsonValueKind.Object)
        {
            var items = privileges.EnumerateObject().ToList();
            if (items.Count == 0)
            {
                Console.WriteLine($"Empty set ({executionTime} sec)");
                return;
            }

            var keyWidth = items.Max(i => Math.Max("User".Length, i.Name.Length));
            var valueWidth = items.Max(i => Math.Max("Privilege".Length, (i.Value.GetString() ?? "").Length));

            Console.WriteLine($"| {Pad("User", keyWidth)} | {Pad("Privilege", valueWidth)} |");
            var separator = "+" + new string('-', keyWidth + 2) + "+" +
                          new string('-', valueWidth + 2) + "+";
            Console.WriteLine(separator);

            foreach (var item in items)
            {
                var value = item.Value.GetString();
                Console.WriteLine($"| {Pad(item.Name, keyWidth)} | {Pad(value ?? "", valueWidth)} |");
            }

            Console.WriteLine($"{items.Count} row(s) in set ({executionTime} sec)");
        }
    }

    private static void DisplayGroupsAsTable(JsonElement groups, string executionTime = "0.00")
    {
        if (groups.ValueKind == JsonValueKind.Null || !groups.EnumerateArray().Any())
        {
            Console.WriteLine($"Empty set ({executionTime} sec)");
            return;
        }

        var groupArray = groups.EnumerateArray().ToList();

        // Get columns from first group
        var columns = groupArray[0].EnumerateObject().Select(p => p.Name).ToList();
        var widths = new Dictionary<string, int>();

        foreach (var col in columns)
        {
            var maxWidth = col.Length;
            foreach (var g in groupArray)
            {
                if (g.TryGetProperty(col, out var value))
                {
                    var str = GetDisplayString(value);
                    maxWidth = Math.Max(maxWidth, str.Length);
                }
            }
            widths[col] = maxWidth;
        }

        // Print header
        var headerParts = columns.Select(c => Pad(c, widths[c]));
        Console.WriteLine("| " + string.Join(" | ", headerParts) + " |");
        Console.WriteLine("|" + string.Join("+", columns.Select(c => new string('-', widths[c] + 2))) + "|");

        // Print rows
        foreach (var g in groupArray)
        {
            var rowParts = columns.Select(c =>
            {
                if (g.TryGetProperty(c, out var value))
                    return Pad(GetDisplayString(value), widths[c]);
                return Pad("NULL", widths[c]);
            });
            Console.WriteLine("| " + string.Join(" | ", rowParts) + " |");
        }

        Console.WriteLine($"{groupArray.Count} row(s) in set (0.00 sec)");
    }

    private static void DisplayAggregatesAsTable(JsonElement aggregates, string executionTime = "0.00")
    {
        if (aggregates.ValueKind == JsonValueKind.Object)
        {
            var items = aggregates.EnumerateObject().ToList();
            if (items.Count == 0)
            {
                Console.WriteLine($"Empty set ({executionTime} sec)");
                return;
            }

            var maxWidth = items.Max(i => Math.Max("Result".Length, i.Name.Length));
            var valueWidth = items.Max(i => Math.Max("Value".Length, GetDisplayString(i.Value).Length));

            Console.WriteLine($"| {Pad("Result", maxWidth)} | {Pad("Value", valueWidth)} |");
            var separator = "+" + new string('-', maxWidth + 2) + "+" +
                          new string('-', valueWidth + 2) + "+";
            Console.WriteLine(separator);

            foreach (var item in items)
            {
                var displayValue = GetDisplayString(item.Value);
                Console.WriteLine($"| {Pad(item.Name, maxWidth)} | {Pad(displayValue, valueWidth)} |");
            }

            Console.WriteLine($"{items.Count} row(s) in set ({executionTime} sec)");
        }
    }

    private static void DisplayErrorPlain(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERROR: {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Display an error message with formatting
    /// </summary>
    public static void DisplayError(string content, string? query = null)
    {
        // Try to parse as structured error response
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, options);
            if (errorResponse != null && (!string.IsNullOrEmpty(errorResponse.Message) || !string.IsNullOrEmpty(errorResponse.Type)))
            {
                DisplayStructuredError(errorResponse);
                return;
            }
        }
        catch (JsonException)
        {
            // Not a structured error, display as-is
        }

        // Fallback: display as plain text
        DisplayErrorPlain(content);
    }

    private static void DisplayStructuredError(ErrorResponse error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERROR: {error.Message}");
        Console.ResetColor();

        if (error.Line.HasValue && error.Column.HasValue && !string.IsNullOrEmpty(error.Query))
        {
            var lines = error.Query.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (error.Line.Value > 0 && error.Line.Value <= lines.Length)
            {
                var errorLine = lines[error.Line.Value - 1];
                string lineNumberPart = $"  {error.Line.Value} | ";
                Console.WriteLine($"\n{lineNumberPart}{errorLine}");
                
                // Print pointer ^
                Console.Write(new string(' ', lineNumberPart.Length));
                
                // Handle tabs for alignment
                for (int i = 0; i < error.Column.Value - 1; i++)
                {
                    if (i < errorLine.Length && errorLine[i] == '\t') Console.Write("\t");
                    else Console.Write(" ");
                }
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("^");
                Console.ResetColor();
                Console.WriteLine($"(Line: {error.Line.Value}, Column: {error.Column.Value})\n");
            }
        }
    }

    private static string GetDisplayString(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null: return "NULL";
            case JsonValueKind.String: return element.GetString() ?? "";
            case JsonValueKind.Number:
                return element.GetRawText();
            case JsonValueKind.True: return "true";
            case JsonValueKind.False: return "false";
            case JsonValueKind.Array: 
                var items = element.EnumerateArray().Select(e => GetDisplayString(e)).ToList();
                if (items.Count == 0) return "[]";
                return string.Join("\n", items.Select(x => $"- {x}"));
            case JsonValueKind.Object: 
                var props = element.EnumerateObject().Select(p => $"{p.Name}: {GetDisplayString(p.Value)}").ToList();
                if (props.Count == 0) return "{}";
                return "{ " + string.Join(", ", props) + " }";
            default: return element.ToString();
        }
    }

    private static string Pad(string str, int width, char padChar = ' ')
    {
        if (str == null) return new string(padChar, width);
        if (str.Length > width) 
        {
            if (width > 3) return str.Substring(0, width - 3) + "...";
            return str.Substring(0, width);
        }
        return str + new string(padChar, width - str.Length);
    }
    
    private static string PadLeft(string str, int width, char padChar = ' ')
    {
        if (str == null) return new string(padChar, width);
        if (str.Length >= width) return str;
        return new string(padChar, width - str.Length) + str;
    }
    private static void DisplaySolveResult(JsonElement root, string executionTime)
    {
        var success = root.GetProperty("Success").GetBoolean();
        var conceptName = root.TryGetProperty("ConceptName", out var cProp) ? cProp.GetString() : "Unknown";
        
        Console.WriteLine($"\n=== REASONING ENGINE RESULT: {conceptName} ===\n");

        if (root.TryGetProperty("Steps", out var stepsProp) && stepsProp.ValueKind == JsonValueKind.Array)
        {
            Console.WriteLine("Execution Steps:");
            var steps = stepsProp.EnumerateArray().ToList();
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i].GetString();
                if (i == steps.Count - 1)
                {
                    Console.WriteLine($"  => {step}");
                }
                else
                {
                    Console.WriteLine($"  - {step}");
                }
            }
            Console.WriteLine();
        }

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("STATUS: SUCCESS");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("STATUS: FAILED or HALTED");
            Console.ResetColor();
            if (root.TryGetProperty("ErrorMessage", out var errProp))
            {
                var errMsg = errProp.GetString();
                if (errMsg != null && errMsg.Contains("violated", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Violation: {errMsg}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"Error: {errMsg}");
                }
            }
        }

        // (Phase 17) Explanations
        if (root.TryGetProperty("Traces", out var tracesProp) && tracesProp.ValueKind == JsonValueKind.Array)
        {
            var traces = tracesProp.EnumerateArray().ToList();
            if (traces.Count > 0)
            {
                Console.WriteLine("\nExplanations (How facts were derived):");
                foreach (var trace in traces)
                {
                    var target = trace.GetProperty("TargetVariable").GetString();
                    var value = GetDisplayString(trace.GetProperty("Value"));
                    var mechanism = trace.GetProperty("Mechanism").GetString();
                    var source = trace.GetProperty("Source").GetString();
                    
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"  • {target} = {value}");
                    Console.ResetColor();
                    Console.WriteLine($" derived via {mechanism}");
                    Console.WriteLine($"    Source: {source}");
                    
                    if (trace.TryGetProperty("Inputs", out var inputsProp) && inputsProp.ValueKind == JsonValueKind.Object)
                    {
                        var inputs = inputsProp.EnumerateObject().Select(p => $"{p.Name}={GetDisplayString(p.Value)}");
                        Console.WriteLine($"    Inputs: {string.Join(", ", inputs)}");
                    }
                    Console.WriteLine();
                }
            }
        }

        Console.WriteLine("\nDerived Facts (Goals Achieved):");
        if (root.TryGetProperty("DerivedFacts", out var factsProp) && factsProp.ValueKind == JsonValueKind.Object)
        {
            var facts = factsProp.EnumerateObject().ToList();
            if (facts.Count == 0)
            {
                Console.WriteLine("  (No facts derived)");
            }
            else
            {
                Console.WriteLine("  +-----------------+-----------------+");
                Console.WriteLine("  | Variable        | Value           |");
                Console.WriteLine("  +-----------------+-----------------+");
                foreach (var fact in facts)
                {
                    Console.WriteLine($"  | {Pad(fact.Name, 15)} | {Pad(GetDisplayString(fact.Value), 15)} |");
                }
                Console.WriteLine("  +-----------------+-----------------+");
            }
        }

        Console.WriteLine($"\nCompleted in {executionTime} sec");
    }
}
