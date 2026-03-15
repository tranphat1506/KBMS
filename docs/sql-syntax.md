# KBQL Syntax Reference (KBDDL + KBDML)

## Tổng quan

KBQL (Knowledge Base Query Language) là ngôn ngữ truy vấn của KBMS, bao gồm 2 phần:
- **KBDDL** (Knowledge Base Definition Language): Định nghĩa cấu trúc cơ sở tri thức
- **KBDML** (Knowledge Base Manipulation Language): Thao tác dữ liệu và truy vấn

---

## KBDDL - Knowledge Base Definition Language

### Knowledge Base Management

#### CREATE KNOWLEDGE BASE
Tạo một cơ sở tri thức mới.

```sql
CREATE KNOWLEDGE BASE <kb_name> [DESCRIPTION '<description>']
```

**Tham số:**
- `<kb_name>`: Tên của Knowledge Base (phải unique, chỉ chứa chữ cái, số, underscore)
- `<description>`: Mô tả tùy chọn về KB

**Ví dụ:**
```sql
CREATE KNOWLEDGE BASE geometry
CREATE KNOWLEDGE BASE geometry DESCRIPTION 'Hệ thống tri thức hình học'
```

---

#### DROP KNOWLEDGE BASE
Xóa một cơ sở tri thức.

```sql
DROP KNOWLEDGE BASE <kb_name>
```

**Tham số:**
- `<kb_name>`: Tên của Knowledge Base cần xóa

**Lưu ý:** Xóa KB sẽ xóa tất cả dữ liệu liên quan (concepts, objects, rules, etc.)

**Ví dụ:**
```sql
DROP KNOWLEDGE BASE geometry
```

---

#### USE
Chọn Knowledge Base hiện tại để làm việc.

```sql
USE <kb_name>
```

**Tham số:**
- `<kb_name>`: Tên của Knowledge Base

**Ví dụ:**
```sql
USE geometry
```

---

### Concept Management

#### CREATE CONCEPT
Tạo một khái niệm (concept) mới với đầy đủ thông tin.

```sql
CREATE CONCEPT <concept_name>
    VARIABLES (<var1>:<type>, <var2>:<type>, ...)
    [ALIASES <alias1>, <alias2>, ...]
    [BASE_OBJECTS <base1>, <base2>, ...]
    [CONSTRAINTS <constraint_expr1>, <constraint_expr2>, ...]
    [SAME_VARIABLES <var1>=<var2>, <var3>=<var4>, ...]
```

**Tham số:**
- `<concept_name>`: Tên của Concept (unique trong KB)
- `<var_name>`: Tên biến
- `<type>`: Kiểu dữ liệu (number, string, boolean, object)
- `<aliasN>`: Tên gọi khác của concept
- `<baseN>`: Các đối tượng nền (base objects)
- `<constraint_exprN>`: Biểu thức ràng buộc
- `<var1>=<var2>`: Các biến tương đương nhau

**Kiểu dữ liệu hỗ trợ (giống MySQL):**

### Numeric Types
| Kiểu | SQL | Storage Size | Mô tả |
|-------|-----|--------------|---------|
| `TINYINT` | TINYINT | 1 byte | -128 ~ 127 |
| `SMALLINT` | SMALLINT | 2 bytes | -32,768 ~ 32,767 |
| `INT` | INT | 4 bytes | ±2.1 tỷ |
| `BIGINT` | BIGINT | 8 bytes | ±9.2e18 |
| `FLOAT` | FLOAT | 4 bytes | ~7 chữ số thập phân |
| `DOUBLE` | DOUBLE | 8 bytes | ~15 chữ số thập phân |
| `DECIMAL` | DECIMAL(p,s) | biến | Số thập phân chính xác (cho tiền, GPS) |
| `number` | DOUBLE | 8 bytes | Alias cho DOUBLE (để tương thích cũ) |

### String Types
| Kiểu | SQL | Mô tả |
|-------|-----|---------|
| `VARCHAR` | VARCHAR(n) | Chuỗi có độ dài thay đổi |
| `CHAR` | CHAR(n) | Chuỗi có độ dài cố định |
| `TEXT` | TEXT | Chuỗi dài không giới hạn |
| `string` | VARCHAR | Alias cho VARCHAR (để tương thích cũ) |

### Boolean Type
| Kiểu | SQL | Mô tả |
|-------|-----|---------|
| `BOOLEAN` | BOOLEAN | true/false |
| `boolean` | BOOLEAN | Alias cho BOOLEAN (để tương thích cũ) |

### Date/Time Types
| Kiểu | SQL | Mô tả |
|-------|-----|---------|
| `DATE` | DATE | Ngày (YYYY-MM-DD) |
| `DATETIME` | DATETIME | Ngày giờ (YYYY-MM-DD HH:MM:SS) |
| `TIMESTAMP` | TIMESTAMP | Timestamp |

### Reference Type
| Kiểu | SQL | Mô tả |
|-------|-----|---------|
| `OBJECT` | object | Tham chiếu đến một Concept khác |

