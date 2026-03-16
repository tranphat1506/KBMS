# Integration Mapping - SQL ↔ Storage ↔ AST ↔ COKB

## Tổng quan

Tài liệu này mô tả mapping toàn diện giữa SQL statements, AST nodes, Storage operations, và COKB text structure.

---

## Complete Mapping Matrix

| # | SQL Statement | AST Node | Storage Method | COKB Logical File | Physical File | Status |
|---|--------------|----------|----------------|-------------------|----------------|--------|
| 1 | `CREATE KNOWLEDGE BASE <name>` | `CreateKbNode` | `CreateKb(name)` | N/A | `data/<name>/` (directory) | ✓ |
| 2 | `DROP KNOWLEDGE BASE <name>` | `DropKbNode` | `DropKb(name)` | N/A | `data/<name>/` (entire dir) | ✓ |
| 3 | `USE <name>` | `UseKbNode` | (session state) | N/A | N/A | ✓ |
| 4 | `CREATE CONCEPT <name>` | `CreateConceptNode` | `AddConcept(name)` | `CONCEPTS.TXT` | `data/<kb>/concepts.bin` | ✓ |
| 5 | `DROP CONCEPT <name>` | `DropConceptNode` | `RemoveConcept(name)` | `CONCEPTS.TXT` | `data/<kb>/concepts.bin` | ✓ |
| 6 | `ADD VARIABLE` | `AddVariableNode` | `AddVariableToConcept()` | `<CONCEPT>.TXT` | `data/<kb>/<CONCEPT>.bin` | ✓ |
| 7 | `ADD HIERARCHY <p> IS_A <c>` | `AddHierarchyNode` | `AddHierarchy()` | `HIERARCHY.TXT` | `data/<kb>/hierarchies.bin` | ✓ |
| 8 | `REMOVE HIERARCHY <p> <type> <c>` | `RemoveHierarchyNode` | `RemoveHierarchy()` | `HIERARCHY.TXT` | `data/<kb>/hierarchies.bin` | ✓ |
| 9 | `CREATE RELATION <name>` | `CreateRelationNode` | `CreateRelation()` | `RELATIONS.TXT` | `data/<kb>/relations.bin` | ✓ |
| 10 | `DROP RELATION <name>` | `DropRelationNode` | `DropRelation()` | `RELATIONS.TXT` | `data/<kb>/relations.bin` | ✓ |
| 11 | `CREATE OPERATOR <sym>` | `CreateOperatorNode` | `CreateOperator()` | `OPERATORS.TXT` | `data/<kb>/operators.bin` | ✓ |
| 12 | `DROP OPERATOR <sym>` | `DropOperatorNode` | `DropOperator()` | `OPERATORS.TXT` | `data/<kb>/operators.bin` | ✓ |
| 13 | `CREATE FUNCTION <name>` | `CreateFunctionNode` | `CreateFunction()` | `FUNCTIONS.TXT` | `data/<kb>/functions.bin` | ✓ |
| 14 | `DROP FUNCTION <name>` | `DropFunctionNode` | `DropFunction()` | `FUNCTIONS.TXT` | `data/<kb>/functions.bin` | ✓ |
| 15 | `ADD COMPUTATION TO <c>` | `AddComputationNode` | `AddComputation()` | `<CONCEPT>.TXT` | `data/<kb>/<CONCEPT>.bin` | ✓ |
| 16 | `REMOVE COMPUTATION <var> FROM <c>` | `RemoveComputationNode` | `RemoveComputation()` | `<CONCEPT>.TXT` | `data/<kb>/<CONCEPT>.bin` | ✓ |
| 17 | `CREATE RULE <name>` | `CreateRuleNode` | `CreateRule()` | `RULES.TXT` | `data/<kb>/rules.bin` | ✓ |
| 18 | `DROP RULE <name>` | `DropRuleNode` | `DropRule()` | `RULES.TXT` | `data/<kb>/rules.bin` | ✓ |
| 19 | `INSERT INTO <concept>` | `InsertNode` | `InsertFact()` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✓ |
| 20 | `SELECT <concept>` | `SelectNode` | `LoadFacts()` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✓ |
| 21 | `SELECT ... JOIN ...` | `SelectNode` + `JoinClause` | `LoadFacts() + Join` | `FACTS.TXT` | `data/<kb>/facts.bin` | ⚠️ |
| 22 | `SELECT COUNT(*)` | `SelectNode` + `AggregateClause` | `LoadFacts() + Aggregate` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✗ (planned) |
| 23 | `SELECT ... GROUP BY` | `SelectNode` + `GroupByClause` | `LoadFacts() + Group` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✗ (planned) |
| 24 | `SELECT ... ORDER BY` | `SelectNode` + `OrderByClause` | `LoadFacts() + Sort` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✗ (planned) |
| 25 | `SELECT ... LIMIT` | `SelectNode` + `LimitClause` | `LoadFacts() + Paginate` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✗ (planned) |
| 26 | `UPDATE <concept>` | `UpdateNode` | `UpdateFact()` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✓ |
| 27 | `DELETE FROM <concept>` | `DeleteNode` | `DeleteFact()` | `FACTS.TXT` | `data/<kb>/facts.bin` | ✓ |
| 28 | `SOLVE <concept>` | `SolveNode` | `SelectObjects() + Reasoning` | Multiple | Multiple | ✓ |
| 29 | `SHOW CONCEPTS` | `ShowNode` | `LoadConcepts()` | `CONCEPTS.TXT` | `data/<kb>/concepts.bin` | ✓ |
| 30 | `SHOW RULES` | `ShowNode` | `LoadRules()` | `RULES.TXT` | `data/<kb>/rules.bin` | ✓ |
| 31 | `SHOW RELATIONS` | `ShowNode` | `LoadRelations()` | `RELATIONS.TXT` | `data/<kb>/relations.bin` | ✓ |
| 32 | `SHOW OPERATORS` | `ShowNode` | `LoadOperators()` | `OPERATORS.TXT` | `data/<kb>/operators.bin` | ✓ |
| 33 | `SHOW FUNCTIONS` | `ShowNode` | `LoadFunctions()` | `FUNCTIONS.TXT` | `data/<kb>/functions.bin` | ✓ |
| 34 | `SHOW KNOWLEDGE BASES` | `ShowNode` | `ListKbs()` | N/A | Multiple directories | ✓ |
| 35 | `CREATE USER <name>` | `CreateUserNode` | `CreateUser()` | N/A | `data/users/users.bin` | ✓ |
| 36 | `DROP USER <name>` | `DropUserNode` | `DeleteUser()` | N/A | `data/users/users.bin` | ✓ |
| 37 | `GRANT <privilege>` | `GrantNode` | `GrantPrivilege()` | N/A | `data/users/users.bin` | ✓ |
| 38 | `REVOKE <privilege>` | `RevokeNode` | `RevokePrivilege()` | N/A | `data/users/users.bin` | ✓ |

