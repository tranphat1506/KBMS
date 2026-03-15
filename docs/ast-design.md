# AST Design - Abstract Syntax Tree

## Tổng quan

Tài liệu này mô tả cấu trúc AST (Abstract Syntax Tree) của KBMS, được thiết kế song song với SQL syntax để đảm bảo 1-for-1 mapping giữa statements và nodes.

---

## Mapping: SQL Statement ↔ AST Node ↔ COKB File

| SQL Statement | AST Node Class | COKB Logical File | Status |
|---------------|----------------|-------------------|--------|
| `CREATE KNOWLEDGE BASE` | `CreateKbNode` | N/A (directory) | ✓ |
| `DROP KNOWLEDGE BASE` | `DropKbNode` | N/A (directory) | ✓ |
| `USE <name>` | `UseKbNode` | N/A | ✓ |
| `CREATE CONCEPT` | `CreateConceptNode` | `CONCEPTS.TXT`, `<CONCEPT>.TXT` | ✓ |
| `ADD VARIABLE` | `AddVariableNode` | `<CONCEPT>.TXT` | ✓ |
| `DROP CONCEPT` | `DropConceptNode` | `CONCEPTS.TXT`, `<CONCEPT>.TXT` | ✓ |
| `ADD HIERARCHY` | `AddHierarchyNode` | `HIERARCHY.TXT` | ✓ |
| `REMOVE HIERARCHY` | `RemoveHierarchyNode` | `HIERARCHY.TXT` | ✓ |
| `CREATE RELATION` | `CreateRelationNode` | `RELATIONS.TXT` | ✓ |
| `DROP RELATION` | `DropRelationNode` | `RELATIONS.TXT` | ✓ |
| `CREATE OPERATOR` | `CreateOperatorNode` | `OPERATORS.TXT` | ✓ |
| `DROP OPERATOR` | `DropOperatorNode` | `OPERATORS.TXT` | ✓ |
| `CREATE FUNCTION` | `CreateFunctionNode` | `FUNCTIONS.TXT` | ✓ |
| `DROP FUNCTION` | `DropFunctionNode` | `FUNCTIONS.TXT` | ✓ |
| `ADD COMPUTATION` | `AddComputationNode` | `<CONCEPT>.TXT` | ✓ |
| `REMOVE COMPUTATION` | `RemoveComputationNode` | `<CONCEPT>.TXT` | ✓ |
| `CREATE RULE` | `CreateRuleNode` | `RULES.TXT`, `<CONCEPT>.TXT` | ✓ |
| `DROP RULE` | `DropRuleNode` | `RULES.TXT`, `<CONCEPT>.TXT` | ✓ |
| `INSERT INTO` | `InsertNode` | `FACTS.TXT` | ✓ |
| `SELECT` | `SelectNode` | `FACTS.TXT` | ✓ |
| `SELECT ... JOIN ...` | `SelectNode` with `JoinClause` | `FACTS.TXT` | ✓ (basic) |
| `SELECT COUNT(*)` | `SelectNode` with `AggregateClause` | `FACTS.TXT` | ✗ (planned) |
| `SELECT ... GROUP BY` | `SelectNode` with `GroupByClause` | `FACTS.TXT` | ✗ (planned) |
| `SELECT ... ORDER BY` | `SelectNode` with `OrderByClause` | `FACTS.TXT` | ✗ (planned) |
| `SELECT ... LIMIT` | `SelectNode` with `LimitClause` | `FACTS.TXT` | ✗ (planned) |
| `UPDATE` | `UpdateNode` | `FACTS.TXT` | ✓ |
| `DELETE FROM` | `DeleteNode` | `FACTS.TXT` | ✓ |
| `SOLVE` | `SolveNode` | Multiple files | ✓ |
| `SHOW` | `ShowNode` | Various files | ✓ |
| `CREATE USER` | `CreateUserNode` | N/A (separate) | ✓ |
| `DROP USER` | `DropUserNode` | N/A (separate) | ✓ |
| `GRANT` | `GrantNode` | N/A (separate) | ✓ |
| `REVOKE` | `RevokeNode` | N/A (separate) | ✓ |

