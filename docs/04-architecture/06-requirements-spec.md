# 04.6. Đối soát Yêu cầu Kỹ thuật

Tài liệu này tổng hợp toàn bộ các yêu cầu Chức năng (Functional) và Phi chức năng (Non-functional) dựa trên kiến trúc 4 tầng của [KBMS](../00-glossary/01-glossary.md#kbms).

## 1. Yêu cầu Chức năng

Hệ thống cung cấp đầy đủ các nhóm chức năng từ mức ứng dụng đến mức lưu trữ:

*Bảng 4.1: Đặc tả yêu cầu phi chức năng hệ thống [KBMS](../00-glossary/01-glossary.md#kbms)*
| Tầng Kiến trúc | Nhóm Chức năng | Hành động cụ thể |
| :--- | :--- | :--- |
| **Application** | Thiết kế & Giám sát | [IDE](../00-glossary/01-glossary.md#ide) [Monaco](../00-glossary/01-glossary.md#monaco), Kéo thả đồ thị, Dashboad thông số, [REPL](../00-glossary/01-glossary.md#repl) nâng cao. |
| **Network** | Kết nối & Truyền dẫn | [Handshake](../00-glossary/01-glossary.md#handshake) nhị phân, [Heartbeat](../00-glossary/01-glossary.md#heartbeat), Giải mã [Streaming](../00-glossary/01-glossary.md#streaming) Result Set. |
| **Server** | Xử lý & Suy diễn | Biên dịch [KBQL](../00-glossary/01-glossary.md#kbql), Suy diễn [F-Closure](../00-glossary/01-glossary.md#f-closure), Giải phương trình, [RBAC](../00-glossary/01-glossary.md#rbac). |
| **Storage** | Lưu trữ & Phục hồi | [B+ Tree](../00-glossary/01-glossary.md#b-tree) Search, WAL Recovery, [Checkpoint](../00-glossary/01-glossary.md#checkpoint), AES Encryption. |

## 2. Yêu cầu Phi chức năng

Đảm bảo hệ thống đạt được các tiêu chuẩn kỹ thuật chuyên nghiệp:

### 2.1. Độ bền vững & Nhất quán (ACID Compliance)
*   **Atomicity**: Mọi thay đổi dữ liệu là nguyên tố thông qua nhật ký WAL.
*   **Consistency**: Sau mọi lệnh [KDL](../00-glossary/01-glossary.md#kdl)/[KQL](../00-glossary/01-glossary.md#kql), tri thức phải ở trạng thái nhất quán về mặt logic.
*   **Durability**: Dữ liệu đã cam kết (Commit) không bị mất mát sau sự cố.

### 2.2. Hiệu năng & Khả năng đáp ứng
*   **Độ trễ**: Thời gian phản hồi trung bình < 10ms trên mạng LAN.
*   **Tải**: Hỗ trợ đồng thời 256+ kết nối mà không làm giảm hiệu suất suy diễn.
*   **Truy xuất**: Tìm kiếm bản ghi $O(\log n)$ trên đĩa cứng quy mô TB.

### 2.3. Bảo mật
*   **Xác thực**: Cơ chế LOGIN nhị phân với mật khẩu băm (Hashed).
*   **Phân quyền**: [RBAC](../00-glossary/01-glossary.md#rbac) đa tầng (Role-Based Access Control) đến từng Cơ sở tri thức.
*   **Mã hóa**: [AES-256](../00-glossary/01-glossary.md#aes-256) cho dữ liệu tĩnh trên đĩa cứng.

### 2.4. Khả năng Giám sát
*   **Logging**: Tự động ghi nhật ký mọi hành vi (Audit) và lỗi hệ thống (System Log).
*   **Real-time**: Stream log từ Server về Studio với độ trễ < 1s.
*   **Queryable Logs**: Cho phép dùng [KBQL](../00-glossary/01-glossary.md#kbql) để truy vấn ngược lại lịch sử vận hành.

