# TCL: Ngôn ngữ Kiểm soát Giao dịch

[TCL](../../00-glossary/01-glossary.md#tcl) là tập hợp các lệnh dùng để quản lý sự thực thi đồng nhất của các lệnh [KBQL](../../00-glossary/01-glossary.md#kbql), đảm bảo tính toàn vẹn tri thức ([ACID](../../00-glossary/01-glossary.md#acid)).

---

## 1. Một Giao dịch là gì?

Giao dịch là một đơn vị công việc bao gồm một hoặc nhiều lệnh thực thi trên [KBMS](../../00-glossary/01-glossary.md#kbms). Nếu một phần của giao dịch thất bại, toàn bộ giao dịch có thể được hủy bỏ để không làm sai lệch tri thức hiện tại.

---

## 2. Các lệnh Giao dịch

### 06.2.1. Bắt đầu Giao dịch
```kbql
BEGIN TRANSACTION;
```
Từ thời điểm này, mọi thay đổi dữ liệu hoặc định nghĩa tri thức sẽ được thực thi tạm thời trong bộ đệm giao dịch.

### 06.2.2. Xác nhận Giao dịch
```kbql
COMMIT;
```
Lệnh này xác nhận toàn bộ các thay đổi trong giao dịch và lưu trữ vĩnh viễn vào các file [B+ Tree](../../00-glossary/01-glossary.md#b-tree) và Catalog của hệ thống.

### 06.2.3. Hủy bỏ Giao dịch
```kbql
ROLLBACK;
```
Lệnh này hủy bỏ toàn bộ các lệnh từ lúc `BEGIN TRANSACTION`, đưa tri thức quay về trạng thái trước khi giao dịch bắt đầu.

---

## 3. Vai trò của Giao dịch trong

1.  **Tính Nguyên tử (Atomicity):** Đảm bảo một bộ các [Fact](../../00-glossary/01-glossary.md#fact) liên quan được nạp vào cùng lúc (ví dụ: Thông tin bệnh nhân và các triệu chứng chẩn đoán kèm theo).
2.  **Tính Nhất quán (Consistency):** Ngăn chặn việc Rules thực hiện các thay đổi tri thức không đồng bộ giữa các [Concept](../../00-glossary/01-glossary.md#concept) khác nhau.
3.  **Cách ly (Isolation):** Các giao dịch đang diễn ra không ảnh hưởng đến các truy vấn suy diễn khác cho tới khi `COMMIT` thành công.
