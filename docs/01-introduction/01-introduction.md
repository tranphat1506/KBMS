# 01.1. Giới thiệu đề tài và Đặt vấn đề

## 1. Lý do chọn đề tài

Trong kỷ nguyên chuyển đổi số, nhu cầu về việc quản trị và khai thác tri thức đã trở nên cấp thiết hơn bao giờ hết. Các hệ quản trị cơ sở dữ liệu truyền thống ([RDBMS](../00-glossary/01-glossary.md#rdbms)) mặc dù rất mạnh mẽ trong việc lưu trữ và truy xuất dữ liệu có cấu trúc, nhưng vẫn còn nhiều hạn chế trong việc tự động suy diễn ra các tri thức mới từ tập dữ liệu hiện có. 

Việc tích hợp trí tuệ nhân tạo, cụ thể là các hệ thống dựa trên tri thức (Knowledge-based systems), vào tầng lưu trữ dữ liệu giúp rút ngắn khoảng cách giữa "Dữ liệu thô" và "Tri thức hữu ích" [1]. Đề tài "Xây dựng Hệ quản trị Cơ sở tri thức ([KBMS](../00-glossary/01-glossary.md#kbms)) dựa trên mô hình Đối tượng tính toán" được lựa chọn nhằm giải quyết bài toán này.

## 2. Mục tiêu nghiên cứu

Mục tiêu chính của đề tài là xây dựng một hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) hoàn chỉnh, có khả năng:
1.  **Biểu diễn tri thức chuyên sâu**: Sử dụng mô hình [COKB](../00-glossary/01-glossary.md#cokb) [1], [2] để định nghĩa các thực thể có khả năng tính toán.
2.  **Suy diễn tự động hiệu năng cao**: Áp dụng thuật toán [F-Closure](../00-glossary/01-glossary.md#f-closure) để tìm tập đóng tri thức một cách tối ưu [2], [8].
3.  **Lưu trữ bền vững và an toàn**: Phát triển công cụ lưu trữ nhị phân hỗ trợ chỉ mục [B+ Tree](../00-glossary/01-glossary.md#b-tree) [6] và nhật ký Write-Ahead Logging (WAL) [5], [7].
4.  **Giao diện phát triển trực quan**: Cung cấp Studio [IDE](../00-glossary/01-glossary.md#ide) giúp người dùng thiết kế và kiểm chứng tri thức.

## 3. Đối tượng và Phạm vi nghiên cứu

*   **Đối tượng**: Các mô hình biểu diễn tri thức, thuật toán suy diễn tiến ([Forward Chaining](../00-glossary/01-glossary.md#forward-chaining)) và các kỹ thuật quản lý CSDL.
*   **Phạm vi**: 
    *   **Quản lý tri thức**: Thực hiện các thao tác thêm, sửa, xóa và tìm kiếm tri thức chuyên sâu qua ngôn ngữ [KBQL](../00-glossary/01-glossary.md#kbql).
    *   **Suy diễn tri thức**: Xây dựng bộ máy suy duyễn dựa trên tập luật và sự thật hiện có để sinh ra tri thức mới.
    *   **Lưu trữ**: Nghiên cứu phương pháp lưu trữ tri thức bền vững dưới cấu trúc mô hình đối tượng tính toán ([COKB](../00-glossary/01-glossary.md#cokb)).

## 4. Ý nghĩa khoa học và thực tiễn

Đề tài góp phần chuẩn hóa phương pháp xây dựng hệ thống tri thức có khả năng mở rộng ([Scale-out](../00-glossary/01-glossary.md#scale-out)) và tính linh hoạt cao trong việc thay đổi luật mà không cần can thiệp vào mã nguồn ứng dụng. Đây là nền tảng quan trọng cho việc phát triển các hệ chuyên gia và hệ trợ giúp quyết định thông minh.

## 5. Bố cục đề tài

Báo cáo được tổ chức thành 14 chương với nội dung cụ thể như sau:

*   **Chương 1 – Giới thiệu và Đặt vấn đề**: Trình bày lý do chọn đề tài, mục tiêu nghiên cứu, đối tượng, phạm vi và ý nghĩa của đề tài.
*   **Chương 2 – Cơ sở lý thuyết [COKB](../00-glossary/01-glossary.md#cokb)**: Trình bày tổng quan về mô hình Đối tượng Tính toán ([COKB](../00-glossary/01-glossary.md#cokb)), bao gồm cấu trúc, phân cấp khái niệm và nền tảng logic suy diễn.
*   **Chương 3 – Phân tích và Thiết kế hệ thống**: Khảo sát hiện trạng, xác định yêu cầu chức năng và phi chức năng, phác thảo kiến trúc tổng thể của hệ thống.
*   **Chương 4 – Kiến trúc hệ thống**: Mô tả chi tiết kiến trúc 4 tầng (Client – Network – Server – Storage) và giao tiếp giữa các thành phần.
*   **Chương 5 – Định nghĩa Mô hình và Khái niệm**: Đặc tả các mô hình dữ liệu cốt lõi sử dụng trong hệ thống [KBMS](../00-glossary/01-glossary.md#kbms).
*   **Chương 6 – Ngôn ngữ truy vấn [KBQL](../00-glossary/01-glossary.md#kbql)**: Trình bày cú pháp, ngữ nghĩa và các nhóm lệnh của ngôn ngữ truy vấn tri thức [KBQL](../00-glossary/01-glossary.md#kbql).
*   **Chương 7 – Tầng lưu trữ vật lý**: Mô tả cơ chế lưu trữ nhị phân, cấu trúc trang, chỉ mục [B+ Tree](../00-glossary/01-glossary.md#b-tree) và giao thức nhật ký WAL.
*   **Chương 8 – Bộ máy suy diễn**: Trình bày chi tiết thuật toán [F-Closure](../00-glossary/01-glossary.md#f-closure), giải phương trình và cơ chế suy diễn theo ngữ cảnh.
*   **Chương 9 – Tầng mạng và Giao thức**: Mô tả giao thức truyền tải nhị phân tùy chỉnh dùng để giao tiếp giữa Client và Server qua TCP.
*   **Chương 10 – Tầng máy chủ**: Trình bày cơ chế điều phối phiên làm việc, quản lý nhật ký và hạ tầng vận hành của máy chủ [KBMS](../00-glossary/01-glossary.md#kbms).
*   **Chương 11 – Bộ phân tích cú pháp**: Mô tả kiến trúc [Lexer](../00-glossary/01-glossary.md#lexer), [Parser](../00-glossary/01-glossary.md#parser) và cây cú pháp trừu tượng ([AST](../00-glossary/01-glossary.md#ast)) của ngôn ngữ [KBQL](../00-glossary/01-glossary.md#kbql).
*   **Chương 12 – Giao diện dòng lệnh ([CLI](../00-glossary/01-glossary.md#cli))**: Giới thiệu công cụ [CLI](../00-glossary/01-glossary.md#cli), luồng xử lý lệnh và các kịch bản sử dụng thực tiễn.
*   **Chương 13 – [KBMS](../00-glossary/01-glossary.md#kbms) Studio**: Trình bày ứng dụng Studio dạng [IDE](../00-glossary/01-glossary.md#ide) tích hợp để thiết kế, quản trị và kiểm chứng tri thức trực quan.
*   **Chương 14 – Cài đặt, Kiểm thử và Đánh giá**: Hướng dẫn triển khai hệ thống, trình bày kết quả kiểm thử tích hợp và đánh giá hiệu năng thực tế.
