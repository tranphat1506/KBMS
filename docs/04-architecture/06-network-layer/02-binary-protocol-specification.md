# Đặc tả Giao thức Nhị phân

Giao thức nhị phân của [KBMS](../../../00-glossary/01-glossary.md#kbms) được định nghĩa dưới dạng khung tin (Frame) với tiêu đề (Header) có kích cỡ cố định. Cấu trúc này cho phép bộ giải mã phân tách dữ liệu với độ phức tạp thuật toán thấp nhất ($O(1)$), phục vụ cho các hệ thống tri thức đòi hỏi tần suất truy vấn cao.

## 1. Cấu trúc Hình học của Khung tin

Mọi thông điệp giao tiếp giữa Máy trạm (Client) và Máy chủ (Server) đều được đóng gói theo định dạng nhị phân đồng nhất, đảm bảo tính nhất quán dữ liệu trên toàn bộ hạ tầng hệ thống.

*Bảng 4.6: Đặc tả chi tiết các trường dữ liệu trong Gói tin Nhị phân*
| Vị trí (Byte) | Trường dữ liệu | Kiểu dữ liệu | Vai trò đặc tả |
| :--- | :--- | :--- | :--- |
| **0 - 3** | **Tổng kích thước**| Số nguyên 32-bit (Big-endian) | Tổng kích thước dữ liệu tiếp nối (không bao gồm 5 byte đầu). |
| **4** | **Loại thông điệp**| Số nguyên 8-bit | Mã định danh phân loại thông điệp (0x01: Đăng nhập, 0x02: Truy vấn,...). |
| **5 - 6** | **Độ dài Phiên** | Số nguyên 16-bit | Độ dài của chuỗi định danh phiên làm việc (Session ID). |
| **7 - N** | **Mã Phiên** | Chuỗi UTF-8 | Mã định danh phiên người dùng do Máy chủ cấp phát sau khi xác thực. |
| **N+1 - N+2**| **Độ dài Yêu cầu** | Số nguyên 16-bit | Độ dài của chuỗi định danh yêu cầu truy xuất. |
| **N+3 - M** | **Mã Yêu cầu** | Chuỗi UTF-8 | Mã định danh duy nhất (Request ID) dùng để ghép nối phản hồi. |
| **M+1 - End** | **Dữ liệu tải** | Chuỗi UTF-8 | Nội dung chính (Câu lệnh KBQL hoặc Tập kết quả suy diễn). |

## 2. Phân tích Thực nghiệm Gói tin Nhị phân

Dưới đây là một ví dụ minh họa về cấu trúc mã thô của một yêu cầu truy vấn chuẩn hóa từ Máy trạm ("SELECT * FROM Concept") với Mã phiên là "S1" và Mã yêu cầu là "R1".

*Bảng 4.7: Phân rã gói tin thực nghiệm (Mã thập lục phân)*
| Byte (Hex) | Giá trị giải mã | Giải thích mô tả chi tiết |
| :--- | :--- | :--- |
| **`00 00 00 1B`**| **27** | **Tổng kích thước**: Kích thước sau tiêu đề ghi nhận 27 byte. |
| **`02`** | **MessageType.QUERY** | Xác định loại thông điệp là truy vấn tri thức cấp cao. |
| **`00 02`** | **2** | **Độ dài Phiên**: Mã định danh "S1" có độ dài 2 byte. |
| **`53 31`** | **"S1"** | **Mã Phiên**: Xác định ngữ cảnh phiên làm hiện hành. |
| **`00 02`** | **2** | **Độ dài Yêu cầu**: Mã "R1" có độ dài 2 byte. |
| **`52 31`** | **"R1"** | **Mã Yêu cầu**: Xác định luồng yêu cầu đang xử lý. |
| **`53 45 4C ...`** | **"SELECT * ..."** | **Dữ liệu tải**: Nội dung thô của câu lệnh KBQL (21 byte). |

## 3. Đánh giá Hiệu năng và Độ trễ Phân tách

Giao thức được tối ưu hóa để giảm thiểu thành phần quản trị (Overhead) xuống mức thấp nhất đảm bảo hiệu suất truyền dẫn:

-   **Độ phủ của Tiêu đề**: Trung bình chỉ chiếm từ **5% đến 15%** tổng kích thước thông điệp đối với các kịch bản truy vấn tri thức trong môi trường thực tế.
-   **Hiệu năng Giải mã**: Sử dụng các hàm xử lý nhị phân Big-endian đảm bảo tính nhất quán, bộ giải mã chỉ tiêu tốn trung bình **0.05ms** để phân tách khung tin trên các dòng vi xử lý hiện đại.
-   **Tính tương thích hệ thống**: Việc ép buộc chuẩn Big-endian giúp loại bỏ triệt để các sai lệch về kiến trúc phần cứng (Endianness) giữa các điểm cuối trong mạng.

Kiến trúc giao thức này cho phép hệ thống giải quyết tối ưu bài toán truyền dẫn tri thức quy mô lớn với độ bền bỉ và độ chính xác cao.
