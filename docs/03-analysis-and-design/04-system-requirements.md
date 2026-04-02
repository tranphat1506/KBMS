# 03.4. Đặc tả Yêu cầu Hệ thống KBMS

Phần này trình bày tóm tắt các yêu cầu cốt lõi của hệ thống KBMS, tập trung vào các khả năng chức năng chính và đề xuất khung công nghệ phù hợp để hiện thực hóa kiến trúc 4 tầng đã đề xuất.

---

## 1. Yêu cầu Chức năng Tổng quát

Hệ thống được thiết kế để đáp ứng các nhóm chức năng chính sau đây, tương ứng với mô hình phân tầng:

*   **Quản trị và Tương tác (Application Layer)**: Cung cấp môi trường soạn thảo tri thức chuyên sâu (IDE), hỗ trợ trực quan hóa đồ thị và giao diện dòng lệnh (CLI) để thực thi các kịch bản KBQL phức tạp.
*   **Giao thức và Truyền tải (Network Layer)**: Thiết lập cơ chế giao tiếp nhị phân tối ưu, cho phép truyền tải dữ liệu theo thời gian thực (Streaming) giữa máy khách và máy chủ.
*   **Xử lý và Suy luận (Server Engine Layer)**: Đóng vai trò hạt nhân điều phối, chịu trách nhiệm phân tích cú pháp, tối ưu hóa truy vấn và thực thi các thuật toán suy luận logic (như F-Closure) trên cơ sở tri thức.
*   **Lưu trữ bền vững (Storage Layer)**: Đảm bảo dữ liệu tri thức được tổ chức khoa học dưới dạng phân trang vật lý, hỗ trợ chỉ mục (Indexing) và cơ chế phục hồi sau sự cố (WAL).

---

## 2. Đề xuất Khung Công nghệ (Proposed Tech Stack)

Để đạt được hiệu năng và độ ổn định cao nhất, hệ thống KBMS được đề xuất triển khai dựa trên các nền tảng công nghệ hiện đại sau:

*   **Ngôn ngữ Hệ thống (Server Core)**: Đề xuất sử dụng **C# (.NET Core/Standard)** hoặc **C++** để tận dụng khả năng quản lý bộ nhớ hiệu quả, hỗ trợ đa luồng và các thư viện xử lý socket mạnh mẽ.
*   **Giao diện Người dùng (Application)**: 
    *   **KBMS Studio**: Khuyến nghị sử dụng **React/TypeScript** kết hợp với **Monaco Editor** để xây dựng môi trường lập trình tri thức giàu tính năng.
    *   **CLI**: Sử dụng các thư viện quản lý dòng lệnh tiêu chuẩn để hỗ trợ lịch sử lệnh và định dạng kết quả động.
*   **Giao thức Mạng**: Hiện thực hóa giao thức nhị phân tùy chỉnh trên nền **TCP Socket** để giảm thiểu độ trễ và băng thông truyền tải.
*   **Quản lý Dữ liệu**: Xây dựng bộ máy lưu trữ tự quản (In-house Storage Engine) thay vì sử dụng các hệ quản trị bên thứ ba, nhằm tối ưu hóa riêng cho cấu trúc dữ liệu tri thức COKB.

---

## 3. Yêu cầu Phi chức năng

Bên cạnh các chức năng nghiệp vụ, hệ thống cần hướng tới các mục tiêu chất lượng sau:

*   **Tính toàn vẹn (ACID Compliance)**: Đảm bảo mọi thao tác trên tri thức đều tuân thủ các tính chất Nguyên tố, Nhất quán, Cô lập và Bền vững.
*   **Hiệu năng cao**: Tối ưu hóa tốc độ suy luận và truy xuất dữ liệu với độ trễ thấp, hỗ trợ xử lý đồng thời hàng trăm kết nối.
*   **Bảo mật**: Triển khai cơ chế phân quyền (RBAC) và mã hóa dữ liệu tĩnh (Encryption at Rest) để bảo vệ tài sản tri thức.
*   **Khả năng mở rộng**: Kiến trúc được thiết kế dạng module hóa, cho phép dễ dàng tích hợp thêm các bộ máy suy luận hoặc giao thức lưu trữ mới trong tương lai.
