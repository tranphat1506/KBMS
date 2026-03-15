# KẾ HOẠCH: THIẾT KẾ HOẠCH SONG SONG SQL SYNTAX VÀ SQL STRUCTURE

## Tổng quan

Thiết kế song song Query Language (KBQL = KBDDL + KBDML) với Data Structure để đảm bảo consistency và completeness.

## Nguyên tắc cốt lõi

| Nguyên tắc | Giải thích |
|------------|-------------|
| **1-for-1 Mapping** | Mỗi SQL statement tương ứng 1 data structure |
| **Syntax follows Semantics** | Cú pháp phản ánh ý nghĩa nghiệp vụ |
| **Bidirectional Design** | Design SQL → Structure và Structure → SQL cùng lúc |
| **Consistency First** | Syntax và Structure phải đồng bộ ngay từ đầu |

---

## PHASE 1: STUDY & UNDERSTAND (Song song)

### 1.1 Nghiên cứu COKB Model (Data Structure)
```
┌─────────────────────────────────────────────────────────────┐
│  6 THÀNH PHẦN COKB                                         │
├─────────────────────────────────────────────────────────────┤
│  1. Concept       - Khái niệm cơ bản                        │
│     - Variables, Constraints, Computation Relations            │
│                                                              │
│  2. Hierarchy     - Quan hệ phân cấp (is-a, part-of)         │
│     - Parent → Child mapping                                │
│                                                              │
│  3. Relation      - Quan hệ giữa khái niệm                  │
│     - Domain, Range, Properties                            │
│                                                              │
│  4. Operator      - Toán tử toán học (+, -, *, /, ^, etc.) │
│     - Symbol, ParamTypes, ReturnType, Properties              │
│                                                              │
│  5. Function      - Hàm toán học (sin, cos, sqrt, etc.)    │
│     - Name, ParamTypes, ReturnType, Body (formula)           │
│                                                              │
│  6. Rule          - Luật suy luận                            │
│     - Name, Hypothesis (IF), Conclusion (THEN)          │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 Nghiên cứu User Needs (SQL Syntax)
```
┌─────────────────────────────────────────────────────────────┐
│  NHU CẦU NGƯỜI DÙNG                                       │
├─────────────────────────────────────────────────────────────┤
│  DDL (Data Definition):                                      │
│  - Tạo/Xóa Knowledge Base                                  │
│  - Tạo/Xóa Concept, Rule, Relation, Operator, Function      │
│  - Tạo/Xóa User, Grant/Revoke privilege                  │
│                                                              │
│  DML (Data Manipulation):                                     │
│  - Chọn KB hiện tại (USE)                                  │
│  - Thêm/Xóa/Sửa Object instances                            │
│  - Truy vấn Objects (SELECT)                                │
│ - Suy luận/Tính toán (SOLVE)                               │
│  - Hiển thị thông tin (SHOW)                                │
└─────────────────────────────────────────────────────────────┘
```

---

## PHASE 2: DESIGN MATRIX (Song song - bước quan trọng nhất)

### 2.1 Mapping Matrix

| Data Structure | SQL Statement (DDL/DML) | Syntax Design Considerations |
|----------------|---------------------------|---------------------------|
| **KnowledgeBase** | `CREATE KNOWLEDGE BASE <name> [DESCRIPTION '<desc>']` | - Name phải unique<br>- Optional description |
| | `DROP KNOWLEDGE BASE <name>` | - Cascade delete? |
| **Concept** | `CREATE CONCEPT <name> (<var1>:<type>, <var2>:<type>, ...) [CONSTRAINTS '<expr>']` | - Variable types: int, float, string<br>- Constraints: expressions |
| | `DROP CONCEPT <name>` | - Check if used in objects |
| **Hierarchy** | `CREATE HIERARCHY <parent> IS_A <child>` | - IS_A hoặc PART_OF |
| | `DROP HIERARCHY <parent> <child>` | - Remove one link |
| **Relation** | `CREATE RELATION <name> FROM <domain> TO <range> [PROPERTIES '<props>']` | - Domain = source concept<br>- Range = target concept |
| | `DROP RELATION <name>` | - Remove relation definition |
| **Operator** | `CREATE OPERATOR <symbol> PARAMS (<type1>, <type2>, ...) RETURNS <type> [PROPERTIES '<props>']` | - Symbol: +, -, *, /, ^<br>- Properties: commutative, associative |
| | `DROP OPERATOR <symbol>` | - Remove operator |
| **Function** | `CREATE FUNCTION <name> PARAMS (<type1>, <type2>, ...) RETURNS <type> BODY '<formula>'` | - Body: mathematical expression |
| | `DROP FUNCTION <name>` | - Remove function |
| **Rule** | `CREATE RULE <name> IF <hypothesis> THEN <conclusion>` | - Hypothesis: list of conditions<br>- Conclusion: list of actions |
| | `DROP RULE <name>` | - Remove rule |
| **ObjectInstance** | `INSERT INTO <concept> VALUES (<field1>=<value1>, <field2>=<value2>, ...)` | - Key-value pairs<br>- Values: numbers, strings, identifiers |
| | `SELECT <concept> [WHERE <conditions>]` | - Conditions: field=, field<>, field> |
| | `UPDATE <concept> SET <field>=<value> [WHERE <conditions>]` | - Update specific objects |
| | `DELETE FROM <concept> [WHERE <conditions>]` - Delete matching objects |
| | **SOLVE <concept> KNOWN <known_conditions> FIND <unknown>` | - Known conditions: field=value, etc.<br>- Find: unknown variable |
| | **User** | `CREATE USER <username> PASSWORD '<password>' [ROLE <role>]` | - Role: ROOT, USER<br>- Password hash bằng BCrypt |
| | `DROP USER <username>` | - Remove user |
| | `GRANT <privilege> ON <kb_name> TO <username>` | - Privilege: READ, WRITE, ADMIN |
| | `REVOKE <privilege> ON <kb_name> FROM <username>` | - Remove privilege |

