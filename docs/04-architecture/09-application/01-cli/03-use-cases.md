# Kịch bản Tương tác và Sử dụng CLI

Giao diện dòng lệnh (**KBMS CLI**) là công cụ chuyên dụng dành cho quản trị viên hệ thống và chuyên gia tri thức, cung cấp khả năng điều khiển và tự động hóa các tác vụ quản trị tri thức với hiệu năng cao.

## 1. Kịch bản Phân tích Cấu trúc Tri thức (Concept Analysis)

-   **Mục tiêu**: Thực hiện trích xuất và phân tích định nghĩa của một Khái niệm (**Concept**) phức hợp với số lượng thuộc tính lớn.
-   **Quy trình thực thi**:
    1.  **Gửi lệnh**: Người dùng thực thi lệnh `DESCRIBE <ConceptName>;`.
    2.  **Nhận diện Phản hồi**: CLI tiếp nhận gói tin Siêu dữ liệu với thuộc tính định danh bắt đầu bằng giá trị mô tả.
    3.  **Chuyển đổi Hiển thị**: Lớp `ResponseParser` tự động chuyển sang chế độ hiển thị theo chiều dọc (Vertical Mode) để liệt kê danh sách biến số nhằm tránh hiện tượng tràn khung hình hiển thị (Overflow).

![Chế độ Hiển thị Dọc CLI | width=1.05](../../../assets/diagrams/uc_cli_vertical_mode.png)
*Hình 4.30: Chế độ hiển thị dọc trong giao diện CLI hỗ trợ phân tích cấu trúc.*

## 2. Kịch bản Tự động hóa Nạp Tri thức (Batch Knowledge Import)

-   **Mục tiêu**: Nạp hàng ngàn thực thể tri thức từ tệp kịch bản tri thức có sẵn vào hệ thống.
-   **Quy trình thực thi**:
    1.  **Chuẩn bị**: Khởi tạo tệp tin `.kbql` chứa tập hợp các lệnh nạp dữ kiện.
    2.  **Thực thi lệnh nguồn**: Thực hiện lệnh `SOURCE <filename>.kbql;` tại dấu nhắc của CLI.
    3.  **Chu trình xử lý**: CLI tự động bóc tách từng khối lệnh (phân tách bởi dấu `;`) và chuyển tải tới máy chủ.
    4.  **Kiểm soát Lỗi**: Nếu phát hiện sai lệch, CLI sẽ ngắt tiến trình và chỉ báo chính xác tọa độ dòng lỗi trong tệp nguồn để quản trị viên hiệu chỉnh.

![Thực thi Hàng loạt CLI](../../../assets/diagrams/uc_cli_batch_source.png)
*Hình 4.31: Kịch bản thực thi hàng loạt thông qua tập lệnh nguồn trong CLI.*

## 3. Kịch bản Đăng nhập và Kiểm tra An ninh (Authentication Trace)

-   **Mục tiêu**: Thực hiện đăng nhập bảo mật và xác lập trạng thái phiên làm việc tới máy chủ tri thức.
-   **Quy trình thực thi**:
    1.  Người dùng thực hiện lệnh `LOGIN <username> <password>`.
    2.  CLI đóng gói và truyền tải gói tin đăng nhập bảo mật tới máy chủ xác thực.
    3.  Sau khi nhận thông điệp xác thực thành công, dấu nhắc hệ thống chuyển sang trạng thái sẵn sàng truy vấn tri thức hình thức.

Sự linh hoạt trong các kịch bản tương tác khẳng định CLI là một công cụ đắc lực trong việc quản trị và vận hành hệ thống KBMS chuyên sâu.
