# 4.7.6. Kịch bản Thực thi và Ví dụ Chẩn đoán Tri thức

Tài liệu này trình bày hai kịch bản thực thi điển hình minh họa cho khả năng nội suy và suy diễn tự động của hệ quản trị KBMS. Các ví dụ được thiết kế để kiểm chứng sự phối hợp giữa Luật dẫn, Phương trình và Hệ thống Phân cấp [1].

## 1. Kịch bản: Chẩn đoán Y tế On-the-Fly

Mục tiêu là chẩn đoán tình trạng tăng huyết áp (`is_hypertension`) dựa trên các chỉ số huyết áp tâm thu (`sys`) và tâm trương (`dia`).

-   **Mô hình Tri thức**:
```kbql
CREATE CONCEPT Patient (VARIABLES (name: STRING, sys: INT, dia: INT, is_hypertension: BOOLEAN));
CREATE RULE CalcHighBP SCOPE Patient IF sys > 140 OR dia > 90 THEN SET is_hypertension = true;
```
-   **Truy vấn nội suy**:
```kbql
-- Nạp dữ liệu cơ sở
INSERT INTO Patient ATTRIBUTE ('John Doe', 150, 95);

-- Y cầu nội suy biến 'is_hypertension' trực tiếp trong kết quả
SELECT name, SOLVE(is_hypertension) FROM Patient;
```
-   **Giải thích luồng chạy**:
    -   Bộ máy kích hoạt `ResolveTarget(is_hypertension)`.
    -   Tìm thấy luật `CalcHighBP`.
    -   Thẩm định điều kiện: `sys(150) > 140` $\rightarrow$ Thỏa mãn.
    -   Kết quả: `is_hypertension` được xác định là `true` và trả về bảng kết quả.

## 2. Kịch bản: Tính toán Lực vật lý (Equation Solve)

Mục tiêu là tính toán lực hấp dẫn giữa hai vật thể dựa trên các khối lượng và khoảng cách.

-   **Mô hình Tri thức**:
```kbql
CREATE CONCEPT PhysicsBody (VARIABLES (m1: DOUBLE, m2: DOUBLE, r: DOUBLE, f: DOUBLE));
CREATE FUNCTION Grav(m1, m2, r) RETURNS DOUBLE BODY '(6.67 * m1 * m2) / (r * r)';
CREATE RULE CalcForce SCOPE PhysicsBody IF m1 > 0 AND m2 > 0 THEN SET f = Grav(m1, m2, r);
```
-   **Truy vấn nội suy**:
```kbql
-- Nạp dữ liệu (biết f, m1, m2, cần tìm r)
INSERT INTO PhysicsBody ATTRIBUTE (100.0, 50.0, 0, 0.005);

-- Kích hoạt bộ giải EquationResolver cho biến 'r'
SELECT SOLVE(r) FROM PhysicsBody;
```
-   **Giải thích luồng chạy**:
    -   `ResolveTarget(r)` phát hiện biến `r` nằm trong biểu thức của hàm `Grav` quy định giá trị cho `f`.
    -   `EquationResolver` kích hoạt bộ giải để thực hiện xấp xỉ số học.
    -   Kết quả: Biến `r` được xác định chính xác và trình diễn trên giao diện người dùng.

Các kịch bản trên chứng minh rằng KBMS có thể xử lý các lớp tri thức đa tầng mà người dùng không cần thiết lập các quy trình tính toán thủ công bên ngoài hệ thống.