**Ví dụ:**
```sql
-- Tạo concept DIEM với tọa độ là INT (tiết kiệm storage)
CREATE CONCEPT DIEM
    VARIABLES (x:INT, y:INT, name:VARCHAR(50))

-- Tạo concept TAMGIAC với tọa độ là INT, diện tích là DOUBLE
CREATE CONCEPT TAMGIAC
    VARIABLES (a:INT, b:INT, c:INT, S:DOUBLE)
    ALIASES TRIANGLE, TAMGIAC_VUONG
    BASE_OBJECTS TAMGIAC_DEU, TAMGIAC_CAN
    CONSTRAINTS a>0, b>0, c>0, a+b>c, a+c>b, b+c>a

-- Tạo concept PERSON với age là INT, name là VARCHAR
CREATE CONCEPT PERSON
    VARIABLES (name:VARCHAR(100), age:INT, active:BOOLEAN)

-- Tạo concept PRODUCT cho tài chính với giá là DECIMAL
CREATE CONCEPT PRODUCT
    VARIABLES (name:VARCHAR(200), price:DECIMAL(10,2), quantity:INT)
```

---

#### ADD CONCEPT VARIABLE
Thêm biến vào concept đã tồn tại.

```sql
ADD VARIABLE <var_name>:<type> TO CONCEPT <concept_name>
```

**Ví dụ:**
```sql
ADD VARIABLE perimeter:number TO CONCEPT TAMGIAC
```

---

#### DROP CONCEPT
Xóa một khái niệm.

```sql
DROP CONCEPT <concept_name>
```

**Lưu ý:** Sẽ báo lỗi nếu concept đang được sử dụng bởi objects hoặc rules

**Ví dụ:**
```sql
DROP CONCEPT TAMGIAC
```

---

### Hierarchy Management

#### ADD HIERARCHY
Thêm quan hệ phân cấp giữa các concepts.

```sql
ADD HIERARCHY <parent_concept> IS_A <child_concept>
ADD HIERARCHY <parent_concept> PART_OF <child_concept>
```

**Loại quan hệ:**
- `IS_A`: Khái nghiệm "là một" (kế thừa/đặc biệt hóa)
- `PART_OF`: Khái nghiệm "là một phần của" (thành phần)

**Ví dụ:**
```sql
-- HINHHOC là lớp cha, TAMGIAC là lớp con (TAMGIAC IS_A HINHHOC)
ADD HIERARCHY HINHHOC IS_A TAMGIAC

-- CANH là một phần của TAMGIAC
ADD HIERARCHY CANH PART_OF TAMGIAC
```

---

#### REMOVE HIERARCHY
Xóa quan hệ phân cấp.

```sql
REMOVE HIERARCHY <parent_concept> IS_A <child_concept>
REMOVE HIERARCHY <parent_concept> PART_OF <child_concept>
```

**Ví dụ:**
```sql
REMOVE HIERARCHY HINHHOC IS_A TAMGIAC
```

---

### Relation Management

#### CREATE RELATION
Tạo quan hệ giữa hai concepts.

```sql
CREATE RELATION <relation_name>
    FROM <domain_concept> TO <range_concept>
    [PROPERTIES <prop1>, <prop2>, ...]
```

**Tham số:**
- `<relation_name>`: Tên quan hệ
- `<domain_concept>`: Concept nguồn
- `<range_concept>`: Concept đích
- `<propN>`: Thuộc tính (transitive, symmetric, reflexive, etc.)

**Thuộc tính quan hệ:**
- `transitive`: Bắc cầu (nếu A→B và B→C thì A→C)
- `symmetric`: Đối xứng (nếu A→B thì B→A)
- `reflexive`: Phản xạ (A→A)
- `antisymmetric`: Phản đối xứng
- `functional`: Đơn trị

**Ví dụ:**
```sql
-- Quan hệ cha - con
CREATE RELATION IS_FATHER_OF
    FROM PERSON TO PERSON
    PROPERTIES transitive

-- Quan hệ học lớp
CREATE RELATION STUDIES_IN
    FROM STUDENT TO CLASS
```

---

#### DROP RELATION
Xóa quan hệ.

```sql
DROP RELATION <relation_name>
```

**Ví dụ:**
```sql
DROP RELATION IS_FATHER_OF
```

---

### Operator Management

#### CREATE OPERATOR
Tạo toán tử toán học mới.

```sql
CREATE OPERATOR <symbol>
    PARAMS (<type1>, <type2>, ...)
    RETURNS <type>
    [PROPERTIES <prop1>, <prop2>, ...]
```

**Tham số:**
- `<symbol>`: Ký hiệu toán tử (+, -, *, /, ^, %, etc.)
- `<typeN>`: Kiểu dữ liệu của tham số thứ N
- `<return_type>`: Kiểu dữ liệu trả về
- `<propN>`: Thuộc tính toán tử

**Thuộc tính toán tử:**
- `commutative`: Giao hoán (a + b = b + a)
- `associative`: Kết hợp (a + (b + c) = (a + b) + c)
- `distributive`: Phân phối (a * (b + c) = a*b + a*c)
- `idempotent': Lũy thừa

**Ví dụ:**
```sql
-- Toán tử cộng
CREATE OPERATOR +
    PARAMS (number, number)
    RETURNS number
    PROPERTIES commutative, associative, distributive

-- Toán tử lũy thừa
CREATE OPERATOR ^
    PARAMS (number, number)
    RETURNS number

-- Toán tử so sánh
CREATE OPERATOR =
    PARAMS (number, number)
    RETURNS boolean
