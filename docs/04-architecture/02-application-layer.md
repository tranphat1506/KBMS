# 04.2. Tầng Ứng dụng

Tầng Ứng dụng là điểm chạm đầu tiên giữa người dùng và hệ thống [KBMS](../00-glossary/01-glossary.md#kbms). Nó bao gồm hai thành phần chính phục vụ các mục đích khác nhau.

## 1. KBMS Studio (IDE Chuyên nghiệp)

[KBMS](../00-glossary/01-glossary.md#kbms) Studio được xây dựng trên nền tảng [React](../00-glossary/01-glossary.md#react) và [Electron](../00-glossary/01-glossary.md#electron), đóng vai trò là một môi trường phát triển tri thức toàn diện (Knowledge [IDE](../00-glossary/01-glossary.md#ide)).

*   **Chức năng chính**:
    *   **[Monaco](../00-glossary/01-glossary.md#monaco) Engine**: Soạn thảo mã nguồn [KBQL](../00-glossary/01-glossary.md#kbql) với tính năng [IntelliSense](../00-glossary/01-glossary.md#intellisense) và báo lỗi trực tiếp.
    *   **Visual [Knowledge Designer](../00-glossary/01-glossary.md#knowledge-designer)**: Cho phép thiết kế mô hình tri thức bằng đồ thị kéo thả.
    *   **[Management Dashboard](../00-glossary/01-glossary.md#management-dashboard)**: Giám sát hiệu năng Server, quản lý User và xem [Audit Logs](../00-glossary/01-glossary.md#audit-logs).
*   **Công nghệ**: [React](../00-glossary/01-glossary.md#react), TypeScript, [Monaco](../00-glossary/01-glossary.md#monaco) Editor, Tailwind CSS.

## 2. KBMS [CLI]

[KBMS](../00-glossary/01-glossary.md#kbms) [CLI](../00-glossary/01-glossary.md#cli) là công cụ dòng lệnh mạnh mẽ dành cho các nhà quản trị và nhà phát triển hệ thống.

*   **Chức năng chính**:
    *   **[REPL](../00-glossary/01-glossary.md#repl) (Read-Eval-Print Loop)**: Môi trường tương tác dòng lệnh trực tiếp với Server.
    *   **[Batch Processing](../00-glossary/01-glossary.md#batch-processing)**: Thực thi các kịch bản tri thức (.[kbql](../00-glossary/01-glossary.md#kbql)) quy mô lớn thông qua lệnh `SOURCE`.
    *   **Advanced Formatting**: Hỗ trợ hiển thị kết quả dưới dạng bảng hoặc hàng dọc (`\G`).
*   **Công nghệ**: .NET Core, C#, `LineEditor` custom library.