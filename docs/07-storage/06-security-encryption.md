# 07.6. Quản lý Vật lý & Mã hóa

Tầng thấp nhất của Storage Engine chịu trách nhiệm giao tiếp trực tiếp với hệ điều hành và bảo vệ dữ liệu tĩnh (Data-[at-Rest](../00-glossary/01-glossary.md#at-rest)) thông qua các thuật toán mã hóa tiên tiến.

---

## 1. Ánh xạ Trang vật lý

[KBMS](../00-glossary/01-glossary.md#kbms) V3 không lưu trữ dữ liệu dưới dạng luồng liên tục mà chia thành các khối cố định (Blocks) trên đĩa.

-   **[Page](../00-glossary/01-glossary.md#page) ID to Offset:** Mọi yêu cầu truy xuất trang `PageId` được `DiskManager` chuyển đổi thành vị trí chính xác trong file thông qua công thức:
    $$Offset = PageId \times 16416$$
-   **Truy cập ngẫu nhiên ($O(1)$):** Sử dụng phương thức `FileStream.Seek` để nhảy trực tiếp đến khối dữ liệu cần thiết mà không phải quét toàn bộ tệp, đảm bảo hiệu năng ổn định ngay cả khi tệp cơ sở tri thức lên đến hàng trăm Gigabytes.

---

## 2. Mã hóa dữ liệu tĩnh (AES-256 Encryption)

Mọi dữ liệu trong [KBMS](../00-glossary/01-glossary.md#kbms) V3 đều được mã hóa ở mức trang ([Page](../00-glossary/01-glossary.md#page)-level encryption) trước khi được ghi xuống đĩa cứng để ngăn chặn việc đánh cắp dữ liệu vật lý.

-   **Thuật toán:** [AES-256](../00-glossary/01-glossary.md#aes-256) (Advanced Encryption Standard).
-   **Quy trình ghi:**
    1.  Dữ liệu 16KB được lấy từ [Buffer Pool](../00-glossary/01-glossary.md#buffer-pool).
    2.  Mã hóa bằng `MasterKey` của KB.
    3.  Thêm 32 bytes IV (Initialization Vector) và Padding.
    4.  Ghi khối **16,416 bytes** xuống đĩa.
-   **Quy trình đọc:** Thực hiện ngược lại để giải mã và đưa dữ liệu thô vào RAM.

*Bảng 7.9: Phân rã kích thước trang vật lý sau khi mã hóa [AES-256](../00-glossary/01-glossary.md#aes-256).*

| Thành phần | Kích thước (Bytes) | Ghi chú |
| :--- | :--- | :--- |
| **Dữ liệu logic** | 16,384 | 16KB dữ liệu thật (đã bao gồm 24B [Header](../00-glossary/01-glossary.md#header)). |
| **Phần bù/IV** | 32 | Phần bù mã hóa và vector khởi tạo. |
| **Khối vật lý** | **16,416** | **Kích thước thực tế chiếm dụng trên đĩa**. |


---

## 3. Quản lý tệp tin và Cấp phát

-   **Cấp phát trước (Pre-allocation):** Khi một trang mới được yêu cầu, `DiskManager` thực hiện ghi đè các bytes `0x00` (kỹ thuật Zero-fill) để giữ chỗ trên đĩa, giúp giảm thiểu hiện tượng phân mảnh tệp ([Fragmentation](../00-glossary/01-glossary.md#fragmentation)) ở cấp độ hệ điều hành.
-   **Xác nhận đĩa (Persistence):** Các thay đổi quan trọng luôn đi kèm lệnh `Flush` để ép hệ điều hành ghi trực tiếp xuống phiến đĩa vật lý, bỏ qua các tầng cache của OS để tránh mất dữ liệu khi mất điện đột ngột.

---

## 4. So sánh Dữ liệu: Trước và Sau khi Giải mã

Dưới đây là minh họa sự khác biệt giữa dữ liệu được lưu trữ "tĩnh" trên đĩa và dữ liệu "động" trong bộ nhớ RAM của [KBMS](../00-glossary/01-glossary.md#kbms).

### A. Dữ liệu trên Đĩa
Đây là những gì kẻ tấn công nhìn thấy nếu cố tình đọc file `.kdb` bằng công cụ Hex Editor.

```text
Offset    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F    Annotation
----------------------------------------------------------------------------
00000000  4A 8F 22 C1 90 EB 44 12 AE 33 01 99 FF 2B 73 88    <- 16B IV Part 1
00000010  C2 10 93 42 11 00 55 EF 88 23 12 77 66 11 9A BB    <- 16B IV Part 2
00000020  7F 1A 2C ... (Ciphertext continues ...)             <- AES Payload
00000030  D1 2F 9E ... (Looks like random noise)              <- AES Payload
```
Do được mã hóa [AES-256](../00-glossary/01-glossary.md#aes-256), dữ liệu trên đĩa không có cấu trúc dễ đọc. Các trường như `PageId` hay `LSN` hoàn toàn bị xáo trộn, ngăn chặn mọi nỗ lực khai thác dữ liệu trái phép.

### B. Dữ liệu trong RAM (Decrypted Page Buffer - 16,384B)
Sau khi `DiskManager` đọc tệp, tách IV và giải mã, dữ liệu trở về trạng thái có cấu trúc để sẵn sàng phục vụ xử lý tri thức.

```text
Offset    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F    Annotation
----------------------------------------------------------------------------
00000000  65 00 00 00 01 00 00 00 FF FF FF FF FF FF FF FF    <- [PageId: 101]
00000010  FF FF FF FF D2 3F 00 00 01 00 00 00 D2 3F 00 00    <- [FSP: 16k...]
```
Cơ chế mã hóa trang ([Page](../00-glossary/01-glossary.md#page)-level Encryption) đảm bảo dữ liệu chỉ tồn tại ở dạng tường minh trong vùng nhớ an toàn của [KBMS](../00-glossary/01-glossary.md#kbms) Studio/Server và luôn ở trạng thái "rác vô nghĩa" khi nằm trên thiết bị lưu trữ vật lý.
