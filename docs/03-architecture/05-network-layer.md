# 3.5. Kiến trúc Mạng và Giao thức Giao tiếp (Network Layer)

Để hệ quản trị tri thức thực sự đóng vai trò là một máy chủ (Server) độc lập, có khả năng phục vụ đồng thời nhiều ứng dụng khách(Clients) thông qua môi trường Internet hoặc mạng nội bộ, một hệ thống có khả năng truyền tải dữ liệu ổn định là yếu tố tiên quyết. Thay vì sử dụng các giao thức dựa trên HTTP (như REST API hay GraphQL) với phần tiêu đề (Header) văn bản cồng kềnh, hệ thống đã thiết kế riêng một **Giao thức Nhị phân (Binary Protocol)** chạy trực tiếp trên nền tảng TCP Socket. Kiến trúc này giúp giảm đáng kể độ trễ khi truyền tải các luồng tri thức, đảm bảo tính ổn định và hiệu quả.

## 3.5.1. Đặc tả Cấu trúc Gói tin Nhị phân (Packet Architecture)

Mọi yêu cầu từ Client hay phản hồi từ Server đều được chuẩn hóa thành một đối tượng `Message`. Theo mã nguồn `KBMS.Network/Protocol.cs`, một gói tin nhị phân truyền qua TCP Socket không sử dụng ký tự phân cách (như `\n` trong các giao thức văn bản), mà tuân thủ nghiêm ngặt định dạng chiều dài cố định ở phần đầu (Fixed-Length Header).

Cấu trúc một Frame mạng hoàn chỉnh được phân mảnh thành 5 khối liền kề nhau:

![Sơ đồ cấu trúc Frame gói tin TCP nhị phân của KBMS. | width=1.1](../assets/diagrams/new_network_packet.png)
*Hình 3.8: Cấu trúc đóng gói byte (Byte Layout) của Giao thức KBMS.*

**Bảng 3.7: Chi tiết kỹ thuật của từng khối Byte trong Gói tin TCP**

| Khối dữ liệu | Độ dài | Kiểu Endian | Diễn giải chức năng kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Total Length** | 4 bytes | Big-Endian | Tổng số byte của toàn bộ các phần phía sau cộng lại. Việc đặt kích thước lên đầu giúp Socket tránh tình trạng đọc lố (over-read) và xử lý hiện tượng phân mảnh TCP (TCP fragmentation). |
| **Message Type** | 1 byte | N/A | Xác định loại hành vi của gói tin (định nghĩa theo kiểu Enum). |
| **Session ID** | 2 + X bytes | Big-Endian | 2 byte đầu chứa độ dài chuỗi (X). X byte sau chứa mã phiên (Session ID) định danh người dùng. Nếu rỗng, độ dài bằng 0. |
| **Request ID** | 2 + Y bytes | Big-Endian | 2 byte đầu chứa độ dài chuỗi (Y). Y byte sau chứa mã truy vấn độc nhất, giúp Client ghép cặp (map) câu trả lời bất đồng bộ tương ứng. |
| **Payload** | Tùy biến | UTF-8 | Chứa nội dung chính của thông điệp (câu lệnh KBQL hoặc kết quả truy vấn). |

## 3.5.2. Hệ thống Định tuyến Thông điệp (Message Types)

Sức mạnh của giao thức nằm ở Byte thứ 5 (Message Type). Dựa trên phân tích mã nguồn `MessageType.cs`, hệ thống vận hành 14 loại thông điệp, được chia thành 3 phân hệ phục vụ các quy trình vòng đời khác nhau.

**Bảng 3.8: Tập hợp 14 thông điệp giao tiếp hệ thống**

