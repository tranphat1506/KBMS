# 07.2. Giải phẫu Trang vật lý (Page Anatomy)

KBMS V3 sử dụng mô hình phân trang (Paging) và cấu trúc **Slotted Page** để tối ưu hóa truy xuất ngẫu nhiên và quản lý không gian trống hiệu quả cho các bản ghi có độ dài biến thiên.

---

## 1. Cấu trúc nội tại của Trang (Internal Structure)

Một trang dữ liệu chuẩn 16KB được phân rã thành 4 thành diện diện tích chính: **Header**, **Slot Array**, **Free Space**, và **Tuples**.

![binary_page_layout.png](../assets/diagrams/binary_page_layout.png)
*Hình 7.2: Giải phẫu cấu trúc trang nhị phân Slotted-Page của KBMS.*

*   **Header (24 Bytes)**: Nằm ở đầu trang, chứa các metadata điều phối.
*   **Slot Array**: Danh sách các "khe cắm" trỏ tới dữ liệu, phát triển theo chiều **tiến** (từ đầu trang xuống). Mỗi slot chiếm 8 bytes: `[Offset (4B) | Length (4B)]`.
*   **Free Space**: Vùng nhớ trống nằm giữa Slot Array và Tuples.
*   **Tuples (Dữ liệu)**: Nội dung thực tế, được chèn theo chiều **lùi** (từ cuối trang ngược lên trên) để tận dụng tối đa không gian trống ở giữa.

---

## 2. Đặc tả Header (24-Byte Layout)

Cấu trúc Header được thiết kế để hỗ trợ tối đa cho việc phục hồi dữ liệu (Recovery) và điều hướng B+ Tree:

| Byte Offset | Trường dữ liệu | Ý nghĩa |
| :--- | :--- | :--- |
| **0 - 3** | **PageId** | ID duy nhất của trang trong tệp dữ liệu. |
| **4 - 7** | **LSN** | Log Sequence Number phục vụ cơ chế ghi nhật ký WAL. |
| **8 - 11** | **PrevPageId** | Con trỏ tới trang trước trong chuỗi lá B+ Tree. |
| **12 - 15** | **NextPageId** | Con trỏ tới trang sau phục vụ quét tuần tự (Scan). |
| **16 - 19** | **FreeSpacePointer** | Offset đánh dấu ranh giới bắt đầu của vùng dữ liệu. |
| **20 - 23** | **TupleCount** | Tổng số lượng bản ghi (Slots) hiện có trong trang. |

---

## 3. Quản lý bản ghi biến thiên (Slotted Page Logic)

Cơ chế này cho phép KBMS xử lý các bản ghi có kích thước khác nhau (ví dụ: một đối tượng có tên ngắn và một đối tượng có tên dài) mà không cần định nghĩa lại cấu trúc trang:

1.  **Chèn bản ghi:** Dữ liệu mới được đẩy vào cuối vùng `Free Space`, đồng thời một Slot mới được thêm vào đầu trang để trỏ đến vị trí đó.
2.  **Định danh RID (Record ID):** Một bản ghi được xác định duy nhất bởi cặp `(PageId, SlotId)`.
3.  **Xóa & Thu hồi:** Khi xóa, Slot tương ứng được đánh dấu là trống. KBMS cung cấp lệnh `VACUUM` để dồn dịch dữ liệu và thu hồi không gian trống bị phân mảnh.

---

## 4. Lợi ích của kiến trúc 16KB

1.  **Block Alignment:** 16KB là bội số của block size trên hầu hết các dòng SSD hiện đại, giúp tránh hiện tượng "Read-Modify-Write".
2.  **Binary Search:** Nhờ Slot Array có kích thước cố định, việc tìm kiếm một bản ghi trong trang có độ phức tạp là **O(log N)** bằng tìm kiếm nhị phân trên mảng Slot.
