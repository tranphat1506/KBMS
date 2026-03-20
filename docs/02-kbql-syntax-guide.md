# Hướng Dẫn Cú Pháp KBQL Chi Tiết (v1.0)

KBQL (Knowledge Base Query Language) là ngôn ngữ cốt lõi của KBMS, được thiết kế để xử lý tri thức dựa trên mô hình COKB. KBQL v1.0 bao gồm 6 phân nhánh ngôn ngữ chuyên biệt.

## 1. KDL (Knowledge Definition Language) - Định nghĩa Tri thức
Dùng để thiết kế cấu trúc (Schema) của cơ sở tri thức.

### 1.1. Tạo CSDL Tri thức (CREATE KNOWLEDGE BASE)
```sql
CREATE KNOWLEDGE BASE MyProject DESCRIPTION "Dự án quản lý hình học";
USE MyProject;
```

### 1.2. Định nghĩa Khái niệm (CREATE CONCEPT)
Hỗ trợ các ràng buộc toán học (Constraints) và biến (Variables).
```sql
CREATE CONCEPT <Rectangle> (
    VARIABLES ( width: double, height: double, area: double ),
    CONSTRAINTS ( area = width * height )
);
```

### 1.3. Cập nhật Khái niệm (ALTER CONCEPT) - Cú pháp Block-Centric
```sql
-- Thêm biến và ràng buộc
ALTER CONCEPT <Rectangle> ( 
    ADD ( VARIABLE ( color: string ), CONSTRAINT ( pos_w: width > 0 ) ) 
);
-- Đổi tên biến
ALTER CONCEPT <Rectangle> ( RENAME ( VARIABLE width TO w ) );
-- Xóa biến
ALTER CONCEPT <Rectangle> ( DROP ( VARIABLE color ) );
```

---

## 2. KML (Knowledge Manipulation Language) - Thao tác Dữ liệu
Quản lý các đối tượng cụ thể (Instances) trong CSDL.

### 2.1. Thêm Đối tượng (INSERT)
```sql
INSERT INTO <Rectangle> ATTRIBUTE ( width: 10.5, height: 20.0 );
```

### 2.2. Xuất/Nhập Dữ liệu (IMPORT/EXPORT)
Hỗ trợ định dạng JSON để trao đổi dữ liệu với hệ thống khác.
```sql
EXPORT ( CONCEPT: <Rectangle>, FORMAT: JSON, FILE: "rect_backup.json" );
IMPORT ( FILE: "rect_data.json", INTO: <Rectangle> );
```

---

## 3. KQL (Knowledge Query Language) - Truy vấn & Suy luận
Đây là phần mạnh mẽ nhất của KBMS, cho phép giải toán và tìm kiếm thông minh.

### 3.1. Truy vấn Dữ liệu (SELECT)
```sql
SELECT * FROM <Rectangle> WHERE width > 10 AND height < 50;
SELECT COUNT(*) FROM <Rectangle> AS TotalRect;
```

### 3.2. Giải toán Tự động (SOLVE)
Giải các biến chưa biết dựa trên ràng buộc và tri thức đã nạp.
```sql
SOLVE ON CONCEPT <Rectangle> GIVEN width: 5, height: 10 FIND area SAVE;
```

### 3.3. Kiểm tra Metadata (DESCRIBE)
Xem cấu trúc của một Concept hoặc CSDL.
```sql
DESCRIBE CONCEPT <Rectangle>;
DESCRIBE KNOWLEDGE BASE MyProject;
```

> [!NOTE]
> **Tính năng Subquery**: Hiện tại KBMS 1.0 chưa hỗ trợ truy vấn con trực tiếp (ví dụ: `WHERE id IN (SELECT...)`). Tính năng này dự kiến sẽ có trong bản cập nhật 2.0.

---

## 4. TCL (Transaction Control Language) - Điều khiển Giao dịch
Đảm bảo tính toàn vẹn dữ liệu (ACID) trên RAM Buffer Pool.
```sql
BEGIN TRANSACTION;
INSERT INTO <Rectangle> ATTRIBUTE ( width: 2, height: 3 );
-- Nếu xảy ra lỗi:
ROLLBACK;
-- Nếu thành công:
COMMIT;
```

---

## 5. KCL (Knowledge Control Language) - Kiểm soát Truy cập
Phân quyền và bảo mật cho người dùng.
```sql
CREATE USER gemini PASSWORD 'secure123' ROLE ADMIN;
GRANT PRIVILEGE ADMIN ON MyProject TO gemini;
```

---

## 6. KHL (Knowledge Help & Maintenance) - Bảo trì & Trợ giúp
Tối ưu hóa hệ thống và gỡ lỗi.

### 6.1. Tối ưu hóa (MAINTENANCE)
```sql
MAINTENANCE ( VACUUM, REINDEX (*) );
```

### 6.2. Phân tích Truy vấn (EXPLAIN)
```sql
EXPLAIN ( SELECT * FROM <Rectangle> WHERE width > 10 );
```

### 6.3. Tạo Chỉ mục (CREATE INDEX)
```sql
CREATE INDEX ON <Rectangle> ( width );
```

---
*Lưu ý: Luôn kết thúc câu lệnh bằng dấu chấm phẩy (;) khi sử dụng Command Line.*
