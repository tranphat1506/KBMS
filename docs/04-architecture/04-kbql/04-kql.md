# 06.4. Truy vấn và Suy diễn

[KQL](../../00-glossary/01-glossary.md#kql) là tập hợp các lệnh dùng để lấy thông tin, thực hiện truy vấn dữ liệu và yêu cầu hệ thống thực hiện các phép suy diễn tri thức phức tạp.

---

## 1. Truy vấn Dữ liệu

Lệnh `SELECT` trong [KBQL](../../00-glossary/01-glossary.md#kbql) tương tự SQL nhưng được tối ưu để làm việc với các [Concept](../../00-glossary/01-glossary.md#concept) và thuộc tính tri thức.

### Cú pháp đầy đủ
```kbql
SELECT [<columns> | * | AGGREGATE(<var>)]
FROM <concept> [AS <alias>]
[JOIN <concept> ON <condition>]
[WHERE <conditions>]
[GROUP BY <vars>] 
[HAVING <condition>]
[ORDER BY <vars> {ASC|DESC}]
[LIMIT <n> OFFSET <m>];
```

### Các tính năng đặc biệt
*   **Hàm Tổng hợp (Aggregate):** Hỗ trợ `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`.
*   **HAVING:** Mệnh đề lọc sau khi đã thực hiện `GROUP BY`.
*   **Hàm Tính toán CALC():** Thực hiện các biểu thức toán học phức tạp ngay trong lúc truy vấn.
    *Ví dụ:* `SELECT name, CALC(price * 1.1) AS price_tax FROM Product;`

---

## 2. Giải quyết Tri thức

Đây là lệnh đặc trưng của [KBMS](../../00-glossary/01-glossary.md#kbms), dùng để kích hoạt bộ máy giải quyết vấn đề (Problem Solver) dựa trên các công thức và luật đã định nghĩa.

### Cú pháp
```kbql
SOLVE ON CONCEPT <name>
GIVEN <inputs>
FIND <outputs>
[SAVE]
[USING <rule_or_relation_list>];
```

### Cách hoạt động
1.  **Input:** Nạp các giá trị đã biết vào phần `GIVEN`.
2.  **Inference:** Hệ thống sử dụng [Forward Chaining](../../00-glossary/01-glossary.md#forward-chaining) và [Equation](../../00-glossary/01-glossary.md#equation) Solving để tìm giá trị của các biến trong phần `FIND`.
3.  **Persist (Tùy chọn):** Nếu có từ khóa `SAVE`, các kết quả vừa tìm thấy sẽ được lưu trực tiếp thành một [Fact](../../00-glossary/01-glossary.md#fact) mới vào Concept tương ứng.
4.  **Result:** Trả về kết quả sau khi đã suy diễn thành công.

---

## 3. Kiểm tra Hệ thống

Dùng để liệt kê các thành phần đang có trong Cơ sở Tri thức.

*   **SHOW CONCEPTS**: Liệt kê tất cả các Khái niệm.
*   **SHOW RULES**: Liệt kê các luật suy diễn.
*   **SHOW RELATIONS**: Liệt kê các quan hệ giữa các Concept.
*   **SHOW [HIERARCHIES](../../00-glossary/01-glossary.md#hierarchies)**: Xem cấu trúc phân cấp cây tri thức.
*   **SHOW OPERATORS / FUNCTIONS**: Xem các toán tử và hàm tùy biến.

---

## 4. Phân tích & Mô tả

Công cụ dành cho lập trình viên để hiểu sâu về cấu trúc và cách thực thi của hệ thống.

*   **DESCRIBE {CONCEPT|KB|[RULE](../../00-glossary/01-glossary.md#rule)|...} <name>**: Hiển thị chi tiết cấu trúc định nghĩa của một đối tượng.
*   **EXPLAIN (<kbql_statement>)**: Hiển thị kế hoạch thực thi ([Execution Plan](../../00-glossary/01-glossary.md#execution-plan)) của một câu lệnh, rất hữu ích để tối ưu hóa truy vấn.

---

## 5. Truy vấn Metadata

Bạn có thể truy vấn trực tiếp vào danh mục hệ thống để lấy thông tin metadata.
*Ví dụ:* `SELECT * FROM <concept_name>.variables;` trả về danh sách các biến và kiểu dữ liệu của Concept đó.
