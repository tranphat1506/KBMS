# Cấu trúc Cây B+ Tree

KBMS sử dụng cây B+ Tree làm cấu trúc dữ liệu chính để quản lý và truy xuất các Fact (bản ghi) trong mỗi Concept. B+ Tree là lựa chọn tối ưu cho việc truy vấn dữ liệu từ bộ nhớ ngoài (Disk).

## 1. Cấu trúc Hình học của Cây

Cây B+ Tree trong KBMS được chia thành hai loại Node khác nhau:

### Internal Nodes (Node Nội bộ)
*   **Vai trò:** Dẫn hướng tìm kiếm.
*   **Nội dung:** Chứa các Key (thường là ID) và các con trỏ (PageId) trỏ đến các Node con.
*   **Hành vi:** Không chứa dữ liệu thực tế của Fact.

### Leaf Nodes (Node Lá)
*   **Vai trò:** Lưu trữ dữ liệu thực tế.
*   **Nội dung:** Chứa các bộ khóa (Key) và nội dung của Fact (Tuple).
*   **Liên kết:** Mỗi Node lá đều có con trỏ `NextPageId` trỏ đến Node lá tiếp theo, cho phép hệ thống thực hiện quét dữ liệu (Scan) theo phạm vi (Range Scan) cực nhanh mà không cần duyệt lại từ gốc.

---

## 2. Các Thuật toán Cốt lõi

### Thuật toán Tìm kiếm (Search)
1.  Bắt đầu từ Root Page.
2.  So sánh khóa cần tìm với các khóa trong Node nội bộ để chọn đúng PageId của Node con.
3.  Lặp lại cho đến khi chạm tới Node lá.
4.  Tại Node lá, duyệt qua các Slot để lấy dữ liệu.
*   **Độ phức tạp:** $O(\log n)$, nơi $n$ là số lượng bản ghi.

### Thuật toán Chèn và Tách Node (Insert & Split)
Khi một Node (Lá hoặc Nội bộ) bị đầy và không thể chèn thêm bản ghi mới:
1.  **Split:** Tạo một Page mới cùng cấp.
2.  **Move:** Chuyển một nửa số lượng khóa sang Page mới.
3.  **Update Parent:** Chèn khóa phân tách vào Node cha để dẫn hướng đến Page mới vừa tạo.
4.  **Propagate:** Nếu Node cha cũng đầy, quá trình tách sẽ được lặp lại ngược lên trên cho tới tận Root. Nếu Root bị tách, chiều cao của cây sẽ tăng thêm 1 lớp.

### Sơ đồ Tách Node (Split)
![diagram_19291940.png](../assets/diagrams/diagram_19291940.png)
*Hình: diagram_19291940.png*

---

## 3. Quản lý Index trong KBMS

Trong KBMS, mỗi Concept mặc định sẽ có một "Clustered Index" dựa trên cột ID (thường là biến đầu tiên).
*   **Dữ liệu đi kèm Index:** Khác với "Secondary Index" (chỉ chứa RowId), Clustered Index của KBMS chứa **toàn bộ nội dung** của Tuple ngay tại các Node lá.
*   **Lợi ích:** Truy cập trực tiếp dữ liệu chỉ sau một lần duyệt cây, giảm thiểu số lượng Fetch Page từ Buffer Pool.

---

## 4. Ví dụ minh họa

Giả sử ta có Concept `Product` với `id` là khóa chính:
*   **Root:** Chứa khóa [100, 200, 300].
*   **Node con 1:** Chứa các sản phẩm có ID từ 1 đến 99.
*   **Node con 2:** Chứa các sản phẩm từ 100 đến 199.
*   ...
*   Khi cần tìm sản phẩm ID=150, hệ thống sẽ đi từ Root $\rightarrow$ chọn Node con 2 $\rightarrow$ đọc dữ liệu tại Node lá đó.
