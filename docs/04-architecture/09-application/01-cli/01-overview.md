# Đặc tả Giao diện Dòng lệnh (KBMS CLI)

Trong hệ sinh thái ứng dụng của **[KBMS](../../../00-glossary/01-glossary.md#kbms)**, phân hệ **KBMS-CLI** đóng vai trò là công cụ quản trị và khai thác tri thức trực tiếp dành cho kỹ sư phần mềm và quản trị viên hệ thống. Thay vì thông qua giao diện đồ họa phức hợp, CLI thiết lập kết nối trực tiếp với máy chủ thông qua giao thức nhị phân, cung cấp khả năng kiểm soát hệ thống với độ trễ tối thiểu.

## 1. Các Tính năng Điều khiển Hạt nhân

Giao diện dòng lệnh được thiết kế với các cơ chế tương tác hiện đại nhằm tối ưu hóa trải nghiệm người dùng trong môi trường console:

-   **Chu trình REPL (Read-Eval-Print Loop)**: Hệ thống thực hiện tiếp nhận câu lệnh tri thức, truyền tải tới máy chủ, tiếp nhận phản hồi và kết xuất kết quả tức thời ra màn hình điều khiển.
-   **Cơ chế Hiệu chỉnh Dòng lệnh Nâng cao**: Tích hợp các phím chức năng điều hướng và thao tác nhanh thông qua lớp `LineEditor.cs`:
    -   **Duyệt Lịch sử**: Sử dụng phím mũi tên Lên/Xuống để truy xuất các câu lệnh đã thực thi trước đó.
    -   **Điều hướng vị trí**: Các phím Home/End cho phép di chuyển nhanh con trỏ tới đầu hoặc cuối dòng lệnh.
    -   **Quản lý Bộ đệm**: Phím Escape (nhấn hai lần) thực hiện xóa sạch bộ đệm nhập liệu hiện hành.
-   **Hỗ trợ Nhập liệu Đa dòng (Multi-line Input)**: CLI cho phép nhập các khối lệnh tri thức dài và phức tạp. Chế độ thụt đầu dòng tự động với ký hiệu `->` giúp người dùng phân biệt rõ giữa dòng khởi tạo và dòng tiếp nối của câu lệnh.
-   **Đa dạng hình thức Hiển thị**: Thông qua `ResponseParser.cs`, CLI cung cấp hai chế độ hiển thị linh hoạt:
    -   **Chế độ Bảng (Table Mode)**: Kết xuất dữ liệu dưới dạng bảng chuẩn hóa (phong cách MySQL).
    -   **Chế độ Dọc (Vertical Mode - \G)**: Hiển thị dữ liệu theo cặp thuộc tính - giá trị trên từng hàng dọc, tự động kích hoạt cho các lệnh mô tả cấu trúc (`DESCRIBE`, `EXPLAIN`) để tối ưu hóa khả năng đọc các thực thể tri thức phức hợp.

## 2. Các Nhóm Lệnh Hệ thống Đặc biệt

Bên cạnh ngôn ngữ truy vấn tri thức, CLI cung cấp tập hợp các lệnh điều phối công cụ:

*Bảng 4.11: Danh mục các lệnh điều khiển đặc quyền trong giao diện CLI*
| Lệnh điều khiển | Đặc tả Chức năng |
| :--- | :--- |
| **`LOGIN <user> <pass>`** | Thực hiện đăng nhập bảo mật (mật khẩu được ẩn trong lịch sử lệnh). |
| **`SOURCE <path>`** | Thực thi tệp tin kịch bản tri thức (`.kbql`) từ hệ thống tệp tin cục bộ. |
| **`CONNECT`** | Thiết lập lại kết nối vật lý tới máy chủ KBMS theo cách thủ công. |
| **`CLEAR`** | Giải phóng và làm sạch bộ nhớ hiển thị của màn hình điều khiển. |

## 3. Tối ưu hóa Trải nghiệm và Quản trị Chuyên sâu

Để đảm bảo tính chuyên nghiệp của một hệ quản trị tri thức, CLI được tích hợp các cơ chế tự động hóa:

-   **Chế độ Thực thi Kịch bản**: Cho phép xử lý các tệp tin chứa hàng ngàn lệnh tri thức một cách tự động, tích hợp khả năng dừng thực thi và báo lỗi chính xác khi phát sinh sự cố tại bất kỳ dòng lệnh nào.
-   **Tự động Tái thiết lập Kết nối (Auto-reconnect)**: CLI duy trì cơ chế giám sát trạng thái kết nối (**Heartbeat**) và tự động thử lại tiến trình kết nối khi phát hiện sự gián đoạn mạng đột ngột.
-   **Phân tích Phản hồi line-accurate**: Hệ thống bóc tách các gói tin lỗi từ máy chủ để chỉ ra chính xác vị trí dòng và cột phát sinh sai lệch cú pháp trong mã nguồn tri thức.