**Legend:**
- ✓ = Đã hỗ trợ và đã implement
- ⚠️ = Đã hỗ trợ cơ bản nhưng cần implement đầy đủ
- ✗ (planned) = Được định nghĩa nhưng chưa implement

**Overall Progress:**
- Total features: 38
- Implemented: 30 (78.9%)
- Planned: 8 (21.1%)

---

## Detailed Flow: SQL → AST → Storage → COKB Text

### Flow 1: CREATE KNOWLEDGE BASE

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: SQL Input                                              │
│ SQL: CREATE KNOWLEDGE BASE geometry DESCRIPTION '...'       │
└────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Lexical Analysis (Lexer)                                │
│ Tokens: CREATE, KNOWLEDGE, BASE, geometry, DESCRIPTION, ...   │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Parsing (Parser)                                       │
│ AST: CreateKbNode                                               │
│   - Type: "CREATE_KNOWLEDGE_BASE"                               │
│   - KbName: "geometry"                                          │
│   - Description: "..."                                           │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 4: Permission Check (AuthManager)                          │
│ Action: "CREATE_KB"                                            │
│ Required: SystemAdmin = true (or ROOT)                         │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 5: Execute (KnowledgeManager → StorageEngine)              │
│ Method: StorageEngine.CreateKb(name, ownerId, desc)             │
│   1. Create directory: data/geometry/                           │
│   2. No COKB text file needed (KB = directory)              │
│   3. Save metadata to memory cache                                  │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Result: Created KB directory                                     │
└─────────────────────────────────────────────────────────────────┘
```

---

### Flow 2: CREATE CONCEPT

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: SQL Input                                              │
│ SQL: CREATE CONCEPT TAMGIAC VARIABLES (a:number, ...)        │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Parse AST                                           │
│ AST: CreateConceptNode                                           │
│   - Type: "CREATE_CONCEPT"                                    │
│   - ConceptName: "TAMGIAC"                                    │
│   - Variables: [{a: "number"}, {b: "number"}, ...]           │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Execute Storage Engine                                  │
│                                                               │
│ Update 1: CONCEPTS.TXT                                    │
│ COKB Text:                                                    │
│ begin_concepts                                              │
│ TAMGIAC                                                     │
│ ...                                                         │
│ end_concepts                                                │
│                                                               │
│ Update 2: Create TAMGIAC.TXT                               │
│ COKB Text:                                                    │
│ begin_concept: TAMGIAC[a, b, c, S]                 │
│ a: number;                                                  │
│ b: number;                                                  │
│ c: number;                                                  │
│ S: number;                                                  │
│                                                             │
│ begin_variables                                           │
│ a: number;                                                  │
│ b: number;                                                  │
│ c: number;                                                  │
│ S: number;                                                  │
│ end_variables                                             │
│                                                             │
│ begin_constraints                                        │
│ a>0                                                         │
│ b>0                                                         │
│ c>0                                                         │
│ a+b>c                                                      │
│ end_constraints                                          │
│                                                             │
│ end_concept                                                  │
│                                                               │
│ Step 4: Serialize to Binary                                     │
│ - CONCEPTS.TXT → concepts.bin                                   │
│ - TAMGIAC.TXT → TAMGIAC.bin                                  │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Result: Both files created/updated                              │
└─────────────────────────────────────────────────────────────────┘
```

