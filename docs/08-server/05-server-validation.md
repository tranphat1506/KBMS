# 05. Xác thực Hoạt động (Server Validation)

Máy chủ KBMS là trung tâm điều phối mọi yêu cầu, do đó nó được kiểm soát chặt chẽ từ khi khởi động đến khi xử lý các kịch bản thực tế.

## 1. Kiểm thử Khởi động (Startup Proof)

Hệ thống sử dụng bộ nạp **`SystemKbBootstrapper.cs`** để khởi tạo các danh mục hệ thống (Catalog).

*   **Bootstrapping**: Tạo tệp `system.kdb` và đăng ký các Concept hệ thống (`users`, `audit_logs`).
*   **Log khởi động**:

![Placeholder: Ảnh chụp màn hình Console của Server khi vừa bật, hiển thị các dòng [INFO] Bootstrapping System KB..., [INFO] Loading User Catalog... và [SUCCESS] Server bound to localhost:3307](../assets/diagrams/placeholder_server_startup_log.png)

## 2. Kiểm thử Tích hợp Đa luồng (Multi-threading Proof)

Sử dụng **`CliServerIntegrationTests.cs`** để mô phỏng 100+ tình huống thực tế qua Socket nhị phân.

| Tình huống | Mô tả | Kết quả |
| :--- | :--- | :--- |
| **Login-Query** | Đăng nhập và thực hiện SELECT ngay lập tức. | **Success** |
| **Drop-While-Use** | Xóa KB khi đang có Session truy cập. | **Error Caught (Locked)** |
| **Large-Metadata** | Truy vấn Concept có hàng trăm biến. | **Success** |

![Placeholder: Ảnh chụp kết quả chạy XUnit cho tệp 'CliServerIntegrationTests.cs' với danh sách dài 111 test case tích hợp thành công](../assets/diagrams/placeholder_server_integration_test_results.png)

---

> [!TIP]
> **Giám sát thời gian thực**: Bạn có thể theo dõi hiệu năng của Server qua các chỉ số CPU/RAM được đo đạc bởi `ManagementManager.cs` trong phần Dashboard của Studio.
