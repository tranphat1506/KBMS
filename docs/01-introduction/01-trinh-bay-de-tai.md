# Giới thiệu đề tài

## 1. Lý do chọn đề tài

Trong kỷ nguyên chuyển đổi số, nhu cầu về việc quản trị và khai thác tri thức đã trở nên cấp thiết hơn bao giờ hết. Các hệ quản trị cơ sở dữ liệu truyền thống (RDBMS) mặc dù rất mạnh mẽ trong việc lưu trữ và truy xuất dữ liệu có cấu trúc, nhưng vẫn còn nhiều hạn chế trong việc tự động suy diễn ra các tri thức mới từ tập dữ liệu hiện có. 

Việc tích hợp trí tuệ nhân tạo, cụ thể là các hệ thống dựa trên tri thức (Knowledge-based systems), vào tầng lưu trữ dữ liệu giúp rút ngắn khoảng cách giữa "Dữ liệu thô" và "Tri thức hữu ích" [1]. Đề tài "Xây dựng Hệ quản trị Cơ sở tri thức (KBMS) dựa trên mô hình COKB" được lựa chọn nhằm giải quyết bài toán này.

## 2. Mục tiêu nghiên cứu

Mục tiêu chính của đề tài là xây dựng một hệ thống KBMS hoàn chỉnh, có khả năng:
1.  **Biểu diễn tri thức chuyên sâu**: Sử dụng mô hình COKB [1], [3] để định nghĩa các thực thể có khả năng tính toán.
2.  **Suy diễn tự động hiệu năng cao**: Áp dụng thuật toán F-Closure để tìm tập đóng tri thức một cách tối ưu [2], [4].
3.  **Lưu trữ bền vững và an toàn**: Phát triển công cụ lưu trữ nhị phân hỗ trợ chỉ mục B+ Tree [5], [10] và nhật ký Write-Ahead Logging (WAL) [5].
4.  **Giao diện phát triển trực quan**: Cung cấp Studio IDE giúp người dùng thiết kế và kiểm chứng tri thức.

## 3. Đối tượng và Phạm vi nghiên cứu

*   **Đối tượng**: Các mô hình biểu diễn tri thức, thuật toán suy diễn tiến (Forward Chaining) và các kỹ thuật quản lý CSDL.
*   **Phạm vi**: 
    *   **Quản lý tri thức**: Thực hiện các thao tác thêm, sửa, xóa và tìm kiếm tri thức chuyên sâu qua ngôn ngữ KBQL.
    *   **Suy diễn tri thức**: Xây dựng bộ máy suy duyễn dựa trên tập luật và sự thật hiện có để sinh ra tri thức mới.
    *   **Lưu trữ**: Nghiên cứu phương pháp lưu trữ tri thức bền vững dưới cấu trúc mô hình đối tượng tính toán (COKB).

## 4. Ý nghĩa khoa học và thực tiễn

Đề tài góp phần chuẩn hóa phương pháp xây dựng hệ thống tri thức có khả năng mở rộng (Scale-out) và tính linh hoạt cao trong việc thay đổi luật mà không cần can thiệp vào mã nguồn ứng dụng. Đây là nền tảng quan trọng cho việc phát triển các hệ chuyên gia và hệ trợ giúp quyết định thông minh.

## 5. Bố cục đề tài

Báo cáo được tổ chức thành 05 chương trọng tâm với nội dung chi tiết như sau:

*   **Chương 1 – Giới thiệu và Cơ sở lý thuyết**: Trình bày lý do chọn đề tài, mục tiêu nghiên cứu, đồng thời phân tích tổng quan về mô hình Đối tượng Tính toán (COKB) và nền tảng logic suy diễn đại số.
*   **Chương 2 – Phân tích và Thiết kế hệ thống**: Khảo sát hiện trạng, phân tích các yêu cầu chức năng (Truy vấn, Suy diễn) và phi chức năng để phác thảo kiến trúc tổng thể.
*   **Chương 3 – Kiến trúc hệ thống và các tầng xử lý**: Chương trọng tâm mô tả chi tiết kiến trúc 4 tầng (Mạng, Máy chủ, Suy diễn, Lưu trữ) cùng các thành phần Lexer/Parser và công cụ CLI/Studio.
*   **Chương 4 – Cài đặt và Kiểm thử hệ thống**: Hướng dẫn triển khai môi trường, cài đặt các phân hệ và trình bày kết quả thực nghiệm, đánh giá hiệu năng vận hành thực tế.
*   **Chương 5 – Kết luận và Hướng phát triển**: Tổng kết các kết quả đạt được, chỉ ra những hạn chế hiện tại và đề xuất lộ trình nâng cấp hệ thống trong tương lai.
