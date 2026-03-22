# Hướng Dẫn Cú Pháp KBQL Chi Tiết (v1.1)

KBQL (Knowledge Base Query Language) là ngôn ngữ cốt lõi của KBMS, hỗ trợ mô hình tri thức thực thể và tính toán (COKB). Ngôn ngữ này được chia thành các nhóm lệnh chính: DDL (Định nghĩa), DML (Thao tác), DQL (Truy vấn & Suy luận), TCL (Giao dịch) và KML (Quản trị).

---

## 1. KDL (Knowledge Definition Language) - Định nghĩa Tri thức

### 1.1. Quản lý Knowledge Base
```sql
CREATE KNOWLEDGE BASE <KB_NAME> [DESCRIPTION '<String>'];
DROP KNOWLEDGE BASE <KB_NAME>;
USE <KB_NAME>;
```

### 1.2. Định nghĩa Khái niệm (CREATE CONCEPT)
Concept là thực thể chính chứa các biến (Variables), ràng buộc (Constraints), phương trình (Equations) và luật (Rules). Các khối con bên trong có thể viết trực tiếp hoặc bao quanh bởi dấu ngoặc đơn `( ... )`.

```sql
CREATE CONCEPT <ConceptName> (
    VARIABLES ( 
        id: INT, 
        price: DECIMAL(10, 2), 
        name: STRING(100),
        is_active: BOOLEAN
    ),
    -- Các khối sau đây đều hỗ trợ ngoặc đơn (optional)
    ALIASES ( alias1, alias2 ),
    CONSTRAINTS ( total_price = price * 1.1 ),
    EQUATIONS ( x + y = z ),
    PROPERTIES ( schema: 'inventory', version: '1.0' ),
    
    -- Khối RULES hỗ trợ nhiều luật, không bắt buộc tiền tố RULE:
    RULES ( 
        IF price > 1000 THEN luxury = true,
        RULE: PremiumRule: IF price > 500 THEN status = 'premium'
    )
);
```
**Kiểu dữ liệu hỗ trợ:** `INT`, `BIGINT`, `DECIMAL(L, S)`, `DOUBLE`, `FLOAT`, `STRING(L)`, `TEXT`, `BOOLEAN`, `DATE`, `DATETIME`.

### 1.3. Cập nhật Khái niệm (ALTER CONCEPT)
```sql
ALTER CONCEPT <Name> (
    ADD ( VARIABLE ( new_var: TYPE ) ),
    DROP ( VARIABLE <VarName> ),
    RENAME ( VARIABLE <OldName> TO <NewName> )
);
```

### 1.4. Định nghĩa Phân cấp (HIERARCHY)
Thiết lập mối quan hệ kế thừa hoặc thành phần giữa các Concept.
```sql
-- Tạo phân cấp (Child IS_A Parent hoặc Child PART_OF Parent)
CREATE HIERARCHY <ChildConcept> IS_A <ParentConcept>;
CREATE HIERARCHY <ChildConcept> PART_OF <ParentConcept>;

-- Xóa phân cấp (Lưu ý: Cú pháp ngược lại với CREATE)
REMOVE HIERARCHY <ParentConcept> IS_A <ChildConcept>;
```

### 1.5. Định nghĩa Quan hệ (RELATION)
```sql
CREATE RELATION <RelName> 
    [FROM <DomainConcept>] [TO <RangeConcept>]
    [PARAMS (p1, p2)]
    [RULES (...)];
```

### 1.6. Toán tử & Hàm tùy chỉnh (OPERATOR & FUNCTION)
```sql
-- Tạo toán tử tùy chỉnh (Ví dụ: +, -, *, / hoặc bất kỳ biểu tượng nào)
CREATE OPERATOR <Symbol> PARAMS (TYPE1, TYPE2) RETURNS TYPE_RES BODY "implementation";

-- Tạo hàm tùy chỉnh
CREATE FUNCTION <Name> PARAMS (p1 TYPE, p2 TYPE) RETURNS TYPE_RES BODY "implementation";
```

### 1.7. Ràng buộc & Logic bổ sung (INDEX & TRIGGER)
```sql
-- Tạo chỉ mục để tăng tốc truy vấn
CREATE INDEX <IndexName> ON <ConceptName> (var1, var2);

-- Tạo Trigger để tự động thực thi hành động khi có sự kiện (INSERT, UPDATE, DELETE)
CREATE TRIGGER <Name> ON ( INSERT OF <ConceptName> ) DO ( <Statement> );
```

---

## 2. KML (Knowledge Manipulation Language) - Thao tác Dữ liệu

### 2.1. Thêm và Cập nhật Dữ liệu
```sql
-- Thêm theo tên hoặc vị trí
INSERT INTO <Concept> ATTRIBUTE ( id: 1, name: 'A' );
INSERT INTO <Concept> ATTRIBUTE ( 2, 'B' );

-- Cập nhật với điều kiện
UPDATE <Concept> ATTRIBUTE ( SET price: price * 1.05 ) WHERE id = 1;

-- Xóa dữ liệu
DELETE FROM <Concept> WHERE id = 1;
```

### 2.2. Nhập/Xuất Dữ liệu
```sql
EXPORT (CONCEPT: <Name>, FORMAT: JSON, FILE: "path/to/file.json");
IMPORT (CONCEPT: <Name>, FORMAT: JSON, FILE: "path/to/file.json");
```

---

## 3. KQL (Knowledge Query Language) - Truy vấn & Suy luận

### 3.1. Truy vấn Dữ liệu (SELECT)
Hỗ trợ JOIN, WHERE, GROUP BY, ORDER BY, LIMIT.
```sql
-- Truy vấn thực thể (Default là CONCEPT)
SELECT * FROM <ConceptName> WHERE price > 100 ORDER BY price DESC;

-- Truy vấn thực thể cụ thể (RELATION, RULE, HIERARCHY)
SELECT * FROM RELATION <RelName>;
SELECT * FROM HIERARCHY <ConceptName>;

-- Truy vấn siêu dữ liệu (Metadata)
SELECT * FROM system.concepts;
SELECT * FROM <ConceptName>.variables;
```

### 3.2. Giải toán & Suy luận (SOLVE)
```sql
SOLVE ON CONCEPT <ConceptName> 
    GIVEN id: 1, price: 100 
    FIND total_price [SAVE];
```

### 3.3. Phân tích & Bảo trì
```sql
DESCRIBE (CONCEPT : <Name>);
DESCRIBE (HIERARCHY : 'Parent:Child');
EXPLAIN (SELECT * FROM <ConceptName>);
MAINTENANCE ( VACUUM, REINDEX(<Concept>), CHECK(CONSISTENCY:<Concept>) );
SHOW (CONCEPTS | HIERARCHIES | RULES | KNOWLEDGE BASES) [IN <KB>];
```

---

## 4. TCL (Transaction Control Language)
```sql
BEGIN TRANSACTION;
-- Các lệnh KBQL
COMMIT; -- Hoặc ROLLBACK;
```

---
*Lưu ý: Luôn sử dụng dấu chấm phẩy (;) để kết thúc mỗi câu lệnh.*
