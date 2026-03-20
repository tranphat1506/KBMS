# Chi Tiết Giao Thức Mạng KBMS (KBP - KBMS Binary Protocol)

KBMS sử dụng một giao thức truyền tin nhị phân tùy chỉnh (KBP) chạy trên nền TCP/IP (mặc định cổng 3307). Giao thức này được thiết kế dựa trên các nguyên lý của MySQL X Protocol và gRPC để đảm bảo tính hiệu quả, hỗ trợ truyền tin dòng (streaming) và khả năng mở rộng.

## 1. Cấu trúc Gói tin (Packet Structure)

Mỗi thông điệp được gửi qua mạng có cấu trúc header cố định để giúp Client/Server định giới hạn (delimitation) dữ liệu một cách chính xác.

| Thành phần | Kích thước | Mô tả |
| :--- | :--- | :--- |
| **Total Length** | 4 Bytes | Độ dài toàn bộ gói tin (Little Endian). |
| **Message Type** | 1 Byte | Loại thông điệp (LOGIN, QUERY, ROW, v.v.). |
| **Payload** | N Bytes | Dữ liệu thực tế (thường là chuỗi JSON mã hóa UTF-8). |

> [!NOTE]
> Header 5-byte này giúp tránh hiện tượng "dính gói" (TCP stream fragmentation), cho phép bên nhận đọc chính xác số byte cần thiết trước khi xử lý logic.

---

## 2. Các Loại Thông điệp (Message Types)

Dựa trên `MessageType.cs`, KBMS định nghĩa các mã loại sau:

| Enum Name | Value (HEX) | Chi tiết |
| :--- | :--- | :--- |
| **LOGIN** | `0x01` | Client gửi thông tin đăng nhập (`username password`). |
| **QUERY** | `0x02` | Client gửi câu lệnh KBQL (VD: `SELECT`, `INSERT`). |
| **RESULT** | `0x03` | Server trả về kết quả cuối cùng hoặc phản hồi thành công. |
| **ERROR** | `0x04` | Server thông báo lỗi (Parser, Runtime, hoặc Auth). |
| **LOGOUT** | `0x05` | Client yêu cầu ngắt kết nối an toàn. |
| **METADATA** | `0x06` | (Streaming) Chứa thông tin cột, schema của kết quả. |
| **ROW** | `0x07` | (Streaming) Chứa dữ liệu của một bản ghi (Object). |
| **FETCH_DONE**| `0x08` | (Streaming) Đánh dấu kết thúc dòng dữ liệu. |

---

## 3. Quy trình Truyền tin Dòng (Row-based Streaming)

Đây là cải tiến quan trọng trong phiên bản Level 2, cho phép Client hiển thị dữ liệu ngay lập tức mà không cần đợi Server xử lý xong toàn bộ hàng triệu bản ghi.

### Sơ đồ Sequence:

```mermaid
sequenceDiagram
    participant Client
    participant Server
    
    Client->>Server: QUERY "SELECT * FROM Person;"
    Note over Server: Thực thi truy vấn...
    Server->>Client: METADATA { "Columns": ["name", "age"] }
    Server->>Client: ROW { "name": "An", "age": 20 }
    Server->>Client: ROW { "name": "Binh", "age": 22 }
    Note over Server: ... n rows ...
    Server->>Client: FETCH_DONE { "executionTime": 0.05 }
    Server->>Client: RESULT { "success": true, "count": 2 }
```

1.  **Giai đoạn Khởi tạo**: Server nhận lệnh `SELECT`.
2.  **Giai đoạn Metadata**: Server gửi Header của bảng để Client chuẩn bị giao diện (tạo cột).
3.  **Giai đoạn Streaming**: Từng bản ghi được đóng gói vào Message `ROW` và đẩy xuống Host.
4.  **Giai đoạn Kết thúc**: Server gửi `FETCH_DONE` để Client biết đã nhận đủ, sau đó gửi `RESULT` để đồng bộ trạng thái cuối cùng.

---

## 4. Đặc tả Payload (Payload Specification)

Hiện tại, Payload mặc định sử dụng định dạng **JSON** để tối ưu hóa sự linh hoạt trong quá trình phát triển (v1.x). 

### Ví dụ Payload ROW:
```json
{
  "name": "Gemini",
  "role": "Assistant",
  "score": 9.5
}
```

### Hướng phát triển (Roadmap):
Trong tương lai, trường `Payload Type` có thể được thêm vào Header để hỗ trợ **Google Protocol Buffers (Protobuf)**, giúp giảm 60-80% dung lượng băng thông so với JSON.

---

## 5. Xử lý Lỗi (Error Handling)

Khi có lỗi xảy ra, Server sẽ gửi thông điệp loại `ERROR` với Payload có cấu trúc:

```json
{
  "error": "Mô tả chi tiết lỗi",
  "code": "PARSER_ERROR | RUNTIME_ERROR | AUTH_ERROR",
  "query": "Nội dung câu lệnh gây lỗi (nếu có)"
}
```

Client khi nhận được `ERROR` sẽ dừng việc streaming và hiển thị thông báo đỏ trên CLI/UI.