---

### Flow 3: ADD COMPUTATION

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: SQL Input                                              │
│ SQL: ADD COMPUTATION TO TAMGIAC VARIABLES a, b, c, S         │
│         FORMULA 'sqrt(((a+b+c)/2)*...)'                  │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Parse AST                                           │
│ AST: AddComputationNode                                      │
│   - Type: "ADD_COMPUTATION"                                   │
│   - ConceptName: "TAMGIAC"                                     │
│   - InputVariables: ["a", "b", "c"]                           │
│   - ResultVariable: "S"                                       │
│   - Formula: "sqrt(((a+b+c)/2)...)"                          │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Execute Storage Engine                                  │
│                                                               │
│ Update TAMGIAC.TXT (add to begin_computation_relations):          │
│                                                             │
│ begin_relation                                                 │
│ flag = 1                                                       │
│ f = {a, b, c}                                               │
│ rf = 1                                                        │
│ vf = {S}                                                     │
│ expr = `sqrt(((a+b+c)/2)*...)`                              │
│ cost = 1                                                      │
│ end_relation                                                   │
│                                                             │
│ end_computation_relations                                     │
│                                                             │
│ Step 4: Serialize to Binary                                     │
│ - TAMGIAC.TXT → TAMGIAC.bin                                  │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Result: Computation added to concept                        │
└─────────────────────────────────────────────────────────────────┘
```

---

### Flow 4: INSERT INTO (Fact)

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: SQL Input                                              │
│ SQL: INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5, S=0)    │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Parse AST                                           │
│ AST: InsertNode                                                │
│   - Type: "INSERT"                                              │
│   - ConceptName: "TAMGIAC"                                     │
│   - Values: {a: 3, b: 4, c: 5, S: 0}                    │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Execute Storage Engine                                  │
│                                                               │
│ Update FACTS.TXT:                                                │
│ begin_facts                                                      │
│ [TAMGIAC, a=3, b=4, c=5, S=0]                               │
│ ...                                                             │
│ end_facts                                                       │
│                                                               │
│ Step 4: Serialize to Binary                                     │
│ - FACTS.TXT → facts.bin                                        │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Result: Fact inserted                                       │
└─────────────────────────────────────────────────────────────────┘
```

---

