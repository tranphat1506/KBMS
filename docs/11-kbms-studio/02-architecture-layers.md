# Kiến trúc 4 Tầng & Luồng Xử lý Chi tiết

KBMS Studio vận hành dựa trên một kiến trúc phân tầng chặt chẽ, từ giao diện người dùng đến các trang dữ liệu nhị phân thấp cấp trên đĩa cứng.

## 1. Sơ đồ Luồng xử lý 4 Tầng (4-Tier Flow)

Sơ đồ dưới đây mô tả cách một câu lệnh Tri thức (như `SOLVE`) đi qua hệ thống:

![4_tier_studio_flow.png](../assets/diagrams/4_tier_studio_flow.png)

---

## 2. Đặc tả các Tầng

### Tầng 1: Application UI (React/Electron)
*   **Frontend**: React xử lý trạng thái luồng (Redux). Khi người dùng nhấn "Run", lệnh được chuyển xuống Electron Main process.
*   **Auth**: Quản lý Session Token và quyền truy cập cục bộ trên giao diện.

### Tầng 2: Network & Protocol (Socket)
*   **Binary Framing**: Chuyển đổi lệnh thành các gói tin nhị phân. 
*   **Telemetry**: Gửi tín hiệu nhịp tim (Heartbeat) định kỳ để duy trì kết nối Socket ổn định giữa Studio và Server.

### Tầng 3: Knowledge Engine (Server)
*   **KnowledgeManager**: Điều phối việc phân tách câu lệnh bằng Lexer/Parser.
*   **Reasoning**: Nếu là lệnh `SOLVE`, bộ máy suy diễn sẽ thực hiện Forward Chaining trên các Page dữ liệu được nạp vào RAM.
*   **Logs**: Ghi lại nhật ký truy cập (Audit) đồng thời Stream Log trở lại Studio UI.

### Tầng 4: Storage Layer (Binary Data)
*   **B+ Tree**: Tìm kiếm bản ghi Object dựa trên Index.
*   **LRU Cache**: Quản lý các trang dữ liệu (Pages) trong bộ nhớ đệm để tối ưu hóa tốc độ.
*   **WAL (Write-Ahead Logging)**: Đảm bảo tính toàn vẹn tri thức ngay cả khi mất điện đột ngột.

---

## 3. Luồng Thông báo Thời gian thực (Notification Flow)

Khác với luồng Request-Response thông thường, hệ thống Notification sử dụng cơ chế **Server Push**:

![4_tier_notification_flow.png](../assets/diagrams/4_tier_notification_flow.png)

1.  **Trigger**: Một sự kiện an ninh hoặc hệ thống xảy ra tại Server.
2.  **Push**: Server đóng gói `MessageType.NOTIFICATION` và đẩy qua Socket.
3.  **Dispatch**: Electron Main nhận gói tin và gửi vào Redux Store của React.
4.  **UI Feedback**: Component `NotificationToasts` hiển thị thông báo popup, đồng thời cập nhật số lượng tin nhắn mới tại `NotificationBell`.

---

## 4. Quy trình Đăng nhập (Authentication Flow)

1.  **Handshake**: Studio gửi gói tin `LOGIN` kèm User/Pass được mã hóa Base64.
2.  **Server Verify**: Server kiểm tra trong hệ thống quản lý User (Tầng 4).
3.  **Session Record**: Nếu khớp, Server tạo một `SessionContext` trong RAM (Tầng 3) và trả về thông báo thành công.
4.  **Prompt Update**: Studio cập nhật trạng thái đã đăng nhập và cho phép thao tác với Knowledge Base.

> [!IMPORTANT]
> **Data Consistency**
> Mọi thay đổi về tri thức đều phải đi qua Tầng 4 (WAL) trước khi được phản hồi lên Tầng 1, đảm bảo tính nhất quán (ACID) tuyệt đối cho hệ thống. 🔒
