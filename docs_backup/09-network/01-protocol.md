# 09.1. Giao thức Truyền tải Dữ liệu nhị phân

Để đạt được hiệu năng tối ưu và khả năng tương thích cao giữa Server (C#), [CLI](../00-glossary/01-glossary.md#cli) và Studio (Node.js/[Electron](../00-glossary/01-glossary.md#electron)), [KBMS](../00-glossary/01-glossary.md#kbms) V3 sử dụng một giao thức truyền tải nhị phân ([Binary Protocol](../00-glossary/01-glossary.md#binary-protocol)) tùy chỉnh chạy trên nền TCP.

---

## 1. Cấu trúc Gói tin

Mọi thông điệp trao đổi giữa Client và Server đều được đóng gói theo định dạng nhị phân với [Header](../00-glossary/01-glossary.md#header) cố định và Payload biến thiên.

### Đặc tả Header nhị phân:

*Bảng 9.1: Đặc tả cấu trúc Header nhị phân của gói tin trong giao thức KBMS*
| Offset | Trường dữ liệu | Kiểu | Mô tả |
| :--- | :--- | :--- | :--- |
| **0 - 3** | **Total Length**| Int32 | Tổng kích thước gói tin (Big-Endian). |
| **4** | **Message Type**| Byte | Loại thông điệp (LOGIN, QUERY, RESULT, ...). |
| **5 - 6** | **SessionLen** | UInt16 | Độ dài của Session ID chuỗi. |
| **7 - N** | **Session ID** | String | Định danh phiên làm việc (UTF-8). |
| **N+1 - N+2**| **RequestLen** | UInt16 | Độ dài của Request ID chuỗi. |
| **N+3 - M** | **Request ID** | String | Định danh yêu cầu (Dùng để bắt cặp phản hồi). |
| **M+1 - End** | **Payload** | String | Nội dung chính của thông điệp (JSON/Text). |

---

## 2. Danh mục Loại thông điệp

*Bảng 9.2: Danh mục các loại thông điệp trong giao thức nhị phân KBMS*
| Giá trị | Tên loại | Mục đích sử dụng |
| :--- | :--- | :--- |
| **1** | **LOGIN** | Gửi thông tin đăng nhập (User/Pass). |
| **2** | **QUERY** | Gửi mã nguồn KBQL để thực hiện suy diễn/truy vấn. |
| **3** | **RESULT** | Trả về thông tin kết quả đơn lẻ hoặc thông báo thành công. |
| **4** | **ERROR** | Trả về thông tin lỗi (Runtime, Parser, Auth). |
| **6** | **METADATA** | Trả về định nghĩa cột và dữ liệu thống kê của tập kết quả. |
| **7** | **ROW** | Trả về một lô (Batch) dữ liệu kết quả dưới dạng lưới. |
| **8** | **FETCH_DONE** | Thông báo đã hoàn tất việc gửi dữ liệu cho một Request. |
| **10** | **STATS** | Yêu cầu/Trả về thông số hiệu năng hệ thống (ROOT only). |
| **11** | **LOGS_STREAM** | Đăng ký nhận luồng log thời gian thực từ Server (ROOT only). |

---

## 3. Quy trình Trao đổi (Handshake & Query Flow)

1. **Authentication**: Client gửi gói `LOGIN`. Server kiểm tra và trả về `RESULT:SUCCESS` kèm theo một `Session ID` duy nhất.
2. **Querying**: 
    - Client gửi gói `QUERY` kèm theo mã `RequestId`.
    - Server gửi lần lượt các gói `METADATA`, sau đó là nhiều gói `ROW` (mỗi gói chứa 100 bản ghi). 
    - Server kết thúc bằng gói `FETCH_DONE` để Client đóng trạng thái chờ.
3. **[Pipelining](../00-glossary/01-glossary.md#pipelining)**: Nhờ có `RequestId`, Client có thể gửi nhiều yêu cầu đồng thời mà không cần chờ yêu cầu trước đó kết thúc (Asynchronous Multi-plexing).

---

## 4. Bảo mật tại tầng Truyền tải

- **[Big-Endian](../00-glossary/01-glossary.md#big-endian) Consistency**: Đảm bảo dữ liệu số (Length, Type) luôn được đọc đúng trên các kiến trúc CPU khác nhau (x64, ARM).
- **UTF-8 Encoding**: Toàn bộ chuỗi ký tự (SessionID, Payload) được mã hóa UTF-8 để hỗ trợ tiếng Việt có dấu trong các định nghĩa tri thức.
- **[Session Isolation](../00-glossary/01-glossary.md#session-isolation)**: Server sử dụng Session ID trong Header để định danh quyền hạn và không gian tên (Current KB) cho mỗi gói tin nhận được.