### Flow 5: SELECT

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: SQL Input                                              │
│ SQL: SELECT TAMGIAC WHERE a=3 AND b=4                       │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Parse AST                                           │
│ AST: SelectNode                                                  │
│   - Type: "SELECT"                                             │
│   - ConceptName: "TAMGIAC"                                      │
│   - Conditions: [{Field: "a", Operator: "=", Value: 3}, ...]      │
└─────────────────────────────┬───────────────────────────────────�
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Execute Storage Engine                                  │
│                                                               │
│ 1. Deserialize facts.bin → FACTS.TXT (if not cached)              │
│ 2. Parse FACTS.TXT to List<Fact>                              │
│ 3. Filter facts by conditions                                        │
│                                                               │
│ Filter logic:                                                     │
│   - Read all facts with ConceptName = "TAMGIAC"                     │
│   - Apply each condition: field = value                         │
└─────────────────────────────┬───────────────────────────────────�
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Result: List<Fact> matching conditions                     │
└─────────────────────────────────────────────────────────────────┘
```

---

### Flow 6: SOLVE (Reasoning)

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: SQL Input                                              │
│ SQL: SOLVE TAMGIAC FOR S GIVEN a=3, b=4, c=5            │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Parse AST                                           │
│ AST: SolveNode                                                 │
│   - Type: "SOLVE"                                               │
│   - ConceptName: "TAMGIAC"                                     │
│   - FindVariable: "S"                                          │
│   - Known: {a: 3, b: 4, c: 5}                                 │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Load Knowledge                                      │
│   - Load CONCEPTS.TXT → ConceptDetail                           │
│   - Load RULES.TXT → Rules                                         │
│   - Load OPERATORS.TXT → Operators                                   │
│   - Load FUNCTIONS.TXT → Functions                                   │
│   - Load facts from FACTS.TXT → matching facts                     │
│                                                               │
│ Step 4: Reasoning Engine Execution                             │
│                                                               │
│ 1. Find applicable computation rules for TAMGIAC                    │
│   - Use forward chaining with known values                             │
│   - Execute: a=3, b=4, c=5                                 │
│   - Computation: S = sqrt(((3+4+5)/2) * ...)                   │
│   - Result: S = 6                                              │
└─────────────────────────────┬───────────────────────────────────�
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Result: S = 6                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                          CLI Client                            │
│  ┌──────────────────────────────────────────────────┐  │
│  │  User Input: SQL Query                         │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────┬───────────────────────────────────────────┘
                    │ TCP
                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Server Layer                           │
│  ┌──────────────┐  ┌──────────────────────────────┐      │
│  │  Connection │  │   Authentication    │      │      │
│  │  Manager    │←→│   Manager        │      │      │
│  └──────────────┘  └──────────────────────┘      │      │
│                     │           │                        │      │
│  │           │         ↓                        │      │
│  │           │   ┌──────────────────────┐  │      │
│ │           │   │ Query Parser      │  │      │
│ │           │   │ (Lexer → AST)      │  │      │
│ │           │   └──────────────────────┘  │      │
│ │           │                                 │      │
│ │           │   ┌──────────────────┐      │      │
│ │           │   │ Knowledge Manager│      │      │
│ │           │   │ (Reasoning Engine)│      │      │
│ │           │   └──────────────────┘      │      │
│ │           │                                 │      │
│ │           │                                 │      │
│ │           │   ┌──────────────────┐      │      │
│ │           │   │   Storage Engine     │      │      │
│ │           │   │   (Text ↔ Binary) │      │      │
│ │           │   │   - Parser         │      │      │
│ │           │   │   - Serializer    │      │      │
│ │           │   │   - Decrypter     │      │      │
│ │           │   │   - COKB Parser  │      │      │
│ │           │   └──────────────────┘      │      │
│ └───────────────┴───────────────────────────────────────┴──────────────────────┘
                    │ File I/O
                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Physical Storage                           │
│                                                              │
│   data/                                                       │
│     ├── geometry/                                            │
│     │   ├── CONCEPTS.TXT ← Logical (concepts list)             │
│     │   ├── concepts.bin ← Serialized binary                │
│     │   │                                                      │
│     │   ├── HIERARCHY.TXT ← Logical (hierarchies)        │
│     │   ├── hierarchies.bin ← Serialized binary            │
│     │   │                                                      │
│     │   ├── RELATIONS.TXT ← Logical (relations)         │
│     │   ├── relations.bin ← Serialized binary              │
│     │   │                                                      │
│     │   ├── OPERATORS.TXT ← Logical (operators)          │
│     │   ├── operators.bin ← Serialized binary            │
│     │   │                                                      │
│     │   ├── FUNCTIONS.TXT ← Logical (functions)        │
│     │   ├── functions.bin ← Serialized binary              │
│     │   │                                                      │
│     │   ├── RULES.TXT ← Logical (global rules)            │
│     │   ├── rules.bin ← Serialized binary                │
│     │   │                                                      │
│     │   ├── <CONCEPT>.TXT ← Logical (concept details)    │
│     │   │   - TAMGIAC.TXT                                     │
│     │   │   - PERSON.TXT                                    │
│     │   │   - TAMGIAC.bin ← Serialized binary                │
│     │   │   - PERSON.bin ← Serialized binary                │
│     │   │                                                      │
│     │   ├── FACTS.TXT ← Logical (facts)                 │
│     │   └── facts.bin ← Serialized binary                │
│     │   │                                                      │
│     │   └── users/                                                   │
│     │       └── users.bin ← Serialized binary                │
└─────────────────────────────────────────────────────────────────┘
```