---

### 2.2 Syntax Specification (Song song với Structure)

#### DDL Statements

```sql
-- Knowledge Base Management
CREATE KNOWLEDGE BASE <name> [DESCRIPTION '<description>']
DROP KNOWLEDGE BASE <name>
USE <name>

-- Concept Management
CREATE CONCEPT <name> (
    <var_name>:<type>,
    <var_name>:<type>,
    ...
) [CONSTRAINTS '<constraint_expression>']
DROP CONCEPT <name>

-- Hierarchy Management
CREATE HIERARCHY <parent_concept> IS_A <child_concept>
CREATE HIERARCHY <parent_concept> PART_OF <child_concept>
DROP HIERARCHY <parent_concept> <child_concept>

-- Relation Management
CREATE RELATION <name> FROM <domain_concept> TO <range_concept>
    [PROPERTIES '<properties>']
DROP RELATION <name>

-- Operator Management
CREATE OPERATOR <symbol>
    PARAMS (<type1>, <type2>, ...)
    RETURNS <return_type>
    [PROPERTIES '<properties>']
DROP OPERATOR <symbol>

-- Function Management
CREATE FUNCTION <name>
    PARAMS (<type1> <param1>, <type2> <param2>, ...)
    RETURNS <return_type>
    BODY '<formula_expression>'
DROP FUNCTION <name>

-- Rule Management
CREATE RULE <name>
    IF <hypothesis_expression>
    THEN <conclusion_expression>
DROP RULE <name>

-- User Management
CREATE USER <username> PASSWORD '<password>' [ROLE <role>]
DROP USER <username>
GRANT <privilege> ON <kb_name> TO <username>
REVOKE <privilege>> ON <kb_name> FROM <username>
```

#### DML Statements

```sql
-- Data Query
SELECT <concept_name> [WHERE <conditions>]
SELECT <concept_name> [JOIN <relation_name>] [WHERE <conditions>]

-- Data Manipulation
INSERT INTO <concept_name> VALUES (
    <field1> = <value1>,
    <field2> = <value2>,
    ...
)
UPDATE <concept_name> SET <field> = <value> [WHERE <conditions>]
DELETE FROM <concept_name> [WHERE <conditions>]

-- Reasoning
SOLVE <concept_name>
    KNOWN <known_conditions>
    FIND <unknown_variable>

-- Information
SHOW KNOWLEDGE BASES
SHOW CONCEPTS [IN <kb_name>]
SHOW RULES [IN <kb_name>]
SHOW RELATIONS [IN <kb_name>]
SHOW OPERATORS [IN <kb_name>]
SHOW FUNCTIONS [IN <kb_name>]
SHOW USERS
SHOW PRIVILEGES ON <kb_name>
SHOW PRIVILEGES OF <username>
```

