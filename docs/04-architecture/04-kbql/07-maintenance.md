# 06.7. Bảo trì Hệ thống

Hệ quản trị [KBMS](../../00-glossary/01-glossary.md#kbms) cung cấp một bộ các lệnh quản trị chuyên sâu giúp tối ưu hóa bộ nhớ, chỉ mục và kiểm tra tính nhất quán của tri thức.

---

## 1. Lệnh Bảo trì

Lệnh `MAINTENANCE` được dùng để thực hiện các tác vụ dọn dẹp và sửa lỗi hệ thống cấp thấp.

### Cú pháp
```kbql
MAINTENANCE (
    {VACUUM | REINDEX | CHECK CONSISTENCY} [ON CONCEPT <name>],
    ...
);
```

---

## 2. Các hành động cụ thể

### Thu gọn Bộ nhớ (VACUUM)
Khi [Fact](../../00-glossary/01-glossary.md#fact) bị xóa, không gian bộ nhớ trong cây [B+ Tree](../../00-glossary/01-glossary.md#b-tree) không được giải phóng ngay lập tức. Lệnh `VACUUM` thực hiện việc sắp xếp lại các [Page](../../00-glossary/01-glossary.md#page) dữ liệu và thu hồi dung lượng đĩa cứng dư thừa.
*Ví dụ:* `MAINTENANCE (VACUUM);`

### Đánh lại Chỉ mục
Cập nhật lại toàn bộ các Index gắn với [Concept](../../00-glossary/01-glossary.md#concept). Hữu ích khi hệ thống gặp sự cố mất điện hoặc phát hiện sai lệch chỉ mục.
*Ví dụ:* `MAINTENANCE (REINDEX ON CONCEPT Patient);`

### Kiểm tra Tính nhất quán
Kiểm tra xem các liên kết giữa Fact, [Rule](../../00-glossary/01-glossary.md#rule) và Hierarchy có còn hợp lệ hay không. Đảm bảo tri thức không bị mâu thuẫn.
*Ví dụ:* `MAINTENANCE (CHECK CONSISTENCY ON CONCEPT (*));`

---

## 3. Tần suất thực hiện bản khuyến nghị

*   **VACUUM:** Sau mỗi lần xóa dữ liệu quy mô lớn (Batch delete).
*   **REINDEX:** Định kỳ hàng tuần hoặc sau khi thực hiện việc thay đổi Metadata phức tạp.
*   **CHECK CONSISTENCY:** Thực hiện trước khi triển khai hệ thống suy diễn vào môi trường quan trọng.
