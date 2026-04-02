# Đặc tả Giao diện Quản trị (KBMS Studio)

**KBMS Studio** là môi trường phát triển tích hợp dành cho việc quản trị tri thức. Giao diện được xây dựng trên nền tảng công nghệ web hiện đại, cung cấp khả năng điều phối hệ thống thông qua các công cụ trực quan.

Dưới đây là các phân hệ chức năng và bố cục giao diện chính trong Studio:

## 1. Bố cục Giao diện Tổng thể

Cấu trúc Studio được phân bổ thành các khu vực chức năng chính, giúp người dùng quản lý dự án tri thức:

-   **Trình Duyệt Đối tượng**: Quản lý cấu trúc cây phả hệ của tất cả các Khái niệm, Quan hệ và Luật dẫn.
-   **Trình Biên tập**: Khu vực làm việc chính để soạn thảo mã nguồn tri thức.
-   **Bảng Giám sát**: Theo dõi trạng thái hoạt động của hệ thống như CPU, tài nguyên bộ nhớ và hoạt động vào/ra dữ liệu.

## 2. Trình Thiết kế Tri thức

Đây là khu vực định nghĩa mô hình tri thức thông qua ngôn ngữ KBQL:

-   **Trình soạn thảo mã nguồn**: Tích hợp tính năng tô màu cú pháp và gợi ý từ khóa dựa trên lược đồ tri thức hiện hành.
-   **Kiểm tra Lỗi**: Các sai sót về cú pháp được hệ thống phản hồi và chỉ thị trực tiếp trên giao diện soạn thảo.

## 3. Phân hệ Truy vấn và Phân tích

Dành cho việc thực thi các lệnh khai thác và theo dõi kết quả:

-   **Lưới Dữ liệu**: Kết quả truy vấn được trình bày dưới dạng bảng dữ liệu, hỗ trợ các thao tác xem và phân loại cơ bản.
-   **Bảng Truy vết Suy luận**: Hiển thị chi tiết các bước logic giải quyết vấn đề đối với các lệnh tìm kiếm lời giải.

## 4. Đặc điểm Giao diện và Tương tác

Studio được thiết kế nhằm tối ưu hóa hiệu suất làm việc của chuyên gia tri thức:

-   **Thiết kế Đồng nhất**: Sử dụng giao diện tối màu giúp tập trung vào các cấu trúc logic và giảm mỏi mắt khi làm việc kéo dài.
-   **Cập nhật Dữ liệu Luồng**: Kết quả truy vấn từ máy chủ được cập nhật trực tiếp vào lưới dữ liệu ngay khi tiếp nhận các gói tin, đảm bảo tính phản hồi tức thời của ứng dụng.
-   **Bố cục Linh hoạt**: Tự động điều chỉnh không gian hiển thị khi người dùng mở hoặc đóng các bảng điều phối dữ liệu.
