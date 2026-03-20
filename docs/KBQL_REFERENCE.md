# KBQL Reference (Knowledge Base Query Language)

KBQL is the specialized language for interacting with KBMS, organized into five functional sub-languages.

---

## 1. KDL (Knowledge Definition Language)
KDL is used to define the schema and logic of your knowledge base.

### CREATE
Defines new knowledge entities.
- **KNOWLEDGE BASE**: `CREATE KNOWLEDGE BASE <KbName> [DESCRIPTION "<text>"];`
- **CONCEPT**: 
  ```kbql
  CREATE CONCEPT <ConceptName> (
      VARIABLES ( <var>: <type> [, ...] )
      [CONSTRAINTS ( [<name>:] <expression> [, ...] )]
      [RULES ( [TYPE <kind>] [VARIABLES (...)] [IF <hyp>] [THEN <concl>] [, ...] )]
  );
  ```
- **RELATION**: `CREATE RELATION <RelName> ( <arg1>, <arg2> [, ...] );`
- **OPERATOR**: `CREATE OPERATOR <Symbol> ...;` (Internal use)
- **FUNCTION**: `CREATE FUNCTION <Name> ...;` (Internal use)

### DROP
Removes existing knowledge entities.
- **KNOWLEDGE BASE**: `DROP KNOWLEDGE BASE <KbName>;`
- **CONCEPT**: `DROP CONCEPT <ConceptName>;`
- **RELATION**: `DROP RELATION <RelName>;`
- **RULE**: `DROP RULE <RuleId>;`
- **USER**: `DROP USER <Username>;`

### ADD / REMOVE
Manages relationships between entities.
- **HIERARCHY**: `ADD HIERARCHY <Child> IS_A <Parent>;`
- **HIERARCHY**: `REMOVE HIERARCHY <Child> IS_A <Parent>;`

---

## 2. KML (Knowledge Manipulation Language)
KML is used to manage object instances (facts).

### INSERT
Adds a new instance to a concept.
```kbql
INSERT INTO <ConceptName> ATTRIBUTE ( <var1>:<val1>, <var2>:<val2> );
```

### UPDATE
Modifies existing instances.
```kbql
UPDATE <ConceptName> ATTRIBUTE ( SET <var1>:<val1> [, ...] ) [WHERE <conditions>];
```

### DELETE
Removes instances.
```kbql
DELETE FROM <ConceptName> [WHERE <conditions>];
```

---

## 3. KQL (Knowledge Query Language)
KQL is used to retrieve and reason over knowledge.

### SELECT
Retrieves instances from a concept.
```kbql
SELECT [* | <var1>, <var2>] FROM <ConceptName> [WHERE <conditions>] [ORDER BY <var> [ASC|DESC]];
```

### SOLVE
Invokes the Inference Engine to derive new facts.
```kbql
SOLVE ON CONCEPT <ConceptName> 
[GIVEN <var1>:<val1>, ...] 
FIND <varA>, <varB> 
[SAVE];
```

### SHOW
Inspects system state.
- `SHOW KNOWLEDGE BASES;`
- `SHOW CONCEPTS [IN <KbName>];`
- `SHOW CONCEPT <Name> [IN <KbName>];`
- `SHOW RULES [IN <KbName>] [TYPE <kind>];`
- `SHOW USERS;`

---

## 4. TCL (Transaction Control Language)
TCL ensures data integrity through atomic operations.

### BEGIN TRANSACTION
Starts a new transaction using **Shadow Paging**.

### COMMIT
Promotes all shadow changes to the main knowledge base and persists to disk.

### ROLLBACK
Aborts the current transaction and discards all changes.

---

## 5. KCL (Knowledge Control Language)
KCL manages security and access control.

### LOGIN
Authenticates a user session.
```kbql
LOGIN <username> <password>;
```

### CREATE USER
```kbql
CREATE USER <username> <password> [ADMIN <true/false>];
```

### GRANT / REVOKE
```kbql
GRANT ALL ON KB <KbName> TO <username>;
REVOKE ALL ON KB <KbName> FROM <username>;
```

---

## Supported Data Types
- `int`: 32-bit integer.
- `double`: 64-bit floating point.
- `string`: UTF-8 string.
- `bool`: Boolean (`true`/`false`).
