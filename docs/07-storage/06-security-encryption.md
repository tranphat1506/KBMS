# 07.6. Quản lý Vật lý & Mã hóa (Physical Disk Manager)

Tầng thấp nhất của Storage Engine chịu trách nhiệm giao tiếp trực tiếp với hệ điều hành và bảo vệ dữ liệu tĩnh (Data-at-Rest) thông qua các thuật toán mã hóa tiên tiến.

---

## 1. Ánh xạ Trang vật lý (Physical Block Mapping)

KBMS V3 không lưu trữ dữ liệu dưới dạng luồng liên tục mà chia thành các khối cố định (Blocks) trên đĩa.

-   **Page ID to Offset:** Mọi yêu cầu truy xuất trang `PageId` được `DiskManager` chuyển đổi thành vị trí chính xác trong file thông qua công thức:
    $$Offset = PageId \times 16416$$
-   **Truy cập ngẫu nhiên ($O(1)$):** Sử dụng phương thức `FileStream.Seek` để nhảy trực tiếp đến khối dữ liệu cần thiết mà không phải quét toàn bộ tệp, đảm bảo hiệu năng ổn định ngay cả khi tệp cơ sở tri thức lên đến hàng trăm Gigabytes.

---

## 2. Mã hóa dữ liệu tĩnh (AES-256 Encryption)

Mọi dữ liệu trong KBMS V3 đều được mã hóa ở mức trang (Page-level encryption) trước khi được ghi xuống đĩa cứng để ngăn chặn việc đánh cắp dữ liệu vật lý.

-   **Thuật toán:** AES-256 (Advanced Encryption Standard).
-   **Quy trình ghi:**
    1.  Dữ liệu 16KB được lấy từ Buffer Pool.
    2.  Mã hóa bằng `MasterKey` của KB.
    3.  Thêm 32 bytes IV (Initialization Vector) và Padding.
    4.  Ghi khối **16,416 bytes** xuống đĩa.
-   **Quy trình đọc:** Thực hiện ngược lại để giải mã và đưa dữ liệu thô vào RAM.

| Thành phần | Kích thước (Bytes) | Ghi chú |
| :--- | :--- | :--- |
| **Dữ liệu logic** | 16,384 | 16KB dữ liệu thật (đã bao gồm 24B Header). |
| **Phần bù/IV** | 32 | Phần bù mã hóa và vector khởi tạo. |
| **Khối vật lý** | **16,416** | **Kích thước thực tế chiếm dụng trên đĩa**. |

---

## 3. Quản lý tệp tin và Cấp phát (Allocation)

-   **Cấp phát trước (Pre-allocation):** Khi một trang mới được yêu cầu, `DiskManager` thực hiện ghi đè các bytes `0x00` (kỹ thuật Zero-fill) để giữ chỗ trên đĩa, giúp giảm thiểu hiện tượng phân mảnh tệp (Fragmentation) ở cấp độ hệ điều hành.
-   **Xác nhận đĩa (Persistence):** Các thay đổi quan trọng luôn đi kèm lệnh `Flush` để ép hệ điều hành ghi trực tiếp xuống phiến đĩa vật lý, bỏ qua các tầng cache của OS để tránh mất dữ liệu khi mất điện đột ngột.
