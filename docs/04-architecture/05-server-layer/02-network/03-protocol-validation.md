# 4.5.2.3. Xác thực Giao thức (Protocol Validation)

Để đảm bảo tính toàn vẹn của dữ liệu trong quá trình truyền dẫn nhị phân, Module I trải qua các quy trình xác thực nghiêm ngặt về cả cấu trúc gói tin lẫn khả năng chịu tải đồng thời.

## 1. Kiểm thử Cấu trúc Gói tin (Binary Frame Integrity)

Quá trình xác thực sử dụng bộ kịch bản mô phỏng tại `Protocol.cs` để kiểm tra khả năng đóng gói (Marshalling) và giải mã (Unmarshalling) dữ liệu:

*   **Xác thực Handshake**: Mô phỏng kịch bản Client gửi gói tin `LOGIN` và nhận diện chính xác phản hồi `RESULT:SUCCESS`.
*   **Xác thực Luồng dữ liệu (Streaming)**: Kiểm tra khả năng nhận diện dấu hiệu kết thúc luồng (`FETCH_DONE`) trong các truy vấn có tập kết quả lớn.

### Minh chứng Thực thi:
![Kịch bản kiểm thử đóng gói](../../../assets/diagrams/code_test_binary_protocol.png)
*Hình 4.xx: Kết quả chạy Unit Test cho khung truyền dẫn nhị phân.*

![Nhật ký gói tin](../../../assets/diagrams/terminal_test_protocol_hex.png)
*Hình 4.xx: Nhật ký byte nhị phân được bóc tách trực tiếp từ Socket.*

## 2. Kiểm thử Khả năng Xử lý Đồng thời (Concurrency Proof)

Để chứng minh tính hiệu quả của mô hình xử lý bất đồng bộ (TAP), hệ thống được thử nghiệm với kịch bản 256 kết nối giả lập thực thi truy vấn cùng một thời điểm:

*   **Độ trễ (Latency)**: Kiểm tra thời gian phản hồi trung bình khi hàng đợi ThreadPool bị chiếm dụng cao.
*   **Tính cô lập (Isolation)**: Đảm bảo dữ liệu của các phiên (Session) khác nhau không bị trộn lẫn trong quá trình ghi xuống Socket chung.

### Minh chứng Hiệu năng:
![Bằng chứng Server xử lý đồng thời](../../../assets/diagrams/terminal_test_concurrent_clients.png)
*Hình 4.xx: Bằng chứng hệ thống xử lý ổn định 256 kết nối đồng thời không xảy ra hiện tượng nghẽn luồng (Blocking).*

Việc xác thực thành công Tầng Mạng ghi dấu sự hoàn thiện của Phân hệ I. Tại thời điểm này, các byte dữ liệu thô đã sẵn sàng để được chuyển giao cho Phân hệ II — nơi trí tuệ ngôn ngữ của KBMS bắt đầu bóc tách các ký pháp để xây dựng nên cấu trúc tri thức AST phức tạp.
