# Ngôn ngữ Truy vấn Tri thức (KQL)

**KQL** (Knowledge Query Language) tập hợp các lệnh để truy xuất thông tin, thực hiện truy vấn dữ liệu và yêu cầu hệ thống thực hiện các phép suy diễn tri thức phức tạp.

## 1. Cơ chế Truy vấn Tri thức

Lệnh `SELECT` trong [KBQL](../../00-glossary/01-glossary.md#kbql) tương đương với tiêu chuẩn SQL nhưng được tối ưu hóa để tương tác với các Khái niệm ([Concept](../../00-glossary/01-glossary.md#concept)) và cấu trúc tri thức.

### 1.1. Cấu trúc Lệnh Truy vấn Toàn phần

```kbql
SELECT [<columns> | * | AGGREGATE(<var>)]
FROM <concept> [AS <alias>]
[JOIN <concept> ON <condition>]
[WHERE <filter_conditions>]
[GROUP BY <variables>] 
[HAVING <filter_conditions>]
[ORDER BY <variables> {ASC | DESC}]
[LIMIT <n> OFFSET <m>];
```

### 1.2. Các Tính năng Mở rộng

*   **Hàm Tổng hợp (Aggregate):** Tích hợp các hàm `COUNT`, `SUM`, `AVG`, `MIN`, `MAX` trên các tập thuộc tính của Concept.
*   **Mệnh đề lọc sau nhóm (HAVING):** Cho phép lọc dữ liệu sau khi đã thực hiện gom nhóm tri thức.
*   **Biểu thức Tính toán CALC():** Hỗ trợ thực thi các công thức toán học ngay trong tiến trình truy vấn.
    *Ví dụ:* `SELECT name, CALC(price * 1.1) AS price_tax FROM Product;`

## 2. Quy trình Giải quyết Tri thức (Problem Solving)

Lệnh `SOLVE` là đặc trưng của hệ thống [KBMS](../../00-glossary/01-glossary.md#kbms), dùng để kích hoạt bộ máy giải quyết vấn đề (Problem Solver) dựa trên các công thức và luật dẫn đã được định cấu hình.

### 2.1. Cú pháp thực thi

```kbql
SOLVE ON CONCEPT <name>
GIVEN <known_inputs>
FIND <target_outputs>
[SAVE]
[USING <constraints_or_rules_list>];
```

### 2.2. Cơ chế Vận hành

1.  **Dữ liệu đầu vào (Input):** Nạp các giá trị tri thức đã biết vào khối `GIVEN`.
2.  **Suy diễn (Inference):** Hệ thống áp dụng thuật toán Suy diễn tiến (**Forward Chaining**) và Giải phương trình (**Equation Solving**) để tìm giá trị các biến đích trong khối `FIND`.
3.  **Lưu trữ (Persistence):** Nếu sử dụng từ khóa `SAVE`, kết quả suy diễn sẽ được lưu trữ trực tiếp thành một Sự kiện (**[Fact](../../00-glossary/01-glossary.md#fact)**) mới.
4.  **Kết quả (Output):** Trả về kết quả sau khi đã hoàn tất quy trình suy luận logic.

## 3. Quản trị và Giám sát Hệ thống

Cung cấp các công cụ để liệt kê và kiểm tra các thành phần trong cơ sở tri thức hiện tại:

*   **SHOW CONCEPTS**: Liệt kê danh mục các Khái niệm.
*   **SHOW RULES**: Hiển thị các luật suy diễn đã định nghĩa.
*   **SHOW RELATIONS**: Liệt kê các quan hệ ngữ nghĩa giữa các khái niệm.
*   **SHOW HIERARCHIES**: Hiển thị cấu trúc cây phân cấp tri thức.
*   **SHOW OPERATORS / FUNCTIONS**: Liệt kê các toán tử và hàm số tùy biến.

## 4. Phân tích và Đặc tả Kỹ thuật

Các công cụ hỗ trợ nhà phát triển nắm bắt cấu trúc và phương thức thực thi của hệ thống:

*   **DESCRIBE {CONCEPT | RULE | ...} <name>**: Hiển thị chi tiết cấu trúc định nghĩa của đối tượng tri thức.
*   **EXPLAIN (<kbql_statement>)**: Đặc tả kế hoạch thực thi (**Execution Plan**) của câu lệnh, phục vụ việc tối ưu hóa hiệu năng truy vấn.

## 5. Truy cập Dữ liệu Siêu dữ liệu (Metadata)

Hệ thống cho phép truy vấn trực tiếp vào danh mục siêu dữ liệu để lấy thông tin về cấu trúc:
*Ví dụ:* `SELECT * FROM <concept_name>.variables;` trả về danh sách thuộc tính và kiểu dữ liệu tương ứng.
