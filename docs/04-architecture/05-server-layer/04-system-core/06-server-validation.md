# 4.5.4.3. Xác thực Hạ tầng và Điều phối Máy chủ (Server Validation)

Hạ tầng máy chủ KBMS đóng vai trò trung tâm điều phối, do đó mọi quy trình từ khởi động hệ thống đến xử lý các kịch bản tương tác thực tế đều được kiểm chuẩn chặt chẽ nhằm đảm bảo tính sẵn sàng cao và độ tin cậy tuyệt đối.

## 1. Kiểm thử Quy trình Khởi động (Bootstrapping Validation)

Hệ thống sử dụng bộ nạp đặc quyền `SystemKbBootstrapper.cs` để thiết lập môi trường vận hành ban đầu. Quy trình xác thực tập trung vào:

*   **Tính toàn vẹn của Cơ sở tri thức hệ thống (System KB)**: Đảm bảo tệp `system.kdb` được khởi tạo và cấu trúc chính xác các Concept nền tảng (`users`, `audit_logs`).
*   **Kích hoạt các Trình quản lý (Managers Activation)**: Xác nhận trạng thái sẵn sàng của bộ ba điều phối: `ConnectionManager`, `ManagementManager` và `SystemLogger`.

### Minh chứng Thực thi:
![Nhật ký khởi động máy chủ](../../../assets/diagrams/terminal_test_server_bootlogs.png)
*Hình 4.xx: Nhật ký khởi động hệ thống ghi nhận trạng thái sẵn sàng của các phân hệ hạ tầng.*

---

## 2. Kiểm thử Tích hợp Hệ thống (End-to-End Integration)

Sử dụng bộ công cụ kiểm thử tích hợp `CliServerIntegrationTests.cs`, hệ thống thực hiện mô phỏng hơn 100 tình huống tương tác thực tế thông qua các kết nối Socket nhị phân:

*Bảng: Ma trận danh mục các kịch bản kiểm thử tích hợp Tầng Máy chủ*
| Kịch bản kiểm thử | Mô tả mục tiêu | Trạng thái |
| :--- | :--- | :--- |
| **Luồng Login-Query** | Xác thực quy trình đăng nhập và truy vấn dữ liệu liên tục. | **Thành công** |
| **Xử lý Xung đột Tài nguyên** | Kiểm tra cơ chế khóa (Locking) khi thực hiện xóa KB đang có phiên làm việc truy cập. | **Lỗi được bẫy (Locked)** |
| **Truy vấn Dữ liệu Quy mô lớn** | Kiểm tra độ ổn định khi truyền tải metadata của các Concept có hàng trăm thuộc tính. | **Thành công** |

### Minh chứng Thực nghiệm:
![Kịch bản kiểm thử tích hợp](../../../assets/diagrams/code_test_server_integration.png)
*Hình 4.xx: Mã nguồn các kịch bản kiểm thử tích hợp cuối-đến-cuối (End-to-End).*

![Kết quả thực thi kiểm thử tích hợp](../../../assets/diagrams/result_test_cli.png)
*Hình 4.xx: Kết quả thực thi vượt qua 111 kịch bản kiểm thử tích hợp hệ thống.*

---

## 3. Xác thực An ninh và Phân quyền (Security & RBAC Validation)

Quy trình xác thực bảo mật tập trung vào việc thực thi mô hình phân quyền dựa trên vai trò (RBAC) thông qua bộ kịch bản `AuthV3Tests.cs`:

*   **Quyền hạn Đặc quyền (Root Access)**: Xác nhận quyền thực thi không hạn chế đối với các lệnh định nghĩa cấu trúc (DDL) và quản trị hệ thống.
*   **Quyền hạn Người dùng (Non-root Access)**: Kiểm chứng khả năng từ chối truy cập (Access Denied) khi người dùng thông thường cố gắng can thiệp vào các vùng tri thức nhạy cảm hoặc thực hiện các lệnh quản trị hạ tầng.

### Minh chứng Bảo mật:
![Kiểm thử bảo mật RBAC](../../../assets/diagrams/code_test_security.png)
*Hình 4.xx: Kịch bản kiểm thử khả năng cô lập quyền hạn giữa các vai trò người dùng.*

![Kết quả từ chối truy cập](../../../assets/diagrams/result_test_security.png)
*Hình 4.xx: Minh chứng hệ thống từ chối thực thi yêu cầu của người dùng không đủ thẩm quyền.*

Việc xác thực thành công Phân hệ III xác nhận rằng hạ tầng KBMS đã sẵn sàng để tiếp nhận và điều phối các lệnh thực thi. Tại giai đoạn cuối cùng này, AST đã được xác thực sẽ được chuyển giao cho Phân hệ IV — nơi các chiến lược tối ưu hóa truy vấn và cơ chế định hướng tri thức được kích hoạt để tạo ra kết quả cuối cùng.
