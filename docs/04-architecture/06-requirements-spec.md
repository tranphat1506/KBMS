# 04.6. Đối soát Yêu cầu Kỹ thuật (Requirements Spec)

Tài liệu này tổng hợp toàn bộ các yêu cầu Chức năng (Functional) và Phi chức năng (Non-functional) dựa trên kiến trúc 4 tầng của KBMS.

## 1. Yêu cầu Chức năng (Functional Requirements)

Hệ thống cung cấp đầy đủ các nhóm chức năng từ mức ứng dụng đến mức lưu trữ:

| Tầng Kiến trúc | Nhóm Chức năng | Hành động cụ thể |
| :--- | :--- | :--- |
| **Application** | Thiết kế & Giám sát | IDE Monaco, Kéo thả đồ thị, Dashboad thông số, REPL nâng cao. |
| **Network** | Kết nối & Truyền dẫn | Handshake nhị phân, Heartbeat, Giải mã Streaming Result Set. |
| **Server** | Xử lý & Suy diễn | Biên dịch KBQL, Suy diễn F-Closure, Giải phương trình, RBAC. |
| **Storage** | Lưu trữ & Phục hồi | B+ Tree Search, WAL Recovery, Checkpoint, AES Encryption. |

## 2. Yêu cầu Phi chức năng (Non-functional Requirements)

Đảm bảo hệ thống đạt được các tiêu chuẩn kỹ thuật chuyên nghiệp:

### 2.1. Độ bền vững & Nhất quán (ACID Compliance)
*   **Atomicity**: Mọi thay đổi dữ liệu là nguyên tố thông qua nhật ký WAL.
*   **Consistency**: Sau mọi lệnh KDL/KQL, tri thức phải ở trạng thái nhất quán về mặt logic.
*   **Durability**: Dữ liệu đã cam kết (Commit) không bị mất mát sau sự cố.

### 2.2. Hiệu năng & Khả năng đáp ứng (Performance)
*   **Độ trễ**: Thời gian phản hồi trung bình < 10ms trên mạng LAN.
*   **Tải**: Hỗ trợ đồng thời 256+ kết nối mà không làm giảm hiệu suất suy diễn.
*   **Truy xuất**: Tìm kiếm bản ghi $O(\log n)$ trên đĩa cứng quy mô TB.

### 2.3. Bảo mật (Security)
*   **Xác thực**: Cơ chế LOGIN nhị phân với mật khẩu băm (Hashed).
*   **Phân quyền**: RBAC đa tầng (Role-Based Access Control) đến từng Cơ sở tri thức.
*   **Mã hóa**: AES-256 cho dữ liệu tĩnh trên đĩa cứng.

### 2.4. Khả năng Giám sát (Observability)
*   **Logging**: Tự động ghi nhật ký mọi hành vi (Audit) và lỗi hệ thống (System Log).
*   **Real-time**: Stream log từ Server về Studio với độ trễ < 1s.
*   **Queryable Logs**: Cho phép dùng KBQL để truy vấn ngược lại lịch sử vận hành.

---

> [!IMPORTANT]
> Toàn bộ các yêu cầu trên được thiết kế để KBMS có thể vận hành như một hệ quản trị tri thức cấp doanh nghiệp ổn định và tin cậy.
