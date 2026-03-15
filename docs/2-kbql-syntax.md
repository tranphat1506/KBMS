# Ngôn ngữ truy vấn KBQL (Knowledge Base Query Language)

Ngôn ngữ KBQL bao gồm 2 tập lệnh: KBDDL (Định nghĩa) và KBDML (Thao tác).

## 1. KBDDL (Knowledge Base Definition Language)

### Quản trị KB
```sql
CREATE KNOWLEDGE BASE <name>;
USE <name>;
DROP KNOWLEDGE BASE <name>;
```

### Quản trị Concept
```sql
CREATE CONCEPT <name>
    VARIABLES (<var>:<type>, ...)
    [ALIASES <alias>, ...]
    [BASE_OBJECTS <parent_concept>, ...]
    [SAME_VARIABLES <var1>=<var2>, ...]
    [CONSTRAINTS <expr>, ...]
    [COMPUTATION ...]
    [CONSTRUCT_RELATIONS <relation_name>(args), ...]
    [EQUATIONS <expr1=expr2>, ...];

DROP CONCEPT <name>;
ADD VARIABLE <var>:<type> TO CONCEPT <name>;
```
*Các kiểu dữ liệu:* `INT`, `DOUBLE`, `VARCHAR`, `BOOLEAN`, `OBJECT`. Mở rộng cho phép nhúng trực tiếp tên Concept khác làm variable.

### Quan hệ & Phân cấp (Relation & Hierarchy)
```sql
ADD HIERARCHY <parent> IS_A <child>;
ADD HIERARCHY <parent> PART_OF <child>;

CREATE RELATION <name> FROM <source> TO <target> [PROPERTIES transitive, symmetric];
DROP RELATION <name>;
```

### Toán tử & Khái niệm tính toán (Operator, Function, Computation)
```sql
CREATE OPERATOR + PARAMS (number, number) RETURNS number;
CREATE FUNCTION heron PARAMS (DOUBLE a, DOUBLE b, DOUBLE c) RETURNS DOUBLE BODY '...';
ADD COMPUTATION TO <concept> VARIABLES a, b, c, S FORMULA '...' COST 1;
```

### Quản trị Luật (Rules)
```sql
CREATE RULE <name>
    TYPE <deduction|constraint|computation>
    SCOPE <concept>
    IF <conditions>
    THEN <conclusions>;
```

### Phân quyền
```sql
CREATE USER <name> PASSWORD '<pass>' ROLE <ROOT|USER>;
GRANT <READ|WRITE|ADMIN> ON <kb_name> TO <user>;
```

## 2. KBDML (Knowledge Base Manipulation Language)

### Truy vấn (Select)
```sql
SELECT <concept>;
SELECT <concept> WHERE <conditions>;
SELECT <concept> JOIN <relation> WHERE <conditions>;
SELECT COUNT(*) FROM <concept> GROUP BY <var>;
```

### Thêm / Sửa / Xóa đối tượng (Insert/Update/Delete)
```sql
INSERT INTO <concept> VALUES (field1=val1, field2=val2);
UPDATE <concept> SET field1=val1 WHERE conditions;
DELETE FROM <concept> WHERE conditions;
```

### Suy luận (Solve)
```sql
SOLVE <concept> KNOWN <known_vars> FIND <target_vars>;
-- Ví dụ: 
SOLVE TAMGIAC KNOWN a=3, b=4, is_right=true FIND S, c;
```