```

---

#### DROP OPERATOR
Xóa toán tử.

```sql
DROP OPERATOR <symbol>
```

**Ví dụ:**
```sql
DROP OPERATOR ^
```

---

### Function Management

#### CREATE FUNCTION
Tạo hàm toán học mới.

```sql
CREATE FUNCTION <function_name>
    PARAMS (<type1> <param1>, <type2> <param2>, ...)
    RETURNS <type>
    BODY '<formula_expression>'
    [PROPERTIES <prop1>, <prop2>, ...]
```

**Tham số:**
- `<function_name>`: Tên hàm
- `<paramN>`: Tên tham số thứ N
- `<typeN>`: Kiểu dữ liệu của tham số thứ N
- `<return_type>`: Kiểu dữ liệu trả về
- `<formula_expression>`: Biểu thức công thức toán học (sử dụng tên tham số)
- `<propN>`: Thuộc tính của hàm

**Ví dụ:**
```sql
-- Hàm tính diện tích tam giác theo Heron
CREATE FUNCTION heron
    PARAMS (number a, number b, number c)
    RETURNS number
    BODY 'sqrt(((a+b+c)/2) * (((a+b+c)/2)-a) * (((a+b+c)/2)-b) * (((a+b+c)/2)-c))'

-- Hàm căn bậc 2
CREATE FUNCTION sqrt
    PARAMS (DOUBLE x)
    RETURNS DOUBLE
    BODY 'x^(1/2)'

-- Hàm tính chu vi tam giác
CREATE FUNCTION triangle_perimeter
    PARAMS (INT a, INT b, INT c)
    RETURNS INT
    BODY 'a + b + c'

-- Hàm tính khoảng cách giữa 2 điểm (sử dụng Concept)
CREATE FUNCTION distance_between_points
    PARAMS (DIEM P1, DIEM P2)
    RETURNS DOUBLE
    BODY 'sqrt((P2.x - P1.x)^2 + (P2.y - P1.y)^2)'

-- Hàm tính tổng giá trị sản phẩm
CREATE FUNCTION total_price
    PARAMS (DECIMAL(10,2) unitPrice, INT quantity)
    RETURNS DECIMAL(12,2)
    BODY 'unitPrice * quantity'
```

---

#### DROP FUNCTION
Xóa hàm.

```sql
DROP FUNCTION <function_name>
```

**Ví dụ:**
```sql
DROP FUNCTION heron
```

---

### Computation Relation Management

#### ADD COMPUTATION RELATION
Thêm quan hệ tính toán vào concept.

```sql
ADD COMPUTATION TO <concept_name>
    VARIABLES <var1>, <var2>, ..., <result_var>
    FORMULA '<formula_expression>'
    [COST <weight>]
```

**Tham số:**
- `<concept_name>`: Tên concept
- `<var1>, <var2>, ...`: Các biến đầu vào
- `<result_var>`: Biến kết quả
- `<formula_expression>`: Biểu thức tính toán
- `<weight>`: Trọng số (số nguyên, mặc định = 1)

**Ví dụ:**
```sql
-- Công thức Heron cho tính diện tích tam giác
ADD COMPUTATION TO TAMGIAC
    VARIABLES a, b, c, S
    FORMULA 'sqrt(((a+b+c)/2) * (((a+b+c)/2)-a) * (((a+b+c)/2)-b) * (((a+b+c)/2)-c))'
    COST 1

-- Công thức tính chu vi
ADD COMPUTATION TO TAMGIAC
    VARIABLES a, b, c, perimeter
    FORMULA 'a + b + c'
    COST 1
```

---

#### REMOVE COMPUTATION RELATION
Xóa quan hệ tính toán.

```sql
REMOVE COMPUTATION <result_var> FROM <concept_name>
```

**Ví dụ:**
```sql
REMOVE COMPUTATION S FROM TAMGIAC
```

---

### Rule Management

#### CREATE RULE
Tạo luật suy luận với cú pháp cải tiến.

```sql
CREATE RULE <rule_name>
    [TYPE <rule_type>]
    SCOPE <concept_name>
    IF <hypothesis_expression>
    THEN <conclusion_expression>
    [COST <weight>]
```

**Tham số:**
- `<rule_name>`: Tên luật (unique trong KB)
- `<rule_type>`: Loại luật (deduction, default, constraint, computation)
- `<concept_name>`: Concept mà luật áp dụng
- `<hypothesis_expression>`: Biểu thức điều kiện
- `<conclusion_expression>`: Kết luận (có thể nhiều, cách nhau bởi dấu phẩy)
- `<weight>`: Trọng số/chi phí (số nguyên, mặc định = 1)

**Loại luật:**
- `deduction`: Luật suy luận thông thường
- `default`: Luật mặc định
- `constraint`: Luật ràng buộc
- `computation`: Luật tính toán

**Ví dụ:**
```sql
-- Luật kiểm tra tam giác vuông
CREATE RULE check_right_triangle
    TYPE deduction
    SCOPE TAMGIAC
    IF a^2 + b^2 = c^2 OR a^2 + c^2 = b^2 OR b^2 + c^2 = a^2
    THEN is_right = true, type = 'RIGHT'

-- Luật Heron tính diện tích
CREATE RULE heron_formula
    TYPE computation
    SCOPE TAMGIAC
    IF a>0 AND b>0 AND c>0
    THEN p = (a+b+c)/2, S = sqrt(p*(p-a)*(p-b)*(p-c))
    COST 1

-- Luật mặc định: tam giác cân
CREATE RULE default_isosceles
    TYPE default
    SCOPE TAMGIAC
    IF true
    THEN is_isosceles = false

