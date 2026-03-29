# KBQL Data Definition (DDL)

DDL (Data Definition Language) cung cấp các lệnh để định nghĩa cấu trúc tri thức, bao gồm việc tạo Cơ sở Tri thức (Knowledge Base), Khái niệm (Concept) và các Luật (Rule).

## 1. CREATE KNOWLEDGE BASE

Lệnh này khởi tạo một vùng chứa logic mới cho toàn bộ dữ liệu và luật của bạn.

### Skeleton Lệnh
```kbql
CREATE KNOWLEDGE BASE <kb_name> [DESCRIPTION "<description_text>"];
```

### Ý tưởng & Chi tiết
*   **kb_name:** Tên định danh của KB, không được chứa khoảng trắng.
*   **DESCRIPTION:** Một chuỗi văn bản mô tả mục đích của KB này, hỗ trợ việc quản lý metadata.
*   **Hành vi hệ thống:** Khi thực thi, KBMS sẽ tạo một thư mục tương ứng trong `data/` và khởi tạo các file catalog (`KbCatalog`, `ConceptCatalog`, `UserCatalog`).

### Ví dụ
```kbql
CREATE KNOWLEDGE BASE MedicalKB DESCRIPTION "Hệ thống chẩn đoán bệnh lý.";
```

---

## 2. CREATE CONCEPT

Định nghĩa một thực thể hoặc lớp đối tượng trong hệ thống. `Concept` tương tự như một `Table` trong SQL nhưng linh hoạt hơn trong việc tích hợp vào bộ máy suy diễn.

### Skeleton Lệnh
```kbql
CREATE CONCEPT <concept_name> (
    VARIABLES (
        <var_name>: <data_type>,
        ...
    )
);
```

### Chi tiết các thành phần
*   **concept_name:** Tên của khái niệm (ví dụ: `Patient`, `Product`).
*   **VARIABLES:** Danh sách các thuộc tính định nghĩa khái niệm đó.
*   **data_type:** Hỗ trợ các kiểu dữ liệu cơ bản: `INT`, `STRING`, `DECIMAL`, `BOOLEAN`, `DATETIME`.

### Thuật toán Lưu trữ
Khi một `Concept` được tạo:
1.  **Metadata Registration:** Ghi lại cấu trúc biến vào `ConceptCatalog`.
2.  **Storage Allocation:** Hệ thống cấp phát một `StoragePool` và khởi tạo một cây B+ Tree trống để lưu trữ các Fact (dữ liệu) của Concept này.

### Ví dụ
```kbql
CREATE CONCEPT Patient (
    VARIABLES (
        id: INT,
        name: STRING,
        age: INT,
        symptom: STRING
    )
);
```

---

## 3. CREATE RULE

Đây là thành phần quan trọng nhất định nghĩa logic suy diễn của hệ thống. Luật (Rule) cho phép hệ thống tự động cập nhật hoặc suy luận thêm thông tin dựa trên các Fact hiện có.

### Skeleton Lệnh
```kbql
CREATE RULE <rule_name>
SCOPE <concept_name>
IF <condition_expression>
THEN <action_expression>;
```

### Ý tưởng & Thuật toán
*   **SCOPE:** Giới hạn phạm vi hoạt động của luật trên một Concept cụ thể.
*   **IF (Tiền đề):** Một biểu thức logic trả về boolean. Nếu điều kiện này đúng, luật sẽ được kích hoạt.
*   **THEN (Hệ quả):** Hành động thực thi khi điều kiện IF được thỏa mãn. Thường là lệnh `SET` để gán giá trị mới cho biến hoặc kích hoạt một sự kiện khác.
*   **Thuật toán Suy diễn Tiến (Forward Chaining):** 
    *   Hệ thống đăng ký Rule vào `ReasoningEngine`.
    *   Mỗi khi có lệnh `INSERT` hoặc `UPDATE` vào Concept nằm trong `SCOPE`, Engine sẽ kiểm tra điều kiện `IF`.
    *   Nếu `IF` đúng, `THEN` sẽ được thực thi ngay lập tức, có thể dẫn đến việc kích hoạt các luật khác (hiệu ứng lan truyền).

### Ví dụ
```kbql
-- Tự động tính toán trạng thái sức khỏe dựa trên nhiệt độ
CREATE RULE CheckFever
SCOPE Patient
IF temperature > 37.5
THEN SET diagnosis = 'Fever';
```

---

## 4. Các Lệnh Quản lý Khác

*   **USE <kb_name>:** Chuyển đổi ngữ cảnh làm việc sang KB được chỉ định.
*   **DROP KNOWLEDGE BASE <kb_name>:** Xóa toàn bộ dữ liệu và cấu trúc của KB.
*   **DROP CONCEPT <concept_name>:** Xóa một Concept và toàn bộ Fact liên quan.
