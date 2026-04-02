# 4.3.3 Đặc tả Cấu trúc Dữ liệu Slotted Page (Data Layout)

Hệ thống KBMS [K00] sử dụng cấu trúc **Slotted Page** làm đơn vị phân trang cơ sở. Mô hình này được áp dụng nhằm quản lý các thực thể tri thức có kích thước biến thiên và tối ưu hóa phân bố không gian lưu trữ thực tế bên trong mỗi trang dữ liệu nhị phân.

## 4.3.3.1 Tổ chức Phân vùng Dữ liệu trong Slotted Page

Một trang dữ liệu tiêu chuẩn có dung lượng **16,384 Bytes** (16 KB) được cấu trúc thành các phân vùng chức năng sau:

![slotted_page_v3.png | width=0.7](../../assets/diagrams/slotted_page_v3.png)
*Hình 4.13: Sơ đồ tổ chức phân vùng dữ liệu trong cấu trúc Slotted Page.*

1.  **Header (24 Bytes)**: Chứa các trường dữ liệu điều phối, định danh trang và các liên kết cấu trúc giữa các trang.
2.  **Slot Array**: Danh sách các con trỏ logic (Slots) phát triển tuyến tính từ Byte 24. Mỗi slot có kích thước cố định 8 Bytes (`[Offset: 4B | Length: 4B]`).
3.  **Free Space**: Vùng nhớ khả dụng nằm giữa Slot Array và vùng lưu trữ thực thể (Tuple Storage).
4.  **Tuple Storage**: Vùng lưu trữ các thực thể tri thức dưới định dạng nhị phân, được phân bổ từ byte cuối cùng của trang và phát triển ngược về phía Slot Array.

## 4.3.3.2 Đặc tả Kĩ thuật của Header Trang

Header trang chứa các tham số kĩ thuật cần thiết để duy trì cấu trúc lồng nhau của [B+ Tree](../../00-glossary/01-glossary.md#b-tree) và hỗ trợ giao thức phục hồi dữ liệu:

*Bảng 4.2: Đặc tả cấu trúc Page Header và ý nghĩa các trường dữ liệu*
| Byte Offset | Trường dữ liệu | Kiểu | Mô tả kĩ thuật |
| :--- | :--- | :--- | :--- |
| **0 - 3** | **PageId** | Int32 | Định danh vật lý duy nhất của trang trong cơ sở tri thức. |
| **4 - 7** | **LSN** | Int32 | Log Sequence Number đánh dấu phiên bản nhật ký giao dịch liên quan. |
| **8 - 11** | **PrevPageId** | Int32 | Định danh trang phía trước trong cấu trúc liên kết. |
| **12 - 15** | **NextPageId** | Int32 | Định danh trang tiếp theo trong cấu trúc liên kết. |
| **16 - 19** | **FreeSpacePointer**| Int32 | Offset đánh dấu vị trí byte bắt đầu của bản ghi được lưu trữ gần nhất. |
| **20 - 23** | **TupleCount** | Int32 | Tổng số lượng bản ghi (slots) hiện hữu trong trang. |

## 4.3.3.3 Quy trình Sửa trị Bản ghi

Cấu trúc Slotted Page hỗ trợ các phép toán quản lý bản ghi với hiệu quả kĩ thuật tối ưu:

-   **Quy trình Chèn (Insertion)**: Dữ liệu nhị phân được ghi vào vùng `Free Space` theo cơ chế cấp phát ngược chiều. Một Slot mới sẽ được khởi tạo trong Slot Array để lưu trữ tham chiếu vật lý và độ dài của dữ liệu. Định danh bản ghi (RID) được xác định bằng bộ giá trị `(PageId, SlotId)`.
-   **Quy trình Truy xuất (Access)**: Hệ thống sử dụng Slot Array làm bảng chỉ mục nội tại. Việc truy xuất dữ liệu thông qua `SlotId` đạt độ phức tạp thời gian **O(1)** nhờ việc tính toán offset trực tiếp.
-   **Quy trình Thu hồi (Vacuuming)**: Khi dữ liệu bị xóa, slot tương ứng sẽ được đặt giá trị độ dài về 0. Hệ thống thực hiện quy trình tái cấu trúc dữ liệu (`Compaction`) để thu hồi không gian trống định kỳ hoặc khi có yêu cầu cấp phát mới.

Cấu trúc Header cố định kết hợp với cơ chế cấp phát ngược chiều đảm bảo tính linh hoạt cho các thay đổi về kích thước dữ liệu mà không làm thay đổi các tham chiếu logic đến bản ghi.