-- Luật ràng buộc: tổng các góc = 180 độ
CREATE RULE angle_sum_constraint
    TYPE constraint
    SCOPE TAMGIAC
    IF angleA + angleB + angleC <> 180
    THEN error = 'Sum of angles must be 180'
```

---

#### DROP RULE
Xóa luật suy luận.

```sql
DROP RULE <rule_name>
```

**Ví dụ:**
```sql
DROP RULE check_right_triangle
```

---

### User Management

#### CREATE USER
Tạo người dùng mới.

```sql
CREATE USER <username>
    PASSWORD '<password>'
    [ROLE <role>]
    [SYSTEM_ADMIN {true|false}]
```

**Tham số:**
- `<username>`: Tên người dùng (unique)
- `<password>`: Mật khẩu (được hash bằng BCrypt)
- `<role>`: Vai trò (ROOT, USER) - mặc định là USER
- `<system_admin>`: Quyền quản trị hệ thống - mặc định là false

**Vai trò:**
- `ROOT`: Toàn quyền hệ thống, không cần kiểm tra permission
- `USER`: Cần được cấp quyền cụ thể cho từng KB

**Ví dụ:**
```sql
-- Tạo user ROOT
CREATE USER admin
    PASSWORD 'admin123'
    ROLE ROOT

-- Tạo user thường
CREATE USER alice
    PASSWORD 'alice123'

-- Tạo user với system admin
CREATE USER bob
    PASSWORD 'bob123'
    ROLE USER
    SYSTEM_ADMIN true
```

---

#### DROP USER
Xóa người dùng.

```sql
DROP USER <username>
```

**Ví dụ:**
```sql
DROP USER bob
```

---

### Privilege Management

#### GRANT
Cấp quyền cho người dùng.

```sql
GRANT <privilege> ON <kb_name> TO <username>
```

**Tham số:**
- `<privilege>`: Quyền hạn (READ, WRITE, ADMIN)
- `<kb_name>`: Tên Knowledge Base
- `<username>`: Tên người dùng

**Quyền hạn:**
- `READ`: Đọc tri thức (SELECT, SOLVE, SHOW)
- `WRITE`: Đọc và ghi (READ + INSERT, UPDATE, DELETE)
- `ADMIN`: Quản trị (WRITE + CREATE, DROP, GRANT trên KB)

**Ví dụ:**
```sql
GRANT READ ON geometry TO alice
GRANT WRITE ON geometry TO alice
GRANT ADMIN ON geometry TO alice
```

---

#### REVOKE
Thu hồi quyền của người dùng.

```sql
REVOKE <privilege> ON <kb_name> FROM <username>
```

**Ví dụ:**
```sql
REVOKE WRITE ON geometry FROM alice
```

---

## KBDML - Knowledge Base Manipulation Language

### Data Query

#### SELECT
Truy vấn objects từ một concept.

```sql
SELECT <concept_name> [WHERE <conditions>]
SELECT <concept_name> [JOIN <relation_name>] [WHERE <conditions>]
SELECT <concept_name> [JOIN <relation_name> ON <join_condition>] [WHERE <conditions>]
SELECT <concept_name> AS <alias1> JOIN <concept_name> AS <alias2> [ON <condition>] [WHERE <conditions>]
```

**Tham số:**
- `<concept_name>`: Tên concept cần truy vấn
- `<conditions>`: Điều kiện lọc (key=value, key>value, key<value, key<>, key>=value, key<=value)
- `<relation_name>`: Tên relation để join
- `<join_condition>`: Điều kiện join (ví dụ: TAMGIAC.A = DOAN.A)
- `<alias>`: Tên gọi khác cho concept (dùng cho self-join hoặc clarity)

**JOIN Syntax:**

1. **Implicit Join via Relation:**
```sql
-- Tìm tất cả đoạn thẳng có điểm đầu là D1
SELECT DOAN JOIN THUOC WHERE A=D1

-- Tìm tất cả đoạn thẳng song song với đoạn D1D2
SELECT DOAN JOIN SONG WHERE SONG_DOAN=D1D2
```

2. **Explicit Join with ON clause:**
```sql
-- Tìm tam giác có điểm A là D1
SELECT TAMGIAC JOIN DIEM ON TAMGIAC.A = DIEM.Name WHERE DIEM.Name = 'D1'

-- Tìm tất cả đoạn thuộc về một điểm cụ thể
SELECT DOAN JOIN DIEM ON DOAN.A = DIEM.Name WHERE DIEM.x = 0
```

3. **Self-Join with Aliases:**
```sql
-- Tìm cặp điểm có cùng tọa độ x
SELECT D1.Name, D2.Name, D1.x
FROM DIEM AS D1
JOIN DIEM AS D2
WHERE D1.x = D2.x AND D1.Name <> D2.Name
```

4. **Multiple Joins:**
```sql
-- Tìm tam giác có 3 điểm cụ thể
SELECT TAMGIAC
JOIN DIEM AS D1 ON TAMGIAC.A = D1.Name
JOIN DIEM AS D2 ON TAMGIAC.B = D2.Name
JOIN DIEM AS D3 ON TAMGIAC.C = D3.Name
WHERE D1.Name = 'D1' AND D2.Name = 'D2' AND D3.Name = 'D3'
```

**Toán tử so sánh:**
- `=`: Bằng
- `<>`: Khác
- `>`: Lớn hơn
- `<`: Nhỏ hơn
- `>=`: Lớn hơn hoặc bằng
- `<=`: Nhỏ hơn hoặc bằng
- `AND`, `OR`: Kết hợp điều kiện
- `NOT`: Phủ định

**Ví dụ:**
```sql
-- Lấy tất cả objects của concept TAMGIAC
SELECT TAMGIAC

