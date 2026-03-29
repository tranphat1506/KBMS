# 02. Tầng Ứng dụng (Application Layer)

Tầng Ứng dụng là điểm chạm đầu tiên giữa người dùng và hệ thống KBMS. Nó bao gồm hai thành phần chính phục vụ các mục đích khác nhau.

## 1. KBMS Studio (IDE Chuyên nghiệp)

KBMS Studio được xây dựng trên nền tảng React và Electron, đóng vai trò là một môi trường phát triển tri thức toàn diện (Knowledge IDE).

*   **Chức năng chính**:
    *   **Monaco Engine**: Soạn thảo mã nguồn KBQL với tính năng IntelliSense và báo lỗi trực tiếp.
    *   **Visual Knowledge Designer**: Cho phép thiết kế mô hình tri thức bằng đồ thị kéo thả.
    *   **Management Dashboard**: Giám sát hiệu năng Server, quản lý User và xem Audit Logs.
*   **Công nghệ**: React, TypeScript, Monaco Editor, Tailwind CSS.

## 2. KBMS CLI (Command Line Interface)

KBMS CLI là công cụ dòng lệnh mạnh mẽ dành cho các nhà quản trị và nhà phát triển hệ thống.

*   **Chức năng chính**:
    *   **REPL (Read-Eval-Print Loop)**: Môi trường tương tác dòng lệnh trực tiếp với Server.
    *   **Batch Processing**: Thực thi các kịch bản tri thức (.kbql) quy mô lớn thông qua lệnh `SOURCE`.
    *   **Advanced Formatting**: Hỗ trợ hiển thị kết quả dưới dạng bảng hoặc hàng dọc (`\G`).
*   **Công nghệ**: .NET Core, C#, `LineEditor` custom library.

---

> [!TIP]
> **Lựa chọn công cụ**: Studio phù hợp cho công tác nghiên cứu và thiết kế mô hình; CLI phù hợp cho công tác bảo trì và tự động hóa hệ thống.
