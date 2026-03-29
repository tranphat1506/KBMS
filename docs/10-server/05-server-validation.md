# 10.5. Xác thực Máy chủ (Server Validation)

Máy chủ KBMS là trung tâm điều phối mọi yêu cầu, do đó nó được kiểm soát chặt chẽ từ khi khởi động đến khi xử lý các kịch bản thực tế.

## 1. Kiểm thử Khởi động (Startup Proof)

Hệ thống sử dụng bộ nạp **`SystemKbBootstrapper.cs`** để khởi tạo các danh mục hệ thống (Catalog).

*   **Bootstrapping**: Tạo tệp `system.kdb` và đăng ký các Concept hệ thống (`users`, `audit_logs`).
*   **Log khởi động**:

![terminal_test_server_bootlogs.png](../assets/diagrams/terminal_test_server_bootlogs.png)
*Hình 10.3: Nhật ký khởi động máy chủ thành công với đầy đủ các Manager.*

## 2. Kiểm thử Tích hợp Đa luồng (Multi-threading Proof)

Sử dụng **`CliServerIntegrationTests.cs`** để mô phỏng 100+ tình huống thực tế qua Socket nhị phân.

| Tình huống | Mô tả | Kết quả |
| :--- | :--- | :--- |
| **Login-Query** | Đăng nhập và thực hiện SELECT ngay lập tức. | **Success** |
| **Drop-While-Use** | Xóa KB khi đang có Session truy cập. | **Error Caught (Locked)** |
| **Large-Metadata** | Truy vấn Concept có hàng trăm biến. | **Success** |

### Minh chứng Mã nguồn (Integration):
![code_test_server_integration.png](../assets/diagrams/code_test_server_integration.png)
*Hình 10.4: Kịch bản kiểm thử tích hợp (End-to-End) thông qua TCP Sockets.*

### Minh chứng Kết quả (111 Tests):
![result_test_cli.png](../assets/diagrams/result_test_cli.png)
*Hình 10.5: Kết quả thực thi 111 kịch bản kiểm thử tích hợp toàn diện.*

---

## 3. Xác thực Bảo mật (Security & RBAC)

Xác thực tính năng phân quyền người dùng thông qua **`AuthV3Tests.cs`**. 

*   **Root Access**: Toàn quyền thực hiện DDL/DML.
*   **Non-root Access**: Bị từ chối khi thực hiện các lệnh nhạy cảm mà không có quyền.

### Minh chứng Mã nguồn (Security):
![code_test_security.png](../assets/diagrams/code_test_security.png)
*Hình 10.6: Minh chứng mã nguồn kiểm thử bảo mật RBAC.*

### Minh chứng Kết quả (Access Denied):
![result_test_security.png](../assets/diagrams/result_test_security.png)
*Hình 10.7: Kết quả từ chối truy cập (Access Denied) đối với người dùng không có quyền.*

---

> [!TIP]
> **Giám sát thời gian thực**: Bạn có thể theo dõi hiệu năng của Server qua các chỉ số CPU/RAM được đo đạc bởi `ManagementManager.cs` trong phần Dashboard của Studio.