**Legend:**
- ✓ = Đã hỗ trợ
- ✗ (planned) = Được định nghĩa nhưng chưa implement

---

## AST Node Hierarchy

```
AstNode (abstract base)
│
├── DdlNode (DDL statements - abstract)
│   ├── CreateKbNode
│   ├── DropKbNode
│   ├── UseKbNode
│   ├── CreateConceptNode
│   ├── AddVariableNode
│   ├── DropConceptNode
│   ├── AddHierarchyNode
│   ├── RemoveHierarchyNode
│   ├── CreateRelationNode
│   ├── DropRelationNode
│   ├── CreateOperatorNode
│   ├── DropOperatorNode
│   ├── CreateFunctionNode
│   ├── DropFunctionNode
│   ├── AddComputationNode
│   ├── RemoveComputationNode
│   ├── CreateRuleNode
│   ├── DropRuleNode
│   ├── CreateUserNode
│   ├── DropUserNode
│   ├── GrantNode
│   └── RevokeNode
│
└── DmlNode (DML statements - abstract)
    ├── SelectNode
    ├── InsertNode
    ├── UpdateNode
    ├── DeleteNode
    ├── SolveNode
    └── ShowNode
```

---

## Base Classes

### 1. AstNode

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// Base class for all AST nodes
/// </summary>
public abstract class AstNode
{
    /// <summary>
    /// Statement type (e.g., "CREATE_KNOWLEDGE_BASE", "SELECT")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Target Knowledge Base name (if applicable)
    /// </summary>
    public string? KbName { get; set; }

    /// <summary>
    /// Original query string (for debugging)
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// Line number in source (for error reporting)
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number in source (for error reporting)
    /// </summary>
    public int Column { get; set; }
}
```

---

### 2. DdlNode

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// Base class for DDL (Data Definition Language) statements
/// </summary>
public abstract class DdlNode : AstNode
{
    // Additional DDL-specific properties can be added here
}
```

---

### 3. DmlNode

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// Base class for DML (Data Manipulation Language) statements
/// </summary>
public abstract class DmlNode : AstNode
{
    // Additional DML-specific properties can be added here
}
```

---

## DDL Nodes

### 4. CreateKbNode

**SQL:** `CREATE KNOWLEDGE BASE <name> [DESCRIPTION '<desc>']`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE KNOWLEDGE BASE statement
/// </summary>
public class CreateKbNode : DdlNode
{
    /// <summary>
    /// Name of Knowledge Base
    /// </summary>
    public string KbName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}
```

**Example:**
```sql
CREATE KNOWLEDGE BASE geometry DESCRIPTION 'Hệ thống tri thức hình học'
```
↓
```csharp
new CreateKbNode
{
    Type = "CREATE_KNOWLEDGE_BASE",
    KbName = "geometry",
    Description = "Hệ thống tri thức hình học"
}
```

---

### 5. DropKbNode

**SQL:** `DROP KNOWLEDGE BASE <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP KNOWLEDGE BASE statement
/// </summary>
public class DropKbNode : DdlNode
{
    /// <summary>
    /// Name of Knowledge Base to drop
    /// </summary>
    public string KbName { get; set; } = string.Empty;
}
```

---

### 6. UseKbNode

**SQL:** `USE <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for USE statement
/// </summary>
public class UseKbNode : DdlNode
{
    /// <summary>
    /// Name of Knowledge Base to use
    /// </summary>
    public string KbName { get; set; } = string.Empty;
}
```

---

### 7. CreateConceptNode

**SQL:** `CREATE CONCEPT <name> VARIABLES (...) ALIASES ... BASE_OBJECTS ... CONSTRAINTS ...`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE CONCEPT statement
/// </summary>
public class CreateConceptNode : DdlNode
{
    /// <summary>
    /// Name of Concept
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// List of variable definitions
    /// </summary>
    public List<VariableDefinition> Variables { get; set; } = new();

    /// <summary>
    /// List of alias names
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// List of base objects
    /// </summary>
    public List<string> BaseObjects { get; set; } = new();

    /// <summary>
    /// List of constraint expressions
    /// </summary>
    public List<string> Constraints { get; set; } = new();

