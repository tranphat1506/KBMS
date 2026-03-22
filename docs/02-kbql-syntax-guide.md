# Hướng Dẫn Cú Pháp KBQL Chi Tiết (v1.1)

KBQL (Knowledge Base Query Language) là ngôn ngữ cốt lõi của KBMS, hỗ trợ mô hình tri thức thực thể và tính toán (COKB). Phiên bản v1.1 bổ sung hệ thống kiểu dữ liệu chuẩn (True Typing), truy vấn siêu dữ liệu (Metadata) và hỗ trợ truy vấn con (Subquery).

## 1. KDL (Knowledge Definition Language) - Định nghĩa Tri thức

### 1.1. Định nghĩa Khái niệm (CREATE CONCEPT)
Hỗ trợ các hệ thống kiểu dữ liệu chuẩn xác cao:
- **INT**: Số nguyên (long).
- **DECIMAL(L, S)**: Số thực cố định (Độ dài L, chữ số thập phân S), dùng cho tài chính.
- **DOUBLE**: Số thực dấu phẩy động.
- **STRING**: Chuỗi văn bản.

```sql
CREATE CONCEPT <Product> (
    VARIABLES ( 
        id: INT, 
        price: DECIMAL(10, 2), 
        tax_rate: DOUBLE, 
        name: STRING 
    ),
    CONSTRAINTS ( total_price = price * (1 + tax_rate) )
);
```

### 1.2. Cập nhật Khái niệm (ALTER CONCEPT)
```sql
ALTER CONCEPT <Product> ( ADD ( VARIABLE ( category: STRING ) ) );
```

---

## 2. KML (Knowledge Manipulation Language) - Thao tác Dữ liệu

### 2.1. Thêm Đối tượng (INSERT)
Sử dụng từ khóa `ATTRIBUTE` để gán giá trị cho các biến của Concept.
```sql
-- Gán theo tên (Named Syntax)
INSERT INTO <Product> ATTRIBUTE ( id: 1, price: 19.99, tax_rate: 0.08, name: 'Sơn Trà' );

-- Gán theo vị trí (Positional - dựa vào thứ tự khai báo)
INSERT INTO <Product> ATTRIBUTE ( 2, 50.00, 0.1, 'Bánh Mì' );
```

### 2.2. Xuất/Nhập Dữ liệu (IMPORT/EXPORT)
```sql
EXPORT ( CONCEPT: <Product>, FORMAT: JSON, FILE: "data.json" );
IMPORT ( FILE: "data.json", INTO: <Product> );
```

---

## 3. KQL (Knowledge Query Language) - Truy vấn & Suy luận

### 3.1. Truy vấn Dữ liệu (SELECT)
Hỗ trợ các phép toán, hàm tập hợp (`COUNT`, `SUM`, `AVG`) và bộ lọc `WHERE`.

```sql
SELECT * FROM <Product> WHERE price > 10;
SELECT name, price FROM <Product> ORDER BY price DESC;
```

### 3.2. Truy vấn Siêu dữ liệu (Metadata Queries)
Bạn có thể truy vấn trực tiếp cấu trúc của tri thức thông qua bảng ảo `system` hoặc cú pháp chấm `.`.

```sql
-- Liệt kê tất cả các Concept
SELECT * FROM system.concepts;

-- Truy vấn các biến của một Concept cụ thể
SELECT * FROM Person.variables;

-- Truy vấn các ràng buộc của một Concept cụ thể
SELECT * FROM Person.constraints;
```

### 3.3. Truy vấn con (Subquery)
Hỗ trợ lồng các câu lệnh `SELECT` bên trong `WHERE` sử dụng toán tử `IN` hoặc `=`.

```sql
SELECT * FROM <Product> 
WHERE id IN (
    SELECT productId FROM Orders WHERE status = 'PAID'
);
```

### 3.4. Giải toán Tự động (SOLVE)
Sử dụng công cụ suy diễn để tìm giá trị các biến chưa biết.
```sql
SOLVE ON CONCEPT <Product> GIVEN id: 1, price: 100 FIND total_price;
```

---

## 4. Giao thức Truyền tin & Giao dịch
KBMS hỗ trợ thực thi nhiều câu lệnh cùng lúc (Multi-statement) ngăn cách bởi dấu `;`.

```sql
CREATE CONCEPT T1 (VARIABLES(x:INT)); 
INSERT INTO T1 ATTRIBUTE (x:10); 
SELECT * FROM T1;
```

Hỗ trợ giao dịch ACID:
```sql
BEGIN TRANSACTION;
INSERT INTO <Product> ATTRIBUTE (id: 5);
COMMIT; -- Hoặc ROLLBACK;
```

---
*Lưu ý: Luôn sử dụng dấu chấm phẩy (;) để kết thúc mỗi câu lệnh trong môi trường CLI.*
