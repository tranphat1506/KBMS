# 13.1. Tổng quan & Tính năng KBMS Studio là một môi trường phát triển tích hợp (IDE) hiện đại, được xây dựng trên nền tảng **Electron** và **React**, cung cấp trải nghiệm trực quan hóa tri thức thay vì chỉ thao tác dòng lệnh thuần túy.

## 1. Các Phân hệ Cốt lõi

### a. Knowledge Designer (Trình thiết kế trực quan)
*   Cho phép người dùng kéo thả và xem sơ đồ phả hệ Concept/Relation.
*   Tự động phát hiện các xung đột logic khi thiết kế Schema bài toán.

### b. Query Console (Cửa sổ truy vấn Monaco)
*   **Trình soạn thảo Monaco**: Lõi của VS Code được tích hợp sâu, hỗ trợ tô màu cú pháp (Syntax Highlighting) cho KBQL.
*   **Error Squiggles**: Đánh dấu lỗi đỏ tại vị trí chính xác (Dòng/Cột) khi Server trả về `ParseException`.
*   **IntelliSense**: Gợi ý các Concept và Thuộc tính đã định nghĩa.

### c. Management Dashboard (Bảng điều khiển quản diện)
*   **Giám sát RAM/Disk**: Theo dõi thời gian thực thông qua hệ thống telemetry.
*   **User & Session Management**: 
    *   **User Management**: Thêm/Xóa/Sửa tài khoản và phân quyền trực tiếp từ UI (`UserManagement.tsx`).
    *   **Active Sessions**: Kiểm soát danh sách các máy khách đang kết nối, hỗ trợ ngắt kết nối (Kick) các phiên không hợp lệ (`ActiveSessions.tsx`).
*   **Log Analyzer**: Bộ công cụ phân tích nhật ký nâng cao, hỗ trợ lọc theo cấp độ (Level) và thực thể tri thức (`LogAnalyzer.tsx`).

### d. Real-time Notification System
*   **Push Alerts**: Nhận cảnh báo tức thời từ Server khi có sự cố hệ thống hoặc sự kiện an ninh (`NotificationBell.tsx`, `NotificationToasts.tsx`).
*   **Audit Trails**: Xem nhật ký truy cập chi tiết cho từng người dùng.

### e. Advanced Settings & Multi-server
*   **Connect Modal**: Hỗ trợ lưu trữ và chuyển đổi nhanh giữa nhiều máy chủ KBMS (`ConnectModal.tsx`).
*   **System Settings**: Tùy chỉnh tham số Engine và giao diện người dùng (Themes).

---

## 2. Tiêu chuẩn Giao diện (Aesthetics)

Studio được thiết kế với ngôn ngữ **Modern Dark Mode** kết hợp với bộ biểu tượng tối giản, giúp người dùng tập trung tối đa vào các cấu trúc tri thức phức tạp.

*   **Responsive Layout**: Tự động điều chỉnh kích thước khi mở các panel theo dõi dữ liệu.
*   **Streaming UI**: Kết quả trả về từ Server được đổ trực tiếp vào bảng dữ liệu (Data Grid) ngay khi có gói tin đầu tiên, không gây treo giao diện.

> [!TIP]
> **Khả năng Mở rộng**
> Studio hỗ trợ cài đặt các plugin tùy chỉnh để xuất dữ liệu tri thức ra các định dạng đồ họa như SVG hoặc JSON cho các báo cáo học thuật. 🎨
