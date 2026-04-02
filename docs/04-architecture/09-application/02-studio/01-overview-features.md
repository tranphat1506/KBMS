# Đặc tả Giao diện Quản trị và Phát triển (KBMS Studio)

**KBMS Studio** là một môi trường phát triển tích hợp (**IDE**) hiện đại, được xây dựng trên nền tảng kỹ thuật **Electron** và **React**. Studio cung cấp trải nghiệm trực quan hóa tri thức tiên tiến, cho phép người dùng tương tác với hệ thống thông qua các giao diện đồ họa thay vì chỉ giới hạn ở các thao tác dòng lệnh truyền thống.

## 1. Các Phân hệ Chức năng Cốt lõi

Hệ thống Studio được cấu thành từ các phân hệ chuyên biệt nhằm hỗ trợ toàn diện chu trình sống của tri thức:

### 1.1. Thiết kế Tri thức Trực quan (Knowledge Designer)
-   Cung cấp giao diện tương tác cho phép người dùng kiến tạo và xem xét sơ đồ phả hệ giữa các Khái niệm (**Concept**) và Quan hệ (**Relation**).
-   Tích hợp cơ chế tự động phát hiện các xung đột logic tiềm ẩn trong giai đoạn thiết kế mô hình tri thức (Schema Design).

### 1.2. Cửa sổ Truy vấn Tri thức (Query Console)
-   **Trình soạn thảo Monaco**: Tích hợp lõi trình soạn thảo của VS Code, hỗ trợ tính năng tô màu cú pháp (**Syntax Highlighting**) chuyên biệt cho ngôn ngữ **KBQL**.
-   **Chẩn đoán Lỗi Trực tiếp (IntelliSense & Error detection)**: Tự động đánh dấu các sai lệch cú pháp tại vị trí chính xác (Dòng/Cột) dựa trên phản hồi từ bộ phân tích của máy chủ.
-   **Gợi ý Thông minh**: Tự động đề xuất các tên Khái niệm, Thuộc tính và Luật dẫn đã được định nghĩa trong cơ sở tri thức hiện hành.

### 1.3. Bảng Điều phối Quản trị (Management Dashboard)
-   **Giám sát Tài nguyên Hệ thống**: Theo dõi biến động về dung lượng bộ nhớ tạm thời (**RAM/Buffer Pool**) và không gian đĩa vật lý theo thời gian thực.
-   **Quản trị Phiên làm việc và Người dùng**:
    -   **User Management**: Hỗ trợ các thao tác khởi tạo, điều chỉnh và phân quyền người dùng trực tiếp thông qua giao diện đồ họa.
    -   **Active Sessions**: Kiểm soát danh sách các thực thể khách đang kết nối, cung cấp khả năng ngắt các phiên làm việc không hợp lệ để bảo vệ hạ tầng.
-   **Phân tích Nhật ký (Log Analyzer)**: Bộ công cụ bóc tách nhật ký vận hành nâng cao, hỗ trợ lọc dữ liệu theo cấp độ nghiêm trọng và thực thể tri thức liên quan.

### 1.4. Hệ thống Thông báo và Giám sát Kiểm toán
-   **Cảnh báo Tức thời (Push Notifications)**: Tiếp nhận và hiển thị các cảnh báo an ninh hoặc sự cố hệ thống ngay khi máy chủ phát sinh sự kiện.
-   **Truy vết Kiểm toán (Audit Trails)**: Cung cấp nhật ký truy cập chi tiết, cho phép quản trị viên giám sát mọi hành vi tương tác với mạng lưới tri thức.

## 2. Tiêu chuẩn Thiết kế và Trải nghiệm Người dùng

Studio tuân thủ các tiêu chuẩn thiết kế hiện đại nhằm tối ưu hóa hiệu suất làm việc của chuyên gia tri thức:

-   **Ngôn ngữ Thiết kế Đồng nhất**: Sử dụng giao diện tối màu (Modern Dark Mode) kết hợp với hệ thống biểu tượng tối giản, giúp giảm thiểu sự xao nhãng khi làm việc với các cấu trúc logic phức tạp.
-   **Giao diện Kết xuất Luồng (Streaming UI)**: Kết quả truy vấn từ máy chủ được đổ trực tiếp vào lưới dữ liệu (**Data Grid**) ngay khi tiếp nhận các gói tin nhị phân đầu tiên, đảm bảo tính phản hồi liên tục của ứng dụng.
-   **Bố cục Linh hoạt (Responsive Layout)**: Tự động điều chỉnh không gian hiển thị khi kích hoạt các bảng điều phối dữ liệu hoặc sơ đồ đồ thị tri thức.
