# Ngôn ngữ Thao tác và Bảo trì Tri thức (KML)

**KML** (Knowledge Maintenance Language) cung cấp tập hợp các câu lệnh để thực thi việc chèn, cập nhật, xóa các Sự kiện (**[Facts](../../00-glossary/01-glossary.md#fact)**) và quản lý tiến trình chuyển đổi dữ liệu trong cơ sở tri thức.

## 1. Khởi tạo và Chèn Sự kiện (Facts)

Hành vi chèn sự kiện cho phép nạp các thực thể cụ thể vào một Khái niệm (**[Concept](../../00-glossary/01-glossary.md#concept)**) đã định nghĩa.

### 1.1. Chèn thực thể đơn lẻ

```kbql
INSERT INTO <concept_name> ATTRIBUTE (<val1>, <val2>, ...);
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

## 2. Cập nhật và Hiệu chỉnh Sự kiện

Lệnh `UPDATE` cho phép sửa đổi giá trị các thuộc tính của các sự kiện hiện có dựa trên các điều kiện lọc xác định:

```kbql
UPDATE <concept_name> 
ATTRIBUTE (SET <var1>: <new_val1>, <var2>: <new_val2>) 
WHERE <filter_conditions>;
```

> [!NOTE]
> Khi một thuộc tính được cập nhật thành công, hệ thống sẽ tự động kích hoạt lại các Luật dẫn (**Rules**) liên quan để đảm bảo tính nhất quán và toàn vẹn của tri thức (Knowledge Consistency).

## 3. Loại bỏ Sự kiện

Lệnh `DELETE` thực hiện việc giải phóng các thực thể tri thức khỏi khái niệm dựa trên tiêu chí lựa chọn:

```kbql
DELETE FROM <concept_name> WHERE <filter_conditions>;
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

### 4.2. Nhập dữ liệu (Import)
```kbql
IMPORT (
    CONCEPT: <name>, 
    FILE: '<path>', 
    FORMAT: {CSV | JSON | XML}
);
```