-- Lọc theo điều kiện
SELECT TAMGIAC WHERE a=3
SELECT TAMGIAC WHERE a=3 AND b=4
SELECT PERSON WHERE age>=18
SELECT PERSON WHERE active=true AND age>21

-- Sử dụng OR
SELECT PERSON WHERE name='Alice' OR name='Bob'

-- Sử dụng NOT
SELECT PERSON WHERE NOT active=false
```

---

#### SELECT with Aggregation
Truy vấn với các hàm tính toán trên tập kết quả.

```sql
SELECT COUNT(*) FROM <concept_name> [WHERE <conditions>]
SELECT SUM(<variable>) FROM <concept_name> [WHERE <conditions>]
SELECT AVG(<variable>) FROM <concept_name> [WHERE <conditions>]
SELECT MAX(<variable>) FROM <concept_name> [WHERE <conditions>]
SELECT MIN(<variable>) FROM <concept_name> [WHERE <conditions>]
SELECT <variable> GROUP BY <group_var> [HAVING <conditions>]
```

**Tham số:**
- `<variable>`: Tên biến trong concept (type phải là number)
- `<conditions>`: Điều kiện lọc trong WHERE
- `<group_var>`: Biến để nhóm kết quả
- `<conditions>` trong HAVING: Điều kiện lọc sau khi nhóm

**Các hàm aggregation:**

| Hàm | Chức năng | Kiểu trả về |
|------|-----------|-------------|
| `COUNT(*)` | Đếm số lượng objects | number |
| `COUNT(var)` | Đếm số lượng objects có var không null | number |
| `SUM(var)` | Tính tổng giá trị của var | number |
| `AVG(var)` | Tính trung bình giá trị của var | number |
| `MAX(var)` | Tìm giá trị lớn nhất của var | number |
| `MIN(var)` | Tìm giá trị nhỏ nhất của var | number |

**Ví dụ:**
```sql
-- Đếm số tam giác vuông
SELECT COUNT(*) FROM TAMGIAC WHERE is_right=true

-- Đếm số tam giác trong mỗi nhóm isosceles
SELECT COUNT(*), is_isosceles FROM TAMGIAC GROUP BY is_isosceles

-- Tính tổng tất cả các cạnh a
SELECT SUM(a) FROM TAMGIAC

-- Tính diện tích trung bình
SELECT AVG(S) FROM TAMGIAC

-- Tìm cạnh lớn nhất
SELECT MAX(c) FROM TAMGIAC WHERE b>4

-- Tìm diện tích nhỏ nhất
SELECT MIN(S) FROM TAMGIAC WHERE a>3

-- Group và filter với HAVING
SELECT COUNT(*) AS count, is_isosceles
FROM TAMGIAC
GROUP BY is_isosceles
HAVING COUNT(*) > 5
```

---

#### SELECT with ORDER BY và LIMIT
Sắp xếp và giới hạn số lượng kết quả.

```sql
SELECT <concept_name> [WHERE <conditions>] ORDER BY <variable> [ASC|DESC]
SELECT <concept_name> [WHERE <conditions>] [ORDER BY ...] LIMIT <n>
SELECT <concept_name> [WHERE <conditions>] [ORDER BY ...] LIMIT <n> OFFSET <m>
```

**Tham số:**
- `<variable>`: Biến để sắp xếp (type number hoặc string)
- `ASC`: Sắp xếp tăng dần (mặc định)
- `DESC`: Sắp xếp giảm dần
- `<n>`: Số lượng kết quả tối đa trả về
- `<m>`: Số lượng kết quả bỏ qua (pagination)

**Ví dụ:**
```sql
-- Sắp xếp theo diện tích tăng dần
SELECT TAMGIAC ORDER BY S ASC

-- Sắp xếp theo cạnh c giảm dần
SELECT TAMGIAC WHERE b>4 ORDER BY c DESC

-- Lấy 5 tam giác có diện tích lớn nhất
SELECT TAMGIAC ORDER BY S DESC LIMIT 5

-- Phân trang: lấy 10 kết quả, bỏ qua 20 kết quả đầu tiên
SELECT TAMGIAC ORDER BY S ASC LIMIT 10 OFFSET 20