    /// <summary>
    /// List of same variable groups
    /// </summary>
    public List<SameVariableGroup> SameVariables { get; set; } = new();
}

/// <summary>
/// Variable definition in CREATE CONCEPT
/// </summary>
public class VariableDefinition
{
    /// <summary>
    /// Name of variable
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data type (number, string, boolean, object)
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Same variable group
/// </summary>
public class SameVariableGroup
{
    /// <summary>
    /// From variable name
    /// </summary>
    public string FromVariable { get; set; } = string.Empty;

    /// <summary>
    /// To variable name (equivalent)
    /// </summary>
    public string ToVariable { get; set; } = string.Empty;
}
```

**Example:**
```sql
CREATE CONCEPT TAMGIAC
    VARIABLES (a:number, b:number, c:number, S:number)
    ALIASES TRIANGLE
    BASE_OBJECTS TAMGIAC_CAN
    CONSTRAINTS a>0, b>0, c>0, a+b>c
```
↓
```csharp
new CreateConceptNode
{
    Type = "CREATE_CONCEPT",
    ConceptName = "TAMGIAC",
    Variables = new List<VariableDefinition>
    {
        new() { Name = "a", Type = "number" },
        new() { Name = "b", Type = "number" },
        new() { Name = "c", Type = "number" },
        new() { Name = "S", Type = "number" }
    },
    Aliases = new List<string> { "TRIANGLE" },
    BaseObjects = new List<string> { "TAMGIAC_CAN" },
    Constraints = new List<string>
    {
        "a>0",
        "b>0",
        "c>0",
        "a+b>c"
    }
}
```

---

### 8. AddVariableNode

**SQL:** `ADD VARIABLE <var>:<type> TO CONCEPT <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for ADD VARIABLE statement
/// </summary>
public class AddVariableNode : DdlNode
{
    /// <summary>
    /// Name of Concept
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Variable name
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// Variable type
    /// </summary>
    public string VariableType { get; set; } = string.Empty;
}
```

---

### 9. DropConceptNode

**SQL:** `DROP CONCEPT <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP CONCEPT statement
/// </summary>
public class DropConceptNode : DdlNode
{
    /// <summary>
    /// Name of Concept to drop
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;
}
```

---

### 10. AddHierarchyNode

**SQL:** `ADD HIERARCHY <parent> IS_A <child>` hoặc `PART_OF`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for ADD HIERARCHY statement
/// </summary>
public class AddHierarchyNode : DdlNode
{
    /// <summary>
    /// Parent concept name
    /// </summary>
    public string ParentConcept { get; set; } = string.Empty;

    /// <summary>
    /// Child concept name
    /// </summary>
    public string ChildConcept { get; set; } = string.Empty;

    /// <summary>
    /// Hierarchy type (IS_A or PART_OF)
    /// </summary>
    public HierarchyType Type { get; set; }
}
```

---

### 11. RemoveHierarchyNode

**SQL:** `REMOVE HIERARCHY <parent> IS_A <child>` hoặc `PART_OF`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for REMOVE HIERARCHY statement
/// </summary>
public class RemoveHierarchyNode : DdlNode
{
    /// <summary>
    /// Parent concept name
    /// </summary>
    public string ParentConcept { get; set; } = string.Empty;

    /// <summary>
    /// Child concept name
    /// </summary>
    public string ChildConcept { get; set; } = string.Empty;

    /// <summary>
    /// Hierarchy type (IS_A or PART_OF)
    /// </summary>
    public HierarchyType Type { get; set; }
}
```

---

### 12. CreateRelationNode

**SQL:** `CREATE RELATION <name> FROM <domain> TO <range> PROPERTIES ...`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE RELATION statement
/// </summary>
public class CreateRelationNode : DdlNode
{
    /// <summary>
    /// Name of Relation
    /// </summary>
    public string RelationName { get; set; } = string.Empty;

    /// <summary>
    /// Domain concept (source)
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Range concept (target)
    /// </summary>
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// Optional properties (transitive, symmetric, etc.)
    /// </summary>
    public List<string> Properties { get; set; } = new();
}
```

---

### 13. DropRelationNode

**SQL:** `DROP RELATION <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP RELATION statement
/// </summary>
public class DropRelationNode : DdlNode
{
    /// <summary>
    /// Name of Relation to drop
    /// </summary>
    public string RelationName { get; set; } = string.Empty;
}
```

---

### 14. CreateOperatorNode

**SQL:** `CREATE OPERATOR <symbol> PARAMS (...) RETURNS ... PROPERTIES ...`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE OPERATOR statement
/// </summary>
public class CreateOperatorNode : DdlNode
{
    /// <summary>
    /// Operator symbol (+, -, *, /, ^, etc.)
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// List of parameter types
    /// </summary>
    public List<string> ParamTypes { get; set; } = new();

