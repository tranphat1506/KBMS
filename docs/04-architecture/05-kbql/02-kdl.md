# Ngôn ngữ Định nghĩa Tri thức (KDL)

**KDL** (Knowledge Definition Language) bao gồm tập hợp các lệnh chuyên dụng để định nghĩa cấu trúc dữ liệu, các ràng buộc logic và hệ thống quan hệ bên trong cơ sở tri thức.

## 1. Quản lý Cơ sở Tri thức

Cơ sở tri thức (Knowledge Base) là vùng chứa logic cấp cao nhất. Các lệnh quản trị bao gồm:

*   **CREATE KNOWLEDGE BASE <name> [DESCRIPTION "<text>"]**: Khởi tạo cơ sở tri thức mới với phần mô tả tùy chọn.
    *Ví dụ:* `CREATE KNOWLEDGE BASE PhysicsDB DESCRIPTION "Hệ tri thức Vật lý";`
*   **ALTER KNOWLEDGE BASE <name> SET (DESCRIPTION: "<text>")**: Hiệu chỉnh thông tin mô tả của cơ sở tri thức hiện tại.
    *Ví dụ:* `ALTER KNOWLEDGE BASE PhysicsDB SET (DESCRIPTION: "Cập nhật mô tả");`
*   **DROP KNOWLEDGE BASE <name>**: Giải phóng toàn bộ dữ liệu thực thể và cấu trúc hình thức liên quan.
    *Ví dụ:* `DROP KNOWLEDGE BASE PhysicsDB;`
*   **USE <name>**: Chuyển đổi ngữ cảnh làm việc sang cơ sở tri thức được chỉ định.
    *Ví dụ:* `USE PhysicsDB;`

## 2. Định nghĩa và Đặc tả Khái niệm (Concept)

Khái niệm (**Concept**) là thực thể hình thức hạt nhân trong hệ thống KBMS. Mỗi khái niệm đóng vai trò là một khuôn mẫu tri thức, cho phép đặc tả các thuộc tính và hành vi logic của đối tượng.

### 2.1. Cấu trúc Lệnh Khởi tạo Khái niệm

Lệnh `CREATE CONCEPT` cho phép định nghĩa một khái niệm mới thông qua các khối thành phần sau:

```kbql
CREATE CONCEPT <name> (
    VARIABLES (<var>: <type>, ...),
    ALIASES (<alias1>, ...),
    BASE_OBJECTS (<obj1>, ...),
    CONSTRAINTS (<expressions>),
    SAME_VARIABLES (<var1>, ...),
    CONSTRUCT_RELATIONS (<rel_definitions>),
    PROPERTIES (<prop1>, ...),
    RULES (<logic_rules>),
    EQUATIONS (<math_equations>)
);
```

*Ví dụ:*
```kbql
CREATE CONCEPT Patient (
    VARIABLES (name: STRING, age: INT, sys: INT, dia: INT, is_hypertension: BOOLEAN),
    CONSTRAINTS (age > 0)
);
```

### 2.2. Đặc tả các Khối Thành phần Nâng cao

Các khối thành phần trong lệnh định nghĩa khái niệm bao gồm:

-   **VARIABLES**: Khai báo danh sách các thuộc tính cơ sở (Tên: Kiểu dữ liệu).
-   **ALIASES**: Cung cấp các định danh thay thế để tăng tính linh hoạt trong truy vấn.
-   **BASE_OBJECTS**: Liệt kê các đối tượng thành phần (thường áp dụng trong quan hệ `PART_OF`).
-   **CONSTRAINTS**: Các điều kiện ràng buộc logic mà thực thể phải đảm bảo tính hợp lệ.
-   **SAME_VARIABLES**: Cơ chế đồng nhất hóa các biến số khác nhau về cùng một thực thể tri thức.
-   **RULES / EQUATIONS**: Tích hợp trực tiếp các luật dẫn và phương trình toán học vào cấu trúc khái niệm.

### 2.3. Hiệu chỉnh Cấu trúc Khái niệm (ALTER CONCEPT)

KBQL hỗ trợ hiệu chỉnh cấu trúc khái niệm mà không làm ảnh hưởng đến các thực thể (Facts) hiện có:

