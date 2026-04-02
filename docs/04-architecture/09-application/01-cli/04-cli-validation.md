# Kiểm định và Xác thực Giao diện Dòng lệnh

Giao diện dòng lệnh (**KBMS-CLI**) là công cụ quản trị tri thức mạnh mẽ, hỗ trợ các thao tác tương tác trực tiếp và thực thi các kịch bản tri thức quy mô lớn thông qua giao thức nhị phân.

## 1. Kiểm định Chu kỳ Phiên làm việc (Session Validation)

Mọi thao tác tương tác trong giao diện CLI đều được ghi nhận và xác thực thông qua hệ thống nhật ký của máy chủ:

-   **Bắt tay Nhị phân (Binary Handshake)**: CLI thực hiện gửi gói tin định danh hạ tầng và tiếp nhận mã hiệu phiên làm việc duy nhất (**SESSION_ID**) để xác lập kết nối.
-   **Thực thi Kịch bản Nguồn (Batch Execution)**: Thực hiện lệnh `SOURCE` để xử lý hàng loạt các câu lệnh truy vấn tri thức, đảm bảo tính toàn vẹn và hiệu suất truyền tải.

### Minh chứng Thực nghiệm

![Nhật ký Tương tác CLI](../../../assets/diagrams/terminal_test_cli_query.png)
*Hình 4.32: Giao diện tương tác CLI thực thi câu lệnh truy vấn và xác lập phiên làm việc.*

## 2. Kiểm định Định dạng Kết xuất Dữ liệu (Format Validation)

Hệ thống thực hiện xác thực khả năng căn chỉnh bảng dữ liệu động và kết xuất kết quả truy vấn dưới hình thức trực quan:

-   **Ánh xạ Siêu dữ liệu**: Giao diện CLI tự động bóc tách thông tin tiêu đề để thiết lập độ rộng cột linh hoạt.
-   **Kết xuất Tập kết quả (ResultSet)**: Thực thi lệnh `SELECT` để xác nhận khả năng hiển thị tập hợp các sự kiện tri thức dưới dạng bảng chuẩn hóa.

*Bảng 4.11: Kịch bản kiểm lý định dạng kết xuất dữ liệu trong giao diện CLI*
| Kịch bản Truy vấn | Đặc tả Kết quả | Hình thức Hiển thị |
| :--- | :--- | :--- |
| **Truy vấn Chọn lọc (SELECT)** | Bao gồm nhiều bản ghi tri thức. | Hình thái bảng với màu sắc phân biệt phần tiêu đề. |
| **Mô tả Khái niệm (DESCRIBE)** | Bao gồm định nghĩa cấu trúc nội tại. | Chế độ hiển thị dọc mô tả biến và ràng buộc logic. |

![Kết quả Mô tả Cấu trúc CLI](../../../assets/diagrams/placeholder_cli_describe_output.png)
*Hình 4.33: Kết quả thực thi lệnh DESCRIBE hiển thị định nghĩa cấu trúc tri thức trong CLI.*

Việc kiểm định thành công giao diện CLI khẳng định tính sẵn sàng của công cụ quản trị trong việc điều phối và giám sát hệ thống tri thức **KBMS** một cách chuyên sâu thông qua ngôn ngữ dòng lệnh.
