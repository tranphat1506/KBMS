# Đặc tả Kiến trúc Tầng Mạng

Tầng Mạng của hệ quản trị [KBMS](../../../00-glossary/01-glossary.md#kbms) được thiết kế như một lớp trừu tượng trung gian, đảm nhiệm vai trò thiết lập kênh truyền dẫn tin cậy giữa các trạm làm việc và nhân xử lý tri thức (Core). Mục tiêu cốt lõi của kiến trúc này là tối ưu hóa băng thông cho các tập hợp tri thức quy mô lớn và đảm bảo tính đáp ứng thời gian thực cho các truy vấn suy diễn.

## 1. Yêu cầu và Mục tiêu Thiết kế Hệ thống

Để phục vụ các hệ thống tri thức kết nối vạn vật (IoT) phức tạp, kiến trúc mạng KBMS được xác lập dựa trên các tiêu chí kỹ thuật nghiêm ngặt:

-   **Tối ưu hóa độ trễ (Low Latency)**: Yêu cầu xử lý gói tin với các thành phần quản trị (Overhead) tối thiểu nhằm đạt tốc độ giải mã khung tin dưới ngưỡng 1ms.
-   **Hiệu suất khai thác băng thông**: Giảm thiểu kích thước tiêu đề (Header) so với dữ liệu tải (Payload) để tối ưu hóa việc truyền tải hàng triệu bộ bản ghi tri thức.
-   **Tính tương hợp đa nền tảng**: Giao thức được đặc tả theo định dạng nhị phân chuẩn hóa, cho phép các ngôn ngữ lập trình khác nhau dễ dàng triển khai bộ giải mã mà không phụ thuộc vào thư viện bên thứ ba.
-   **Khả năng chịu tải đồng thời**: Cấu trúc thiết kế hỗ trợ xử lý số lượng lớn kết nối đồng thời mà không gây hiện tượng nghẽn luồng xử lý tại trung tâm.

## 2. Sơ đồ Kiến trúc và Luồng Dữ liệu

Kiến trúc mạng được triển khai theo mô hình **Sự kiện bất đồng bộ (Asynchronous Event-driven)**, thực hiện việc tách biệt hoàn toàn giữa tiến trình thu nhận dữ liệu nhị phân thô và tiến trình xử lý logic tri thức.

![Kiến trúc Chi tiết Tầng Mạng KBMS](../../assets/diagrams/network_architecture_v3.png)
*Hình 4.14: Sơ đồ Kiến trúc Tầng Mạng mô tả luồng điều phối từ TcpListener đến ConnectionManager.*

Các thành phần cốt lõi trong sơ đồ bao gồm:

1.  **TcpListener**: Cổng tiếp nhận các kết nối mạng thô tại tầng chuyển vận (Transport Layer).
2.  **ConnectionManager**: Khối điều phối trung tâm đảm nhiệm vai trò kiểm soát kết nối, giới hạn định mức người dùng và khởi tạo ngữ cảnh phiên làm việc.
3.  **Bộ xử lý Giao thức (Protocol Processor)**: Chuyển hóa dòng dữ liệu nhị phân thành các đối tượng thông điệp có cấu trúc dựa trên logic đặc tả.
4.  **Quản lý Phiên (Session Management)**: Lưu giữ trạng thái cục bộ của mỗi kết nối, đảm bảo tính cách ly dữ liệu giữa các thực thể người dùng khác nhau.

## 3. Lý đạo Lựa chọn Giao thức Nhị phân

Hệ thống ưu tiên sử dụng **Giao thức Nhị phân tùy chỉnh (Custom Binary Protocol)** thay vì các giao thức dựa trên văn bản (như HTTP/REST) vì các lý do học thuật và kỹ thuật sau:

-   **Hiệu năng xử lý**: Tiến trình giải mã nhị phân trực tiếp trên bộ nhớ vượt trội về tốc độ so với việc phân tích (Parsing) các chuỗi văn bản phức tạp.
-   **Khả năng kiểm soát luồng**: Cho phép tự định nghĩa cơ chế ghép kênh (Multiplexing) để truyền tải song song nhiều yêu cầu trên cùng một kết nối vật lý thông qua mã định danh yêu cầu (Request ID).
-   **Tối ưu hóa kích thước thông điệp**: Tiêu đề của gói tin KBMS được tinh gọn tối đa (khoảng 10-20 byte), giúp giảm thiểu đáng kể tải trọng mạng so với các tiêu đề HTTP truyền thống.

Kiến trúc này đảm bảo hệ thống không chỉ vận hành như một bộ máy suy luận mạnh mẽ mà còn là một hạ tầng truyền dẫn tri thức tối ưu và tin cậy.
