# 09.3. Xác thực Giao thức (Protocol Validation)

Tầng mạng đảm bảo mọi thông điệp nhị phân được truyền dẫn không sai lệch và có độ trễ thấp nhất.

## 1. Kiểm thử Gói tin Nhị phân (Binary Packet Testing)

Sử dụng bộ kịch bản **`Protocol.cs`** để đóng gói và giải mã mockup:

*   **Handshake**: Mô phỏng quá trình Client gửi gói tin LOGIN và chờ phản hồi RESULT.
*   **Streaming**: Kiểm tra khả năng nhận diện dấu hiệu kết thúc luồng dữ liệu (`FETCH_DONE`).

### Minh chứng Mã nguồn (Handshake):
![code_test_binary_protocol.png](../assets/diagrams/code_test_binary_protocol.png)
*Hình 9.1: Kịch bản kiểm thử đóng gói và phân rã gói tin nhị phân.*

### Minh chứng Luồng dữ liệu (Gói tin):
![terminal_test_protocol_hex.png](../assets/diagrams/terminal_test_protocol_hex.png)
*Hình 9.2: Nhật ký gói tin nhị phân (Little-endian) truyền trên Socket.*

## 2. Kiểm thử Tải Kết nối (Concurrency Proof)

Mô phỏng 256 người dùng cùng thực thi truy vấn để kiểm tra giới hạn của `Asynchronous Sockets`.

### Minh chứng Kết quả (Concurrency):
![terminal_test_concurrent_clients.png](../assets/diagrams/terminal_test_concurrent_clients.png)
*Hình 9.3: Bằng chứng Server xử lý đồng thời nhiều Client mà không nghẽn.*

---

> [!IMPORTANT]
> Toàn bộ giao tiếp giữa Studio/CLI và Server đều dựa trên mô hình Socket bất đồng bộ (Async/Await) của .NET 8, đã được tối ưu hóa qua các bài test tải của `CliServerIntegrationTests`.
