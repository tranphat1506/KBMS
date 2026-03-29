# KBQL Data Query (DQL)

DQL (Data Query Language) là nhóm các lệnh quan trọng nhất được sử dụng để lấy thông tin từ cơ sở tri thức. KBQL hỗ trợ cú pháp `SELECT` mạnh mẽ, tích hợp khả năng tính toán thông qua `CALC()`.

## 1. SELECT Statement

Lệnh dùng để trích xuất dữ liệu từ các Khái niệm (Concepts).

### Skeleton Lệnh
```kbql
SELECT [<alias>.]<field_name> [AS <alias_name>], ...
FROM <concept_name> [<alias>]
[JOIN <other_concept> <other_alias> ON <condition>]
[WHERE <condition_expression>]
[LIMIT <n>];
```

### Ý tưởng & Chi tiết
*   **Field Selection:** Danh sách các thuộc tính cần lấy. Có thể sử dụng `<alias>.*` để lấy toàn bộ dữ liệu.
*   **AS Alias:** Đặt tên tạm thời cho các cột khi hiển thị kết quả.
*   **JOIN:** Kết hợp dữ liệu từ hai hoặc nhiều Concept khác nhau thông qua một điều kiện chung (thường là ID).
*   **WHERE:** Lọc dữ liệu dựa trên các biểu thức logic.
*   **LIMIT:** Giới hạn số lượng bản ghi trả về, hữu ích cho tập dữ liệu lớn.

### Ví dụ
```kbql
-- Lấy thông tin nhân viên cùng tên phòng ban
SELECT 
    e.name AS EmployeeName, 
    d.name AS DepartmentName
FROM Emp e 
JOIN Dept d ON e.dept_id = d.id
WHERE e.salary > 70000;
```

---

## 2. CALC() Function

Chức năng tính toán (Calculation) được tích hợp trực tiếp trong KBQL để thực hiện các phép toán phức tạp ngay trong truy vấn.

### Chi tiết & Cách hoạt động
*   **Ý tưởng:** Tách phần logic tính toán toán học phức tạp ra khỏi core engine để đảm bảo hiệu năng.
*   **Biểu thức bên trong:** Có thể bao gồm các phép toán cộng (+), trừ (-), nhân (*), chia (/) và các hàm toán học như `Sqrt()`, `Pow()`.
*   **Xử lý Dynamic:** KBMS sử dụng bộ thư viện đánh giá biểu thức (expression evaluator) để tính toán giá trị cho từng bản ghi tại thời điểm thực thi truy vấn.

### Ví dụ
```kbql
-- Tính toán giá sản phẩm sau thuế 10%
SELECT 
    name, 
    price, 
    CALC(price * 1.1) AS PriceWithTax
FROM Product;
```

---

## 3. Thuật toán Xử lý Query (Query Processing)

Khi nhận một câu lệnh `SELECT`, hệ thống sẽ thực hiện theo các bước sau:

1.  **Parsing:** Biến đổi chuỗi SQL thành cây cú pháp (AST).
2.  **Binding:** Kiểm tra xem các Concept và biến (variables) có tồn tại trong catalog hay không.
3.  **Join Processing:** Nếu có lệnh JOIN, hệ thống sử dụng thuật toán **Nested Loop Join** hoặc **Hash Join** (tùy thuộc vào cấu trúc Index hiện có).
4.  **Filtering:** Áp dụng điều kiện `WHERE` để loại bỏ các Fact không phù hợp.
5.  **Projection:** Chỉ giữ lại những cột (fields) được chỉ định trong phần `SELECT`.
6.  **Expression Evaluation:** Tính toán các biểu thức `CALC()` trên từng dòng dữ liệu trước khi trả về.

---

## 4. Truy vấn Metadata

KBQL cho phép bạn truy vấn các thông tin hệ thống của chính nó.

### Cú pháp đặc biệt
```kbql
-- Xem danh sách các biến của một Concept
SELECT * FROM <concept_name>.variables;

-- Liệt kê tất cả Concepts trong KB hiện tại
SHOW CONCEPTS IN <kb_name>;
```
