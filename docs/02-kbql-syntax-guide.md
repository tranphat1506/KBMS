# Toàn tập Cú Pháp Ngôn ngữ Truy vấn Tri Thức (KBQL V2)

KBQL (Knowledge Base Query Language) phiên bản V2 là ngôn ngữ đặc thù quản trị hệ sinh thái KBMS thông qua tính năng đóng gói **Khối Block ngoặc tròn `()`**. 

Tài liệu này là Tự điển (Dictionary) liệt kê **ĐẦY ĐỦ 100% CÁC LỆNH** hiện hành cấu thành nên 5 mảng: KDL, KML, KQL, KCL, và TCL.

---

## 1. KDL (Knowledge Definition Language) - Khởi tạo Cấu trúc
KDL (nâng cấp từ DDL truyền thống) cho phép bạn khởi tạo và thay đổi Schema trên file `.kmf`.

### **1.1. Quản lý Không Gian Tri Thức (Knowledge Bases)**
```sql
CREATE KNOWLEDGE BASE <Ten_Du_An>;
DROP KNOWLEDGE BASE <Ten_Du_An>;
USE <Ten_Du_An>;
```

### **1.2. Khởi tạo Concept (Khuôn đúc Thực thể)**
Gồm nhiều Block tham số hỗ trợ quy trình thiết kế Dữ liệu + Toán học.
```sql
CREATE CONCEPT <HinhHoc> 
(
    VARIABLES (
        A: DIEM,
        B: DIEM,
        Canh: INT
    )
    ALIASES (
        Hinh, Shape
    )
    BASE_OBJECTS (
        BaseShape
    )
    CONSTRAINTS (
        C1: "A > 0",
        C2: "Canh >= A + B"
    )
    SAME_VARIABLES (
        A = B
    )
    CONSTRUCT_RELATIONS (
        ToaDo(A, B)
    )
    PROPERTIES (
        Author: "Tran Phat",
        Version: "2.0"
    )
    RULES (
        Rule1, Rule2
    )
    EQUATIONS (
        Eq1: "ChuVi = A + B + Canh",
        Eq2: "DienTich = A * B"
    )
);
```

### **1.3. Cập nhật Concept Động (Evolving Schema)**
Chỉnh sửa nóng cấu trúc mà không phải `DROP` làm mất dữ liệu sự kiện:
```sql
ALTER CONCEPT <HinhHoc> 
(
    ADD (
        VARIABLES ( M: INT, N: INT ),
        RULES ( Rule3 )
    )
    REMOVE (
        CONSTRAINTS ( C1, C2 ),
        PROPERTIES ( Version, Author )
    )
    EDIT (
        EQUATIONS ( Eq1: "ChuVi = A + B + Canh + M" )
    )
);
```

### **1.4. Xóa Concept / Entity**
```sql
DROP CONCEPT <HinhHoc>;
```

### **1.5. Các Đối tượng Logic Suy Diễn / Ngữ Nghĩa khác**
Hệ thống cho phép gõ các lệnh tạo lập siêu cấu trúc:
```sql
-- Tạo Hàm hệ thống
CREATE FUNCTION <TinhDienTich> ( PARAMS(...) RETURNS(...) BODY(...) PROPERTIES(...) );
DROP FUNCTION <TinhDienTich>;

-- Tạo Toán tử suy diễn
CREATE OPERATOR <PhanGiao> ( PARAMS(...) RETURNS(...) BODY(...) PROPERTIES(...) );
DROP OPERATOR <PhanGiao>;

-- Tạo Mạng ngữ nghĩa (Relations)
CREATE RELATION <ThuocVe> ( FROM(...) TO(...) PARAMS(...) RULES(...) EQUATIONS(...) PROPERTIES(...) );
DROP RELATION <ThuocVe>;

-- Tạo Luật độc lập (Independent Rules)
CREATE RULE <LuatPytago> ( TYPE(...) SCOPE(...) IF(...) THEN(...) COST(...) );
DROP RULE <LuatPytago>;
```

---

## 2. KML (Knowledge Manipulation Language) - Thao Tác Sự Kiện
Nhóm KML (tương đương DML truyền thống) tương tác trực tiếp với RAM Buffer Pool hoặc file `.kdf` khi nạp dữ kiện/sự kiện thực tế (Fact Injection/Instantiation).

### **2.1. Thêm Sự Kiện (INSERT)**
Điền dữ kiện vào thuộc tính (Map cứng qua khối block `ATTRIBUTES`):
```sql
-- Dựa trên Từ khóa (Named Fields)
INSERT INTO <HinhHoc> ATTRIBUTE ( A:1, B:2, Canh:3 );

-- Dựa trên Giá trị vị trí (Positional Fields)
INSERT INTO <HinhHoc> ATTRIBUTE ( 1, 2, 3 );
```

