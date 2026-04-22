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

## 9. Ví dụ Thực tế - Xây dựng Hệ Tri thức Hình học

Dưới đây là ví dụ hoàn chỉnh về việc xây dựng một cơ sở tri thức hình học phẳng:

```kbql
-- Bước 1: Khởi tạo Knowledge Base
CREATE KNOWLEDGE BASE GeometryDB
DESCRIPTION "Hệ tri thức Hình học Phẳng - KBMS Demo";

USE GeometryDB;

-- Bước 2: Định nghĩa Khái niệm Điểm (Point)
CREATE CONCEPT Point (
    VARIABLES (
        x: DECIMAL,
        y: DECIMAL,
        label: STRING
    ),
    ALIASES (p, pt, coordinate),
    CONSTRAINTS (
        x IS NOT NULL,
        y IS NOT NULL
    )
);

-- Bước 3: Định nghĩa Khái niệm Đoạn thẳng (LineSegment)
CREATE CONCEPT LineSegment (
    VARIABLES (
        startPoint: Point,
        endPoint: Point,
        length: DECIMAL,
        slope: DECIMAL
    ),
    EQUATIONS (
        'length = Sqrt((endPoint.x - startPoint.x)^2 +
                      (endPoint.y - startPoint.y)^2)',
        'slope = (endPoint.y - startPoint.y) /
                (endPoint.x - startPoint.x)'
    ),
    CONSTRAINTS (
        length > 0,
        startPoint != endPoint
    )
);

-- Bước 4: Định nghĩa Khái niệm Đường tròn (Circle)
CREATE CONCEPT Circle (
    VARIABLES (
        center: Point,
        radius: DECIMAL,
        area: DECIMAL,
        circumference: DECIMAL
    ),
    EQUATIONS (
        'area = 3.14159 * radius^2',
        'circumference = 2 * 3.14159 * radius'
    ),
    CONSTRAINTS (radius > 0)
);

-- Bước 5: Định nghĩa Khái niệm Tam giác (Triangle)
CREATE CONCEPT Triangle (
    VARIABLES (
        vertexA: Point,
        vertexB: Point,
        vertexC: Point,
        sideA: DECIMAL,
        sideB: DECIMAL,
        sideC: DECIMAL,
        area: DECIMAL,
        perimeter: DECIMAL
    ),
    BASE_OBJECTS (vertexA, vertexB, vertexC),
    EQUATIONS (
        'sideA = Sqrt((vertexB.x - vertexC.x)^2 +
                     (vertexB.y - vertexC.y)^2)',
        'sideB = Sqrt((vertexA.x - vertexC.x)^2 +
                     (vertexA.y - vertexC.y)^2)',
        'sideC = Sqrt((vertexA.x - vertexB.x)^2 +
                     (vertexA.y - vertexB.y)^2)',
        'perimeter = sideA + sideB + sideC',
        'area = Sqrt(perimeter/2 *
                    (perimeter/2 - sideA) *
                    (perimeter/2 - sideB) *
                    (perimeter/2 - sideC))'
    ),
    CONSTRAINTS (
        sideA + sideB > sideC,
        sideB + sideC > sideA,
        sideA + sideC > sideB
    )
);

-- Bước 6: Thiết lập Phân cấp kế thừa
CREATE CONCEPT Shape (
    VARIABLES (color: STRING, filled: BOOLEAN)
);

ADD HIERARCHY Point IS_A Shape;
ADD HIERARCHY LineSegment IS_A Shape;
ADD HIERARCHY Triangle IS_A Shape;

-- Bước 7: Định nghĩa Luật dẫn cho phân loại tam giác
CREATE RULE ClassifyRightTriangle SCOPE Triangle
IF ABS(sideA^2 + sideB^2 - sideC^2) < 0.001
THEN SET type = 'Right';

CREATE RULE ClassifyEquilateral SCOPE Triangle
IF ABS(sideA - sideB) < 0.001 AND ABS(sideB - sideC) < 0.001
THEN SET type = 'Equilateral';

CREATE RULE ClassifyIsosceles SCOPE Triangle
IF ABS(sideA - sideB) < 0.001 OR ABS(sideB - sideC) < 0.001
THEN SET type = 'Isosceles';

-- Bước 8: Định nghĩa Quan hệ giữa các hình
CREATE RELATION Tangent FROM Circle TO LineSegment;
CREATE RELATION Inscribed FROM Triangle TO Circle;

-- Bước 9: Tạo chỉ mục để tối ưu truy vấn
CREATE INDEX idx_point_label ON Point (label);
CREATE INDEX idx_triangle_type ON Triangle (type);

-- Bước 10: Tạo Trigger tự động tính toán khi thêm tam giác mới
CREATE TRIGGER CalculateTriangleMetrics
ON (INSERT OF Triangle)
DO (
    UPDATE Triangle
    ATTRIBUTE (SET type: 'Scalene')
    WHERE type IS NULL
);
```

### 9.1. Ví dụ Hiệu chỉnh Cấu trúc (ALTER)

```kbql
-- Thêm thuộc tính mới vào Concept Point
ALTER CONCEPT Point (
    ADD (
        VARIABLE z: DECIMAL,
        CONSTRAINT z IS NOT NULL
    )
);

-- Thêm luật mới vào Triangle
ALTER CONCEPT Triangle (
    ADD (
        RULE IF area > 100 THEN SET size = 'Large'
    )
);

-- Xóa ràng buộc cũ
ALTER CONCEPT LineSegment (
    DROP (
        CONSTRAINT length > 0
    ),
    ADD (
        CONSTRAINT length >= 0
    )
);
```
