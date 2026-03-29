# 04. Tầng Máy chủ (Server Layer)

Tầng Máy chủ (Server Layer) là thành phần phức tạp nhất của KBMS, chịu trách nhiệm cho toàn bộ quá trình biên dịch ngôn ngữ và suy diễn tự động.

## 1. Knowledge Manager (Orchestrator)

Knowledge Manager là bộ điều phối trung tâm của Server. Nó chịu trách nhiệm:
*   **KB Isolation**: Sử dụng `StoragePool` để đảm bảo mỗi người dùng có thể làm việc trên một Cơ sở tri thức độc lập mà không gây xung đột tài nguyên.
*   **Thread-safe Operations**: Quản lý các lệnh nạp/hủy KB thông qua cơ chế khóa (Mutual Exclusion) để duy trì tính toàn vẹn của danh lục hệ thống.
*   **Execution Routing**: Điều phối luồng làm việc giữa Parser (Biên dịch) và Inference Engine (Suy diễn).

## 2. KBQL Parser & Compiler

*   **Phân tích Cú pháp**: Sử dụng thuật toán đệ quy xuống (Recursive Descent) để biên dịch câu lệnh KBQL thành cấu trúc AST (Abstract Syntax Tree).
*   **Hỗ trợ**: Biên dịch các tập lệnh KDL (Định nghĩa), KQL (Truy vấn), KCL (Bảo mật), KML (Bảo trì) và TCL (Giao dịch).

## 3. Reasoning Engine (Bộ máy Suy diễn)

*   **Forward Chaining**: Thuật toán suy diễn tiến dựa trên điểm đóng **F-Closure**.
*   **Mô hình COKB**: Thực hiện suy diễn trên các đối tượng tính toán phức tạp, giải quyết các bài toán về Quan hệ (IS-A, PART-OF) và Phương trình toán học.

## 4. Dịch vụ Bảo mật & Xác thực (Security & Auth)

Hệ thống bảo vệ tri thức thông qua cơ chế xác thực đa tầng và phân quyền dựa trên vai trò (RBAC):
*   **Authentication**: Đối soát thông tin đăng nhập với `UserCatalog` lưu trong System KB. Mật khẩu được mã hóa an toàn.
*   **Authorization (RBAC)**: Phân chia 3 cấp độ truy cập:
    *   **ROOT**: Quyền cao nhất, quản lý toàn bộ hệ thống, user và cấu hình.
    *   **ADMIN**: Quản lý các Cơ sở tri thức (KB) và Concept.
    *   **RESEARCHER**: Quyền đọc và thực thi suy diễn (Read-only / Solve).

## 5. Hệ thống Nhật ký & Chẩn đoán (Native Logging)

KBMS V3 sở hữu cơ chế nhật ký độc đáo, biến log thành một dạng tri thức có thể truy vấn:
*   **System Logs**: Lưu trữ các sự kiện vận hành, lỗi hệ thống và hiệu năng RAM/CPU.
*   **Audit Logs**: Ghi lại mọi câu lệnh KBQL của người dùng, thời gian thực hiện và trạng thái thành công/thất bại.
*   **Persistence**: Mọi log đều được `SystemLogger` chèn trực tiếp vào các concept `system_logs` và `audit_logs` bên trong **System KB**.
*   **Real-time Streaming**: Hỗ trợ đẩy log trực tiếp tới Studio thông qua WebSockets/TCP phục vụ giám sát thời gian thực.

---

> [!NOTE]
> Server Layer được thiết kế theo hướng **Thread-safe**, cho phép nhiều người dùng cùng truy cập và thực thi suy diễn đồng thời trên các vùng tri thức khác nhau.
