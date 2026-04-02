# Đặc tả Quản lý Lưu trữ Vật lý và Vùng đệm

Hệ quản trị **[KBMS](../../../00-glossary/01-glossary.md#kbms)** thực hiện việc phân tách giữa cấu trúc lưu trữ vật lý trên thiết bị ngoại vi và cấu trúc lưu trữ logic trong bộ nhớ tạm thời thông qua hai phân hệ then chốt: Bộ quản lý đĩa vật lý (**Disk Manager**) và Bộ quản lý vùng đệm (**Buffer Pool Manager**). Cơ chế này cho phép hệ thống vận hành với các tập dữ liệu có quy mô lớn vượt xa dung lượng bộ nhớ vật lý khả dụng của máy chủ.

## 1. Phân hệ Trừu tượng hóa Lưu trữ Vật lý (Disk Manager)

Phân hệ `DiskManager` chịu trách nhiệm tương tác trực tiếp với hệ thống tệp tin của hệ điều hành để thực thi các thao tác đọc và ghi dữ liệu theo đơn vị trang (**Page**) có kích thước cố định.

1.  **Cơ chế Truy cập Ngẫu nhiên**: Hệ thống sử dụng phương thức định vị con trỏ tệp tin (**Seek**) để truy xuất trực tiếp vị trí vật lý của trang dữ liệu thông qua định danh trang (**PageId**). Địa chỉ vị trí vật lý (Offset) được xác định theo quy tắc hình thức:
    $$Offset = PageId \times PageSize_{Physical}$$
    Trong đó, $PageSize_{Physical}$ bao gồm 16,384 Bytes dữ liệu logic và 32 Bytes dành cho siêu dữ liệu (**Metadata**) mã mã hóa.
2.  **Mã hóa Dữ liệu Tĩnh (Encryption at Rest)**: Các trang dữ liệu được mã hóa theo tiêu chuẩn **AES-256-CBC** trước khi thực hiện thao tác ghi xuống thiết bị lưu trữ. Khi yêu cầu đọc trang được kích hoạt, dữ liệu sẽ được giải mã trước khi chuyển nạp vào khung trang trong bộ nhớ đệm, đảm bảo tính bảo mật và toàn vẹn tuyệt đối cho tri thức hệ thống.

## 2. Phân hệ Điều phối Vùng nhớ đệm (Buffer Pool Manager)

Phân hệ `BufferPoolManager` đóng vai trò là lớp quản trị bộ nhớ tạm thời, duy trì danh sách các khung trang (Page Frames) trong RAM nhằm tối ưu hóa hiệu năng nhập/xuất (I/O).

![Sơ đồ Vùng đệm](../../assets/diagrams/buffer_pool_v3.png)
*Hình 4.12: Quy trình điều phối trang dữ liệu giữa bộ nhớ RAM và thiết bị lưu trữ vật lý.*

1.  **Giải thuật Thay thế LRU (Least Recently Used)**: Khi dung lượng vùng đệm đạt ngưỡng giới hạn, hệ thống thực thi giải thuật LRU để xác định và giải phóng trang dữ liệu ít được truy cập nhất. Nếu trang dữ liệu có dấu hiệu biến động nội dung (**Dirty Page**), hệ thống bắt buộc thực hiện quy trình đồng bộ hóa ghi xuống đĩa trước khi thu hồi khung trang.
2.  **Cơ chế Ghim trang (Pinning)**: Để duy trì tính nhất quán trong quá trình xử lý đa nhiệm, KBMS sử dụng tham số `PinCount`. Các trang đang bị chiếm dụng bởi các phân hệ cấp cao sẽ được tăng giá trị ghim. Bộ quản lý vùng đệm cam kết không giải phóng các trang có chỉ số ghim lớn hơn 0, đảm bảo an toàn dữ liệu trong các tiến trình suy diễn song hành.

## 3. Đặc tả Thông số Kỹ thuật Hạ tầng Lưu trữ

Dưới đây là các thông số cấu hình tiêu chuẩn của hạ tầng lưu trữ KBMS:

*Bảng 4.1: Đặc tả thông số kỹ thuật của hệ thống lưu trữ*
| Tham số Kỹ thuật | Giá trị Đặc tả | Mô tả Chức năng |
| :--- | :--- | :--- |
| **Kích thước Trang (Logic)** | 16,384 Bytes | Kích thước dữ liệu khả dụng của một trang phân khe (Slotted Page). |
| **Kích thước Khối (Vật lý)** | 16,416 Bytes | Kích thước lưu trữ thực tế bao gồm dữ liệu và siêu dữ liệu bảo mật. |
| **Dung lượng Vùng đệm** | 100 Khung trang | Số lượng trang tối đa được duy trì đồng thời trong bộ nhớ RAM. |
| **Giao thức Mã hóa** | AES-256-CBC | Thuật toán bảo mật dữ liệu tri thức trên phương tiện lưu trữ vật lý. |
