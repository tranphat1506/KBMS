# Tổng quan Kiến trúc và Mô hình Điều phối Hệ thống

Hệ quản trị cơ sở tri thức **KBMS** được xây dựng dựa trên kiến trúc phân lớp (Layered Architecture), cho phép tách biệt các tầng chức năng nhằm tối ưu hóa quá trình xử lý tri thức và quản trị dữ liệu. Kiến trúc này hỗ trợ việc chuyển đổi mô hình lý thuyết **COKB** [1] thành một hệ thống thực thi ổn định, đảm bảo tính mở rộng và khả năng bảo trì mã nguồn trong dài hạn.

Nội dung chương này tập trung phân tích cấu trúc tổng thể của hệ thống, luồng dữ liệu giữa các tầng và các giải pháp công nghệ cốt lõi được áp dụng trong quá trình triển khai.

## 1. Kiến trúc Phân lớp Chức năng

Hệ thống được chia thành bốn lớp chức năng chính, mỗi lớp đảm nhiệm một vai trò cụ thể trong chu trình xử lý tri thức từ mức ứng dụng đến mức lưu trữ vật lý:

![Kiến trúc Phân lớp KBMS](../assets/diagrams/kbms_4layer_architecture.png)
*Hình 4.1: Sơ đồ kiến trúc phân lớp chức năng của hệ thống KBMS.*

Đặc tả các lớp chức năng:

-   **Lớp Ứng dụng (Application Layer)**: Cung cấp giao diện tương tác cho người dùng. Phân hệ này bao gồm **KBMS Studio** (môi trường phát triển tích hợp dựa trên React và Electron) và **KBMS CLI** (giao diện dòng lệnh). Các ứng dụng này hỗ trợ biên tập tri thức, trực quan hóa mô hình và quản trị hệ thống.
-   **Lớp Mạng (Network Layer)**: Thực hiện truyền dẫn dữ liệu giữa Client và Server thông qua các gói tin nhị phân. Lớp này quản lý việc tuần tự hóa đối tượng (Serialization), thiết lập phiên làm việc (Session) và đảm bảo an toàn dữ liệu bằng các giao thức socket bất đồng bộ.
-   **Lớp Xử lý Server (Server Engine Layer)**: Là thành phần điều phối trung tâm của hệ thống. Tại đây, các câu lệnh ngôn ngữ **KBQL** được phân tích cú pháp để tạo thành Cây cú pháp trừu tượng (**AST**). Dựa trên AST, hệ thống điều hướng yêu cầu tới bộ máy suy diễn (**Inference Engine**) hoặc bộ phân tích truy vấn dữ liệu.
-   **Lớp Lưu trữ (Storage Layer)**: Đảm nhiệm việc lưu trữ và truy xuất dữ liệu từ các thiết bị lưu trữ thứ cấp. Sử dụng cấu trúc **Slotted Page** và chỉ mục **B+ Tree** [5, 10], lớp này đảm bảo các thuộc tính **ACID** cho giao dịch và sử dụng nhật ký ghi trước (**WAL**) [5] để phục hồi dữ liệu khi xảy ra sự cố.

## 2. Quy trình Điều phối và Luồng Xử lý Dữ liệu

Quy trình xử lý một yêu cầu trong KBMS bắt đầu từ việc tiếp nhận chuỗi ký tự từ lớp ứng dụng và chuyển hóa thành các tác vụ thực thi tại hạ tầng. Đối tượng trung tâm xuyên suốt quá trình này là Cây cú pháp trừu tượng (AST).

![Sơ đồ Tuần tự Hệ thống](../assets/diagrams/kbms_general_system_sequence.png)
*Hình 4.2: Sơ đồ tuần tự mô tả luồng xử lý và điều phối dữ liệu qua các lớp.*

Khi một lệnh được gửi đến, luồng xử lý diễn ra theo các bước:
1.  **Tiếp nhận**: Lớp mạng nhận gói tin và giải mã nội dung lệnh.
2.  **Phân tích**: Bộ phân tích (Parser) xây dựng AST từ câu lệnh.
3.  **Điều hướng**: Hệ thống kiểm tra loại lệnh trong AST. Nếu là lệnh suy diễn, thông tin sẽ được đưa vào mạng lưới **Rete** [9]. Nếu là lệnh quản trị dữ liệu, hệ thống sẽ thực hiện truy xuất trực tiếp các trang dữ liệu (Pages) thông qua Buffer Pool [5].
4.  **Phản hồi**: Kết quả thực thi được đóng gói và gửi ngược lại phía người dùng.

## 3. Các Phân hệ Phụ trợ và Quản trị Hệ thống

Bên cạnh các luồng xử lý tri thức chính, hệ thống triển khai các phân hệ phụ trợ để đảm bảo an ninh và chẩn đoán trạng thái vận hành.

![Quy trình Chẩn đoán và Bảo mật](../assets/diagrams/kbms_security_diagnostics_flow.png)
*Hình 4.3: Sơ đồ luồng chẩn đoán và kiểm soát an ninh hệ thống.*

Các phân hệ này bao gồm:
-   **Kiểm soát truy cập (RBAC)**: Xác thực người dùng và phân quyền dựa trên vai trò trước khi thực thi các lệnh đặc quyền.
-   **Ghi nhật ký (Logging)**: Lưu trữ nhật ký kiểm toán (Audit Log) để theo dõi các hành vi tác động đến cơ sở tri thức.
-   **Giám sát (Monitoring)**: Theo dõi các chỉ số tài nguyên như CPU, bộ nhớ RAM và trạng thái của các tệp tin lưu trữ.

## 4. Tổng hợp Công nghệ và Thuật toán Nền tảng

Bảng dưới đây tóm tắt các giải pháp công nghệ chính được ứng dụng trong quá trình cài đặt hệ thống:

*Bảng 4.1: Đặc tả công nghệ và thuật toán tại các phân lớp*
| Lớp kiến trúc | Phân hệ triển khai | Công nghệ và Thuật toán cốt lõi |
| :--- | :--- | :--- |
| **Ứng dụng** | `kbms-studio-ui`, `KBMS.CLI` | React, Electron, TypeScript, Monaco Editor |
| **Mạng** | `KBMS.Server.Network` | Asynchronous Sockets, AES-256, Binary Protocol |
| **Server** | `KBMS.Parser`, `KnowledgeManager`| Phân tích cú pháp LL(k), TAP (Multithreading) |
| **Suy luận** | `KBMS.Reasoning.InferenceEngine`| Suy diễn tiến (Forward Chaining) [6], Mạng Rete [9] |
| **Lưu trữ** | `KBMS.Storage.V3` | Slotted Page, Cây B+ (B+ Tree) [5, 10], WAL [5] |
