# Bộ máy Suy diễn Tri thức (Reasoning Engine)

Bộ máy suy diễn là thành phần trung tâm của KBMS, giúp biến một cơ sở dữ liệu (Database) tĩnh thành một cơ sở tri thức (Knowledge Base) linh hoạt bằng cách áp dụng các luật logic và toán học.

## 1. Biểu diễn Tri thức (Knowledge Representation)

Trong KBMS, tri thức được biểu diễn dưới dạng các đối tượng (Concepts) và các ràng buộc (Constraints) giữa chúng.

### Các thành phần của một Concept
*   **Variables (Biến):** Các thuộc tính của đối tượng (ví dụ: `Price`, `Quantity`).
*   **Rules (Luật):** Logic `IF-THEN` để suy diễn ra giá trị hoặc sự kiện mới.
*   **Equations (Phương trình):** Các công thức toán học mô tả mối quan hệ giữa các biến.
*   **Constraints (Ràng buộc):** Các điều kiện mà dữ liệu buộc phải thỏa mãn.
*   **Relations (Quan hệ):** Mối liên kết giữa các Concept (IS-A, PART-OF, ...).

---

## 2. Quy trình Suy diễn (Reasoning Lifecycle)

Khi một bản ghi dữ liệu (Fact) được đưa vào hệ thống, bộ máy suy diễn thực hiện các bước sau:

1.  **Preprocessing (Tiền xử lý):** Nạp toàn bộ metadata của Concept, bao gồm các luật kế thừa từ IS-A và Part-Of.
2.  **Initialization (Khởi tạo):** Đưa các Fact đầu vào vào tập **GT (Ground Truth)**.
3.  **Cyclic Evaluation (Đánh giá vòng lặp):** Hệ thống liên tục quét qua tập hợp các Rules và Equations cho đến khi không còn Fact nào mới được sinh ra.
4.  **Verification (Kiểm chứng):** Sau khi suy diễn xong, hệ thống kiểm tra lại toàn bộ các `Constraints` để đảm bảo kết quả suy diễn không vi phạm bất kỳ quy tắc nào.

---

## 3. Các thực thể Quan hệ Đặc biệt

### IS-A (Kế thừa)
*   **Ý tưởng:** Nếu A **IS-A** B, thì mọi biến và luật của B sẽ được "truyền" sang A.
*   **Ví dụ:** `Square` IS-A `Rectangle`. Khi tính diện tích cho `Square`, hệ thống có thể dùng luôn luật `Area = Width * Height` của `Rectangle`.

### PART-OF (Thành phần)
*   **Ý tưởng:** Mô tả cấu trúc phân rã của đối tượng. Một đối tượng lớn được tạo từ nhiều đối tượng con.
*   **Ví dụ:** `Car` PART-OF `Engine`, `Wheel`. Hệ thống tự động ánh xạ các biến từ các phần con vào đối tượng cha để suy diễn tổng thể.

### CONSTRUCT_RELATIONS (Quy tắc quan hệ)
*   **Ý tưởng:** Định nghĩa các quy luật chung cho một mối quan hệ cho trước (ví dụ: Quan hệ `CongRuence` - Bằng nhau trong hình học).
*   **Ứng dụng:** Khi hai đối tượng thỏa mãn quan hệ này, các phương trình tương ứng (như `Object1.Size = Object2.Size`) sẽ được tự động tiêm vào hệ thống để giải.

---

## 4. Giải trình và Truy vết (Traceability)

Bộ máy suy diễn KBMS không phải là một "Black Box" (Hộp đen). Nó cung cấp khả năng giải thích lý do tại sao một giá trị được tạo ra.
*   **Derivation Trace:** Lưu trữ nguồn gốc (Source) của mỗi biến được suy diễn, bao gồm cả các biến đầu vào được dùng tại bước đó.
*   **Cơ chế:** Mỗi lần một Rule hoặc Equation kích hoạt thành công, một bản ghi Trace sẽ được đẩy vào kết quả truy vấn.