    /// <summary>
    /// Return type
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Optional properties
    /// </summary>
    public List<string> Properties { get; set; } = new();
}
```

---

### 15. DropOperatorNode

**SQL:** `DROP OPERATOR <symbol>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP OPERATOR statement
/// </summary>
public class DropOperatorNode : DdlNode
{
    /// <summary>
    /// Operator symbol to drop
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
}
```

---

### 16. CreateFunctionNode

**SQL:** `CREATE FUNCTION <name> PARAMS (...) RETURNS ... BODY ...`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE FUNCTION statement
/// </summary>
public class CreateFunctionNode : DdlNode
{
    /// <summary>
    /// Function name
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// List of parameter definitions (type + name)
    /// </summary>
    public List<ParamDefinition> Params { get; set; } = new();

    /// <summary>
    /// Return type
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Function body (formula expression)
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Optional properties
    /// </summary>
    public List<string> Properties { get; set; } = new();
}

/// <summary>
/// Parameter definition with name
/// </summary>
public class ParamDefinition
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter type
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
```

---

### 17. DropFunctionNode

**SQL:** `DROP FUNCTION <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP FUNCTION statement
/// </summary>
public class DropFunctionNode : DdlNode
{
    /// <summary>
    /// Function name to drop
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
}
```

---

### 18. AddComputationNode

**SQL:** `ADD COMPUTATION TO <concept> VARIABLES ... FORMULA ...`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for ADD COMPUTATION statement
/// </summary>
public class AddComputationNode : DdlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// List of input variables
    /// </summary>
    public List<string> InputVariables { get; set; } = new();

    /// <summary>
    /// Result variable
    /// </summary>
    public string ResultVariable { get; set; } = string.Empty;

    /// <summary>
    /// Formula expression
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Optional cost/weight
    /// </summary>
    public int? Cost { get; set; }
}
```

---

### 19. RemoveComputationNode

**SQL:** `REMOVE COMPUTATION <var> FROM <concept>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for REMOVE COMPUTATION statement
/// </summary>
public class RemoveComputationNode : DdlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Result variable to remove
    /// </summary>
    public string ResultVariable { get; set; } = string.Empty;
}
```

---

### 20. CreateRuleNode

**SQL:** `CREATE RULE <name> TYPE <t> SCOPE <c> IF ... THEN ...`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE RULE statement
/// </summary>
public class CreateRuleNode : DdlNode
{
    /// <summary>
    /// Rule name
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Rule type (deduction, default, constraint, computation)
    /// </summary>
    public RuleType Type { get; set; }

    /// <summary>
    /// Scope concept (optional, if null applies to all)
    /// </summary>
    public string? ScopeConcept { get; set; }

    /// <summary>
    /// Content/description
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// List of condition expressions (IF part)
    /// </summary>
    public List<string> Hypothesis { get; set; } = new();

    /// <summary>
    /// List of conclusion expressions (THEN part)
    /// </summary>
    public List<string> Conclusion { get; set; } = new();

    /// <summary>
    /// Variables used in rule (with types)
    /// </summary>
    public List<VariableDefinition> Variables { get; set; } = new();

    /// <summary>
    /// Optional cost
    /// </summary>
    public int? Cost { get; set; }
}

/// <summary>
/// Rule type enum
/// </summary>
public enum RuleType
{
    Deduction,
    Default,
    Constraint,
    Computation
}
```

