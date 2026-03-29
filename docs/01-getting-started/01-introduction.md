# Hướng dẫn Bắt đầu (Getting Started)

Chào mừng bạn đến với KBMS (Knowledge Base Management System) – hệ quản trị cơ sở tri thức hiện đại tích hợp khả năng suy diễn tự động.

## 1. Yêu cầu Hệ thống

Để chạy KBMS, máy tính của bạn cần đáp ứng:
*   **Runtime:** .NET 6.0 trở lên (cho Server và CLI).
*   **Bộ nhớ:** Tối thiểu 512MB RAM cho các KB nhỏ; 2GB+ cho các bài toán suy diễn phức tạp.
*   **Lưu trữ:** Tối thiểu 100MB cho bộ cài đặt và dữ liệu mẫu.

---

## 2. Quy trình Cài đặt & Khởi chạy

### Bước 1: Khởi động Server
Server là thành phần cốt lõi xử lý các truy vấn và suy diễn.
1.  Truy cập thư mục `KBMS.Server`.
2.  Chạy lệnh: `dotnet run`.
*   **Mặc định:** Server sẽ lắng nghe tại cổng **TCP 5000** (hoặc cấu hình trong `kbms.ini`).

### Bước 2: Kết nối từ Client
Bạn có thể kết nối với Server thông qua CLI hoặc KBMS Studio.
*   **CLI:** Chạy `dotnet run --project KBMS.CLI`.
*   **Studio:** Mở ứng dụng Electron từ thư mục `kbms-studio`.

---

## 3. Tạo Cơ sở Tri thức đầu tiên

Thực hiện các lệnh KBQL sau trong giao diện CLI hoặc Studio để làm quen:

```kbql
-- 1. Tạo KB mới
CREATE KNOWLEDGE BASE LearningKB;
USE LearningKB;

-- 2. Định nghĩa một Concept cơ bản
CREATE CONCEPT Person (VARIABLES(id: INT, name: STRING, age: INT));

-- 3. Tạo một Rule tự động
CREATE RULE CheckAdult 
SCOPE Person 
IF age >= 18 
THEN SET status = 'Adult';

-- 4. Chèn dữ liệu (Sự kiện)
INSERT INTO Person ATTRIBUTE (1, 'John Doe', 20);

-- 5. Truy vấn và Xem kết quả suy diễn
SELECT name, age, status FROM Person;
```

---

## 4. Cấu hình Hệ thống (kbms.ini)

Mọi cấu hình quan trọng đều nằm trong tệp `kbms.ini` tại thư mục gốc:
*   `StoragePath`: Đường dẫn lưu trữ tệp `.dat`, `.kbf`.
*   `Port`: Cổng mạng cho Server.
*   `InferenceLimit`: Giới hạn số vòng lặp suy diễn để tránh vòng lặp vô tận (mặc định 50).
*   `BufferPoolSize`: Số lượng Page tối đa lưu trên RAM (mặc định 100).
