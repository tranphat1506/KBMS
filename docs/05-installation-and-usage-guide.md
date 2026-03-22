# Cài đặt & Hướng dẫn Sử dụng KBMS v1.1

Tài liệu này hướng dẫn cách triển khai hệ thống KBMS và các kịch bản sử dụng thực tế cho phiên bản 1.1.

## 1. Yêu cầu Hệ thống
- **Hệ điều hành**: Windows, Linux, hoặc macOS.
- **Môi trường**: .NET 8.0 SDK trở lên.
- **Công cụ**: Terminal (PowerShell, Bash, hoặc CMD).

## 2. Các bước Cài đặt

### Bước 1: Clone mã nguồn
```bash
git clone https://github.com/tranphat1506/KBMS.git
cd KBMS
```

### Bước 2: Build toàn bộ Solution
```bash
dotnet build
```

### Bước 3: Khởi chạy Server
Server sẽ lắng nghe tại cổng `3307`.
```bash
cd KBMS.Server
dotnet run
```

### Bước 4: Khởi chạy CLI Client
```bash
cd ../KBMS.CLI
dotnet run
```

---

## 3. Các kịch bản sử dụng thực tế trong v1.1

### Kịch bản 1: Quản lý Sản phẩm với Độ chính xác cao (True Typing)
KBMS v1.1 hỗ trợ kiểu `DECIMAL` giúp tính toán thuế và giá trị đơn hàng một cách tuyệt đối chính xác.

```sql
-- 1. Tạo Database
CREATE KNOWLEDGE BASE Store;
USE Store;

-- 2. Tạo Concept với kiểu DECIMAL
CREATE CONCEPT Laptop (
    VARIABLES ( 
        id: INT, 
        base_price: DECIMAL(10, 2), 
        tax_rate: DOUBLE, 
        total: DECIMAL(10, 2) 
    ),
    CONSTRAINTS ( total = base_price * (1 + tax_rate) )
);

-- 3. Thêm dữ liệu (Hệ thống tự làm tròn 19.995 -> 20.00 cho DECIMAL(x,2))
INSERT INTO Laptop ATTRIBUTE (id: 1, base_price: 19.995, tax_rate: 0.1);

-- 4. Truy vấn và xem kết quả bảng
SELECT * FROM Laptop;
```

### Kịch bản 2: Truy vấn Siêu dữ liệu qua Bảng ảo (Metadata)
Bạn có thể dùng `SELECT` để xem toàn bộ cấu trúc của Database mà không cần các lệnh `DESCRIBE` riêng lẻ.

```sql
-- Liệt kê các Concept đang có
SELECT Name, Description FROM system.concepts;

-- Xem chi tiết các biến của một Concept
SELECT * FROM Laptop.variables;
```

### Kịch bản 3: Thực thi Chuỗi lệnh (Multi-statement)
Bạn có thể dán một đoạn code dài vào CLI để thực thi cùng lúc.

```sql
CREATE CONCEPT T1 (v:INT); INSERT INTO T1 ATTRIBUTE (v:1); SELECT * FROM T1;
```

### Kịch bản 4: Phân quyền & Bảo mật (TCL/KCL)
```sql
CREATE USER manager PASSWORD 'mypass123' ROLE USER;
GRANT PRIVILEGE READ ON Store TO manager;
```

### Kịch bản 5: Phân tích & Suy luận (SOLVE & EXPLAIN)
Khi gặp một bài toán hình học phức tạp, sử dụng `SOLVE` để tìm lời giải và `EXPLAIN` để xem các bước suy luận.

```sql
-- Ví dụ Hình chữ nhật
CREATE CONCEPT Rect ( VARIABLES(w:DOUBLE, h:DOUBLE, a:DOUBLE), CONSTRAINTS(a=w*h) );
INSERT INTO Rect ATTRIBUTE (w:5, h:10);

-- Giải tìm diện tích
SOLVE ON CONCEPT Rect FIND a;

-- Giải thích cách Server thực thi truy vấn
EXPLAIN ( SELECT * FROM Rect WHERE w > 2 );
```

---

## 4. Các lỗi thường gặp (Troubleshooting)
1. **Lỗi kết nối**: Đảm bảo Port `3307` không bị tường lửa chặn hoặc đang bị Process khác sử dụng.
2. **Quên dấu `;`**: CLI của v1.1 yêu cầu chặt chẽ dấu chấm phẩy ở cuối mỗi câu lệnh (hoặc chuỗi lệnh).
3. **Sai kiểu dữ liệu**: Nếu bạn chèn một chuỗi (String) vào cột `INT`, hệ thống sẽ trả về lỗi `Invalid cast`.
