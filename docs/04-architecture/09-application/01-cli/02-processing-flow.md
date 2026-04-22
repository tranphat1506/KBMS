# Luồng Xử lý và Phân tích Phản hồi của CLI

Giao diện dòng lệnh (CLI) thực thi chu trình điều phối dữ liệu khép kín, từ giai đoạn thu thập dữ liệu đầu vào tới giai đoạn truyền tải nhị phân và kết xuất kết quả trực quan cho người dùng cuối.

## 1. Quy trình Nhận lệnh và Truyền tải

Khi người dùng thực thi một câu lệnh, CLI thực hiện quy trình theo các giai đoạn sau:

![Sơ đồ Luồng Xử lý CLI | width=1.05](../../../assets/diagrams/cli_processing_flow.png)
*Hình 4.29: Sơ đồ tuần tự mô tả luồng xử lý câu lệnh và phản hồi từ Server của CLI.*

1.  **Kiểm tra và Thu thập Dữ liệu Đầu vào**: Hệ thống thực hiện kiểm tra và thu thập các dòng nội dung của câu lệnh từ người dùng cho đến khi tiếp nhận ký hiệu kết thúc câu lệnh (dấu `;`).
2.  **Tạo Gói tin**: Đóng gói nội dung lệnh thành cấu trúc nhị phân `Message` theo định dạng `QUERY` hoặc `LOGIN` phù hợp với tầng mạng.
3.  **Truyền tải Nhị phân**: Gửi gói tin qua Socket (`KBMS.Network`) và duy trì trạng thái chờ đợi phản hồi từ máy chủ.

## 2. Phân tích và Kết xuất Phản hồi

Thành phần trọng yếu của CLI nằm ở lớp `ResponseParser.cs`. Do kết quả từ máy chủ có thể là một luồng dữ liệu liên tục (**Streaming Rows**), CLI phải thực hiện xử lý và phân tách từng gói tin nhị phân để hiển thị ra màn hình điều khiển:

-   **Siêu dữ liệu (METADATA)**: Xác lập định nghĩa các cột dữ liệu.
-   **Dữ liệu Bản ghi (ROW)**: Chứa dữ liệu thực tế cho từng thực thể tri thức trong tập kết quả.
-   **Kết quả Tổng quát (RESULT)**: Các thông báo xác nhận trạng thái thực thi thành công.
-   **Thông báo Lỗi (ERROR)**: Chứa thông tin chẩn đoán bao gồm nội dung lỗi và tọa độ phát sinh sai lệch (Dòng, Cột).

### Quy trình Hiển thị Bảng Dữ liệu Động

Lớp `ResponseParser` thực hiện vẽ biểu đồ bảng theo thuật toán tối ưu hóa không gian:

1.  **Dựng khung Tiêu đề (Header Rendering)**: Ngay khi tiếp nhận Siêu dữ liệu, CLI tính toán độ rộng cột lớn nhất dựa trên tên thuộc tính để thiết lập khung tiêu đề chuẩn hóa.
2.  **Hỗ trợ Ô dữ liệu đa dòng**: Nếu giá trị trong một ô chứa ký hiệu xuống dòng, hệ thống tự động phân tách và vẽ đường kẻ phân cách hàng để đảm bảo tính mỹ thuật và cân đối của bảng dữ liệu.
3.  **Chuyển đổi Chế độ Hiển thị**: Đối với các phản hồi thuộc nhóm `EXPLAIN` hoặc `DESCRIBE`, hệ thống tự động chuyển sang chế độ hiển thị theo cặp thuộc tính - giá trị trên từng hàng dọc để tối ưu hóa khả năng đọc.

## 3. Cơ chế Thực thi Hàng loạt và Quản lý Luồng

CLI hỗ trợ thực thi khối lượng lớn lệnh thông qua tệp tin kịch bản tri thức. Luồng xử lý được thực hiện tuần tự nhằm đảm bảo tính nhất quán của mạng lưới tri thức hệ thống.

Để duy trì trạng thái vận hành ổn định, CLI thực thi hai luồng xử lý đồng thời:
-   **Luồng Chính (Main Thread)**: Chịu trách nhiệm tương tác và tiếp nhận dữ liệu đầu vào từ người dùng.
-   **Luồng Giám sát (Heartbeat Thread)**: Duy trì tín hiệu định kỳ tới máy chủ để đảm bảo kết nối không bị ngắt quãng do các chính sách về thời gian chờ (Timeout).
