# Hướng Dẫn Cài Đặt & 05 Kịch Bản Sử Dụng Thực Tế

Tài liệu này hướng dẫn bạn cách triển khai hệ thống KBMS và cung cấp các ví dụ thực tế để khai thác tối đa sức mạnh của tri thức tính toán (COKB).

## 1. Hướng Dẫn Cài Đặt (Installation)

### 1.1. Yêu cầu Hệ thống
- **Môi trường**: .NET 8.0 SDK (Windows, macOS, Linux).
- **RAM**: Tối thiểu 512MB (Khuyên dùng 1GB để tối ưu Buffer Pool).

### 1.2. Các bước triển khai
1.  **Khởi động Server**:
    Mở Terminal 1, di chuyển đến thư mục `KBMS.Server` và chạy lệnh:
    ```bash
    dotnet run
    ```
    Server sẽ lắng nghe tại cổng `34000`.

2.  **Khởi động Client**:
    Mở Terminal 2, di chuyển đến thư mục `KBMS.CLI` và chạy lệnh:
    ```bash
    dotnet run
    ```
    Kết nối thành công khi bạn thấy dấu nhắc `kbms>`.

---

## 2. 05 Kịch Bản Sử Dụng Thực Tế (Usage Scenarios)

### Kịch bản 1: Quản lý Hình học & Giải toán Tự động
Định nghĩa một khái niệm hình học và yêu cầu hệ thống tự giải các biến còn thiếu.
```sql
-- Tạo KB và Concept với ràng buộc
CREATE KNOWLEDGE BASE Geometry;
USE Geometry;
CREATE CONCEPT Triangle (
    VARIABLES ( a: double, b: double, c: double, s: double ),
    CONSTRAINTS ( s = (a + b + c) / 2 )
);
-- Giải toán (Inference)
SOLVE ON CONCEPT Triangle GIVEN a: 3, b: 4, c: 5 FIND s SAVE;
```

### Kịch bản 2: Hệ thống Phản ứng Dữ liệu (Triggers)
Tự động thông báo hoặc ghi log khi có thay đổi dữ liệu quan trọng.
```sql
CREATE TRIGGER NotifyOnNewRect 
ON INSERT OF Rectangle 
DO ( SHOW CONCEPTS ); -- Ví dụ hành động tự động
```

### Kịch bản 3: Quản lý Giao dịch An toàn (ACID)
Đảm bảo chuỗi hành động được thực hiện hoàn toàn hoặc không thực hiện gì nếu lỗi.
```sql
BEGIN TRANSACTION;
INSERT INTO Rectangle ATTRIBUTE ( width: 10, height: 20 );
UPDATE Rectangle SET width: 15 WHERE height: 20;
COMMIT; -- Chốt dữ liệu xuống đĩa cứng (.kdf)
```

### Kịch bản 4: Tối ưu hóa Truy vấn Lớn (Indexing)
Tăng tốc tìm kiếm khi CSDL có hàng nghìn đối tượng.
```sql
CREATE INDEX ON Rectangle ( width );
EXPLAIN ( SELECT * FROM Rectangle WHERE width > 50 );
```

### Kịch bản 5: Quản trị & Bảo trì Hệ thống (Maintenance)
Dọn dẹp log và kiểm tra tính nhất quán của tri thức.
```sql
MAINTENANCE ( VACUUM, CHECK ( CONSISTENCY: * ) );
```

---

## 3. Câu Hỏi Thường Gặp (FAQ)

- **Làm sao để thoát?**: Sử dụng lệnh `EXIT;`.
- **Dữ liệu được lưu ở đâu?**: Trong thư mục `Data/` dưới dạng các file `.kmf`, `.kdf`, `.klf`, `.kif`.
- **Hỗ trợ Subquery không?**: Hiện tại v1.0 chưa hỗ trợ, chúng tôi đang phát triển cho v2.0.

---
*© 2026 KBMS Team. Chúc bạn có trải nghiệm tuyệt vời với KBMS!*