-- Kết hợp WHERE, ORDER BY, và LIMIT
SELECT TAMGIAC WHERE a>3 AND b>4 ORDER BY S DESC LIMIT 1
```

---

#### SELECT with Spatial/Geometric Queries
Truy vấn không gian trong hình học.

```sql
SELECT <concept_name> NEAREST <reference_object>
SELECT <concept_name> INSIDE <geometric_object>
SELECT INTERSECT(<geometric_object1>, <geometric_object2>)
SELECT <concept_name> WHERE <spatial_condition>
```

**Tham số:**
- `<reference_object>`: Đối tượng tham chiếu (điểm, đường, hình, v.v.)
- `<geometric_object>`: Đối tượng hình học (DUONGTRON, DUONGTHANG, DAUGIAC, v.v.)
- `<spatial_condition>`: Điều kiện không gian

**Các phép toán không gian:**

| Phép toán | Cú pháp | Mô tả | Trạng thái |
|-----------|---------|--------|-----------|
| NEAREST | `SELECT DIEM NEAREST D1` | Tìm điểm gần nhất đến D1 | ✗ (planned) |
| INSIDE | `SELECT DIEM INSIDE DUONGTRON` | Tìm điểm nằm trong đường tròn | ✗ (planned) |
| OUTSIDE | `SELECT DIEM OUTSIDE DUONGTRON` | Tìm điểm nằm ngoài đường tròn | ✗ (planned) |
| INTERSECT | `SELECT INTERSECT(DUONG1, DUONG2)` | Tìm giao điểm 2 đường | ✗ (planned) |
| PARALLEL | `SELECT DOAN WHERE PARALLEL(DOAN1)` | Tìm đoạn song song | ✗ (planned) |
| PERPENDICULAR | `SELECT DOAN WHERE PERPENDICULAR(DOAN1)` | Tìm đoạn vuông góc | ✗ (planned) |
| COLLINEAR | `SELECT DIEM WHERE COLLINEAR(D1, D2)` | Tìm điểm thẳng hàng với D1, D2 | ✗ (planned) |
| DISTANCE | `SELECT DIEM WHERE DISTANCE(DIEM, D1) < 5` | Tìm điểm trong bán kính 5 | ✗ (planned) |
| AREA | `SELECT TAMGIAC WHERE AREA(S) < AREA(DUONGTRON)` | So sánh diện tích | ✗ (planned) |

**Ví dụ:**
```sql
-- Tìm điểm gần nhất với điểm D1
SELECT DIEM NEAREST D1
-- Expected: Danh sách điểm sắp xếp theo khoảng cách đến D1

-- Tìm điểm nằm trong đường tròn tâm O bán kính 5
SELECT DIEM INSIDE DUONGTRON WHERE DUONGTRON.O = 'O' AND DUONGTRON.R = 5

-- Tìm giao điểm 2 đường thẳng
SELECT INTERSECT(DUONG1, DUONG2)
-- Expected: Giao điểm (nếu có)

-- Tìm các điểm trong bán kính 5 từ tâm O
SELECT DIEM WHERE DISTANCE(DIEM, 'O') < 5

-- Tìm tam giác có diện tích lớn hơn đường tròn bán kính 3
SELECT TAMGIAC WHERE S > AREA(DUONGTRON, R=3)

-- Tìm các đoạn song song với đoạn AB
SELECT DOAN WHERE PARALLEL(DOAN_AB)

-- Tìm điểm thẳng hàng với D1 và D2
SELECT DIEM WHERE COLLINEAR(D1, D2)
```

**Functions không gian:**

```sql
-- Function tính khoảng cách giữa 2 điểm
CREATE FUNCTION DISTANCE
    PARAMS (DIEM D1, DIEM D2)
    RETURNS number
    BODY 'sqrt((D2.x - D1.x)² + (D2.y - D1.y)²)'

-- Function kiểm tra điểm nằm trong đường tròn
CREATE FUNCTION INSIDE_CIRCLE
    PARAMS (DIEM P, DUONGTRON C)
    RETURNS boolean
    BODY 'DISTANCE(P, C.O) < C.R'

-- Function tính giao điểm 2 đường thẳng
CREATE FUNCTION LINE_INTERSECTION
    PARAMS (DUONGTHANG L1, DUONGTHANG L2)
    RETURNS DIEM
    BODY '...'

-- Function kiểm tra 3 điểm thẳng hàng
CREATE FUNCTION IS_COLLINEAR
    PARAMS (DIEM D1, DIEM D2, DIEM D3)
    RETURNS boolean
    BODY '(D2.x - D1.x) * (D3.y - D1.y) == (D3.x - D1.x) * (D2.y - D1.y)'
```

---

### Data Manipulation

#### INSERT
Thêm một object instance mới.

```sql
INSERT INTO <concept_name> VALUES (
    <field1> = <value1>,
    <field2> = <value2>,
    ...
)
```

**Tham số:**
- `<concept_name>`: Tên concept
- `<fieldN>`: Tên biến trong concept
- `<valueN>`: Giá trị (số, chuỗi trong dấu nháy đơn, true/false)

**Ví dụ:**
```sql
-- Thêm object TAMGIAC
INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5, S=6)

-- Thêm object PERSON
INSERT INTO PERSON VALUES (name='Alice', age=30, active=true)

-- Thêm object với chuỗi có dấu nháy
INSERT INTO PERSON VALUES (name='O''Connor', age=25, active=false)
```

---

#### UPDATE
Cập nhật các object instances.

```sql
UPDATE <concept_name>
    SET <field1> = <value1>, <field2> = <value2>, ...
    [WHERE <conditions>]
```

**Ví dụ:**
```sql
-- Cập nhật một object
UPDATE TAMGIAC SET S=6 WHERE a=3 AND b=4 AND c=5

-- Cập nhật nhiều objects
UPDATE PERSON SET age=age+1 WHERE active=true

-- Cập nhật nhiều fields
UPDATE TAMGIAC SET a=6, b=8 WHERE a=3 AND b=4
```

---

#### DELETE
Xóa các object instances.

```sql
DELETE FROM <concept_name> [WHERE <conditions>]
```

**Ví dụ:**
```sql
-- Xóa object cụ thể
DELETE FROM TAMGIAC WHERE a=3 AND b=4 AND c=5

