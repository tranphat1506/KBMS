# TCL: Ngôn ngữ Kiểm soát Giao dịch (Transaction Control Language)

TCL là tập hợp các lệnh dùng để quản lý sự thực thi đồng nhất của các lệnh KBQL, đảm bảo tính toàn vẹn tri thức (ACID).

---

## 1. Một Giao dịch là gì? (Transaction)

Giao dịch là một đơn vị công việc bao gồm một hoặc nhiều lệnh thực thi trên KBMS. Nếu một phần của giao dịch thất bại, toàn bộ giao dịch có thể được hủy bỏ để không làm sai lệch tri thức hiện tại.

---

## 2. Các lệnh Giao dịch

### Bắt đầu Giao dịch (BEGIN TRANSACTION)
```kbql
BEGIN TRANSACTION;
```
Từ thời điểm này, mọi thay đổi dữ liệu hoặc định nghĩa tri thức sẽ được thực thi tạm thời trong bộ đệm giao dịch.

### Xác nhận Giao dịch (COMMIT)
```kbql
COMMIT;
```
Lệnh này xác nhận toàn bộ các thay đổi trong giao dịch và lưu trữ vĩnh viễn vào các file B+ Tree và Catalog của hệ thống.

### Hủy bỏ Giao dịch (ROLLBACK)
```kbql
ROLLBACK;
```
Lệnh này hủy bỏ toàn bộ các lệnh từ lúc `BEGIN TRANSACTION`, đưa tri thức quay về trạng thái trước khi giao dịch bắt đầu.

---

## 3. Vai trò của Giao dịch trong KBMS

1.  **Tính Nguyên tử (Atomicity):** Đảm bảo một bộ các Fact liên quan được nạp vào cùng lúc (ví dụ: Thông tin bệnh nhân và các triệu chứng chẩn đoán kèm theo).
2.  **Tính Nhất quán (Consistency):** Ngăn chặn việc Rules thực hiện các thay đổi tri thức không đồng bộ giữa các Concept khác nhau.
3.  **Cách ly (Isolation):** Các giao dịch đang diễn ra không ảnh hưởng đến các truy vấn suy diễn khác cho tới khi `COMMIT` thành công.