**Example:**
```sql
CREATE RULE heron_formula
    TYPE computation
    SCOPE TAMGIAC
    IF a>0 AND b>0 AND c>0
    THEN p = (a+b+c)/2, S = sqrt(p*(p-a)*(p-b)*(p-c))
```
↓
```csharp
new CreateRuleNode
{
    Type = "CREATE_RULE",
    RuleName = "heron_formula",
    Type = RuleType.Computation,
    ScopeConcept = "TAMGIAC",
    Hypothesis = new List<string> { "a>0 AND b>0 AND c>0" },
    Conclusion = new List<string>
    {
        "p = (a+b+c)/2",
        "S = sqrt(p*(p-a)*(p-b)*(p-c))"
    },
    Variables = new List<VariableDefinition>
    {
        new() { Name = "a", Type = "number" },
        new() { Name = "b", Type = "number" },
        new() { Name = "c", Type = "number" },
        new() { Name = "p", Type = "number" },
        new() { Name = "S", Type = "number" }
    }
}
```

---

### 21. DropRuleNode

**SQL:** `DROP RULE <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP RULE statement
/// </summary>
public class DropRuleNode : DdlNode
{
    /// <summary>
    /// Rule name to drop
    /// </summary>
    public string RuleName { get; set; } = string.Empty;
}
```

---

### 22. CreateUserNode

**SQL:** `CREATE USER <name> PASSWORD '<pass>' [ROLE <r>] [SYSTEM_ADMIN {true|false}]`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for CREATE USER statement
/// </summary>
public class CreateUserNode : DdlNode
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password (will be hashed)
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User role (ROOT or USER, default: USER)
    /// </summary>
    public string Role { get; set; } = "USER";

    /// <summary>
    /// System admin flag
    /// </summary>
    public bool SystemAdmin { get; set; }
}
```

---

### 23. DropUserNode

**SQL:** `DROP USER <name>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DROP USER statement
/// </summary>
public class DropUserNode : DdlNode
{
    /// <summary>
    /// Username to drop
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
```

---

### 24. GrantNode

**SQL:** `GRANT <privilege> ON <kb> TO <user>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for GRANT statement
/// </summary>
public class GrantNode : DdlNode
{
    /// <summary>
    /// Privilege to grant (READ, WRITE, ADMIN)
    /// </summary>
    public string Privilege { get; set; } = string.Empty;

    /// <summary>
    /// Knowledge Base name
    /// </summary>
    public string KbName { get; set; } = string.Empty;

    /// <summary>
    /// Username to grant privilege to
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
```

---

### 25. RevokeNode

**SQL:** `REVOKE <privilege> ON <kb> FROM <user>`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for REVOKE statement
/// </summary>
public class RevokeNode : DdlNode
{
    /// <summary>
    /// Privilege to revoke (READ, WRITE, ADMIN)
    /// </summary>
    public string Privilege { get; set; } = string.Empty;

    /// <summary>
    /// Knowledge Base name
    /// </summary>
    public string KbName { get; set; } = string.Empty;

    /// <summary>
    /// Username to revoke privilege from
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
```

---

## DML Nodes

### 26. SelectNode

**SQL:** `SELECT <concept> [WHERE <conditions>] [JOIN ...] [GROUP BY ...] [HAVING ...] [ORDER BY ...] [LIMIT ...]`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for SELECT statement
/// </summary>
public class SelectNode : DmlNode
{
    /// <summary>
    /// Concept name to select from
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();

    /// <summary>
    /// Optional JOIN clauses
    /// </summary>
    public List<JoinClause> Joins { get; set; } = new();

    /// <summary>
    /// Optional Aggregation clause (COUNT, SUM, AVG, MAX, MIN)
    /// </summary>
    public AggregateClause? Aggregate { get; set; }

    /// <summary>
    /// Optional GROUP BY clause
    /// </summary>
    public List<string> GroupBy { get; set; } = new();

    /// <summary>
    /// Optional HAVING clause (filter after aggregation)
    /// </summary>
    public Condition? Having { get; set; }

    /// <summary>
    /// Optional ORDER BY clause
    /// </summary>
    public OrderByClause? OrderBy { get; set; }