| Phân hệ | Giá trị Byte | Từ khóa (Enum) | Chức năng cốt lõi |
| :--- | :--- | :--- | :--- |
| **Bảo mật & Phiên** | 1, 5, 12 | `LOGIN`, `LOGOUT`, `SESSIONS` | Xác thực người dùng và phân bổ không gian bộ nhớ (Session) độc lập trên Server. |
| **Luồng Dữ liệu** | 2, 3, 7, 8 | `QUERY`, `RESULT`, `ROW`, `FETCH_DONE` | Truyền lệnh KBQL (`QUERY`) và tiếp nhận kết quả dạng dòng chảy (`ROW`) liên tục thay vì nạp một lần. |
| **Phân tích (IDE)** | 4, 10, 11, 14, 15 | `ERROR`, `STATS`, `LOGS_STREAM`, `LSP_...` | Truyền luồng lỗi, chi phí suy diễn và hỗ trợ gợi ý cú pháp cho hệ thống Editor. |
| **Bảo trì** | 6, 13 | `METADATA`, `MANAGEMENT_CMD` | Trao đổi siêu dữ liệu (Schema) và lệnh quản trị hệ thống mức thấp. |

## 3.5.3. Mô hình Bất đồng bộ và Quản lý Đa phiên (Concurrency & Sessions)

Khi triển khai thực tế, một hệ quản trị phải đối mặt với bài toán Bất đồng bộ (Concurrency) — nhiều ứng dụng truy cập đồng thời. Lớp Mạng của KBMS giải quyết vấn đề này thông qua cơ chế quản lý **Session ID**.

Mỗi Client khi `LOGIN` thành công sẽ được cấp một Session ID. Bất kỳ lệnh `QUERY` nào đi kèm ID này sẽ được Server định tuyến vào một "Không gian làm việc" (Working Memory) cô lập tạm thời. Điều này đảm bảo dữ kiện (Fact) nạp bởi Ứng dụng A không gây kích hoạt nhầm luật trong phiên truy vấn của Ứng dụng B. Để bảo vệ an toàn luồng (Thread-safety) khi đọc/ghi trực tiếp vào TCP Socket, mã nguồn `Protocol.cs` áp dụng cơ chế khóa bất đồng bộ `SemaphoreSlim`.

## 3.5.4. Kịch bản Giao tiếp qua Bài toán Hình học (Data Streaming)

Điểm nổi bật nhất của kiến trúc Lớp Mạng nằm ở cơ chế **Data Streaming**. Thay vì máy chủ gộp hàng triệu kết quả tam giác vào nhiều gói tin để gửi đến Client (điều này sẽ gây tràn bộ nhớ RAM của cả Client lẫn Server), hệ thống chia nhỏ kết quả thành từng thông điệp `ROW`.

Quay lại bài toán truy vết định lý Pythagoras, luồng giao tiếp TCP diễn ra như sau:

![Sơ đồ giao tiếp truyền tải dữ liệu bất đồng bộ (Data Streaming). | width=1.1](../assets/diagrams/new_network_sequence.png)
*Hình 3.9: Luồng giao tiếp Data Streaming tránh tràn bộ nhớ.*

Khi Client gửi gói tin `QUERY` chứa lệnh `FIND TamGiac WITH HAS_FIRED...`, Lớp Suy diễn (Reasoning Layer) sẽ tìm ra kết quả. Mỗi khi tìm thấy một tam giác vuông, Server lập tức đóng gói thành một thông điệp `ROW` và đẩy qua Socket. Sau khi gửi hết 1 triệu kết quả, Server mới chốt lại bằng thông điệp `FETCH_DONE`. 

Cơ chế này minh chứng cho sự phối hợp cực kỳ chặt chẽ từ tầng thấp (Network Layer TCP), đi qua tầng phân tích ngữ pháp (KBQL Layer), len lỏi vào tầng suy diễn (Reasoning Layer), và truy xuất dữ liệu từ ổ cứng (Storage Layer) — biến dự án thành một **Hệ Quản trị Cơ sở Tri thức (KBMS)** thực thụ, đạt chuẩn công nghiệp.
