# 07.6. Định dạng Tệp tin (File Formats)

Hệ thống KBMS quản lý tri thức bền vững thông qua các loại tệp tin chuyên biệt. Việc hiểu rõ vai trò của từng loại tệp giúp quản trị viên thực hiện công tác bảo trì và sao lưu hiệu quả.

---

## 1. Phân loại tệp tin chính

| Định dạng | Tên gọi | Vai trò kỹ thuật |
| :--- | :--- | :--- |
| **.dat** | **Data File** | Chứa dữ liệu thực tế (Instances) của các Concept. Được cấu trúc dưới dạng B+ Tree nhị phân. |
| **.kbf** | **Knowledge Base File** | Lưu trữ định nghĩa Schema, Rules, Equations và Relations (Metadata). |
| **.log** | **WAL Log File** | Nhật ký ghi trước mọi thay đổi I/O để phục hồi trạng thái khi gặp sự cố đột ngột. |
| **.kbql** | **Script File** | Tệp văn bản chứa chuỗi câu lệnh KBQL hỗ trợ chế độ chạy hàng loạt (Batch Execution). |
| **.ini** | **Config File** | Tệp cấu hình hệ thống (Cổng kết nối, Đường dẫn dữ liệu, Master Key). |

## 2. Tương tác giữa các tệp tin

Khi người dùng thực hiện lệnh `USE KB Project_Alpha`, Server sẽ kích hoạt chuỗi hành động sau:
1.  **Nạp Metadata**: Đọc tệp `Project_Alpha.kbf` để tái cấu trúc mô hình tri thức trong bộ nhớ RAM (Parser/Inference Engine).
2.  **Kết nối Data**: Trỏ `DiskManager` tới tệp `Project_Alpha.dat` để sẵn sàng truy xuất các trang dữ liệu qua Buffer Pool.
3.  **Kích hoạt WAL**: Khởi tạo hoặc tiếp tục ghi vào `Project_Alpha.log` để bảo vệ các giao dịch tri thức sắp tới.

## 3. Bảo mật tệp tin (Encryption at Rest)

Mọi tệp **.dat** và **.kbf** đều được mã hóa bằng thuật toán **AES-256**.
*   **Master Key**: Được lưu trữ trong tệp cấu hình `.ini` hoặc biến môi trường.
*   **Data Key**: Mỗi KB có thể có một khóa con (Sub-key) riêng để tăng cường tính bảo mật đa tầng.

---

> [!TIP]
> **Sao lưu (Backup)**: Một bản sao lưu KB đầy đủ phải bao gồm cả hai tệp **.dat** và **.kbf** tương ứng để đảm bảo tính toàn vẹn của cả Dữ liệu và Tri thức.
