# 04.4. Tầng Máy chủ

Tầng Máy chủ (Server Layer) là thành phần phức tạp nhất của [KBMS](../00-glossary/01-glossary.md#kbms), chịu trách nhiệm cho toàn bộ quá trình biên dịch ngôn ngữ và suy diễn tự động.

## 1. [Knowledge Manager]

[Knowledge Manager](../00-glossary/01-glossary.md#knowledge-manager) là bộ điều phối trung tâm của Server. Nó chịu trách nhiệm:
*   **KB Isolation**: Sử dụng `StoragePool` để đảm bảo mỗi người dùng có thể làm việc trên một Cơ sở tri thức độc lập mà không gây xung đột tài nguyên.
*   **[Thread-safe](../00-glossary/01-glossary.md#thread-safe) Operations**: Quản lý các lệnh nạp/hủy KB thông qua cơ chế khóa (Mutual Exclusion) để duy trì tính toàn vẹn của danh lục hệ thống.
*   **Execution Routing**: Điều phối luồng làm việc giữa [Parser](../00-glossary/01-glossary.md#parser) (Biên dịch) và [Inference Engine](../00-glossary/01-glossary.md#inference-engine) (Suy diễn).

## 2. KBQL Parser & Compiler

*   **Phân tích Cú pháp**: Sử dụng thuật toán đệ quy xuống ([Recursive Descent](../00-glossary/01-glossary.md#recursive-descent)) để biên dịch câu lệnh [KBQL](../00-glossary/01-glossary.md#kbql) thành cấu trúc [AST](../00-glossary/01-glossary.md#ast) (Abstract Syntax Tree).
*   **Hỗ trợ**: Biên dịch các tập lệnh [KDL](../00-glossary/01-glossary.md#kdl) (Định nghĩa), [KQL](../00-glossary/01-glossary.md#kql) (Truy vấn), [KCL](../00-glossary/01-glossary.md#kcl) (Bảo mật), [KML](../00-glossary/01-glossary.md#kml) (Bảo trì) và [TCL](../00-glossary/01-glossary.md#tcl) (Giao dịch).

## 3. Reasoning Engine

*   **[Forward Chaining](../00-glossary/01-glossary.md#forward-chaining)**: Thuật toán suy diễn tiến dựa trên điểm đóng **[F-Closure](../00-glossary/01-glossary.md#f-closure)**.
*   **Mô hình [COKB](../00-glossary/01-glossary.md#cokb)**: Thực hiện suy diễn trên các đối tượng tính toán phức tạp, giải quyết các bài toán về Quan hệ (IS-A, PART-OF) và Phương trình toán học.

## 4. Dịch vụ Bảo mật & Xác thực

Hệ thống bảo vệ tri thức thông qua cơ chế xác thực đa tầng và phân quyền dựa trên vai trò ([RBAC](../00-glossary/01-glossary.md#rbac)):
*   **Authentication**: Đối soát thông tin đăng nhập với `UserCatalog` lưu trong [System KB](../00-glossary/01-glossary.md#system-kb). Mật khẩu được mã hóa an toàn.
*   **Authorization ([RBAC](../00-glossary/01-glossary.md#rbac))**: Phân chia 3 cấp độ truy cập:
    *   **ROOT**: Quyền cao nhất, quản lý toàn bộ hệ thống, user và cấu hình.
    *   **ADMIN**: Quản lý các Cơ sở tri thức (KB) và [Concept](../00-glossary/01-glossary.md#concept).
    *   **RESEARCHER**: Quyền đọc và thực thi suy diễn (Read-only / [Solve](../00-glossary/01-glossary.md#solve)).

## 5. Hệ thống Nhật ký & Chẩn đoán

[KBMS](../00-glossary/01-glossary.md#kbms) V3 sở hữu cơ chế nhật ký độc đáo, biến log thành một dạng tri thức có thể truy vấn:
*   **System Logs**: Lưu trữ các sự kiện vận hành, lỗi hệ thống và hiệu năng RAM/CPU.
*   **[Audit Logs](../00-glossary/01-glossary.md#audit-logs)**: Ghi lại mọi câu lệnh [KBQL](../00-glossary/01-glossary.md#kbql) của người dùng, thời gian thực hiện và trạng thái thành công/thất bại.
*   **Persistence**: Mọi log đều được `SystemLogger` chèn trực tiếp vào các [concept](../00-glossary/01-glossary.md#concept) `system_logs` và `audit_logs` bên trong **[System KB](../00-glossary/01-glossary.md#system-kb)**.
*   **Real-time [Streaming](../00-glossary/01-glossary.md#streaming)**: Hỗ trợ đẩy log trực tiếp tới Studio thông qua WebSockets/TCP phục vụ giám sát thời gian thực.

- Server Layer được thiết kế theo hướng **[Thread-safe](../00-glossary/01-glossary.md#thread-safe)**, cho phép nhiều người dùng cùng truy cập và thực thi suy diễn đồng thời trên các vùng tri thức khác nhau.
