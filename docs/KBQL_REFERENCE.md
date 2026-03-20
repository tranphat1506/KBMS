# KBQL Reference (Knowledge Base Query Language)

KBQL is the specialized language for interacting with KBMS. It is divided into five functional sub-languages.

## 1. KDL (Knowledge Definition Language)
Used to define the structure of knowledge.

### `CREATE CONCEPT`
Defines a new knowledge concept with variables, constraints, and rules.
```kbql
CREATE CONCEPT ConceptName (
    VARIABLES (
        var1: type,
        var2: type
    )
    CONSTRAINTS (
        var1 = var2 * 2
    )
    RULES (
        kind: IF hypothesis THEN conclusion
    )
);
```

### `ADD HIERARCHY`
Defines an inheritance relationship.
```kbql
ADD HIERARCHY ChildConcept IS_A ParentConcept;
```

---

## 2. KML (Knowledge Manipulation Language)
Used to manage object instances.

### `INSERT`
```kbql
INSERT INTO ConceptName ATTRIBUTE ( var1: val1, var2: val2 );
```

### `UPDATE`
```kbql
UPDATE ConceptName SET ( var1: newVal ) WHERE id: "guid";
```

### `DELETE`
```kbql
DELETE FROM ConceptName WHERE id: "guid";
```

---

## 3. KQL (Knowledge Query Language)
Used to retrieve and reason over knowledge.

### `SELECT`
```kbql
SELECT * FROM ConceptName WHERE var1: val1;
```

### `SOLVE`
Invokes the inference engine to derive missing facts.
```kbql
SOLVE ON CONCEPT ConceptName 
GIVEN var1: val1, var2: val2 
FIND resultVar 
SAVE;
```

---

## 4. TCL (Transaction Control Language)
Used to manage atomic operations.

### `BEGIN TRANSACTION`
Starts a new transaction (initializes shadow pool).

### `COMMIT`
Promotes shadow changes to main memory and persists to disk.

### `ROLLBACK`
Discards all changes made since the last `BEGIN TRANSACTION`.

---

## 5. KCL (Knowledge Control Language)
Used for security and multi-user management.

### `LOGIN`
```kbql
LOGIN username password;
```

### `CREATE USER`
```kbql
CREATE USER username password;
```

### `GRANT` / `REVOKE`
```kbql
GRANT ALL ON KB kbName TO username;
REVOKE ALL ON KB kbName FROM username;
```