### **2.2. Sửa Sự Kiện (UPDATE)**
```sql
UPDATE <HinhHoc> ATTRIBUTE ( SET A: 10, Canh: 15 ) WHERE B = 2;
```

### **2.3. Xóa Sự Kiện (DELETE)**
Xóa bỏ các Instance/Facts đang chạy trên vùng nhớ:
```sql
DELETE FROM <HinhHoc> WHERE Canh < 0;
```

---

## 3. KQL (Knowledge Query Language) - Truy Vấn Động & Tĩnh (Cốt Lõi)
Ngôn ngữ này tách biệt hoàn toàn giữa việc `Đọc dữ liệu thụ động` (Static Query) và `Giải toán tự động` (Inference Problem Solving).

### **3.1. Truy Vấn Chuẩn (SELECT - RDBMS Equivalency)**
Cú pháp SELECT của KBMS hỗ trợ sức mạnh truy xuất khổng lồ kết hợp Filtering, Grouping.
```sql
-- Đọc tất cả
SELECT * FROM <HinhHoc>;

-- Đổi tên và đếm dòng
SELECT COUNT(*), A, Canh AS ChieuDai FROM <HinhHoc>;

-- Ghép bảng (Join) và Lọc sâu
SELECT A, B FROM <HinhHoc> 
JOIN <ToaDo> ON HinhHoc.A = ToaDo.X
WHERE A > 0 AND Canh != NULL
GROUP BY Canh 
HAVING Canh >= 2 
ORDER BY Canh DESC 
LIMIT 10 OFFSET 0;
```

### **3.2. Chức Năng Cốt Lõi: Giải Toán Kéo Theo (SOLVE / INFERENCE)**
Giao việc cho Não Bộ Suy Diễn (KnowledgeManager/Forward & Backward Chaining Engine):
```sql
-- Cung cấp A và B, tự động tính C và lưu xuống Database nếu muốn
SOLVE ON CONCEPT <HinhHoc> GIVEN A=1, B=2 FIND CanhC SAVE true;
```

### **3.3. Hiển thị Metadata (SHOW)**
Giao tiếp để xem danh sách sơ đồ CSDL:
```sql
SHOW KNOWLEDGE BASES;
SHOW CONCEPTS;
SHOW CONCEPT <HinhHoc>;
SHOW RULES;
SHOW RELATIONS;
SHOW OPERATORS;
SHOW FUNCTIONS;
SHOW HIERARCHIES;
SHOW USERS;
```

---

## 4. KCL (Knowledge Control Language) - Phân Quyền & Bảo Mật
Nhóm lệnh hệ thống thiết lập RBAS (Role-Based Access System).

### **4.1. Account Management (Tạo & Xóa Người Dùng)**
```sql
CREATE USER root PASSWORD '123' ROLE 'ADMIN' SYSTEM_ADMIN true;
DROP USER lechautranphat;
```

### **4.2. Access Delegation (Gán và Tước Quyền)**
Cho phép hạn chế quyền `READ`, `WRITE`, `EXECUTE`, `ALL` trên từng Knowledge Base cụ thể.
```sql
GRANT READ ON <DuAnToanHoc> TO lechautranphat;
GRANT ALL ON <DuAnVatLy> TO root;

REVOKE WRITE ON <DuAnToanHoc> FROM lechautranphat;
```

### **4.3. Kiểm Tra Quyền Hạn**
```sql
SHOW PRIVILEGES ON <DuAnToanHoc>;
SHOW PRIVILEGES OF lechautranphat;
```

---

## 5. TCL (Transaction Control Language) - Quản Trị Phiên Giao Dịch RAM
Đây là khối bảo vệ tầng `Storage Layer (RAM)` trước sự cố của `Physical Storage Layer (.kdf)`.

```sql
-- KHỞI TẠO: Mở một vòng bo Shadow Paging cho RAM Session hiện tại. 
-- Các lệnh KML sẽ ngừng rải xuống .kdf.
BEGIN TRANSACTION;

-- LỆNH BỊ TREO VÀO LOG WAL:
INSERT INTO <HinhHoc> ATTRIBUTE ( A:1 );
UPDATE <HinhHoc> ATTRIBUTE ( SET A:10 ) WHERE A = 1;

-- CHỐT SỔ (XẢ RAM XUỐNG ĐĨA .KDF VÀ TIÊU DIỆT BUFFER TẠM):
COMMIT; 

-- BỎ SUỘC: Vứt bỏ mọi thay đổi KML đang treo, khôi phục RAM.
ROLLBACK; 
```

Với cấu trúc tổng hợp **đầy đủ của hơn 30+ Câu lệnh hệ thống khổng lồ**, KBQL V2 chứng minh nó không hề thua kém T-SQL hay PL/SQL truyền thống, mà còn vượt mặt khi kết hợp hệ suy diễn tư duy `SOLVE` và `ALTER CONCEPT Block` chuyên sâu.