-- Xóa theo điều kiện
DELETE FROM PERSON WHERE age<18

-- Xóa tất cả (cẩn thận!)
DELETE FROM TAMGIAC
```

---

### Reasoning

#### SOLVE
Suy luận và tính toán dựa trên các quy tắc.

```sql
SOLVE <concept_name>
    FOR <unknown_variable>
    GIVEN <known_conditions>
    [USING <rule_type>]
```

**Tham số:**
- `<concept_name>`: Tên concept
- `<unknown_variable>`: Biến cần tìm
- `<known_conditions>`: Các điều kiện đã biết (key=value, cách nhau bởi AND)
- `<rule_type>`: Loại luật để sử dụng (deduction, default, constraint, computation)

**Ví dụ:**
```sql
-- Tìm diện tích tam giác
SOLVE TAMGIAC
    FOR S
    GIVEN a=3, b=4, c=5

-- Tìm cạnh c khi biết a, b và S
SOLVE TAMGIAC
    FOR c
    GIVEN a=3, b=4, S=6

-- Sử dụng luật computation
SOLVE TAMGIAC
    FOR S
    GIVEN a=3, b=4, c=5
    USING computation
```

---

### Information Display

#### SHOW
Hiển thị thông tin về hệ thống.

```sql
SHOW KNOWLEDGE BASES
SHOW CONCEPTS [IN <kb_name>]
SHOW CONCEPT <concept_name> [IN <kb_name>]
SHOW RULES [IN <kb_name>] [TYPE <rule_type>]
SHOW RELATIONS [IN <kb_name>]
SHOW OPERATORS [IN <kb_name>]
SHOW FUNCTIONS [IN <kb_name>]
SHOW USERS
SHOW PRIVILEGES ON <kb_name>
SHOW PRIVILEGES OF <username>
```

**Ví dụ:**
```sql
-- Hiển thị tất cả KB
SHOW KNOWLEDGE BASES

-- Hiển thị concepts trong KB
SHOW CONCEPTS
SHOW CONCEPTS IN geometry

-- Hiển thị chi tiết một concept
SHOW CONCEPT TAMGIAC

-- Hiển thị rules theo loại
SHOW RULES
SHOW RULES IN geometry
SHOW RULES TYPE computation

-- Hiển thị users và quyền
SHOW USERS
SHOW PRIVILEGES ON geometry
SHOW PRIVILEGES OF alice
```

---

## Tóm tắt Cú pháp DDL

| Command | Mô tả |
|---------|--------|
| `CREATE KNOWLEDGE BASE <name> [DESC '<desc>']` | Tạo KB mới |
| `DROP KNOWLEDGE BASE <name>` | Xóa KB |
| `USE <name>` | Chọn KB hiện tại |
| `CREATE CONCEPT <name> VARIABLES (...) [...]` | Tạo Concept |
| `ADD VARIABLE <var>:<type> TO CONCEPT <name>` | Thêm biến vào Concept |
| `DROP CONCEPT <name>` | Xóa Concept |
| `ADD HIERARCHY <p> IS_A <c>` | Tạo quan hệ IS_A |
| `ADD HIERARCHY <p> PART_OF <c>` | Tạo quan hệ PART_OF |
| `REMOVE HIERARCHY <p> <type> <c>` | Xóa quan hệ phân cấp |
| `CREATE RELATION <name> FROM <d> TO <r>` | Tạo Relation |
| `DROP RELATION <name>` | Xóa Relation |
| `CREATE OPERATOR <sym> PARAMS (...) RETURNS ...` | Tạo Toán tử |
| `DROP OPERATOR <sym>` | Xóa Toán tử |
| `CREATE FUNCTION <name> PARAMS (...) RETURNS ...` | Tạo Hàm |
| `DROP FUNCTION <name>` | Xóa Hàm |
| `ADD COMPUTATION TO <c> VARIABLES ... FORMULA '...'` | Thêm quan hệ tính toán |
| `REMOVE COMPUTATION <v> FROM <c>` | Xóa quan hệ tính toán |
| `CREATE RULE <name> TYPE <t> SCOPE <c> IF ... THEN ...` | Tạo Luật |
| `DROP RULE <name>` | Xóa Luật |
| `CREATE USER <name> PASS '<p>' [ROLE <r>]` | Tạo User |
| `DROP USER <name>` | Xóa User |
| `GRANT <priv> ON <kb> TO <user>` | Cấp quyền |
| `REVOKE <priv> ON <kb> FROM <user>` | Thu hồi quyền |

---

## Tóm tắt Cú pháp DML

| Command | Mô tả | Trạng thái |
|---------|--------|-----------|
| `SELECT <c> [WHERE <cond>]` | Truy vấn objects | ✓ |
| `INSERT INTO <c> VALUES (...)` | Thêm object | ✓ |
| `UPDATE <c> SET ... [WHERE ...]` | Cập nhật object | ✓ |
| `DELETE FROM <c> [WHERE ...]` | Xóa object | ✓ |
| `SOLVE <c> FOR <v> GIVEN ... [USING ...]` | Suy luận/tính toán | ✓ |
| `SELECT <c> JOIN <r> [WHERE <cond>]` | Truy vấn với JOIN | ⚠️ (Cần làm rõ) |
| `SELECT COUNT(*) FROM <c> [WHERE ...]` | Đếm số lượng | ✗ (Cần bổ sung) |
| `SELECT SUM(var) FROM <c> [WHERE ...]` | Tính tổng | ✗ (Cần bổ sung) |
| `SELECT AVG(var) FROM <c> [WHERE ...]` | Tính trung bình | ✗ (Cần bổ sung) |
| `SELECT MAX(var) FROM <c> [WHERE ...]` | Tìm giá trị lớn nhất | ✗ (Cần bổ sung) |
| `SELECT MIN(var) FROM <c> [WHERE ...]` | Tìm giá trị nhỏ nhất | ✗ (Cần bổ sung) |
| `SELECT ... GROUP BY var [HAVING cond]` | Group aggregation | ✗ (Cần bổ sung) |
| `SELECT ... ORDER BY var [ASC|DESC]` | Sắp xếp kết quả | ✗ (Cần bổ sung) |
| `SELECT ... LIMIT n [OFFSET m]` | Giới hạn kết quả | ✗ (Cần bổ sung) |
| `SHOW KNOWLEDGE BASES` | Hiển thị danh sách KB | ✓ |
| `SHOW CONCEPTS [IN <kb>]` | Hiển thị danh sách Concepts | ✓ |
| `SHOW CONCEPT <c> [IN <kb>]` | Hiển thị chi tiết Concept | ✓ |
| `SHOW RULES [IN <kb>] [TYPE <t>]` | Hiển thị danh sách Rules | ✓ |
| `SHOW RELATIONS [IN <kb>]` | Hiển thị danh sách Relations | ✓ |
| `SHOW OPERATORS [IN <kb>]` | Hiển thị danh sách Operators | ✓ |
| `SHOW FUNCTIONS [IN <kb>]` | Hiển thị danh sách Functions | ✓ |
| `SHOW USERS` | Hiển thị danh sách Users | ✓ |
| `SHOW PRIVILEGES ON <kb>` | Hiển thị quyền trên KB | ✓ |

**Legend:**
- ✓ = Đã hỗ trợ
- ⚠️ = Đã hỗ trợ nhưng cần làm rõ syntax
- ✗ = Chưa hỗ trợ

---

## Quy tắc phân quyền

### ROOT
- Toàn quyền hệ thống
- Không cần kiểm tra permission cho bất kỳ hành động nào

### USER
- `CREATE KNOWLEDGE BASE`: Cần SystemAdmin = true
- `DROP KNOWLEDGE BASE`: Cần ADMIN privilege trên KB
- `SELECT` / `SOLVE` / `SHOW`: Cần ít nhất READ privilege trên KB
- `INSERT` / `UPDATE` / `DELETE`: Cần WRITE privilege trên KB
- `CREATE CONCEPT` / `CREATE RULE` / `CREATE OPERATOR` / `CREATE FUNCTION`: Cần ADMIN privilege trên KB
- `GRANT`: Chỉ ROOT hoặc SystemAdmin = true
- `REVOKE`: Chỉ ROOT hoặc SystemAdmin = true

---

## Ví dụ Hoàn chỉnh

```sql
-- Tạo Knowledge Base về hình học
CREATE KNOWLEDGE BASE geometry DESCRIPTION 'Hệ thống tri thức hình học'

