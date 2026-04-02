# 12.1. Tổng quan Giao diện Dòng lệnh
Quản trị

`KBMS.CLI` là một ứng dụng Console (C#) mạnh mẽ, cung cấp khả năng tương tác trực tiếp với máy chủ [KBMS](../00-glossary/01-glossary.md#kbms) thông qua giao thức nhị phân và dòng lệnh [KQL](../00-glossary/01-glossary.md#kql)/[KDL](../00-glossary/01-glossary.md#kdl).

## 1. Các Tính năng Chính

*   **[REPL](../00-glossary/01-glossary.md#repl) (Read-Eval-Print Loop)**: Người dùng nhập câu lệnh, [CLI](../00-glossary/01-glossary.md#cli) gửi tới Server, nhận kết quả và in ra ngay lập tức.
*   **[Advanced Line Editing](../00-glossary/01-glossary.md#advanced-line-editing)**: Hỗ trợ đầy đủ các phím di chuyển và thao tác nhanh (`LineEditor.cs`):
    *   **Mũi tên Lên/Xuống**: Duyệt lịch sử lệnh.
    *   **Home/End**: Di chuyển nhanh đến đầu/cuối dòng.
    *   **Delete/Backspace**: Xóa ký tự tại con trỏ.
    *   **Esc x2**: Xóa sạch bộ đệm đang nhập (Double Escape to Clear).
*   **[Multi-line Input](../00-glossary/01-glossary.md#multi-line-input)**: Hỗ trợ nhập các câu lệnh dài. Chế độ thụt đầu dòng tự động `->` giúp phân biệt dòng tiếp nối.
*   **Display Modes**: Cung cấp hai chế độ hiển thị chính thông qua `ResponseParser.cs`:
    *   **[Table Mode](../00-glossary/01-glossary.md#table-mode)**: Hiển thị bảng ngang (MySQL style).
    *   **Vertical Mode (\G)**: Hiển thị theo hàng dọc, tự động kích hoạt cho các lệnh `DESCRIBE` hoặc `EXPLAIN` để tối ưu hóa việc đọc cấu trúc tri thức phức tạp.
*   **Chế độ Chạy Script (`SOURCE`)**: Cho phép thực thi các tệp tin truy vấn tri thức lớn (`.kbql`) một cách tự động, tự dừng khi gặp lỗi.
*   **Xử lý Phản hồi Phức tạp**: Sử dụng `ResponseParser.cs` để hiển thị kết quả truy vấn dưới dạng bảng (Table), đồ thị tri thức hoặc thông báo lỗi line-accurate.
*   **Tự động kết nối lại (Auto-reconnect)**: CLI tự động thử kết nối lại nếu máy chủ bị ngắt kết nối đột ngột ([Heartbeat](../00-glossary/01-glossary.md#heartbeat) timeout).

---

## 2. Các Lệnh Hệ thống Đặc biệt

*Bảng 12.1: Danh mục lệnh điều khiển CLI*
| Lệnh | Mô tả |
| :--- | :--- |
| `LOGIN <u|p>` | Đăng nhập bảo mật (mật khẩu không lưu vào lịch sử). |
| `SOURCE <path>` | Thực thi script từ tệp tin bên ngoài. |
| `CONNECT` | Kết nối lại máy chủ thủ công. |
| `CLEAR` | Xóa sạch màn hình console. |

---

## 3. Trải nghiệm người dùng

Để mang lại trải nghiệm chuyên nghiệp như một hệ quản trị CSDL thực thụ, CLI được tích hợp các phím tắt:
*   **Mũi tên Lên/Xuống**: Xem lại lịch sử lệnh đã gõ.
*   **Home/End**: Di chuyển con trỏ nhanh trong dòng lệnh.
*   **Esc x2**: Xóa nhanh input đang nhập dở.

