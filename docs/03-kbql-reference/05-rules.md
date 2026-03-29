# Hệ Suy diễn & Tập luật (Reasoning & Rules)

Sức mạnh thực sự của KBMS nằm ở bộ máy Suy diễn (Inference Engine). Thay vì chỉ lưu trữ dữ liệu tĩnh, KBMS có khả năng tự động tính toán, suy luận và đưa ra kết luận dựa trên các luật đã định nghĩa.

## 1. Thuật toán Suy diễn Tiến (Forward Chaining)

KBMS sử dụng thực thi suy diễn tiến để liên tục cập nhật trạng thái của tri thức khi có dữ liệu đầu vào mới.

### Ý tưởng cốt lõi
Khi bộ sự kiện (GT - Ground Truth) thay đổi, hệ thống sẽ kích hoạt một vòng lặp:
1.  **Match:** Tìm tất cả các luật (Rules) hoặc phương trình (Equations) mà tiền đề đã sẵn sàng (đủ biến số).
2.  **Fire:** Thực thi các luật đó để tạo ra sự kiện mới hoặc gán giá trị mới.
3.  **Repeat:** Tiếp tục vòng lặp cho đến khi không còn gì để suy diễn thêm (Đạt tới điểm đóng - Closure).

---

## 2. Các thành phần của Bộ máy Suy diễn

### a. Luật Logic (IF-THEN Rules)
Dùng để mô tả các mối quan hệ nhân quả.
*   **Skeleton:** `IF <condition> THEN <action>`
*   **Ví dụ:** `IF status = 'High' THEN SET alert = true;`

### b. Quan hệ tính toán (Computation Relations)
Dùng để định nghĩa các biến phụ thuộc hoàn toàn vào các biến khác qua công thức toán học.
*   **Hành vi:** Hệ thống tự động tính toán ngay khi các biến đầu vào có giá trị.

### c. Giải phương trình (Equation Solving) - Tính năng Cao cấp
Khác với lập trình thông thường (chỉ tính xuôi `y = x + 1`), KBMS có khả năng giải ngược phương trình.
*   **Thuật toán 1D:** Sử dụng **Brent's Method** để tìm nghiệm của phương trình 1 ẩn.
*   **Thuật toán 2D:** Sử dụng **Newton-Raphson** để giải hệ phương trình 2 ẩn đồng thời.
*   **Ví dụ:** Nếu bạn định nghĩa phương trình `u = r * i`, khi biết `u` và `r`, hệ thống tự tính được `i`.

---

## 3. Quản lý phân cấp (IS-A) và Thành phần (PART-OF)

### Phân cấp (IS-A)
KBMS hỗ trợ kế thừa tri thức. Một Concept `Square` có thể kế thừa từ `Rectangle`.
*   **Ý tưởng:** Mọi luật định nghĩa cho `Rectangle` sẽ tự động được áp dụng cho `Square`.

### Thành phần (PART-OF)
Mô tả các đối tượng phức tạp được cấu thành từ các đối tượng đơn giản hơn.
*   **Ví dụ:** Một `Triangle` có 3 thành phần là `Edge`. Các luật về tam giác sẽ tham chiếu đến thuộc tính của từng cạnh.

---

## 4. Giải thích Quá trình Suy diễn (Trace)

Mọi kết luận mà KBMS đưa ra đều có thể được giải trình.
*   **Cơ chế:** Hệ thống lưu lại `DerivationTrace` cho mỗi biến được suy diễn.
*   **Thông tin Trace:** Gồm giá trị, phương thức (Luật hay Phương trình), và các biến đầu vào đã dùng.
*   **Ứng dụng:** Giúp người dùng hiểu được "Tại sao hệ thống lại đưa ra kết luận này?" (Thường dùng trong các hệ chuyên gia).

---

## 5. Ví dụ Tổng lực

Giả sử ta có Concept `TamGiac` với 3 cạnh `a, b, c` và diện tích `S`. Ta định nghĩa phương trình Heron:
`S = Sqrt(p * (p - a) * (p - b) * (p - c))` với `p = (a + b + c) / 2`.

Khi thực hiện:
```kbql
INSERT INTO TamGiac ATTRIBUTE (3, 4, 5);
SELECT S FROM TamGiac; -- Kết quả tự động là 6 qua hệ thống suy diễn.
```
