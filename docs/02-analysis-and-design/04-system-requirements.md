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

## 2. Công nghệ sử dụng

Để đạt được hiệu năng và độ ổn định cao nhất, hệ thống KBMS được triển khai dựa trên các nền tảng công nghệ hiện đại sau:

-   **Lõi hệ thống (Server Engine)**: Sử dụng nền tảng **.NET Core** [41] của Microsoft. Đây là khung làm việc đa nền tảng, hỗ trợ các tính năng hiện đại như Garbage Collection (GC) tối ưu, quản lý bộ nhớ an toàn và thư viện lập trình bất đồng bộ (Async/Await) mạnh mẽ, giúp hệ thống xử lý hàng ngàn kết nối đồng thời với độ trễ tối thiểu.
-   **Giao diện Quản trị (KBMS Studio)**: Được phát triển dựa trên thư viện **React** [42] kết hợp với ngôn ngữ **TypeScript**. Việc sử dụng mô hình Component-based giúp giao diện Studio có tính linh hoạt cao và dễ dàng mở rộng. Đặc biệt, hệ thống tích hợp **Monaco Editor** [43] (bộ lõi của VS Code) để cung cấp môi trường soạn thảo tri thức chuyên nghiệp với các tính năng như Highlight cú pháp và IntelliSense.
-   **Giao thức Truyền tải (Network Protocol)**: Hệ thống hiện thực hóa giao thức nhị phân tùy chỉnh trên nền **TCP Socket** dựa trên các nguyên lý mạng máy tính tiêu chuẩn [44]. Giao thức này được thiết kế để tối giản hóa kích thước gói tin, giảm thiểu chi phí Overhead so với các giao thức dạng văn bản như JSON hay XML, từ đó tối ưu hóa băng thông truyền tải tri thức.
-   **Lưu trữ và Truy xuất (Storage Engine)**: Xây dựng bộ máy lưu trữ tự quản dựa trên cấu trúc **Cây B+** (B+ Tree) [5, 10] và cơ chế ghi nhật ký trước (**WAL**) [5] để đảm bảo tính bền vững (Durability) và tuân thủ các tính chất ACID trong giao dịch tri thức.

---

## 3. Yêu cầu Phi chức năng

Bên cạnh các chức năng nghiệp vụ, hệ thống cần hướng tới các mục tiêu chất lượng sau:

*   **Tính toàn vẹn**: Đảm bảo mọi thao tác trên tri thức đều tuân thủ các tính chất Nguyên tố, Nhất quán, Cô lập và Bền vững.
*   **Hiệu năng cao**: Tối ưu hóa tốc độ suy luận và truy xuất dữ liệu với độ trễ thấp, hỗ trợ xử lý đồng thời hàng trăm kết nối.
*   **Bảo mật**: Triển khai cơ chế phân quyền (RBAC) và mã hóa dữ liệu tĩnh (Encryption at Rest) để bảo vệ tài sản tri thức.
*   **Khả năng mở rộng**: Kiến trúc được thiết kế dạng module hóa, cho phép dễ dàng tích hợp thêm các bộ máy suy luận hoặc giao thức lưu trữ mới trong tương lai.
