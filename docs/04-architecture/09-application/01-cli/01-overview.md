# Đặc tả Giao diện Dòng lệnh (KBMS CLI)

Trong hệ sinh thái ứng dụng của **KBMS**, phân hệ **KBMS-CLI** đóng vai trò là công cụ quản trị và khai thác tri thức trực tiếp dành cho kỹ sư phần mềm và quản trị viên hệ thống. Thay vì thông qua giao diện đồ họa phức hợp, CLI thiết lập kết nối trực tiếp với máy chủ thông qua giao thức nhị phân, cung cấp khả năng kiểm soát hệ thống với độ trễ tối thiểu.

## 1. Các Tính năng Điều khiển Hạt nhân

Giao diện dòng lệnh được thiết kế với các cơ chế tương tác nhằm tối ưu hóa hiệu quả làm việc của người dùng trong môi trường console:

-   **Chu trình REPL**: Hệ thống thực hiện tiếp nhận câu lệnh tri thức, truyền tải tới máy chủ, tiếp nhận phản hồi và kết xuất kết quả tức thời ra màn hình điều khiển.
-   **Cơ chế Hiệu chỉnh Dòng lệnh**: Tích hợp các phím chức năng điều hướng và thao tác nhanh thông qua lớp `LineEditor.cs`:
    -   **Duyệt Lịch sử**: Sử dụng phím mũi tên Lên/Xuống để truy xuất các câu lệnh đã thực thi trước đó.
    -   **Điều hướng vị trí**: Các phím Home/End để di chuyển nhanh con trỏ tới đầu hoặc cuối dòng lệnh.
    -   **Quản lý Bộ đệm**: Phím Escape để xóa bộ đệm nhập liệu hiện hành.
-   **Hỗ trợ Nhập liệu Đa dòng**: CLI cho phép nhập các khối lệnh tri thức dài và phức tạp. Chế độ thụt đầu dòng tự động với ký hiệu `->` giúp phân biệt rõ giữa dòng khởi tạo và dòng tiếp nối của câu lệnh.
-   **Các hình thức Hiển thị Dữ liệu**: Thông qua `ResponseParser.cs`, CLI cung cấp hai chế độ hiển thị:
    -   **Chế độ Bảng**: Kết xuất dữ liệu dưới dạng bảng chuẩn hóa.
    -   **Chế độ Dọc**: Hiển thị dữ liệu theo cặp thuộc tính - giá trị trên từng hàng dọc, tự động kích hoạt cho các lệnh mô tả cấu trúc để tối ưu hóa khả năng đọc các thực thể tri thức phức hợp.

## 2. Các Nhóm Lệnh Hệ thống

Bên cạnh ngôn ngữ truy vấn tri thức, CLI cung cấp tập hợp các lệnh điều phối hệ thống:

*Bảng 4.10: Danh mục các lệnh điều khiển trong giao diện CLI*
| Lệnh điều khiển | Đặc tả Chức năng |
| :--- | :--- |
| **`LOGIN <user> <pass>`** | Thực hiện đăng nhập bảo mật. |
| **`SOURCE <path>`** | Thực thi tệp tin kịch bản tri thức từ hệ thống tệp tin cục bộ. |
| **`CONNECT`** | Thiết lập lại kết nối vật lý tới máy chủ KBMS. |
| **`CLEAR`** | Xóa sạch màn hình điều khiển. |

## 3. Cơ chế Vận hành và Quản trị

Để đảm bảo hiệu quả vận hành, CLI được tích hợp các cơ chế tự động hóa:

-   **Chế độ Thực thi Kịch bản**: Xử lý các tệp tin chứa nhiều lệnh tri thức một cách tự động, chỉ báo lỗi chính xác khi phát sinh sự cố tại bất kỳ dòng lệnh nào.
-   **Tái thiết lập Kết nối**: CLI duy trì cơ chế giám sát trạng thái kết nối và tự động thử lại tiến trình kết nối khi phát hiện sự gián đoạn mạng.
-   **Phân tích Phản hồi**: Hệ thống bóc tách các gói tin lỗi từ máy chủ để chỉ ra vị trí dòng và cột phát sinh sai lệch cú pháp trong mã nguồn tri thức.
