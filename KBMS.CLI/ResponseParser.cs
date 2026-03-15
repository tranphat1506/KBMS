using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KBMS.Network;

namespace KBMS.CLI;

/// <summary>
/// Parser and formatter for server responses - MySQL-like plain text output
/// </summary>
public static class ResponseParser
{
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
            var jsonDoc = JsonDocument.Parse(message.Content);
            DisplayJsonAsTable(jsonDoc);
        }
        catch (JsonException)
        {
            // Not valid JSON, display as-is (plain text message)
            Console.WriteLine(message.Content);
        }
    }

    /// <summary>
    /// Display JSON data as MySQL-like table
    /// </summary>
    private static void DisplayJsonAsTable(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        Console.WriteLine(root.ToString());
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
            DisplaySimpleListAsTable(relationsProp, "Relation", executionTime);
        }
        else if (root.TryGetProperty("operators", out var operatorsProp))
        {
            DisplaySimpleListAsTable(operatorsProp, "Operator", executionTime);
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
        var first = objectArray[0];

        // Get all column names from first object, excluding Id and KbId
        var columns = first.EnumerateObject()
            .Select(p => p.Name)
            .Where(c => !string.Equals(c, "Id", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(c, "KbId", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Calculate column widths
        var widths = new Dictionary<string, int>();
        foreach (var col in columns)
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
            widths[col] = maxWidth;
        }

        // Print header
        var headerParts = columns.Select(c => Pad(c, widths[c]));
        Console.WriteLine("| " + string.Join(" | ", headerParts) + " |");
        Console.WriteLine("|" + string.Join("+", columns.Select(c => new string('-', widths[c] + 2))) + "|");

        // Print rows
        foreach (var obj in objectArray)
        {
            var rowParts = columns.Select(c =>
            {
                if (obj.TryGetProperty(c, out var value))
                    return Pad(GetDisplayString(value), widths[c]);
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
        var maxWidth = "Concept".Length;

        foreach (var c in conceptArray)
        {
            if (c.TryGetProperty("Name", out var name))
            {
                maxWidth = Math.Max(maxWidth, (name.GetString() ?? "").Length);
            }
        }

        Console.WriteLine($"| {Pad("Concept", maxWidth)} |");
        Console.WriteLine($"|{new string('-', maxWidth + 2)}|");

        foreach (var c in conceptArray)
        {
            var name = c.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() : "";
            Console.WriteLine($"| {Pad(name ?? "", maxWidth)} |");
        }

        Console.WriteLine($"{conceptArray.Count} row(s) in set ({executionTime} sec)");
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

        foreach (var r in ruleArray)
        {
            if (r.TryGetProperty("Name", out var name))
                nameWidth = Math.Max(nameWidth, (name.GetString() ?? "").Length);
            if (r.TryGetProperty("RuleType", out var type))
                typeWidth = Math.Max(typeWidth, (type.GetString() ?? "").Length);
            if (r.TryGetProperty("Scope", out var scope))
                scopeWidth = Math.Max(scopeWidth, (scope.GetString() ?? "").Length);
        }

        Console.WriteLine($"| {Pad("Name", nameWidth)} | {Pad("Type", typeWidth)} | {Pad("Scope", scopeWidth)} |");
        var separator = "+" + new string('-', nameWidth + 2) + "+" +
                      new string('-', typeWidth + 2) + "+" +
                      new string('-', scopeWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var r in ruleArray)
        {
            var name = r.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var type = r.TryGetProperty("RuleType", out var typeProp) ? typeProp.GetString() ?? "" : "";
            var scope = r.TryGetProperty("Scope", out var scopeProp) ? scopeProp.GetString() ?? "" : "";
            Console.WriteLine($"| {Pad(name, nameWidth)} | {Pad(type, typeWidth)} | {Pad(scope, scopeWidth)} |");
        }

        Console.WriteLine($"{ruleArray.Count} row(s) in set ({executionTime} sec)");
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

        foreach (var f in funcArray)
        {
            if (f.TryGetProperty("Name", out var name))
                nameWidth = Math.Max(nameWidth, (name.GetString() ?? "").Length);
            if (f.TryGetProperty("ReturnType", out var ret))
                returnTypeWidth = Math.Max(returnTypeWidth, (ret.GetString() ?? "").Length);
        }

        Console.WriteLine($"| {Pad("Name", nameWidth)} | {Pad("Returns", returnTypeWidth)} |");
        var separator = "+" + new string('-', nameWidth + 2) + "+" +
                      new string('-', returnTypeWidth + 2) + "+";
        Console.WriteLine(separator);

        foreach (var f in funcArray)
        {
            var name = f.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var ret = f.TryGetProperty("ReturnType", out var retProp) ? retProp.GetString() ?? "" : "";
            Console.WriteLine($"| {Pad(name, nameWidth)} | {Pad(ret, returnTypeWidth)} |");
        }

        Console.WriteLine($"{funcArray.Count} row(s) in set ({executionTime} sec)");
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
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content);
            if (errorResponse != null)
            {
                DisplayErrorPlain(errorResponse.Message);
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

    private static string GetDisplayString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => "NULL",
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
            JsonValueKind.Object => "{...}",
            _ => element.ToString()
        };
    }

    private static string Pad(string str, int width, char padChar = ' ')
    {
        if (str.Length >= width) return str;
        return str + new string(padChar, width - str.Length);
    }
}
