# 10.1. Kiến trúc Điều phối và Quản trị Hệ thống (Orchestration & Management)

Máy chủ [KBMS](../00-glossary/01-glossary.md#kbms) V3 (`KbmsServer.cs`) được thiết kế trên mô hình xử lý hiệu năng cao, tích hợp khả năng điều phối mạng bất đồng bộ và cơ chế quản trị tri thức tự thân (Self-managed).

---

## 1. Mô hình Xử lý Luồng Bất đồng bộ (TAP Pattern)

Để tối ưu hóa thông lượng, [KBMS](../00-glossary/01-glossary.md#kbms) sử dụng kiến trúc **Task-based Asynchronous Pattern ([TAP](../00-glossary/01-glossary.md#tap))** của .NET thay vì mô hình một-luồng-mỗi-khách-hàng (1-Thread-Per-Client) truyền thống.

- **Non-blocking Acceptance**: Vòng lặp chính sử dụng `AcceptTcpClientAsync` để lắng nghe kết nối mới mà không làm nghẽn luồng chính. 
- **ThreadPool Integration**: Mỗi yêu cầu từ phía khách hàng được đẩy trực tiếp vào `ThreadPool` của hệ điều hành, cho phép tận dụng tối đa sức mạnh đa nhân của CPU mà không phải trả chi phí quản lý luồng nặng nề.
- **SemaPhore Isolation**: Để đảm bảo tính toàn vẹn của gói tin trong môi trường bất đồng bộ, lớp `Protocol` sử dụng `SemaphoreSlim` để đồng bộ hóa việc ghi dữ liệu xuống Socket, ngăn chặn hiện tượng trộn lẫn byte (Bytes Interleaving).

---

## 2. Quản lý Phiên và Kết nối (Session Management)

Thành phần `ConnectionManager.cs` chịu trách nhiệm duy trì trạng thái và bảo mật cho từng kết nối:
- **[Session Isolation](../00-glossary/01-glossary.md#session-isolation)**: Mỗi khách hàng được gán một `SessionID` duy nhất cùng không gian tên (Namespace) tri thức riêng biệt.
- **Dọn dẹp Tự động (Cleanup Task)**: Một tiến trình ngầm được khởi chạy mỗi 30 giây để quét và tự động đóng các Socket không hoạt động vượt quá ngưỡng `DefaultTimeoutSeconds` (mặc định 60 giây), giúp giải phóng tài nguyên hệ thống.

---

## 3. Quản trị Hệ thống bằng Tri thức (System KB)

Một đặc điểm đột phá của [KBMS](../00-glossary/01-glossary.md#kbms) V3 là việc sử dụng chính mô hình tri thức [COKB](../00-glossary/01-glossary.md#cokb) để quản lý hệ thống. Một cơ sở tri thức đặc biệt tên là **`system`** (tệp `system.kdb`) được khởi tạo để lưu trữ:

### 3.1. Nhật ký Kiểm toán (Audit Logs)
Lưu trữ mọi yêu cầu từ người dùng để phục vụ mục đích bảo mật và phân tích hiệu năng. Các trường dữ liệu bao gồm: `timestamp`, `username`, `command` ([KBQL](../00-glossary/01-glossary.md#kbql)), `status`, và `duration_ms`.

### 3.2. Quản lý Người dùng (RBAC)
Hệ thống phân quyền dựa trên vai trò (Role-Based Access Control):
- **ROOT**: Toàn quyền trên mọi Knowledge Base và có quyền quản trị hệ thống.
- **USER**: Chỉ có quyền thao tác trên các vùng tri thức được chỉ định.
- **Authentication**: Mật khẩu được băm (Hashed) và lưu trữ trực tiếp trong [Concept](../00-glossary/01-glossary.md#concept) `user_catalog` của hệ thống.

---

## 4. Phân tích Hệ thống thông qua KBQL

Người quản trị ([DBA](../00-glossary/01-glossary.md#dba)) có khả năng truy vấn trạng thái máy chủ ngay bằng ngôn ngữ [KBQL](../00-glossary/01-glossary.md#kbql), ví dụ:
```sql
-- Thống kê các truy vấn có thời gian xử lý > 500ms để tối ưu hóa
SELECT username, command, duration_ms 
FROM audit_logs 
WHERE duration_ms > 500 
IN system;
```
Cơ chế này mang lại khả năng quản trị đồng nhất: mọi thứ trong [KBMS](../00-glossary/01-glossary.md#kbms) đều là tri thức, ngay cả chính nhật ký vận hành của nó.
