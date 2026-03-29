# 03.2. Tác nhân & Vai trò Hệ thống (Actors & Roles)

Hệ thống KBMS được thiết kế để phục vụ các nhu cầu khác nhau từ học tập, nghiên cứu đến triển khai hệ thống công nghiệp.

## 1. Phân loại Người dùng

### a. Học viên / Nghiên cứu sinh (Researchers)
*   **Nhu cầu**: Thiết kế các mô hình tri thức lý thuyết (Hình học, Y học, Hóa học).
*   **Công cụ**: Sử dụng **KBMS Studio** làm môi trường chính để vẽ đồ thị tri thức và định nghĩa tập luật.
*   **Hành động**: CREATE CONCEPT, INSERT FACT, SOLVE bài toán.

### b. Quản trị viên (System Administrators)
*   **Nhu cầu**: Giám sát sức khỏe hệ thống và duy trì tính toàn vẹn của dữ liệu lớn.
*   **Công cụ**: Sử dụng **System Dashboard** trong Studio và **CLI Management commands**.
*   **Hành động**: REINDEX, CHECKPOINT, Quản lý Roles (GRANT/REVOKE), Giám sát RAM/Disk.

### c. Nhà phát triển (Developers)
*   **Nhu cầu**: Tích hợp KBMS làm lõi thông minh cho các ứng dụng Expert System khác.
*   **Công cụ**: Sử dụng **KBMS CLI** và tương tác trực tiếp qua **Binary Protocol**.
*   **Hành động**: Bulk Insert, Tự động hóa qua Script (.kbql), Xử lý luồng kết quả Streaming.

## 2. Mô hình Phân quyền (RBAC)

Hệ thống sử dụng cơ chế **Role-Based Access Control** để bảo mật tri thức:
*   **Admin**: Toàn quyền trên mọi Cơ sở tri thức (KB) và quản lý người dùng.
*   **Researcher**: Quyền đọc/ghi/suy diễn trên các KB được cấp phép.
*   **Guest**: Chỉ có quyền đọc (Read-only) trên các KB công khai.

---

> [!IMPORTANT]
> Mọi hành động thao tác dữ liệu của người dùng đều được ghi nhận lại trong **Audit Logs** để phục vụ công tác kiểm soát an ninh.
