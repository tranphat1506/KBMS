# Chi Tiết Lõi Lưu Trữ (Storage Engine Internals)

KBMS 1.0 sử dụng một bộ máy lưu trữ (Storage Engine) hiệu năng cao, được thiết kế đặc biệt để xử lý các đối tượng tri thức phức tạp với tính toàn vẹn dữ liệu tuyệt đối.

## 1. Cơ Chế Buffer Pool (Bộ Đệm RAM)
KBMS không đọc trực tiếp file từ đĩa cho mỗi truy vấn. Thay vào đó, nó sử dụng **Buffer Pool** để quản lý dữ liệu trên RAM.

- **Lazy Load**: File dữ liệu (`.kdf`) chỉ được nạp vào RAM khi có yêu cầu truy cập lần đầu tiên.
- **Cache per KB**: Mỗi Knowledge Base có một vùng nhớ đệm riêng biệt, giúp tránh xung đột tài nguyên giữa các dự án.
- **Tốc độ Truy xuất**: Các câu lệnh `SELECT` và `SOLVE` hầu hết được thực thi trực tiếp trên RAM, mang lại tốc độ cực nhanh.

## 2. Write-Ahead Logging (WAL) - Phục Hồi Dữ Liệu
Để đảm bảo an toàn, KBMS áp dụng cơ chế **WAL** thông qua file `.klf` (Knowledge Log File).

- **Ghi trước khi thực hiện**: Mọi hành động `INSERT`, `UPDATE`, `DELETE` đều được ghi vào log file trước khi cập nhật vào Buffer Pool.
- **Checkpoint**: Sau một khoảng thời gian nhất định, log sẽ được "dọn dẹp" (VACUUM) và dữ liệu được đồng bộ cứng xuống file `.kdf`.
- **Phục hồi sau sự cố**: Nếu Server bị ngắt đột ngột, khi khởi động lại, `WalManager` sẽ quét file `.klf` để tái tạo lại trạng thái dữ liệu mới nhất trên RAM.

## 3. Shadow Paging - Giao Dịch ACID
Khi bạn sử dụng `BEGIN TRANSACTION`, KBMS thực hiện:
1.  **Sao chép bóng (Shadow Copy)**: Tạo một bản sao của Buffer Pool hiện tại.
2.  **Thao tác tạm thời**: Mọi thay đổi chỉ tác động lên bản sao này.
3.  **Commit**: Nếu bạn gọi `COMMIT`, bản sao bóng sẽ được đẩy thành bản chính và ghi vào WAL.
4.  **Rollback**: Nếu bạn gọi `ROLLBACK`, bản sao bóng bị hủy, dữ liệu gốc vẫn giữ nguyên.

## 4. Định Dạng File Hệ Thống (File Formats)

| Đuôi File | Tên Đầy Đủ | Nội Dung Chính |
| :--- | :--- | :--- |
| **.kmf** | Knowledge Metadata File | Chứa Concept, Rule, Relation, User, Privilege (Dạng nhị phân). |
| **.kdf** | Knowledge Data File | Chứa các Instance (Object) thực tế của tri thức. |
| **.klf** | Knowledge Log File | Nhật ký giao dịch phục vụ WAL và Rollback. |
| **.kif** | Knowledge Index File | Chuyên biệt cho tìm kiếm nhanh (B-Tree/Hash). |

---
*Ghi chú: Bạn có thể sử dụng lệnh `MAINTENANCE (VACUUM)` để dọn dẹp file log và nén file dữ liệu vật lý.*
