# Giao thức Mạng & Truyền tin Dòng (Streaming)

KBMS sử dụng một giao thức truyền tin tùy chỉnh dựa trên TCP, được tối ưu hóa cho việc truyền tải tri thức và dữ liệu lớn thông qua cơ chế **Row-based Streaming**.

## 1. Cấu trúc Gói tin (Message Frame)
Mỗi gói tin gửi đi từ Server hoặc Client đều có cấu trúc Fixed-header 5-byte:

- **Type (1 byte)**: Xác định loại message (LOGIN, QUERY, METADATA, ROW, RESULT, ERROR, FETCH_DONE).
- **Length (4 bytes)**: Độ dài của phần nội dung (Payload) theo sau, kiểu Big-endian.

## 2. Các loại Message chính
- `0x01 LOGIN`: Xác thực người dùng.
- `0x02 QUERY`: Gửi câu lệnh KBQL từ Client.
- `0x03 RESULT`: Trả về kết quả JSON cho các lệnh không stream (như INSERT thành công).
- `0x04 METADATA`: Gửi thông tin cấu trúc bảng (Cột, Kiểu dữ liệu) trước khi stream dữ liệu.
- `0x05 ROW`: Gửi dữ liệu của duy nhất MỘT dòng (dưới dạng JSON).
- `0x06 FETCH_DONE`: Báo hiệu đã kết thúc việc stream cho một câu lệnh.
- `0x07 ERROR`: Thông báo lỗi thực thi.

## 3. Cơ chế Streaming (Dòng dữ liệu)
Thay vì trả về toàn bộ kết quả trong một mảng JSON khổng lồ (gây tốn RAM), KBMS v1.1 sử dụng luồng streaming:

1. **Client** gửi `QUERY`.
2. **Server** phân tích và bắt đầu thực thi.
3. Nếu lệnh trả về danh sách (SELECT, SHOW, DESCRIBE...):
   - Server gửi `METADATA` chứa danh sách cột.
   - Server lặp qua từng bản ghi và gửi từng `ROW` riêng biệt.
   - Server gửi `FETCH_DONE` khi hết dữ liệu.
4. **Client** nhận `METADATA` để dựng khung bảng, sau đó render từng `ROW` ngay khi nó vừa cập bến.

## 4. Thực thi Đa lệnh (Multi-statement Support)
Server hỗ trợ nhận chuỗi lệnh cách nhau bởi dấu `;`. Quy trình xử lý:
- Server tách chuỗi thành các câu lệnh đơn lẻ.
- Thực thi tuần tự từng lệnh.
- Với mỗi lệnh, thực hiện quy trình Streaming hoặc Result như trên.
- Nếu một lệnh gặp lỗi (ERROR), Server sẽ **ngừng ngay lập tức** và không thực thi các lệnh phía sau (Stop on Error).

## 5. Unified Tabular Response
Toàn bộ các lệnh mang tính chất liệt kê hoặc mô tả (SHOW, DESCRIBE, EXPLAIN) đều được quy chuẩn về định dạng bảng (`QueryResultSet`) để tận dụng tối đa luồng Streaming và giúp Client hiển thị dữ liệu một cách đồng nhất.