USE geometry

-- Tạo concept TAMGIAC
CREATE CONCEPT TAMGIAC
    VARIABLES (a:number, b:number, c:number, S:number, perimeter:number)
    ALIASES TRIANGLE, TAMGIAC_DEU
    BASE_OBJECTS TAMGIAC_CAN, TAMGIAC_VUONG
    CONSTRAINTS a>0, b>0, c>0, a+b>c, a+c>b, b+c>a

-- Tạo các toán tử cơ bản
CREATE OPERATOR +
    PARAMS (number, number)
    RETURNS number
    PROPERTIES commutative, associative

CREATE OPERATOR *
    PARAMS (number, number)
    RETURNS number
    PROPERTIES commutative, associative

CREATE OPERATOR ^
    PARAMS (number, number)
    RETURNS number

-- Tạo hàm căn bậc 2
CREATE FUNCTION sqrt
    PARAMS (number x)
    RETURNS number
    BODY 'x^(1/2)'

-- Tạo hàm tính diện tích tam giác
CREATE FUNCTION triangle_area
    PARAMS (number a, number b, number c)
    RETURNS number
    BODY 'sqrt(((a+b+c)/2) * (((a+b+c)/2)-a) * (((a+b+c)/2)-b) * (((a+b+c)/2)-c))'

-- Tạo luật Heron
CREATE RULE heron_formula
    TYPE computation
    SCOPE TAMGIAC
    IF a>0 AND b>0 AND c>0
    THEN p = (a+b+c)/2, S = sqrt(p*(p-a)*(p-b)*(p-c))
    COST 1

-- Tạo luật kiểm tra tam giác vuông
CREATE RULE check_right_triangle
    TYPE deduction
    SCOPE TAMGIAC
    IF a^2 + b^2 = c^2 OR a^2 + c^2 = b^2 OR b^2 + c^2 = a^2
    THEN is_right = true, type = 'RIGHT'

-- Thêm object tam giác
INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5, S=0, perimeter=0)

-- Suy luận để tìm S
SOLVE TAMGIAC
    FOR S
    GIVEN a=3, b=4, c=5

-- Kết quả: S = 6

-- Hiển thị thông tin
SHOW CONCEPTS
SHOW RULES TYPE computation
SELECT TAMGIAC WHERE a=3 AND b=4 AND c=5
```