```kbql
ALTER CONCEPT <name> (
    ADD (
        VARIABLE <var>: <type>,
        RULE (<rule_definition>),
        CONSTRAINT (<expression>),
        EQUATION '<expression>'
    ),
    DROP (
        VARIABLE <name>,
        RULE <name>,
        CONSTRAINT <name>
    ),
    RENAME (VARIABLE <old> TO <new>)
);
```

*Ví dụ:*
```kbql
ALTER CONCEPT Patient (
    ADD (VARIABLE weight: INT),
    DROP (CONSTRAINT age_limit)
);
```

## 3. Thiết lập Hệ thống Phân cấp

Cơ cấu phân cấp hỗ trợ thiết lập các quan hệ kế thừa và cấu trúc thành phần giữa các khái niệm:

*   **Kế thừa (`IS_A`)**: Mô hình hóa quan hệ kế thừa tri thức (Ví dụ: *Tam Giác Vuông Kế thừa Tam Giác*).
*   **Thành phần (`PART_OF`)**: Mô hình hóa quan hệ cấu trúc vật lý (Ví dụ: *Động Cơ là Thành phần của Xe Ô tô*).

**Cú pháp thực thi:**
```kbql
ADD HIERARCHY <child> IS_A <parent>;
ADD HIERARCHY <part> PART_OF <whole>;
REMOVE HIERARCHY <parent> IS_A <child>;
```

*Ví dụ:*
```kbql
ADD HIERARCHY Triangle IS_A Shape;
ADD HIERARCHY Engine PART_OF Car;
```

## 4. Định nghĩa Quan hệ Ngữ nghĩa

Lệnh định nghĩa quan hệ cho phép mô tả các liên kết logic và toán học giữa hai khái niệm:

```kbql
CREATE RELATION <name> 
FROM <domain> TO <range>
[PARAMS (<p1>, ...)]
[PROPERTIES (<symmetric, transitive, ...>)];
```

*Ví dụ:*
```kbql
CREATE RELATION Orbits FROM Planet TO Star;
```

## 5. Định nghĩa Luật dẫn Hệ thống

Luật dẫn toàn cục hỗ trợ quy trình suy diễn tự động thông qua cơ chế lan truyền tri thức:

```kbql
CREATE RULE <name>
SCOPE <concept_scope>
IF <condition_expression>
THEN <action_logic>;
```

*Ví dụ:*
```kbql
CREATE RULE CheckBloodPressure SCOPE Patient 
IF sys > 140 OR dia > 90 
THEN SET is_hypertension = true;
```

## 6. Mở rộng Toán tử và Hàm số

Hệ thống cho phép người dùng mở rộng khả năng tính toán thông qua các thành phần thực thi tùy biến:

```kbql
CREATE {OPERATOR | FUNCTION} <identifier>
PARAMS (<p1> <type1>, ...)
RETURNS <type>
BODY '<logic_script>';
```

*Ví dụ:*
```kbql
CREATE FUNCTION GravityForce PARAMS (DOUBLE m1, DOUBLE m2, DOUBLE r) 
RETURNS DOUBLE BODY '(6.6743 * m1 * m2) / (r * r)';
```

## 7. Cơ chế Tối ưu hóa Chỉ mục

Hệ thống sử dụng cấu trúc **Cây B+** để tối ưu hóa hiệu năng truy xuất dữ liệu trên các thuộc tính chỉ định:

```kbql
CREATE INDEX <index_name> ON <concept_name> (<variable_list>);
DROP INDEX <index_name>;
```

*Ví dụ:*
```kbql
CREATE INDEX idx_patient_sys ON Patient (sys);
```

## 8. Quản lý Sự kiện Tự động (Triggers)

Cơ chế Trigger cho phép hệ thống tự động kích hoạt các câu lệnh KBQL khi có sự biến động về dữ liệu thực thể:

```kbql
CREATE TRIGGER <name>
ON ( {INSERT|UPDATE|DELETE} OF <concept> )
DO ( <kbql_statement> );
```

*Ví dụ:*
```kbql
CREATE TRIGGER SyncInventory 
ON (INSERT OF SalesOrder) 
DO (UPDATE Product ATTRIBUTE (SET stock: stock - 1) WHERE id = new.product_id);
```
