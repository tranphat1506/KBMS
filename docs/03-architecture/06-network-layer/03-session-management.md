# Quản lý Phiên và Trạng thái Kết nối

Hệ thống KBMS quản lý các kết nối đa người dùng thông qua mô hình phiên làm việc định danh, giúp tách biệt bối cảnh thực thi tri thức giữa các đối tượng khách hàng khác nhau.

## 4.5.5. Cơ chế Cấp phát và Liên kết Phiên

Mỗi máy khách khi thiết lập kết nối an toàn với Server sẽ được gán một bối cảnh phiên duy nhất. Dữ liệu nốt này bao gồm:

-   **Mã định danh (GUID)**: Một chuỗi ký tự duy nhất được tạo ra ngẫu nhiên để định danh phiên.
-   **Trạng thái Kết nối**: Thông tin về Socket vật lý đang liên kết.
-   **Bối cảnh Tri thức**: Thông tin về cơ sở tri thức (Knowledge Base) đang được sử dụng trong phiên.

## 4.5.6. Ví dụ về Nhật ký Cấp phát Phiên (Session Trace)

Dưới đây là một kịch bản cấp phát phiên thực tế tại máy chủ:

*Bảng 4.6: Nhật ký cấp phát và quản trị phiên làm việc trên máy chủ*
| Thời gian | Sự kiện | Mã phiên (GUID) | Kết quả / Hành động |
| :--- | :--- | :--- | :--- |
| **17:45:01** | `LoginRequest` | - | `Yêu cầu Admin đăng nhập` |
| **17:45:02** | `AuthSuccess` | `8a2f-91b...` | `Khởi tạo Session Context` |
| **17:45:05** | `UseKB` | `8a2f-91b...` | `Liên kết với EnterpriseKB` |
| **18:00:10** | `Heartbeat` | `8a2f-91b...` | `Cập nhật thời gian sống (TTL)` |
| **18:15:20** | `Disconnect` | `8a2f-91b...` | `Giải phóng tài nguyên Phiên` |

Việc tách biệt phiên giúp hệ quản trị tri thức có thể xử lý các bài toán suy luận song song mà không gây xung đột về bối cảnh hay dữ liệu tạm giữa các người dùng.