---

## PHASE 3: STORAGE SCHEMA DESIGN (Song song với SQL)

### 3.1 File Structure Mapping

```
┌─────────────────────────────────────────────────────────────┐
│  SQL STATEMENT → FILE MAPPING                              │
├─────────────────────────────────────────────────────────────┤
│  CREATE KNOWLEDGE BASE → data/<kb_name>/                   │
│                           ├── metadata.bin                 │ ← KB metadata
│                           ├── concepts.bin                 │ ← Concept objects
│                           ├── hierarchies.bin             │ ← Hierarchy
│                           ├── relations.bin                │ ← Relations
│                           ├── operators.bin                │ ← Operators
│                           ├── functions.bin               │ ← Functions
│                           ├── rules.bin                   │ ← Rules
│                           ├── objects.bin                 │ ← Object instances
│                           ├── index.bin                   │ ← Index
│                           └── wal.log                     │ ← Write Ahead Log
│                                                              │
│  CREATE CONCEPT       → data/<kb_name>/concepts.bin         │
│  CREATE HIERARCHY   → data/<kb_name>/hierarchies.bin       │
│  CREATE RELATION      → data/<kb_name>/relations.bin          │
│ CREATE OPERATOR      → data/<kb_name>/operators.bin          │
│ CREATE FUNCTION      → data/<kb_name>/functions.bin         │
│ CREATE RULE          → data/<kb_name>/rules.bin             │
│ INSERT/UPDATE/DELETE → data/<kb_name>/objects.bin           │
│  CREATE USER         → data/users/users.bin                     │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Binary Format Design

```
┌─────────────────────────────────────────────────────────────┐
│                     KBMS Binary File Format                     │
├─────────────────────────────────────────────────────────────────┤
│  [4 bytes]  Magic Bytes: 0x4B 0x42 0x4D 0x53 ("KBMS")        │
│  [2 bytes]  Version: 0x0001 (version 1)                        │
│  [4 bytes]  Data Length (N) - length of encrypted data          │
│  [N bytes]  Encrypted Data:                                     │
│      [16 bytes] IV (Initialization Vector)                      │
│      [M bytes] AES-256 Encrypted Content (JSON)                │
│  [4 bytes] CRC32 Checksum (of all data except checksum)      │
└─────────────────────────────────────────────────────────────────┘
```

---

## PHASE 4: CONSISTENCY CHECK (Song song validation)

### 4.1 Validation Checklist

| Check Item | SQL Syntax | Data Structure | Validation Method |
|-------------|------------|----------------|-------------------|
| **KB Name uniqueness** | `CREATE KNOWLEDGE BASE <name>` | `KnowledgeBase.Name` | Check existing files in data/ |
| **Concept exists in KB** | `CREATE CONCEPT <name>` | `Concept.KbId` | Validate against KnowledgeBase.Id |
| **Variable type validation** | `<var>:<type>` | `Variable.Type` | Enum check (int, float, string) |
| **User authentication** | `CREATE USER <username>` | `User.Username` | Unique constraint |
| **Privilege level** | `GRANT <privilege>` | `User.KbPrivileges[key]` | Enum check (READ, WRITE, ADMIN) |
| **Object-Concept mapping** | `INSERT INTO <concept>` | `ObjectInstance.ConceptName` | Validate concept exists |
| **Rule-KB association** | `CREATE RULE <name>` | `Rule.KbId` | Check KB exists |

---

## PHASE 5: AST DESIGN (Song song với SQL và Structure)

### 5.1 AST Node Hierarchy

```
AstNode (abstract base)
├─ DdlNode (DDL statements - abstract)
│  ├─ CreateKbNode
│  ├─ DropKbNode
│  ├─ UseKbNode
│  ├─ CreateConceptNode
│  ├─ DropConceptNode
│ ├─ CreateRelationNode
│ ├─ CreateOperatorNode
│ ├─ CreateFunctionNode
│  ├─ CreateRuleNode
│ ├─ CreateUserNode
│  ├─ GrantNode
│ └─ RevokeNode
└─ DmlNode (DML statements - abstract)
   ├─ SelectNode
   ├─ InsertNode
   ├─ UpdateNode
   ├─ DeleteNode
   ├─ SolveNode
   └─ ShowNode
