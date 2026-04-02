# Ngôn ngữ Thao tác và Bảo trì Tri thức (KML)

**KML** (Knowledge Maintenance Language) cung cấp tập hợp các câu lệnh để thực thi việc chèn, cập nhật, xóa các Sự kiện (**Facts**) và quản lý tiến trình chuyển đổi dữ liệu trong cơ sở tri thức.

## 1. Khởi tạo và Chèn Sự kiện (Facts)

Hành vi chèn sự kiện cho phép nạp các thực thể cụ thể vào một Khái niệm (**Concept**) đã định nghĩa.

### 1.1. Chèn thực thể đơn lẻ

```kbql
INSERT INTO <concept_name> ATTRIBUTE (<val1>, <val2>, ...);
```

*Ví dụ:*
```kbql
INSERT INTO Patient ATTRIBUTE ('John Doe', 65, 150, 95);
```

### 1.2. Chèn thực thể hàng loạt (Bulk Insert)

Cơ chế `INSERT BULK` được tối ưu hóa để nạp tập dữ liệu lớn vào hệ thống một cách hiệu quả:

```kbql
INSERT BULK INTO <concept_name> ATTRIBUTE (
    (<val1a>, <val2a>, ...),
    (<val1b>, <val2b>, ...),
    ...
);
```

*Ví dụ:*
```kbql
INSERT BULK INTO Patient ATTRIBUTE (
    ('Alice', 45, 120, 80),
    ('Bob', 50, 130, 85)
);
```

## 2. Cập nhật và Hiệu chỉnh Sự kiện

Lệnh `UPDATE` cho phép sửa đổi giá trị các thuộc tính của các sự kiện hiện có dựa trên các điều kiện lọc xác định:

```kbql
UPDATE <concept_name> 
ATTRIBUTE (SET <var1>: <new_val1>, <var2>: <new_val2>) 
WHERE <filter_conditions>;
```

*Ví dụ:*
```kbql
UPDATE Patient 
ATTRIBUTE (SET sys: 125, dia: 82) 
WHERE name = 'Alice';
```

> [!NOTE]
> Khi một thuộc tính được cập nhật thành công, hệ thống sẽ tự động kích hoạt lại các Luật dẫn (**Rules**) liên quan để đảm bảo tính nhất quán và toàn vẹn của tri thức (Knowledge Consistency).

## 3. Loại bỏ Sự kiện

Lệnh `DELETE` thực hiện việc giải phóng các thực thể tri thức khỏi khái niệm dựa trên tiêu chí lựa chọn:

```kbql
DELETE FROM <concept_name> WHERE <filter_conditions>;
```

*Ví dụ:*
```kbql
DELETE FROM Patient WHERE age > 100 OR name = 'Test';
```

## 4. Cơ chế Chuyển đổi và Trao đổi Dữ liệu

KBMS hỗ trợ các công cụ xuất/nhập tri thức để tương tác với các định dạng lưu trữ ngoại vi tiêu chuẩn (CSV, JSON, XML).

### 4.1. Xuất dữ liệu (Export)
```kbql
EXPORT (
    CONCEPT: <name>, 
    FILE: '<path>', 
    FORMAT: {CSV | JSON | XML}
);
```

*Ví dụ:*
```kbql
EXPORT (
    CONCEPT: Patient, 
    FILE: '/var/data/patients_export.csv', 
    FORMAT: CSV
);
```

### 4.2. Nhập dữ liệu (Import)
```kbql
IMPORT (
    CONCEPT: <name>, 
    FILE: '<path>', 
    FORMAT: {CSV | JSON | XML}
);
```

*Ví dụ:*
```kbql
IMPORT (
    CONCEPT: Patient, 
    FILE: '/var/data/patients_import.json', 
    FORMAT: JSON
);
```
