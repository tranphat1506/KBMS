# Hướng dẫn Xử lý lỗi (Troubleshooting)

Trong quá trình vận hành KBMS, bạn có thể gặp phải các thông báo lỗi từ các tầng khác nhau của hệ thống. Dưới đây là cách hiểu và khắc phục các vấn đề phổ biến nhất.

## 1. Lỗi Phân tích Cú pháp (Parser Errors)

Xảy ra khi câu lệnh KBQL không đúng ngữ pháp.

| Thông báo lỗi | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Parse error at line L, column C: Unexpected token...** | Sai cú pháp, thiếu dấu chấm phẩy hoặc từ khóa. | Kiểm tra lại vị trí dòng L và cột C trong editor. Đảm bảo kết thúc lệnh bằng `;`. |
| **Concept 'X' not found** | Truy vấn vào một Khái niệm chưa được định nghĩa. | Kiểm tra lệnh `CREATE CONCEPT` hoặc sử dụng `SHOW CONCEPTS` để xem danh sách. |
| **Variable 'V' not defined in Concept 'X'** | Thuộc tính của Concept không tồn tại. | Sử dụng lệnh `DESCRIBE (CONCEPT: X)` để xem lại cấu trúc các biến. |

---

## 2. Lỗi Hệ suy diễn (Reasoning Errors)

Xảy ra khi bộ máy suy diễn gặp bế tắc hoặc vi phạm logic.

| Thông báo lỗi | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Inference engine halted: FClosure(GT) exhausted...** | Không tìm thấy lời giải cho các mục tiêu (KL) yêu cầu. | Kiểm tra xem dữ liệu đầu vào (Facts) có đầy đủ để kích hoạt các Rule/Equation hay không. |
| **Constraint violated: <expression>** | Dữ liệu suy diễn ra vi phạm các điều kiện ràng buộc. | Rà soát lại logic của các Rule hoặc kiểm tra tính đúng đắn của dữ liệu đầu vào. |
| **Newton-Raphson failed to converge** | Hệ phương trình quá phức tạp hoặc không có nghiệm thực. | Kiểm tra lại các phương trình (Equations) xem có mâu thuẫn hoặc thiếu tham số không. |

---

## 3. Lỗi Máy chủ và Mạng (Server & Network Errors)

Các vấn đề về kết nối giữa Client và Server.

| Thông báo lỗi | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Connection refused** | Server chưa được khởi động hoặc sai Port. | Đảm bảo `KBMS.Server` đang chạy và kiểm tra cài đặt Port trong `kbms.ini`. |
| **Buffer Pool is full (All frames pinned)** | Hệ thống đang xử lý quá nhiều yêu cầu đồng thời vượt quá dung lượng cache. | Tăng giá trị `BufferPoolSize` trong file cấu hình `kbms.ini`. |
| **LSN mismatch in WAL recovery** | File nhật ký bị hỏng hoặc không đồng nhất sau khi sập nguồn. | Xóa file `.log` (Lưu ý: Có thể mất dữ liệu chưa ghi) hoặc thực hiện phục hồi thủ công từ backup. |

---

## 4. Cách gỡ lỗi hiệu quả

*   **Sử dụng lệnh EXPLAIN:** Để xem kế hoạch thực thi của một câu lệnh trước khi chạy.
*   **Theo dõi Trace:** Luôn xem tab **Reasoning Trace** trong KBMS Studio để biết hệ thống đã suy diễn đến bước nào.
*   **Kiểm tra Logs:** Xem file `activity.log` trong thư mục server để biết thêm các chi tiết kỹ thuật về lỗi.
