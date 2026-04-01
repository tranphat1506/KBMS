# 04.3. Tầng Mạng

Tầng Mạng đóng vai trò là "mạch máu" truyền dẫn thông tin giữa các thành phần Application Layer và Server Layer.

## 1. Giao thức Nhị phân (Custom Binary Protocol)

Để tối ưu hóa hiệu năng và giảm thiểu băng thông, [KBMS](../00-glossary/01-glossary.md#kbms) không sử dụng [JSON](../00-glossary/01-glossary.md#json)/XML để giao tiếp mà sử dụng một giao thức nhị phân tùy chỉnh trên nền TCP.

*   **Cấu trúc Gói tin**: Gồm 1 byte `MessageType` và 4 byte `PayloadLength`, sau đó là `Payload` dữ liệu thực tế.
*   **12+ Message Types**: 
    *   `LOGIN`: Xác thực người dùng.
    *   `QUERY`: Gửi yêu cầu [KBQL](../00-glossary/01-glossary.md#kbql).
    *   `LOGS_STREAM`: Yêu cầu [streaming](../00-glossary/01-glossary.md#streaming) log thời gian thực.
    *   `MANAGEMENT_CMD`: Lệnh quản trị hệ thống (Dành cho ROOT).
*   **Encoding**: Sử dụng chuẩn UTF-8 cho chuỗi văn bản và định dạng nhị phân [Little-Endian](../00-glossary/01-glossary.md#little-endian) cho các số nguyên/thực.

## 2. Quản lý Kết nối

*   **Asynchronous Socket I/O**: Server sử dụng mô hình lập trình bất đối xứng để phục vụ hàng trăm kết nối đồng thời với độ trễ tối thiểu.
*   **[Session Isolation](../00-glossary/01-glossary.md#session-isolation)**: Mỗi kết nối được gán một `SessionId` duy nhất, giúp tách biệt dữ liệu tạm thời và ngữ cảnh làm việc (Context) giữa các người dùng.
*   **[Heartbeat](../00-glossary/01-glossary.md#heartbeat) & Timeout**: Đảm bảo giải phóng tài nguyên hệ thống (RAM/Socket) ngay khi kết nối bị ngắt đột ngột sau 30 giây.

**Hiệu năng truyền tin**: So với giao thức [REST](../00-glossary/01-glossary.md#rest)/[JSON](../00-glossary/01-glossary.md#json) truyền thống, Giao thức Nhị phân của [KBMS](../00-glossary/01-glossary.md#kbms) giúp giảm tới 60% kích thước gói tin và 40% thời gian xử lý tuần tự hóa ([Serialization](../00-glossary/01-glossary.md#serialization)).
