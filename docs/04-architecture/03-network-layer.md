# 04.3. Tầng Mạng (Network Layer)

Tầng Mạng đóng vai trò là "mạch máu" truyền dẫn thông tin giữa các thành phần Application Layer và Server Layer.

## 1. Giao thức Nhị phân (Custom Binary Protocol)

Để tối ưu hóa hiệu năng và giảm thiểu băng thông, KBMS không sử dụng JSON/XML để giao tiếp mà sử dụng một giao thức nhị phân tùy chỉnh trên nền TCP.

*   **Cấu trúc Gói tin**: Gồm 1 byte `MessageType` và 4 byte `PayloadLength`, sau đó là `Payload` dữ liệu thực tế.
*   **12+ Message Types**: 
    *   `LOGIN`: Xác thực người dùng.
    *   `QUERY`: Gửi yêu cầu KBQL.
    *   `LOGS_STREAM`: Yêu cầu streaming log thời gian thực.
    *   `MANAGEMENT_CMD`: Lệnh quản trị hệ thống (Dành cho ROOT).
*   **Encoding**: Sử dụng chuẩn UTF-8 cho chuỗi văn bản và định dạng nhị phân Little-Endian cho các số nguyên/thực.

## 2. Quản lý Kết nối (Connection Manager)

*   **Asynchronous Socket I/O**: Server sử dụng mô hình lập trình bất đối xứng để phục vụ hàng trăm kết nối đồng thời với độ trễ tối thiểu.
*   **Session Isolation**: Mỗi kết nối được gán một `SessionId` duy nhất, giúp tách biệt dữ liệu tạm thời và ngữ cảnh làm việc (Context) giữa các người dùng.
*   **Heartbeat & Timeout**: Đảm bảo giải phóng tài nguyên hệ thống (RAM/Socket) ngay khi kết nối bị ngắt đột ngột sau 30 giây.

---

> [!IMPORTANT]
> **Hiệu năng truyền tin**: So với giao thức REST/JSON truyền thống, Giao thức Nhị phân của KBMS giúp giảm tới 60% kích thước gói tin và 40% thời gian xử lý tuần tự hóa (Serialization).
