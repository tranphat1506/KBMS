# Hướng Dẫn Cài Đặt và Sử Dụng KBMS V2

KBMS (Knowledge Base Management System) được xây dựng trên nền tảng .NET Core C#, với mô hình CLI Terminal Client giao tiếp với Back-end Server qua giao thức mạng TCP.

## 1. Yêu Cầu Hệ Thống (Minimum Requirements)
- **Hệ điều hành**: Windows 10/11, macOS, hoặc Linux (Debian/Ubuntu).
- **Môi trường SDK**: .NET 8.0 SDK (hoặc mới hơn).
- **RAM**: Tối thiểu 50MB cho chế độ nền (Idle Server). Khuyến nghị 512MB - 1GB để tận dụng sức mạnh RAM Buffer Pool khi xử lý hàng trăm ngàn Object.
- **Dung lượng Đĩa**: 100MB cho Source Code. Ổ đĩa Data tùy thuộc dung lượng Knowledge Base `.kmf`, `.kdf`.

## 2. Biên Dịch và Khởi Chạy (Build & Run)

Bởi vì kiến trúc tách biệt 2 luồng Client (Frontend) và Server (Backend), bạn cần mở 2 cửa sổ Terminal độc lập để trải nghiệm trọn vẹn sức mạnh mạng:

### Bước 1: Khởi động Lõi CSDL (Storage Engine Server)
1. Mở Terminal / Command Prompt thứ nhất.
2. Trỏ vào thư mục chứa mã nguồn Server:
```bash
cd KBMS.Server
```
3. Bật máy chủ chạy ngầm:
```bash
dotnet run
```
*Hệ thống sẽ nạp Cấu trúc RAM Buffer Pool và hóng kết nối chuẩn bị lắng nghe Command từ Port TCP mặc định.*

### Bước 2: Khởi động Hệ Thống Giao Tiếp (CLI Client)
1. Mở Terminal / Command Prompt thứ hai.
2. Trỏ vào thư mục chứa giao diện CLI:
```bash
cd KBMS.CLI
```
3. Kết nối với Server đã bật:
```bash
dotnet run
```
*Lập tức Prompt KBQL `kbms>` sẽ hiện ra, sẵn sàng cho bạn gõ code.*

## 3. Kịch Bản Sử Dụng CLI Điển Hình (Usage Scenario)

Khi cửa sổ dòng lệnh Client `kbms>` đã mở, bạn có thể chạy quy trình DBMS thử tải sau:

```sql
-- 1. Đăng nhập quyền cao nhất
kbms> root
Password: 123

-- 2. Khởi tạo một Database Tri Thức mới (Mở Folder /data)
kbms> CREATE KNOWLEDGE BASE HinhHoc;
kbms> USE HinhHoc;

-- 3. Bắt đầu phiên Giao Dịch RAM (Chặn ổ đĩa Disk IO)
kbms> BEGIN TRANSACTION;

-- 4. KDL: Định nghĩa Schema / Concept Khối B-Tree
kbms> CREATE CONCEPT <TAMGIAC> 
      (
          VARIABLES ( A: DIEM, B: DIEM, Canh: INT )
      );

-- 5. KML: Đẩy data lên RAM Session (Không hề chạm file đĩa .kdf)
kbms> INSERT INTO <TAMGIAC> ATTRIBUTE ( A:1, B:2, Canh:5 );
kbms> INSERT INTO <TAMGIAC> ATTRIBUTE ( A:4, B:6, Canh:10 );

-- 6. KQL: Truy vấn tốc độ ánh sáng trên RAM
kbms> SELECT A, Canh FROM <TAMGIAC> WHERE Canh >= 5;

-- 7. TCL: Chốt đơn chép nén cấu trúc RAM đè xuống Tệp Binary đĩa cứng
kbms> COMMIT; 

-- Thưởng thức: Mở thư mục /data/HinhHoc để chiêm ngưỡng sự xuất hiện của file transactions.klf, concepts.kmf và objects.kdf.
```

## 4. Xử Lý Khi Gặp Sự Cố (Troubleshooting)
- **Cúp Điện khi Chưa Commit**: Bật lại Server (`dotnet run` KBMS.Server), Engine sẽ tự đọc file `transactions.klf` tái sinh nguyên trạng RAM.
- **Lỗi Mạng TCP**: Đảm bảo cổng Port nội bộ (VD: `5000` hoặc `8080`) mở khóa Firewall và IP cấu hình đang chạy ở `127.0.0.1` (Localhost).
