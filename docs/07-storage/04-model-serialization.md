# 07.4. Ánh xạ Mô hình sang Dữ liệu nhị phân (Model Serialization)

Cỗ máy lưu trữ V3 (Storage Engine V3) đóng vai trò là xương sống bền vững cho KBMS, thực hiện việc chuyển đổi hai chiều giữa các thực thể lập trình (C# Objects) và các khối nhị phân lưu trữ trên đĩa (Binary Pages).

## 1. Chiến lược Nhất quán và Linh hoạt

Hệ thống phân tách chiến lược tuần tự hóa thành hai luồng chính dựa trên đặc thù dữ liệu:

1.  **Dữ liệu mật độ cao (ObjectInstance):** Sử dụng định dạng `Tuple` nhị phân nén với bảng Offset để tối ưu hóa hiệu năng quét (Scan) và tìm kiếm (Index).
2.  **Siêu dữ liệu & Lược đồ (Concept, Relation, Rule, KB):** Sử dụng định dạng JSON-to-Binary (UTF-8) để duy trì tính linh hoạt tối đa khi cấu trúc mô hình tri thức thay đổi.

---

## 2. Đặc tả Kỹ thuật của `ObjectInstance` Tuple

Mỗi bản ghi đối tượng (`ObjectInstance`) được gói gọn trong một đơn vị `Tuple`. Cấu trúc nhị phân của nó tuân thủ sơ đồ phân bổ bộ nhớ sau:

### Cấu trúc vật lý của Tuple
| Thành phần | Kích thước | Mô tả |
| :--- | :--- | :--- |
| **Field Count** | 2 Bytes | Số lượng trường dữ liệu có trong bản ghi. |
| **Offsets Array** | $N \times 2$ Bytes | Mảng vị trí kết thúc của từng trường (tính từ đầu Tuple). |
| **Data Payload** | Biến thiên | Chứa dữ liệu thực tế của các trường đã được mã hóa. |

### Ánh xạ Logic trường dữ liệu
Trong KBMS V3, các trường dữ liệu của một `ObjectInstance` được sắp xếp theo thứ tự cố định:
-   **Trường 0 (ID):** Chứa `GUID` (16 bytes) định danh duy nhất cho đối tượng.
-   **Trường 1 (Schema):** Một chuỗi ký tự UTF-8 chứa tên các biến (Variables) ngăn cách bởi dấu gạch đứng `|`. Đây là "snapshot" của lược đồ tại thời điểm ghi.
-   **Trường 2..N:** Chứa giá trị của các biến tương ứng dưới dạng chuỗi UTF-8.

> [!TIP]
> Việc lưu giữ tên biến (Schema) ngay trong Tuple giúp hệ thống tự phục hồi khi lược đồ của Concept bị thay đổi (Thêm/Xóa thuộc tính) mà không làm hỏng dữ liệu cũ.

---

## 3. Tuần tự hóa Siêu dữ liệu (Metadata Serialization)

Các đối tượng mang tính định nghĩa (Definitions) được quản lý bởi các Catalog chuyên biệt thông qua gói `System.Text.Json`:

### Quy trình lưu trữ Concept & KnowledgeBase
1.  **Chuyển đổi:** Đối tượng C# $\rightarrow$ Chuỗi JSON $\rightarrow$ Mảng Byte (UTF-8).
2.  **Đóng gói:** Mảng byte này được coi như một `Tuple` duy nhất và chèn vào các trang thuộc danh mục hệ thống (`catalog:concepts` hoặc `catalog:kbs`).
3.  **Phân vùng:** 
    -   `KbCatalog`: Quản lý danh sách các cơ sở tri thức toàn cục.
    -   `ConceptCatalog`: Quản lý lược đồ tri thức riêng cho từng KB.

---

## 4. Cơ chế Phục hồi Kiểu dữ liệu (Hydration & Casting)

Vì tầng lưu trữ nhị phân xử lý dữ liệu thô (Raw bytes/Strings) để đảm bảo tốc độ, việc khôi phục các kiểu dữ liệu logic (Strong Typing) được thực hiện ở tầng `V3DataRouter`:

1.  **Giải nén:** Đọc `Tuple` từ trang `SlottedPage`, tách các trường dựa trên bảng Offset.
2.  **Khớp dữ liệu:** Lấy danh sách tên biến từ Trường 1 (Schema).
3.  **Ép kiểu (Late-Binding Casting):** Đối chiếu với định nghĩa của `Concept` hiện tại để chuyển đổi chuỗi sang kiểu dữ liệu đích:
    -   `INT`/`LONG` $\rightarrow$ `long.Parse()`
    -   `FLOAT`/`DOUBLE` $\rightarrow$ `double.Parse()`
    -   `BOOLEAN` $\rightarrow$ `bool.Parse()`
    -   `DECIMAL` $\rightarrow$ `decimal.Parse()`
4.  **Xử lý tiến hóa:** Nếu Concept có thêm biến mới chưa có trong dữ liệu cũ, giá trị `null` sẽ được gán tự động để bảo toàn logic suy diễn.

![Quy trình Serialization](../assets/diagrams/io_strategy.png)
*Hình 7.3: Luồng dữ liệu qua các tầng xử lý từ Model đến Disk.*
