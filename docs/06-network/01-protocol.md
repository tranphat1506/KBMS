# Giao thức Mạng (Network Protocol)

KBMS sử dụng một giao thức truyền tin nhị phân (Binary Protocol) tùy chỉnh chạy trên nền TCP để đảm bảo tốc độ truyền tải tri thức nhanh nhất và khả năng xử lý đồng thời (concurrency) tốt.

## 1. Cấu trúc Gói tin (Packet Structure)

Mỗi thông điệp (Message) gửi đi giữa Client và Server được đóng gói theo định dạng sau:

| Trường | Kích thước | Mô tả |
| :--- | :--- | :--- |
| **Total Length** | 4 bytes (Int32) | Tổng độ dài của các trường phía sau (Big-Endian). |
| **Message Type** | 1 byte | Loại thông điệp (Query, Result, Error, ...). |
| **SessLen** | 2 bytes (UInt16) | Độ dài của chuỗi Session ID. |
| **Session ID** | N bytes (UTF-8) | Định danh phiên làm việc của người dùng. |
| **ReqLen** | 2 bytes (UInt16) | Độ dài của chuỗi Request ID. |
| **Request ID** | M bytes (UTF-8) | Định danh duy nhất cho mỗi yêu cầu truy vấn. |
| **Payload** | P bytes (UTF-8) | Nội dung thực tế (ví dụ: câu lệnh KBQL hoặc kết quả JSON). |

---

## 2. Các loại Thông điệp (Message Types)

Hệ thống định nghĩa các `MessageType` để phân loại hình thức giao tiếp:

*   **Query (0x01):** Client gửi câu lệnh KBQL tới Server.
*   **Result (0x02):** Server trả về bảng dữ liệu kết quả cho Client.
*   **Error (0x03):** Thông báo lỗi (kèm theo dòng, cột và nội dung lỗi).
*   **Status (0x04):** Các thông tin về hiệu năng (Execution time, Facts derived).
*   **Stream (0x05):** Dữ liệu được trả về theo từng phần (Chunk) cho các truy vấn lớn.

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