    /// <summary>
    /// Optional LIMIT clause
    /// </summary>
    public LimitClause? Limit { get; set; }
}

/// <summary>
/// JOIN clause
/// </summary>
public class JoinClause
{
    /// <summary>
    /// Type of join: INNER, LEFT, RIGHT (default: INNER)
    /// </summary>
    public string JoinType { get; set; } = "INNER";

    /// <summary>
    /// Concept or relation name to join
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Optional alias for the joined table
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Join condition (ON clause)
    /// </summary>
    public Condition? OnCondition { get; set; }
}

/// <summary>
/// Aggregation function clause
/// </summary>
public class AggregateClause
{
    /// <summary>
    /// Type of aggregation: COUNT, SUM, AVG, MAX, MIN
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Variable to aggregate (null for COUNT(*))
    /// </summary>
    public string? Variable { get; set; }

    /// <summary>
    /// Optional alias for the aggregate result
    /// </summary>
    public string? Alias { get; set; }
}

/// <summary>
/// ORDER BY clause
/// </summary>
public class OrderByClause
{
    /// <summary>
    /// Variable to order by
    /// </summary>
    public string Variable { get; set; } = string.Empty;

    /// <summary>
    /// Order direction: ASC or DESC (default: ASC)
    /// </summary>
    public string Direction { get; set; } = "ASC";
}

/// <summary>
/// LIMIT clause
/// </summary>
public class LimitClause
{
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Number of results to skip (OFFSET)
    /// </summary>
    public int? Offset { get; set; }
}

/// <summary>
/// Condition in WHERE/HAVING clause
/// </summary>
public class Condition
{
    /// <summary>
    /// Field name
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Operator (=, <>, >, <, >=, <=)
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Value to compare
    /// </summary>
    public object Value { get; set; } = null!;

