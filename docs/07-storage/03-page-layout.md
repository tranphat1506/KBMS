# 07.3. Cấu trúc Trang vật lý (Physical Page Layout)

KBMS V3 sử dụng mô hình phân trang (Paging) và cấu trúc **Slotted Page** để tối ưu hóa truy xuất ngẫu nhiên và quản lý không gian trống hiệu quả cho các bản ghi có độ dài biến thiên.

---

## 1. Giải phẫu Trang (Page Anatomy)

Một trang dữ liệu chuẩn (mặc định 8KB hoặc 16KB) được phân rã thành 4 thành phần chính: **Header**, **Slot Array**, **Free Space**, và **Tuples**.

![binary_page_layout.png](../assets/diagrams/binary_page_layout.png)
*Hình 7.5: Giải phẫu cấu trúc trang nhị phân Slotted-Page của KBMS.*

*   **Header (24 Bytes)**: Nằm ở đầu trang, chứa các metadata điều hướng.
*   **Slot Array**: Danh sách các ô nhớ trỏ tới dữ liệu, phát triển theo chiều **tiến** (Forward). 
*   **Free Space**: Vùng nhớ trống nằm giữa Slot Array và Tuples.
*   **Tuples**: Dữ liệu thực thể thực tế, được chèn theo chiều **lùi** (Backward) từ cuối trang lên trên.

---

## 2. Ví dụ Tính toán (Case Study)

Giả sử chúng ta có Concept `Person` với các thuộc tính: `ID (Int 4B)`, `Age (Int 4B)`, `Salary (Double 8B)`. Tổng kích thước một bản ghi (Tuple) là **16 bytes**.

**Thông số kỹ thuật:**
1. **Page Size**: 16,384 Bytes.
2. **Header**: 24 Bytes.
3. **Slot Size**: 8 Bytes (dùng để quản lý vị trí và độ dài của Tuple).
4. **Chi phí cho mỗi bản ghi**: 16B (Dữ liệu) + 8B (Slot) = **24 Bytes**.

**Tính toán số lượng bản ghi tối đa trên một trang:**
$$MaxRecords = \lfloor (16384 - 24) / (16 + 8) \rfloor = \lfloor 16360 / 24 \rfloor = \mathbf{681}$$

**Phân bổ không gian:**
*   **Header**: 24 Bytes.
*   **Slot Array**: $681 \times 8 = 5448$ Bytes.
*   **Tuples Storage**: $681 \times 16 = 10896$ Bytes.
*   **Tổng cộng**: $24 + 5448 + 10896 = 16368$ Bytes.
*   **Free Space dư thừa**: $16384 - 16368 = \mathbf{16}$ Bytes.

---

## 3. Chi tiết Header (24 Bytes Structure)

Thông số này được đối soát trực tiếp với mã nguồn `SlottedPage.cs`:

| Byte Offset | Trường dữ liệu | Kiểu dữ liệu | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| **0 - 3** | **PageId** | int32 | ID duy nhất của trang trong tệp dữ liệu. |
| **4 - 7** | **LSN** | int32 | Số tuần tự nhật ký (Log Sequence Number) cho WAL. |
| **8 - 11** | **PrevPageId** | int32 | Con trỏ tới trang trước trong chuỗi B+ Tree. |
| **12 - 15** | **NextPageId** | int32 | Con trỏ tới trang sau phục vụ quét tuần tự. |
| **16 - 19** | **FreeSpacePointer** | int32 | Offset đánh dấu ranh giới bắt đầu của Tuple cuối cùng. |
| **20 - 23** | **TupleCount** | int32 | Tổng số lượng bản ghi (Slots) hiện có. |

---

## 4. Cơ chế Slotted Page (Slot Operations)

Để quản lý các bản ghi có độ dài khác nhau mà không làm phân mảnh trang:
*   **Slot Array**: Mỗi slot chiếm **8 bytes**: [Offset (4B) | Length (4B)].
*   **Record ID (RID)**: Một RID cụ thể sẽ bao gồm `(PageId, SlotId)`.
*   **Xóa bản ghi (Delete)**: Thay vì dịch chuyển dữ liệu, KBMS chỉ cần đánh dấu Offset và Length của Slot đó về `0`. Không gian trống sẽ được thu hồi khi thực hiện lệnh `VACUUM`.

## 4. Lợi ích kỹ thuật

1.  **Direct I/O**: Kích thước trang khớp với block size của hệ điều hành, giảm thiểu "Write Amplification".
2.  **Binary Search**: Slot Array cho phép tìm kiếm nhị phân bản ghi ngay trong trang với tốc độ cực cao.
3.  **Persistence**: Cơ chế LSN trong Header kết nối trực tiếp với hệ thống WAL, đảm bảo tính bền vững (Durability) của dữ liệu.

---

> [!IMPORTANT]
> **Lưu ý**: Việc thay đổi cấu trúc Header từ 9B sang 24B trong phiên bản V3 là bước cải tiến quan trọng để hỗ trợ phục hồi dữ liệu và quản lý giao dịch tri thức phức tạp.
