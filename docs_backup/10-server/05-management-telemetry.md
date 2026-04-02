# 10.4. Quản trị & Đo lường từ xa

Một máy chủ [KBMS](../00-glossary/01-glossary.md#kbms) C# hiện đại không thể thiếu khả năng tự theo dõi sức khỏe và phát tán nhật ký hoạt động (Logs). Mã nguồn tại `ManagementManager.cs` và `SystemLogger.cs` đảm nhiệm vai trò này.

## 1. Giám sát Hệ thống

Hệ thống cho phép các Client (đặc biệt là KBMS Studio) truy xuất dữ liệu vận hành thời gian thực thông qua bộ lệnh quản trị cấp cao:

*   **Bộ đếm Metrics (STATS)**: 
    Cung cấp thông tin về số lượng **Active Connections**, thời gian hoạt động của Server (Uptime), và tỷ lệ chiếm dụng RAM của `BufferPool`. Dữ liệu này giúp người quản trị biết khi nào cần mở rộng bộ nhớ máy chủ.
*   **Quản lý phiên (SESSIONS)**: 
    Trích xuất danh sách tất cả các người dùng đang Online. Cho phép thực hiện các thao tác "Kill Session" hoặc giới hạn số lượng kết nối đồng thời để bảo vệ tài nguyên hệ thống.

---

## 2. Hệ thống Nhật ký Luồng (Live Log Streaming)

Đây là tính năng độc đáo nhất của KBMS V3, cho phép "lắng nghe" mọi biến động của máy chủ ngay lập tức.

### 2.1. `SystemLogger.cs`
Lớp này đóng vai trò là "ngòi bút" của hệ thống. Khi bất kỳ lỗi nào xảy ra trong `Storage` hay `Reasoning`, `SystemLogger` sẽ thực hiện đồng thời hai việc:
1.  **Write to Disk**: Ghi vào file `.log` truyền thống để truy vết sau này.
2.  **Write to [System KB](../00-glossary/01-glossary.md#system-kb)**: Ghi trực tiếp một [Fact](../00-glossary/01-glossary.md#fact) mới vào [Concept](../00-glossary/01-glossary.md#concept) `Log` của Knowledge Base `system`.

### 2.2. `LOGS_STREAM` Protocol
Khi một Admin Client gửi yêu cầu `LOGS_STREAM`, `ManagementManager` sẽ thực hiện cơ chế **[Pub/Sub](../00-glossary/01-glossary.md#pubsub) (Publish/Subscribe)**:
*   Bất kỳ dòng log mới nào vừa phát sinh sẽ được "bắn" ngay lập tức vào Socket của client đó dưới dạng gói tin chuẩn.
*   Điều này cho phép xây dựng các bảng điều khiển (Dashboard) giám sát tri thức mà không cần phải làm mới (Refresh) trang web hay ứng dụng.

---

## 3. Lệnh Quản trị Tối cao

Đối với các tác vụ thay đổi cấu hình nóng (Hot-swapping config) hoặc khôi phục dữ liệu, Server cung cấp [Message Type](../00-glossary/01-glossary.md#message-type) `MANAGEMENT_CMD (0x0D)`:
*   **Chỉ dành cho ROOT**: Chỉ người dùng có vai trò ROOT mới có thể kích hoạt các lệnh này.
*   **Tính năng**: Thay đổi tham số `MaxConnections`, cấu hình `BufferPoolSize`, hoặc khởi chạy tiến trình `Checkpoint` thủ công để dọn dẹp WAL.