    /// <summary>
    /// Optional logical operator for chained conditions (AND, OR)
    /// </summary>
    public string? LogicalOperator { get; set; }
}
```

**Example 1: Basic SELECT**
```sql
SELECT TAMGIAC WHERE a=3 AND b=4
```
↓
```csharp
new SelectNode
{
    Type = "SELECT",
    ConceptName = "TAMGIAC",
    Conditions = new List<Condition>
    {
        new() { Field = "a", Operator = "=", Value = 3.0, LogicalOperator = "AND" },
        new() { Field = "b", Operator = "=", Value = 4.0 }
    }
}
```

**Example 2: SELECT with Aggregation**
```sql
SELECT COUNT(*) FROM TAMGIAC WHERE is_right=true
```
↓
```csharp
new SelectNode
{
    Type = "SELECT",
    ConceptName = "TAMGIAC",
    Aggregate = new AggregateClause
    {
        AggregateType = "COUNT",
        Variable = null,  // COUNT(*)
        Alias = "count"
    },
    Conditions = new List<Condition>
    {
        new() { Field = "is_right", Operator = "=", Value = true }
    }
}
```

**Example 3: SELECT with GROUP BY**
```sql
SELECT COUNT(*), is_isosceles FROM TAMGIAC GROUP BY is_isosceles HAVING COUNT(*) > 5
```
↓
```csharp
new SelectNode
{
    Type = "SELECT",
    ConceptName = "TAMGIAC",
    Aggregate = new AggregateClause
    {
        AggregateType = "COUNT",
        Variable = null,
        Alias = "count"
    },
    GroupBy = new List<string> { "is_isosceles" },
    Having = new Condition
    {
        Field = "COUNT(*)",
        Operator = ">",
        Value = 5
    }
}
```

**Example 4: SELECT with ORDER BY and LIMIT**
```sql
SELECT TAMGIAC WHERE a>3 ORDER BY S DESC LIMIT 5
```
↓
```csharp
new SelectNode
{
    Type = "SELECT",
    ConceptName = "TAMGIAC",
    Conditions = new List<Condition>
    {
        new() { Field = "a", Operator = ">", Value = 3 }
    },
    OrderBy = new OrderByClause
    {
        Variable = "S",
        Direction = "DESC"
    },
    Limit = new LimitClause
    {
        Limit = 5
    }
}
```

**Example 5: SELECT with JOIN**
```sql
SELECT TAMGIAC JOIN DIEM AS D ON TAMGIAC.A = D.Name WHERE D.x = 0
```
↓
```csharp
new SelectNode
{
    Type = "SELECT",
    ConceptName = "TAMGIAC",
    Joins = new List<JoinClause>
    {
        new JoinClause
        {
            JoinType = "INNER",
            Target = "DIEM",
            Alias = "D",
            OnCondition = new Condition
            {
                Field = "TAMGIAC.A",
                Operator = "=",
                Value = "D.Name"
            }
        }
    },
    Conditions = new List<Condition>
    {
        new() { Field = "D.x", Operator = "=", Value = 0 }
    }
}
```

---

### 27. InsertNode

**SQL:** `INSERT INTO <concept> VALUES (...)`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for INSERT statement
/// </summary>
public class InsertNode : DmlNode
{
    /// <summary>
    /// Concept name to insert into
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Field-value pairs to insert
    /// </summary>
    public Dictionary<string, object> Values { get; set; } = new();
}
```

**Example:**
```sql
INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5, S=0)
```
↓
```csharp
new InsertNode
{
    Type = "INSERT",
    ConceptName = "TAMGIAC",
    Values = new Dictionary<string, object>
    {
        { "a", 3.0 },
        { "b", 4.0 },
        { "c", 5.0 },
        { "S", 0.0 }
    }
}
```

---

### 28. UpdateNode

**SQL:** `UPDATE <concept> SET ... [WHERE ...]`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for UPDATE statement
/// </summary>
public class UpdateNode : DmlNode
{
    /// <summary>
    /// Concept name to update
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Field-value pairs to update
    /// </summary>
    public Dictionary<string, object> Values { get; set; } = new();

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();
}
```

---

### 29. DeleteNode

**SQL:** `DELETE FROM <concept> [WHERE ...]`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for DELETE statement
/// </summary>
public class DeleteNode : DmlNode
{
    /// <summary>
    /// Concept name to delete from
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Optional WHERE conditions
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();
}
```

---

### 30. SolveNode

**SQL:** `SOLVE <concept> FOR <unknown> GIVEN ... [USING ...]`

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for SOLVE statement
/// </summary>
public class SolveNode : DmlNode
{
    /// <summary>
    /// Concept name
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Unknown variable to find
    /// </summary>
    public string FindVariable { get; set; } = string.Empty;

    /// <summary>
    /// Known conditions (key=value, AND separated)
    /// </summary>
    public Dictionary<string, object> Known { get; set; } = new();

    /// <summary>
    /// Optional rule type filter (deduction, default, constraint, computation)
    /// </summary>
    public string? RuleType { get; set; }
}
```

**Example:**
```sql
SOLVE TAMGIAC FOR S GIVEN a=3, b=4, c=5
```
↓
```csharp
new SolveNode
{
    Type = "SOLVE",
    ConceptName = "TAMGIAC",
    FindVariable = "S",
    Known = new Dictionary<string, object>
    {
        { "a", 3.0 },
        { "b", 4.0 },
        { "c", 5.0 }
    }
}
```

---

### 31. ShowNode

**SQL:** `SHOW KNOWLEDGE BASES`, `SHOW CONCEPTS`, etc.

```csharp
namespace KBMS.Parser.Ast;

/// <summary>
/// AST node for SHOW statement
/// </summary>
public class ShowNode : DmlNode
{
    /// <summary>
    /// Target to show (KNOWLEDGE_BASES, CONCEPTS, RULES, etc.)
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Optional KB name (for SHOW CONCEPTS IN <kb>)
    /// </summary>
    public string? KbName { get; set; }

    /// <summary>
    /// Optional concept name (for SHOW CONCEPT <name>)
    /// </summary>
    public string? ConceptName { get; set; }

    /// <summary>
    /// Optional rule type filter (for SHOW RULES TYPE <type>)
    /// </summary>
    public string? RuleType { get; set; }

