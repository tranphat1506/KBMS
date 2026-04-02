# 13.1. Tổng quan & Tính năng KBMS 

Studio là một môi trường phát triển tích hợp ([IDE](../../../00-glossary/01-glossary.md#ide)) hiện đại, được xây dựng trên nền tảng **[Electron](../../../00-glossary/01-glossary.md#electron)** và **[React](../../../00-glossary/01-glossary.md#react)**, cung cấp trải nghiệm trực quan hóa tri thức thay vì chỉ thao tác dòng lệnh thuần túy.

## 1. Các Phân hệ Cốt lõi

### a. Knowledge Designer
*   Cho phép người dùng kéo thả và xem sơ đồ phả hệ [Concept](../../../00-glossary/01-glossary.md#concept)/Relation.
*   Tự động phát hiện các xung đột logic khi thiết kế Schema bài toán.

### b. Query Console (Cửa sổ truy vấn Monaco)
*   **Trình soạn thảo [Monaco](../../../00-glossary/01-glossary.md#monaco)**: Lõi của VS Code được tích hợp sâu, hỗ trợ tô màu cú pháp (Syntax Highlighting) cho [KBQL](../../../00-glossary/01-glossary.md#kbql).
*   **[Error Squiggles](../../../00-glossary/01-glossary.md#error-squiggles)**: Đánh dấu lỗi đỏ tại vị trí chính xác (Dòng/Cột) khi Server trả về `ParseException`.
*   **[IntelliSense](../../../00-glossary/01-glossary.md#intellisense)**: Gợi ý các Concept và Thuộc tính đã định nghĩa.

### c. Management Dashboard
*   **Giám sát RAM/Disk**: Theo dõi thời gian thực thông qua hệ thống telemetry.
*   **User & Session Management**: 
    *   **User Management**: Thêm/Xóa/Sửa tài khoản và phân quyền trực tiếp từ UI (`UserManagement.tsx`).
    *   **Active Sessions**: Kiểm soát danh sách các máy khách đang kết nối, hỗ trợ ngắt kết nối (Kick) các phiên không hợp lệ (`ActiveSessions.tsx`).
*   **[Log Analyzer](../../../00-glossary/01-glossary.md#log-analyzer)**: Bộ công cụ phân tích nhật ký nâng cao, hỗ trợ lọc theo cấp độ (Level) và thực thể tri thức (`LogAnalyzer.tsx`).

### d. Real-time Notification System
*   **Push Alerts**: Nhận cảnh báo tức thời từ Server khi có sự cố hệ thống hoặc sự kiện an ninh (`NotificationBell.tsx`, `NotificationToasts.tsx`).
*   **[Audit Trails](../../../00-glossary/01-glossary.md#audit-trails)**: Xem nhật ký truy cập chi tiết cho từng người dùng.

### e. Advanced Settings & Multi-server
*   **Connect Modal**: Hỗ trợ lưu trữ và chuyển đổi nhanh giữa nhiều máy chủ [KBMS](../../../00-glossary/01-glossary.md#kbms) (`ConnectModal.tsx`).
*   **System Settings**: Tùy chỉnh tham số Engine và giao diện người dùng (Themes).

---

## 2. Tiêu chuẩn Giao diện

Studio được thiết kế với ngôn ngữ **Modern Dark Mode** kết hợp với bộ biểu tượng tối giản, giúp người dùng tập trung tối đa vào các cấu trúc tri thức phức tạp.

*   **Responsive Layout**: Tự động điều chỉnh kích thước khi mở các panel theo dõi dữ liệu.
*   **[Streaming](../../../00-glossary/01-glossary.md#streaming) UI**: Kết quả trả về từ Server được đổ trực tiếp vào bảng dữ liệu (Data Grid) ngay khi có gói tin đầu tiên, không gây treo giao diện.