```

### 5.2 AST Node Specification

```csharp
// Base node
public abstract class AstNode
{
    public string Type { get; set; }        // Statement type
    public string? KbName { get; set; }    // Target KB (if applicable)
}

// DDL nodes
public class CreateKbNode : AstNode
{
    public string KbName { get; set; }
    public string? Description { get; set; }
}

public class CreateConceptNode : AstNode
{
    public string ConceptName { get; set; }
    public List<VariableDefinition> Variables { get; set; }
    public string? Constraints { get; set; }
}

public class VariableDefinition
{
    public string Name { get; set; }
    public string Type { get; set; }  // int, float, string, bool, etc.
}

// DML nodes
public class SelectNode : AstNode
{
    public string ConceptName { get; set; }
    public Dictionary<string, object>? Conditions { get; set; }
    public List<string>? Joins { get; set; }
}

public class InsertNode : AstNode
{
    public string ConceptName { get; set; }
    public Dictionary<string, object> Values { get; set; }
}

public class SolveNode : AstNode
{
    public string ConceptName { get; set; }
    public Dictionary<string, object> Known { get; set; }
    public string Find { get; set; }
}
```

---

## PHASE 6: IMPLEMENTATION ROADMAP

### 6.1 Implementation Order (Song song development)

```
┌─────────────────────────────────────────────────────────────┐
│  SPRINT 1: FOUNDATION                                      │
├─────────────────────────────────────────────────────────────┤
│  Song song:                                                │
│  [A] Code Data Models (KBMS.Models)                        │
│      [Song song với B]                                      │
│  [B] Define SQL Syntax Specification (document)                │
│                                                              │
│  Deliverables:                                              │
│  - All C# Model classes (Concept, Rule, etc.)                │
│  - Complete SQL syntax documentation                        │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│  SPRINT 2: STORAGE ENGINE                                  │
├─────────────────────────────────────────────────────────────┤
│ Song song:                                                │
│  [A] Code Storage Engine (file operations, serialization)    │
│      [Song song với B]                                      │
│  [B] Define file format specification                       │
│      [Song song với C]                                      │
│  [C] Design index schema (B+ Tree)                         │
│      [Song song với D]                                      │
│                                                              │
│ Deliverables:                                              │
│  - BinaryFormat.cs, Encryption.cs                             │
│  - StorageEngine.cs with CRUD                                │
│  - File format spec document                               │
│                                                              │
│ Deliverables:                                              │
│  - BinaryFormat.cs, Encryption.cs                             │
│  - StorageEngine.cs with CRUD                                │
│  - File format spec document                               │
│                                                              │
│                    ↓
┌─────────────────────────────────────────────────────────────┐
│  SPRINT 3: QUERY PARSER                                    │
├─────────────────────────────────────────────────────────────┤
│ Song song:                                                │
│  [A] Code Lexer (tokenization)                              │
│      [Song song với B]                                      │
│ [B] Define Token Types (enum)                             │
│      [Song song với C]                                      │
│      [C] Code AST nodes                                        │
│      [Song song với D]                                      │
│      [D] Code Parser (AST builder)                             │
│                                                              │
│ Deliverables:                                              │
│  - Lexer.cs, Token.cs, TokenType enum                        │
│  - All AST node classes                                     │
│ - QueryParser.cs with parse() method                       │
│                                                              │
│                    ↓
┌─────────────────────────────────────────────────────────────┐
│  SPRINT 4: SERVER COMPONENTS                               │
├─────────────────────────────────────────────────────────────�
│ Song song:                                                │
│ [A] Code ConnectionManager                                 │
│      [Song song với B]                                      │
│ [B] Code AuthenticationManager                             │
│      [Song song với C]                                      │
│ [C] Code KnowledgeManager (KBQL → Storage Engine)          │
│                                                              │
│ Deliverables:                                              │
│  - ConnectionManager.cs with session tracking                  │
│ - AuthenticationManager.cs with privilege checking           │
│ - KnowledgeManager.cs with query execution                  │
│                                                              │
│                    ↓
┌─────────────────────────────────────────────────────────────┐
│ SPRINT 5: CLI & TESTING                                 │
├─────────────────────────────────────────────────────────────┤
│ Deliverables:                                              │
│ - CLI client with interactive prompt                        │
│ - End-to-end tests for all SQL statements                 │
│ - Performance benchmarks                                 │
└─────────────────────────────────────────────────────────────┘
```

---

## PHASE 7: VERIFICATION MATRIX

### 7.1 SQL Syntax → Storage Engine Mapping

| SQL Statement | Storage Engine Method | File Modified |
|---------------|----------------|----------------|
| CREATE KNOWLEDGE BASE | `CreateKb()` | `data/<name>/metadata.bin` |
| CREATE CONCEPT | `CreateConcept()` | `data/<kb>/concepts.bin` |
| INSERT INTO | `InsertObject()` | `data/<kb>/objects.bin` |
| SELECT | `SelectObjects()` | `data/<kb>/objects.bin` |
| CREATE USER | `CreateUser()` | `data/users/users.bin` |
| GRANT | `GrantPrivilege()` | `data/users/users.bin` |

### 7.2 Integration Test Cases

```sql
-- Test 1: DDL → Storage
CREATE KNOWLEDGE BASE testkb
→ Verify: data/testkb/metadata.bin exists