    /// <summary>
    /// Optional username (for SHOW PRIVILEGES OF <user>)
    /// </summary>
    public string? Username { get; set; }
}
```

---

## AST Examples

### Example 1: Complete Workflow

```sql
CREATE KNOWLEDGE BASE geometry
USE geometry
CREATE CONCEPT TAMGIAC
    VARIABLES (a:number, b:number, c:number, S:number)
    ALIASES TRIANGLE
ADD COMPUTATION TO TAMGIAC
    VARIABLES a, b, c, S
    FORMULA 'sqrt(((a+b+c)/2) * (((a+b+c)/2)-a) * (((a+b+c)/2)-b) * (((a+b+c)/2)-c))'
CREATE RULE check_right_triangle
    TYPE deduction
    SCOPE TAMGIAC
    IF a^2 + b^2 = c^2
    THEN is_right = true
INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5, S=0)
SOLVE TAMGIAC FOR S GIVEN a=3, b=4, c=5
```

↓ AST ↓

```csharp
// 1. Create KB
new CreateKbNode { Type = "CREATE_KNOWLEDGE_BASE", KbName = "geometry" }

// 2. Use KB
new UseKbNode { Type = "USE", KbName = "geometry" }

// 3. Create Concept
new CreateConceptNode
{
    Type = "CREATE_CONCEPT",
    ConceptName = "TAMGIAC",
    Variables = new List<VariableDefinition>
    {
        new() { Name = "a", Type = "number" },
        new() { Name = "b", Type = "number" },
        new() { Name = "c", Type = "number" },
        new() { Name = "S", Type = "number" }
    },
    Aliases = new List<string> { "TRIANGLE" }
}

// 4. Add Computation
new AddComputationNode
{
    Type = "ADD_COMPUTATION",
    ConceptName = "TAMGIAC",
    InputVariables = new List<string> { "a", "b", "c" },
    ResultVariable = "S",
    Formula = "sqrt(((a+b+c)/2) * (((a+b+c)/2)-a) * (((a+b+c)/2)-b) * (((a+b+c)/2)-c))"
}

// 5. Create Rule
new CreateRuleNode
{
    Type = "CREATE_RULE",
    RuleName = "check_right_triangle",
    Type = RuleType.Deduction,
    ScopeConcept = "TAMGIAC",
    Hypothesis = new List<string> { "a^2 + b^2 = c^2" },
    Conclusion = new List<string> { "is_right = true" }
}

// 6. Insert
new InsertNode
{
    Type = "INSERT",
    ConceptName = "TAMGIAC",
    Values = new Dictionary<string, object>
    {
        { "a", 3.0 },
        { "b", 4.0 },
        { "c", 5.0 },
        { "S", 0.0 }
    }
}

// 7. Solve
new SolveNode
{
    Type = "SOLVE",
    ConceptName = "TAMGIAC",
    FindVariable = "S",
    Known = new Dictionary<string, object>
    {
        { "a", 3.0 },
        { "b", 4.0 },
        { "c", 5.0 }
    }
}
```

---

## Token Types

```csharp
namespace KBMS.Parser;

public enum TokenType
{
    IDENTIFIER,    // concept_name, kb_name, variable, etc.
    NUMBER,        // 123, 3.14
    STRING,        // 'hello', "description"
    KEYWORD,       // CREATE, DROP, USE, SELECT, INSERT, etc.
    OPERATOR,      // +, -, *, /, ^, =, <, >, <>, >=, <=
    PUNCTUATION,  // (, ), ,, ;, [, ], {
    WHITESPACE,    // spaces, newlines (ignored)
    COMMENT        // -- comments (ignored)
}
```

---

## Error Handling

```csharp
namespace KBMS.Parser;

public class ParseException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ParseException(string message, int line = 0, int column = 0)
        : base(message)
    {
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return Column > 0
            ? $"Parse error at line {Line}, column {Column}: {Message}"
            : $"Parse error: {Message}";
    }
}
```

---

## Parser Interface

```csharp
namespace KBMS.Parser;

public interface IQueryParser
{
    /// <summary>
    /// Parse a query string into an AST node
    /// </summary>
    AstNode? Parse(string query);

    /// <summary>
    /// Parse multiple queries (semicolon-separated)
    /// </summary>
    List<AstNode> ParseBatch(string queries);
}
```
