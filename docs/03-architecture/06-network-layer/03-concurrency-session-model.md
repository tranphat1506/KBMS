# Mô hình Xử lý Đồng thời và Quản lý Phiên

Để đảm bảo hiệu quả phục vụ hàng trăm kết nối đồng hành mà không làm suy giảm hiệu năng hệ thống, **KBMS** tích lập một mô hình quản lý phiên (Session Management) tập trung. Mô hình này dựa trên các cấu trúc dữ liệu an toàn bộ nhớ (Thread-safe) và cơ chế Vào/Ra bất đồng bộ (Asynchronous I/O).

## 1. Thành phần Điều hướng Kết nối và Ngữ cảnh Phiên

Độ ổn định của tầng mạng phụ thuộc trực tiếp vào lớp `ConnectionManager.cs`. Thiết kế của lớp này tập trung vào việc duy trì tính nhất quán tri thức giữa các thực thể người dùng độc lập.

### 1.1. Hệ thống Quản trị Danh mục Phiên

Hệ thống sử dụng cấu trúc `ConcurrentDictionary<string, Session>` để lưu trữ và truy xuất các vết tích kết nối:

-   **An toàn đa luồng (Thread-safety)**: Cấu trúc này đảm bảo các tác vụ khởi tạo, truy xuất và giải phóng phiên diễn ra đồng bộ mà không gây hiện tượng tranh chấp tài nguyên (Resource Contention).
-   **Định danh Phiên (Session ID)**: Mỗi kết nối được gán một định danh duy nhất sau tiến trình xác thực thành công, đảm bảo mọi yêu cầu tri thức tiếp theo đều được kiểm soát và định tuyến chính xác.

## 2. Cơ chế Đa luồng và Ghép kênh Truyền dẫn (Multiplexing)

Hệ thống KBMS hỗ trợ thực thi song song nhiều yêu cầu trên cùng một kết nối truyền dẫn mạng thông qua kỹ thuật **Ghép kênh**:

-   **Vai trò của Định danh Yêu cầu (Request ID)**: Mỗi yêu cầu từ máy trạm đều mang một định danh duy nhất. Khi máy chủ trả về các khối kết quả tri thức quy mô lớn, các gói tin này vẫn duy trì mã định danh gốc để máy trạm tự động tái cấu trúc luồng dữ liệu (Data Stream).
-   **Luồng xử lý Bất đồng bộ (Non-blocking I/O)**: Quá trình đọc khung tin nhị phân và truyền tải kết quả sử dụng mô hình Vào/Ra không nghẽn. Điều này đảm bảo hệ thống không rơi vào trạng thái chờ (Wait State) khi xử lý các dữ liệu từ bộ máy suy diễn hoặc tầng lưu trữ vật lý.

## 3. Khuyến nghị Kỹ thuật về Tương tác Mạng

Dựa trên thiết kế tiêu chuẩn, việc tích hợp các hệ thống máy trạm với KBMS cần tuân thủ các nguyên tắc sau để đạt tối ưu hiệu năng:

1.  **Xác nhận Kết thúc Truy vấn**: Cần kiểm soát thông điệp `MessageType.FETCH_DONE` để hoàn tất luồng dữ liệu, tránh việc Socket duy trì trạng thái chờ không cần thiết.
2.  **Định vị Lỗi Hình thức**: Khi tiếp nhận thông điệp `MessageType.ERROR`, cần phân tích các tham số về dòng và cột trong Payload để chỉ định chính xác vị trí lỗi trong câu lệnh KBQL.
3.  **Quản lý Hàng đợi Yêu cầu Pipeline**: Máy trạm có khả năng gửi chuỗi lệnh liên tiếp mà không nhất thiết phải chờ phản hồi của lệnh trước đó (Pipelining). Hệ thống máy chủ sẽ tự động xếp hàng và xử lý tuần tự.

## 4. Kiểm chứng Thực nghiệm và Hiệu năng Tải

Trong các kịch bản thử nghiệm áp lực (Stress Test) cao độ:

-   **Khả năng chịu tải**: Hệ thống duy trì mức sử dụng bộ vi xử lý (CPU) ổn định dưới ngưỡng **15%** khi xử lý đồng thời 256 kết nối hoạt động.
-   **Thời gian Phản hồi (Response Time)**: Các yêu cầu truy vấn tri thức thông thường được phản hồi trong khoảng **10ms**, chứng minh tính hiệu quả của mô hình quản trị phiên tập trung.

Sự kết hợp giữa cơ chế quản trị phiên nghiêm ngặt và mô hình đa nhiệm hiện đại giúp KBMS trở thành một hệ chủ tri thức tin cậy trong các môi trường vận hành quy mô lớn.