-- Test 2: Concept Definition
CREATE CONCEPT PERSON (name:string, age:int)
Verify: concepts.bin contains PERSON definition

-- Test 3: DML → Storage
INSERT INTO PERSON VALUES (name='Alice', age=30)
Verify: objects.bin contains new instance

-- Test 4: Query → Retrieval
SELECT PERSON WHERE age=30
Verify: Returns Alice object

-- Test 5: Cascading
DROP KNOWLEDGE BASE testkb
Verify: data/testkb/ directory deleted
```

---

## DOCUMENTATION DELIVERABLES

Các file documentation đã được tạo:

| Document | Đường dẫn | Nội dung |
|-----------|------------|-----------|
| `sql-syntax.md` | `/Users/lechautranphat/Desktop/KBMS/docs/sql-syntax.md` | Complete SQL syntax reference |
| `data-structure.md` | `/Users/lechautranphat/Desktop/KBMS/docs/data-structure.md` | C# class definitions and relationships |
| `storage-schema.md` | `/Users/lechautranphat/Desktop/KBMS/docs/storage-schema.md` | File structure and binary format |
| `ast-design.md` | `/Users/lechautranphat/Desktop/KBMS/docs/ast-design.md` | AST node hierarchy and properties |
| `integration-mapping.md` | `/Users/lechautranphat/Desktop/KBMS/docs/integration-mapping.md` | SQL → Storage method mapping |
| `original_plan_sql_syntax.md` | `/Users/lechautranphat/Desktop/KBMS/original_plan_sql_syntax.md` | Bản sao của plan này (sau khi export) |

---

## CRITICAL FILES TO BE CREATED

| File Path | Purpose |
|------------|----------|
| `KBMS.Models/Concept.cs` | Concept data model |
| `KBMS.Models/Relation.cs` | Relation data model |
| `KBMS.Models/Operator.cs` | Operator data model |
| `KBMS.Models/Function.cs` | Function data model |
| `KBMS.Models/Rule.cs` | Rule data model |
| `KBMS.Models/ObjectInstance.cs` | Object instance model |
| `KBMS.Parser/Ast/AstNode.cs` | Base AST node |
| `KBMS.Parser/Ast/CreateKbNode.cs` | CREATE KB AST node |
| `KBMS.Parser/Ast/CreateConceptNode.cs` | CREATE CONCEPT AST node |
| `KBMS.Parser/Ast/SelectNode.cs` | SELECT AST node |
| `KBMS.Parser/Ast/InsertNode.cs` | INSERT AST node |
| `KBMS.Parser/Ast/SolveNode.cs` | SOLVE AST node |
| `/Users/lechautranphat/Desktop/KBMS/original_plan_sql_syntax.md` | Bản sao của plan này (sau khi export) |
