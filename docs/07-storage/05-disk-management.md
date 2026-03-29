# 07.5. Quản lý Đĩa & Mã hóa (Disk Management & Encryption)

Tầng thấp nhất của Storage Engine chịu trách nhiệm giao tiếp trực tiếp với hệ điều hành và bảo vệ dữ liệu tĩnh (Data-at-Rest).

---

## 1. Ánh xạ Trang vật lý (Physical Block Mapping)

KBMS V3 không lưu trữ dữ liệu dưới dạng luồng liên tục (Stream) mà chia thành các khối cố định (Blocks).

- **Page ID to Offset**: Mọi yêu cầu đọc trang `PageId` được `DiskManager` chuyển đổi thành vị trí file bằng công thức:
  $$Offset = PageId \times 16416$$
- **Random Access ($O(1)$)**: Sử dụng phương thức `FileStream.Seek` để nhảy trực tiếp đến khối dữ liệu cần thiết mà không phải quét toàn bộ tệp, đảm bảo hiệu năng ổn định ngay cả khi tệp CSDL lên đến hàng trăm GB.

---

## 2. Mã hóa Lưu trữ (AES Encryption)

Mọi dữ liệu trong KBMS V3 đều được mã hóa ở mức trang (Page-level encryption) trước khi ghi xuống đĩa cứng.

- **Thuật toán**: AES-256.
- **Quy trình ghi**: 
    1. Lấy dữ liệu 16KB từ Buffer Pool.
    2. Mã hóa dữ liệu bằng `MasterKey`.
    3. Thêm 32 bytes IV (Initialization Vector) và Padding.
    4. Ghi khối 16,416 bytes xuống đĩa.
- **Quy trình đọc**: Thực hiện ngược lại: Đọc khối 16,416 bytes -> Giải mã -> Đưa 16KB dữ liệu thô vào RAM.

---

## 3. Đặc tả Khối dữ liệu đĩa (Disk Block Spec)

Do có lớp mã hóa, kích thước thực tế trên đĩa của một trang lớn hơn kích thước logic trong RAM.

| Thành phần | Kích thước (Bytes) | Mô tả |
| :--- | :--- | :--- |
| **Data Payload (RAM)** | 16,384 | 16KB dữ liệu thật (đã bao gồm 24B Header). |
| **AES IV/Padding (Disk)** | 32 | Phần bù mã hóa và vector khởi tạo 256-bit. |
| **Tổng cộng (On-Disk)** | **16,416** | **Kích thước một khối vật lý thực tế**. |

---

## 4. Tính Toàn vẹn & Hiệu năng

- **Flush to Disk**: Tham số `flushToDisk: true` được sử dụng trong mọi lệnh `WritePage` để yêu cầu Hệ điều hành ghi trực tiếp xuống phiến đĩa (Physical Media), tránh mất dữ liệu do cache của OS khi mất điện.
- **Zero-fill Allocation**: Khi cấp phát trang mới, `DiskManager` thực hiện ghi đè các bytes `0x00` để đảm bảo không gian đĩa được giữ chỗ trước (Pre-allocation), tránh phân mảnh tệp.
