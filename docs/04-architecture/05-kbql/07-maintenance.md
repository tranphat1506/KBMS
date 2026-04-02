# Quản trị và Bảo trì Cơ sở Tri thức

**MAINTENANCE** là một lệnh chuyên biệt trong **[KBQL](../../00-glossary/01-glossary.md#kbql)**, được thiết kế để thực thi các tác vụ dọn dẹp, tối ưu hóa hiệu năng lưu trữ và kiểm chứng tính nhất quán của hệ thống tri thức.

## 1. Đặc tả Lệnh Bảo trì

Lệnh `MAINTENANCE` được thiết kế để thực thi các tác vụ dọn dẹp và sửa đổi hệ thống ở cấp độ vật lý:

```kbql
MAINTENANCE (
    {VACUUM | REINDEX | CHECK CONSISTENCY} [ON CONCEPT <name>],
    ...
);
```

## 2. Đặc tả các Hoạt động Quản trị

### 2.1. Giải phóng và Tối ưu hóa Bộ nhớ (VACUUM)

Khi một Sự kiện (**[Fact](../../00-glossary/01-glossary.md#fact)**) bị xóa, không gian lưu trữ trong cấu trúc cây **[B+ Tree](../../00-glossary/01-glossary.md#b-tree)** sẽ không được giải phóng ngay lập tức để tiết kiệm chi phí I/O. Lệnh `VACUUM` thực hiện việc tái cơ cấu các Trang dữ liệu (**[Page](../../00-glossary/01-glossary.md#page)**) và thu hồi dung lượng đĩa cứng dư thừa.

### 2.2. Tái thiết lập Chỉ mục (REINDEX)

Hoạt động cập nhật toàn diện các cấu trúc chỉ mục (**Index**) liên kết với Khái niệm (**[Concept](../../00-glossary/01-glossary.md#concept)**). Hoạt động này được khuyến nghị thực hiện sau khi hệ thống gặp sự cố mất điện hoặc phát hiện dấu hiệu sai lệch dữ liệu chỉ mục.

### 2.3. Kiểm tra Tính nhất quán Tri thức (CHECK CONSISTENCY)

Tiến trình kiểm chứng các liên kết logic giữa Sự kiện, Luật dẫn (**[Rule](../../00-glossary/01-glossary.md#rule)**) và Hệ thống Phân cấp (**Hierarchy**). Hoạt động này đảm bảo mạng lưới tri thức không tồn tại các mâu thuẫn hình thức hoặc đứt gãy tham chiếu.

## 3. Khuyến nghị Về Chu kỳ Bảo trì

1.  **VACUUM:** Thực thi sau các đợt xóa dữ liệu hàng loạt (Batch deletion) để tối ưu hóa không gian lưu trữ.
2.  **REINDEX:** Thực thi định kỳ hoặc sau các thao tác thay đổi Siêu dữ liệu (Metadata) phức tạp.
3.  **CHECK CONSISTENCY:** Thực thi bắt buộc trước khi triển khai hệ thống suy diễn vào các kịch bản kiểm thử hoặc môi trường vận hành thực tế.
