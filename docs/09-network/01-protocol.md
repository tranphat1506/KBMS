# Giao thức Mạng (Network Protocol)

# 09.1. Giao thức Truyền dẫn Nhị phân (Binary Protocol)
KBMS sử dụng một giao thức truyền tin nhị phân (Binary Protocol) tùy chỉnh chạy trên nền TCP để đảm bảo tốc độ truyền tải tri thức nhanh nhất và khả năng xử lý đồng thời (concurrency) tốt.

## 09.2. Cấu trúc Gói tin (Packet Structure)

Mỗi thông điệp (Message) gửi đi giữa Client và Server được đóng gói theo định dạng sau:

| Trường | Kích thước | Mô tả |
| :--- | :--- | :--- |
| **Total Length** | 4 bytes (Int32) | Tổng độ dài các trường tính từ *SessLen* trở đi (bỏ qua MessageType). |
| **Message Type** | 1 byte | Loại thông điệp (Query, Result, Error, ...). |
| **SessLen** | 2 bytes (UInt16) | Độ dài của chuỗi Session ID. |
| **Session ID** | N bytes (UTF-8) | Định danh phiên làm việc của người dùng. |
| **ReqLen** | 2 bytes (UInt16) | Độ dài của chuỗi Request ID. |
| **Request ID** | M bytes (UTF-8) | Định danh duy nhất cho mỗi yêu cầu truy vấn. |
| **Payload** | P bytes (UTF-8) | Nội dung thực tế (ví dụ: câu lệnh KBQL hoặc kết quả JSON). |

---

## 2. Các loại Thông điệp (Message Types)

Máy chủ được trang bị **12 Giao thức Chuẩn** bao quát mọi tình huống Truy vấn, Phân mảnh dữ liệu, Bảo mật và Telemetry Hệ thống (Thiết kế gốc từ `MessageType.cs`):

1.  **Bảo mật & Xác thực (Security Auth):**
    *   `LOGIN (0x01)`: Xin cấp quyền phiên làm việc bằng thông tin (User/Pass).
    *   `LOGOUT (0x05)`: Đóng kết nối định danh và trả phiên tài nguyên hệ thống.

2.  **Truy vấn (Query Routing):**
    *   `QUERY (0x02)`: Chở luồng văn bản gốc (Chuỗi lệnh KBQL).
    *   `RESULT (0x03)`: Gói kết quả suy diễn ở dạng tổng quát (JSON String) .
    *   `ERROR (0x04)`: Payload chứa tọa độ `Line`, `Column` và `Stacktrace` dùng để tô đỏ biên dịch viên của Designer.

3.  **Kỹ thuật Trả Lô Dữ Liệu Lớn (Tabular Data Streaming - TDS)**
    KBMS xử lý Hàng nghìn Fact mà không làm treo RAM Client bằng luồng C# phân đoạn kinh điển:
    *   `METADATA (0x06)`: Truyền trước sơ đồ lưới dữ liệu (Tên Field, Bề rộng UI Column).
    *   `ROW (0x07)`: Trình bày Payload theo chuỗi vòng lặp "Lô" (Ví dụ: Batch 100 Rows/Lần gói) gửi liên tục qua luồng Stream TCP. 
    *   `FETCH_DONE (0x08)`: Cờ khóa hạ màn chặn chuỗi vòng lặp đọc. Tổng hợp độ trễ thực thi CSDL (Execution Time - ms).

4.  **Hệ Quản trị Trực tiếp (Telemetry System)**
    Dành riêng cho các Client mang quyền hệ thống cấp cao (ROOT Role):
    *   `STATS (0x0A / 10)`: Theo dõi Metrics máy chủ thời gian thực.
    *   `LOGS_STREAM (0x0B / 11)`: Đăng ký lắng nghe Live Log Socket đang chảy của Hệ thống.
    *   `SESSIONS (0x0C / 12)`: Trích xuất danh bạ quản lý Users Online, kiểm soát giới hạn Concurrent Connections.
    *   `MANAGEMENT_CMD (0x0D / 13)`: Đỉnh cao phân quyền - Dùng để chạy lệnh thao tác cấp hệ điều hành (Chỉnh sửa Setting, Kill Session, Cấp Role).

---

## 3. Thuật toán Xử lý Luồng tin (Streaming Algorithm)

Để tránh hiện tượng treo ứng dụng khi truyền tải lượng tri thức lớn, KBMS áp dụng cơ chế **Asynchronous Packet Processing**:

1.  **Header Parsing:** Đọc 4 byte đầu tiên để xác định kích thước gói tin cần nhận.
2.  **Buffer Allocation:** Cấp phát vùng nhớ vừa đủ cho gói tin đó.
3.  **Exact Reading:** Sử dụng hàm `ReadExact` để đảm bảo nhận đủ dữ liệu trước khi giải mã (Decode).
4.  **Payload Dispatching:** Chuyển nội dung UTF-8 vào bộ phân tích cú pháp (Parser) hoặc hiển thị lên UI.

---

## 4. Bảo mật và Kết nối

*   **TCP Keep-Alive:** Duy trì kết nối ổn định giữa KBMS Studio và Server.
*   **Session Management:** Cho phép người dùng thực hiện nhiều truy vấn đồng thời trên cùng một kết nối mà không bị lẫn lộn dữ liệu (nhờ vào Request ID).
*   **Error Propagation:** Khi Server gặp lỗi suy diễn, nó sẽ đóng gói toàn bộ Stack Trace và vị trí lỗi vào một `Error Message` đặc biệt để Client hiển thị nháy đỏ trên trình soạn thảo bài toán.

---

## 5. Kiến trúc Xử lý Chịu tải (Concurrency Threading)

Để duy trì hiệu năng cao phục vụ cho hàng ngàn điểm kết nối TCP, KBMS Server loại bỏ hoàn toàn mô hình 1-Thread-Per-Client truyền thống, thay vào đó áp dụng cấu trúc xử lý Đa luồng Bất đồng bộ (Async/Await) đỉnh cao của .NET Core:
*   **Asynchronous I/O Socket:** Cổng nhận (`AcceptTcpClientAsync`) sẽ quăng toàn bộ tiến trình phân tích Packet vào ThreadPool để tránh tắt nghẽn luồng lắng nghe chính (`_ = HandleClientAsync(client)`).
*   **Tránh Va Chạm Thread Mạng:** Thuật toán gói dữ liệu sử dụng cờ báo `SemaphoreSlim` để khóa luồng ghi byte, tránh trường hợp Response từ hai yêu cầu bất đồng bộ (Async) va chạm và ghi chồng byte lên nhau trên cùng Network Stream.
*   **Thread-Safe Sessions:** Việc thu thập và phát tán Request / Log liên quan tới client được lưu trữ tập trung qua lớp `ConnectionManager` bằng bộ từ điển an toàn luồng `ConcurrentDictionary`.
