# KBQL Data Manipulation (DML)

DML (Data Manipulation Language) trong KBQL cho phép bạn chèn, cập nhật và xóa các Fact (sự kiện) trong một Concept. Khác với SQL truyền thống, mỗi thao tác DML trong KBMS đều có thể kích hoạt bộ máy Suy diễn (Inference Engine).

## 1. INSERT INTO

Lệnh này dùng để tạo mới một Fact vào hệ thống.

### Skeleton Lệnh
```kbql
INSERT INTO <concept_name> ATTRIBUTE (<val1>, <val2>, ..., <valN>);
```

### Ý tưởng & Chi tiết
*   **Tham số:** Các giá trị trong `ATTRIBUTE` phải tương ứng theo đúng thứ tự và kiểu dữ liệu của các biến đã định nghĩa trong `CREATE CONCEPT`.
*   **Hành vi hệ thống:**
    1.  **Validation:** Parser kiểm tra kiểu dữ liệu của từng giá trị.
    2.  **Storage:** Tầng Storage đóng gói dữ liệu thành một `Tuple` và chèn vào cây B+ Tree của Concept đó.
    3.  **Inference Activation:** Ngay sau khi chèn thành công, hệ thống sẽ quét tất cả các Rule có `SCOPE` thuộc Concept này để kiểm tra xem Fact mới có thỏa mãn điều kiện `IF` nào không.

### Ví dụ
```kbql
INSERT INTO Emp ATTRIBUTE (101, 'Alice', 1, 80000, 0.1);
```

---

## 2. UPDATE

Lệnh sửa đổi thông tin của các Fact hiện có.

### Skeleton Lệnh
```kbql
UPDATE <concept_name> 
ATTRIBUTE (SET <var_name>: <new_val>, ...) 
WHERE <condition_expression>;
```

### Ý tưởng & Thuật toán
*   **SET:** Chỉ định biến nào cần thay đổi giá trị.
*   **WHERE:** Chỉ định những Fact nào sẽ bị ảnh hưởng.
*   **Thuật toán & Luồng xử lý:**
    1.  **Search:** Hệ thống duyệt cây B+ Tree dựa trên điều kiện `WHERE` để tìm các Record ID (RID) tương ứng.
    2.  **Modification:** Cập nhật giá trị mới vào các Tuple đã tìm thấy.
    3.  **Re-Inference:** Một đặc điểm quan trọng của KBMS là khi một thuộc tính (ví dụ: `bonus_rate`) được cập nhật, hệ thống sẽ **tự động chạy lại** các Rule liên quan để cập nhật các thuộc tính phụ thuộc khác (ví dụ: `salary`).

### Ví dụ
```kbql
-- Cập nhật tỷ lệ thưởng cho nhân viên và hệ thống tự tính lại tổng lương
UPDATE Emp ATTRIBUTE (SET bonus_rate: 0.15) WHERE id = 101;
```

---

## 3. DELETE FROM

Lệnh xóa Fact khỏi hệ thống.

### Skeleton Lệnh
```kbql
DELETE FROM <concept_name> WHERE <condition_expression>;
```

### Chi tiết
*   **Hành vi:** Hệ thống tìm kiếm Fact thỏa mãn `WHERE` và đánh dấu xóa (hoặc xóa vật lý tùy cấu hình Storage).
*   **Lưu ý:** Việc xóa dữ liệu có thể làm mất đi các tiền đề cho một kết luận nào đó đã được suy diễn trước đó. Trong các phiên bản KBMS nâng cao, điều này có thể kích hoạt cơ chế "Truth Maintenance" để rút lại các kết luận sai sau khi Fact bị xóa.

### Ví dụ
```kbql
DELETE FROM Product WHERE stock = 0;
```
