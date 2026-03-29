# KDL: Ngôn ngữ Định nghĩa Tri thức (Knowledge Definition Language)

KDL là tập hợp các lệnh dùng để định nghĩa cấu trúc, logic và các mối quan hệ trong Cơ sở tri thức (Knowledge Base). 

---

## 1. Quản lý Cơ sở Tri thức (Knowledge Base)

Lệnh khởi tạo hoặc xóa bỏ một vùng chứa tri thức logic.

*   **CREATE KNOWLEDGE BASE <name> [DESCRIPTION "<text>"]**: Khởi tạo KB mới.
*   **ALTER KNOWLEDGE BASE <name> SET (DESCRIPTION: "<text>")**: Cập nhật mô tả cho KB.
*   **DROP KNOWLEDGE BASE <name>**: Xóa toàn bộ dữ liệu và cấu trúc của KB.
*   **USE <name>**: Chuyển ngữ cảnh làm việc sang KB được chỉ định.

---

## 2. Định nghĩa Khái niệm (Concept)

Concept là thực thể cốt lõi trong KBMS, tương tự như Class trong OOP hay Table trong SQL nhưng linh hoạt hơn.

### Tạo Concept (CREATE CONCEPT)
Lệnh này định nghĩa cấu trúc của một Khái niệm. Bạn có thể định nghĩa theo kiểu liệt kê biến đơn giản hoặc sử dụng các khối chuyên sâu:

```kbql
CREATE CONCEPT <name> (
    VARIABLES (<var>: <type>, ...),
    ALIASES (<alias1>, <alias2>),
    BASE_OBJECTS (<obj1>, <obj2>),
    CONSTRAINTS (<if_then_constraints>),
    SAME_VARIABLES (<var1>, <var2>),
    CONSTRUCT_RELATIONS (<rel_definitions>),
    PROPERTIES (<prop1>, <prop2>),
    RULES (<logic_rules>),
    EQUATIONS (<math_equations>)
);
```

#### Chi tiết các khối nâng cao:
*   **ALIASES**: Định nghĩa các tên gọi khác cho Concept.
*   **BASE_OBJECTS**: Liệt kê các đối tượng cơ sở cấu thành nên Concept này (thường dùng trong PART_OF).
*   **CONSTRAINTS**: Các ràng buộc logic nội bộ phải thỏa mãn.
*   **SAME_VARIABLES**: Khai báo các biến ở các thành phần khác nhau nhưng trỏ cùng một giá trị tri thức.
*   **CONSTRUCT_RELATIONS**: Tự động tạo lập quan hệ khi dữ liệu được nạp vào.
*   **PROPERTIES**: Các đặc tính metadata của Concept.
*   **RULES / EQUATIONS**: Định nghĩa luật và phương trình ngay bên trong Concept thay vì tạo lệnh rời.

### Chỉnh sửa Concept (ALTER CONCEPT)
Hỗ trợ thay đổi cấu trúc mà không làm mất dữ liệu hiện có.

#### Cấu trúc lệnh
```kbql
ALTER CONCEPT <name> (
    ADD (
        VARIABLE <var>: <type>,
        RULE (<rule_definition>),
        CONSTRAINT (<expression>),
        EQUATION '<expression>',
        PROPERTY <key>: <value>
    ),
    DROP (
        VARIABLE <name>,
        RULE <name>,
        CONSTRAINT <name>
    ),
    RENAME (VARIABLE <old> TO <new>)
);
```

> [!NOTE]
> `ALTER CONCEPT` là lệnh duy nhất cho phép chỉnh sửa các thực thể nội bộ (internal rules, internal relations) mà không cần xóa đi tạo lại.

---

## 3. Quản lý Phân cấp (Hierarchy)

Định nghĩa quan hệ cha-con hoặc thành phần giữa các Concept.

*   **IS_A / ISA**: Quan hệ kế thừa (ví dụ: *Square IS_A Rectangle*).
*   **PART_OF**: Quan hệ thành phần (ví dụ: *Engine PART_OF Car*).

### Cú pháp
```kbql
ADD HIERARCHY <child> IS_A <parent>;
ADD HIERARCHY <part> PART_OF <whole>;
REMOVE HIERARCHY <parent> IS_A <child>;
```

---

## 4. Định nghĩa Quan hệ (Relation)

Mô tả các mối liên kết ngữ nghĩa giữa hai Concept.

### Cú pháp
```kbql
CREATE RELATION <name> 
FROM <domain> TO <range>
[PARAMS (<p1>, ...)]
[PROPERTIES (<transitive, symmetric, functional, ...>)];
```

---

## 5. Định nghĩa Luật (Rule)

Định nghĩa logic suy diễn tự động (Forward Chaining).

### Cú pháp
```kbql
CREATE RULE <name>
SCOPE <concept>
IF <condition>
THEN <action>;
```
*Ví dụ:* `CREATE RULE FeverCheck SCOPE Patient IF temp > 37.5 THEN SET status = 'Fever';`

---

## 6. Toán tử & Hàm Tùy biến (Operator & Function)

Mở rộng khả năng tính toán của hệ thống.

*   **CREATE OPERATOR <symbol>**: Định nghĩa toán tử mới (ví dụ: `|-|`).
*   **CREATE FUNCTION <name>**: Định nghĩa hàm con xử lý logic.

### Cú pháp chung
```kbql
CREATE {OPERATOR | FUNCTION} <identity>
PARAMS (<p1> <type1>, ...)
RETURNS <type>
BODY '<script_logic>';
```

---

## 7. Công thức tự động (Computation)

Gán công thức tính toán trực tiếp vào các biến của Concept.

### Cú pháp
```kbql
ADD COMPUTATION TO <concept>
VARIABLES <inputs>, <result>
FORMULA '<expression>';
```

---

## 8. Chỉ mục (Index)

Tối ưu hóa tốc độ truy vấn trên các biến cụ thể của Concept (sử dụng B+ Tree).

### Cú pháp
```kbql
CREATE INDEX <index_name> ON <concept_name> (<variable_list>);
DROP INDEX <index_name>;
```

---

## 9. Sự kiện tự động (Trigger)

Tự động thực thi một câu lệnh KBQL khi có sự kiện dữ liệu xảy ra.

### Cú pháp
```kbql
CREATE TRIGGER <name>
ON ( {INSERT|UPDATE|DELETE} OF <concept> )
DO ( <kbql_statement> );
```
*Ví dụ:* `CREATE TRIGGER LogInsert ON (INSERT OF Patient) DO (PRINT 'New patient added');`
