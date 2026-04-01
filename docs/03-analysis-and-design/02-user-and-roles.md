# 03.2. Tác nhân & Vai trò Hệ thống

Hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) được thiết kế để phục vụ các nhu cầu khác nhau từ học tập, nghiên cứu đến triển khai hệ thống công nghiệp.

## 1. Phân loại Người dùng

### a. Học viên / Nghiên cứu sinh
*   **Nhu cầu**: Thiết kế các mô hình tri thức lý thuyết (Hình học, Y học, Hóa học).
*   **Công cụ**: Sử dụng **[KBMS](../00-glossary/01-glossary.md#kbms) Studio** làm môi trường chính để vẽ đồ thị tri thức và định nghĩa tập luật.
*   **Hành động**: CREATE [CONCEPT](../00-glossary/01-glossary.md#concept), INSERT [FACT](../00-glossary/01-glossary.md#fact), [SOLVE](../00-glossary/01-glossary.md#solve) bài toán.

### b. Quản trị viên
*   **Nhu cầu**: Giám sát sức khỏe hệ thống và duy trì tính toàn vẹn của dữ liệu lớn.
*   **Công cụ**: Sử dụng **System Dashboard** trong Studio và **[CLI](../00-glossary/01-glossary.md#cli) Management commands**.
*   **Hành động**: REINDEX, [CHECKPOINT](../00-glossary/01-glossary.md#checkpoint), Quản lý Roles (GRANT/REVOKE), Giám sát RAM/Disk.

### c. Nhà phát triển
*   **Nhu cầu**: Tích hợp [KBMS](../00-glossary/01-glossary.md#kbms) làm lõi thông minh cho các ứng dụng Expert System khác.
*   **Công cụ**: Sử dụng **[KBMS](../00-glossary/01-glossary.md#kbms) [CLI](../00-glossary/01-glossary.md#cli)** và tương tác trực tiếp qua **[Binary Protocol](../00-glossary/01-glossary.md#binary-protocol)**.
*   **Hành động**: [Bulk Insert](../00-glossary/01-glossary.md#bulk-insert), Tự động hóa qua Script (.[kbql](../00-glossary/01-glossary.md#kbql)), Xử lý luồng kết quả [Streaming](../00-glossary/01-glossary.md#streaming).

## 2. Mô hình Phân quyền

Hệ thống sử dụng cơ chế **Role-Based Access Control** để bảo mật tri thức:
*   **Admin**: Toàn quyền trên mọi Cơ sở tri thức (KB) và quản lý người dùng.
*   **Researcher**: Quyền đọc/ghi/suy diễn trên các KB được cấp phép.
*   **Guest**: Chỉ có quyền đọc (Read-only) trên các KB công khai.

- Mọi hành động thao tác dữ liệu của người dùng đều được ghi nhận lại trong **[Audit Logs](../00-glossary/01-glossary.md#audit-logs)** để phục vụ công tác kiểm soát và quản lý.