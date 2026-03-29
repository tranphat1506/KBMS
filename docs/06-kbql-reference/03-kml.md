# 06.3. Thao tác tri thức (KML)

KML là tập hợp các lệnh dùng để chèn, cập nhật, xóa các Sự kiện (Facts) và chuyển đổi dữ liệu trong Cơ sở tri thức.

---

## 1. Chèn Sự kiện (INSERT)

Lệnh chèn một hoặc nhiều Fact vào một Concept.

### Chèn một dòng đơn
```kbql
INSERT INTO <concept_name> ATTRIBUTE (<val1>, <val2>, ...);
```

### Chèn hàng loạt (Bulk Insert)
Dùng để nạp dữ liệu lớn một cách nhanh chóng.
```kbql
INSERT BULK INTO <concept_name> ATTRIBUTE (
    (<val1a>, <val2a>, ...),
    (<val1b>, <val2b>, ...),
    ...
);
```

---

## 2. Cập nhật Sự kiện (UPDATE)

Sửa đổi giá trị các biến của các Fact hiện có dựa trên điều kiện lọc.

### Cú pháp
```kbql
UPDATE <concept_name> 
ATTRIBUTE (SET <var1>: <new_val1>, <var2>: <new_val2>) 
WHERE <conditions>;
```
*Lưu ý:* Khi cập nhật một thuộc tính, hệ thống sẽ tự động chạy lại các Luật (Rules) liên quan để đảm bảo tính nhất quán của tri thức (Consistency).

---

## 3. Xóa Sự kiện (DELETE)

Loại bỏ các Fact khỏi Concept.

### Cú pháp
```kbql
DELETE FROM <concept_name> WHERE <conditions>;
```

---

## 4. Chuyển đổi Dữ liệu (IMPORT / EXPORT)

Công cụ sao lưu và phục hồi tri thức từ các tệp tin bên ngoài.

### Xuất dữ liệu (EXPORT)
```kbql
EXPORT (
    CONCEPT: <name>, 
    FILE: '<path>', 
    FORMAT: {CSV|JSON|XML}
);
```

### Nhập dữ liệu (IMPORT)
```kbql
IMPORT (
    CONCEPT: <name>, 
    FILE: '<path>', 
    FORMAT: {CSV|JSON|XML}
);
```

---

## 5. Đặc điểm của KML trong KBMS

Khác với các hệ quản trị CSDL thông thường, mọi thao tác KML trong KBMS đều có thể kích hoạt:
1.  **Reasoning Engine:** Các luật suy diễn tiến sẽ được kích hoạt ngay lập tức sau khi `INSERT` hoặc `UPDATE`.
2.  **Trigger Engine:** Các sự kiện tự động (`Trigger`) sẽ được thực thi nếu có định nghĩa cho sự kiện tương ứng.
3.  **Validation:** Dữ liệu được kiểm tra kiểu dữ liệu và các ràng buộc (Constraints) ngay tại tầng Parser và Storage.