---

## COKB Text ↔ Binary ↔ C# Object

### Example: Complete Round-Trip

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. SQL → COKB Text (Logical Structure)                     │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ CONCEPTS.TXT:                                                │
│ begin_concepts                                                │
│ TAMGIAC                                                      │
│ PERSON                                                      │
│ end_concepts                                                │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ HIERARCHY.TXT:                                              │
│ begin_Hierarchy                                              │
│ [HINHHOC, TAMGIAC]                                        │
│ [CANH, TAMGIAC]                                              │
│ end_Hierarchy                                                │
└─────────────────────────────┬───────────────────────────────────�
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ RELATIONS.TXT:                                              │
│ begin_Relations                                              │
│ [LA_CHA, PERSON, PERSON] {"transitive"}                   │
│ end_Relations                                                │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ FACTS.TXT:                                                      │
│ begin_facts                                                    │
│ [TAMGIAC, a=3, b=4, c=5, S=6]                              │
│ [PERSON, name="Alice", age=30]                                  │
│ end_facts                                                      │
└─────────────────────────────┬───────────────────────────────────�
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. COKB Text → Binary (Physical Storage)                   │
│ CONCEPTS.TXT → concepts.bin                                   │
│ HIERARCHY.TXT → hierarchies.bin                                 │
│ RELATIONS.TXT → relations.bin                                  │
│ FACTS.TXT → facts.bin                                        │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Binary → C# Objects (In-Memory Cache)                   │
│ Deserialize → concepts.bin → List<string>                          │
│ Deserialize → hierarchies.bin → List<Hierarchy>                    │
│ Deserialize → relations.bin → List<Relation>                         │
│ Deserialize → facts.bin → List<Fact>                                 │
└─────────────────────────────┬───────────────────────────────────�
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. C# Objects → COKB Text (For updates)                │
│ Modify in-memory objects → Serialize back to binary          │
└─────────────────────────────────────────────────────────────────┘
```

---

## Permission Check Matrix

| AST Node | Action | KB Required | Required Privilege (USER) | ROOT? |
|----------|--------|-------------|---------------------------|-------|
| `CreateKbNode` | CREATE_KB | No | SystemAdmin = true | ✓ |
| `DropKbNode` | DROP_KB | Yes | ADMIN on KB | ✓ |
| `UseKbNode` | SELECT | Yes | READ on KB | ✓ |
| `CreateConceptNode` | CREATE_CONCEPT | Yes | ADMIN on KB | ✓ |
| `AddVariableNode` | CREATE_CONCEPT | Yes | ADMIN on KB | ✓ |
| `DropConceptNode` | DROP_CONCEPT | Yes | ADMIN on KB | ✓ |
| `AddHierarchyNode` | CREATE_CONCEPT | Yes | ADMIN on KB | ✓ |
| `RemoveHierarchyNode` | DROP_CONCEPT | Yes | ADMIN on KB | ✓ |
| `CreateRelationNode` | CREATE_RELATION | Yes | ADMIN on KB | ✓ |
| `DropRelationNode` | DROP_RELATION | Yes | ADMIN on KB | ✓ |
| `CreateOperatorNode` | CREATE_OPERATOR | Yes | ADMIN on KB | ✓ |
| `DropOperatorNode` | DROP_OPERATOR | Yes | ADMIN on KB | ✓ |
| `CreateFunctionNode` | CREATE_FUNCTION | Yes | ADMIN on KB | ✓ |
| `DropFunctionNode` | DROP_FUNCTION | Yes | ADMIN on KB | ✓ |
| `AddComputationNode` | CREATE_CONCEPT | Yes | ADMIN on KB | ✓ |
| `RemoveComputationNode` | DROP_CONCEPT | Yes | ADMIN on KB | ✓ |
| `CreateRuleNode` | CREATE_RULE | Yes | ADMIN on KB | ✓ |
| `DropRuleNode` | DROP_RULE | Yes | ADMIN on KB | ✓ |
| `CreateUserNode` | CREATE_USER | No | SystemAdmin = true | ✓ |
| `DropUserNode` | DROP_USER | No | SystemAdmin = true | ✓ |
| `GrantNode` | GRANT | No | SystemAdmin = true | ✓ |
| `RevokeNode` | REVOKE | No | SystemAdmin = true | ✓ |
| `SelectNode` | SELECT | Yes | READ on KB | ✓ |
| `InsertNode` | INSERT | Yes | WRITE on KB | ✓ |
| `UpdateNode` | UPDATE | Yes | WRITE on KB | ✓ |
| `DeleteNode` | DELETE | Yes | WRITE on KB | ✓ |
| `SolveNode` | SELECT | Yes | READ on KB | ✓ |
| `ShowNode` | SELECT | Yes | READ on KB | ✓ |

---

## Error Handling Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Parse Error (Invalid Syntax)                                 │
│    Lexer/Parser throws ParseException                            │
│    → Return ERROR message to client                             │
│                                                                 │
│ 2. Permission Denied                                           │
│    AuthenticationManager.CheckPrivilege() returns false          │
│    → Return ERROR: "Permission denied"                          │
│                                                                 │
│ 3. KB Not Found                                                │
│    StorageEngine.KbExists() returns false                       │
│    → Return ERROR: "Knowledge base not found"                  │
│                                                                 │
│ 4. Concept Not Found                                           │
│    Concept not in CONCEPTS.TXT                               │
│    → Return ERROR: "Concept not found"                          │
│                                                                 │
│ 5. File Corruption                                             │
│    CRC32 checksum mismatch or invalid magic bytes               │
│    → Return ERROR: "Data corrupted, restore from WAL"          │
│                                                                 │
│ 6. COKB Parse Error                                          │
│    Invalid text format, missing begin/end markers              │
│    → Return ERROR with line/column number                             │
│                                                                 │
│ 7. Constraint Violation                                        │
│    Object values violate concept constraints                     │
│    → Return ERROR: "Constraint violation"                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Type Conversion

| SQL Type | AST Value Type | C# Type | COKB Text |
|----------|----------------|----------|-----------|
| `number` | `double` (Number token) | `double` | `number` |
| `string` | `string` (String token) | `string` | `string` |
| `boolean` | `true/false` (Keyword) | `bool` | `boolean` |
| `identifier` | `string` (Identifier token) | `<concept>` | `<concept>` |

---

## Summary: Three-Layer Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Layer 1: SQL Syntax (User Interface)                        │
│  KBQL = KBDDL + KBDML                                    │
│  - DDL: CREATE, DROP, USE, GRANT, REVOKE            │
│  - DML: SELECT, INSERT, UPDATE, DELETE, SOLVE, SHOW      │
└─────────────────────────────┬───────────────────────────────────┘
                              │ 1:1 Mapping
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Layer 2: AST (Internal Representation)                        │
│ AstNode hierarchy with DdlNode and DmlNode                       │
│ - Each SQL statement maps to one AST node class             │
│ - AST nodes capture all query parameters                       │
└─────────────────────────────┬───────────────────────────────────�
                              │ 1:1 Mapping
                              ▼
┌─────────────────────────────────────────────────────────────────�
│ Layer 3: Storage (Persistent Data)                           │
│ ┌──────────────┐          ┌──────────────────────┐    │
│ │ Logical    │          │   Physical       │    │
│ │ (COKB)    │          │   Binary        │    │
│ │ Format   │          │   (Encrypted)   │    │
│ │           │          │                │    │
│ │ begin_xxx   │          │                │    │
│ │ end_xxx     │          │                │    │
│ └──────────────┘          └───────────────────────┘    │
└───────────────────────────────────────────────────────┴──────────────────┘
```

---

## Key Features

### 1. Hybrid Storage Approach
- **Logical**: Human-readable COKB text format for debugging
- **Physical**: Encrypted binary for performance and security
- **Transparent**: Automatic conversion between logical and physical

### 2. COKB Compliance
- All COKB markers respected: begin_xxx, end_xxx
- Proper nested structure for concept details
- Bracket notation for arrays
- Property notation: `{prop1", "prop2"}`

### 3. SQL Enhancements
- More natural syntax (VARIABLES, ALIASES, etc.)
- Type support: number, string, boolean, object
- Rule types: deduction, default, constraint, computation
- Cost/weight for optimization

### 4. Performance Optimization
- Cache deserialized COKB text in memory
- Lazy serialization (only on changes)
- B+ Tree index for fast lookup (future)
- WAL for crash recovery

### 5. Full Bidirectional Support
- SQL ↔ AST (Parser)
- AST ↔ C# (Executor)
- C# ↔ COKB (Serializer)
- COKB ↔ Binary (Storage)
