# KBMS Usage Guide: 5 Real-World Scenarios

This guide demonstrates the versatility of KBMS through five practical application scenarios.

## 1. Geometric Assistant (Numeric Constraints)
**Goal**: Automatically calculate the area and perimeter of a rectangle.

```kbql
CREATE CONCEPT Rectangle (
    VARIABLES ( width: double, height: double, area: double, perimeter: double )
    CONSTRAINTS (
        area = width * height,
        perimeter = 2 * (width + height)
    )
);

-- Solve for missing facts
SOLVE ON CONCEPT Rectangle GIVEN width:5.0, height:10.0 FIND area, perimeter;
```

## 2. Family Tree & Inheritance (IS_A Hierarchy)
**Goal**: Model relationships and inherit properties.

```kbql
CREATE CONCEPT Person ( VARIABLES ( name: string, age: int ) );
CREATE CONCEPT Student ( VARIABLES ( gpa: double ) );

-- Define Hierarchy
ADD HIERARCHY Student IS_A Person;

-- Student now has 'name' and 'age' automatically
INSERT INTO Student ATTRIBUTE ( name: "Alice", age: 20, gpa: 3.8 );
```

## 3. Financial Auditor (Atomic Transactions)
**Goal**: Ensure a transfer between accounts is atomic.

```kbql
CREATE CONCEPT Account ( VARIABLES ( owner: string, balance: double ) );
INSERT INTO Account ATTRIBUTE ( owner: "A", balance: 1000 );
INSERT INTO Account ATTRIBUTE ( owner: "B", balance: 500 );

BEGIN TRANSACTION;
UPDATE Account SET ( balance: 900 ) WHERE owner: "A";
UPDATE Account SET ( balance: 600 ) WHERE owner: "B";
-- If something goes wrong, use ROLLBACK;
COMMIT;
```

## 4. Diagnostic Engine (Conditional Rules)
**Goal**: Use Forward Chaining to diagnose a technical issue.

```kbql
CREATE CONCEPT Computer (
    VARIABLES ( powerOn: bool, screenBlack: bool, status: string )
    RULES (
        D1: IF powerOn=true, screenBlack=true THEN status="Check Monitor Cable"
    )
);

SOLVE ON CONCEPT Computer GIVEN powerOn:true, screenBlack:true FIND status;
```

## 5. Security Access Control (KCL)
**Goal**: Manage multiple users for a specialized knowledge base.

```kbql
CREATE USER auditor p@ssword123;
CREATE KNOWLEDGE BASE secret_reports;

-- Grant access
GRANT ALL ON KB secret_reports TO auditor;

-- Verify access
LOGIN auditor p@ssword123;
USE secret_reports;
```
