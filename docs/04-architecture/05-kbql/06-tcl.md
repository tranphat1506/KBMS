# Ngôn ngữ Kiểm soát Giao dịch (TCL)

**TCL** (Transaction Control Language) tập hợp các lệnh quản lý việc thực thi đồng nhất của chuỗi câu lệnh [KBQL](../../00-glossary/01-glossary.md#kbql), nhằm đảm bảo tính toàn vẹn của tri thức theo tiêu chuẩn [ACID](../../00-glossary/01-glossary.md#acid).

## 1. Định nghĩa về Giao dịch Tri thức

Giao dịch là một đơn vị công việc logic bao gồm một hoặc nhiều thao tác thực thi trên hệ quản trị [KBMS](../../00-glossary/01-glossary.md#kbms). Cơ chế này đảm bảo rằng nếu bất kỳ thành phần nào của giao dịch thất bại, toàn bộ tiến trình sẽ được hủy bỏ để duy trì trạng thái nhất quán của tri thức hiện tại.

## 2. Các Lệnh Thực thi Giao dịch

### 2.1. Khởi tạo Giao dịch (BEGIN)
```kbql
BEGIN TRANSACTION;
```
Sau lệnh này, mọi thay đổi về dữ liệu thực thể hoặc định nghĩa cấu trúc tri thức sẽ được thực thi tạm thời trong bộ đệm giao dịch (Transaction Buffer).

### 2.2. Xác nhận và Lưu trữ (COMMIT)
```kbql
COMMIT;
```
Lệnh `COMMIT` thực hiện việc xác thực toàn bộ các thay đổi trong giao dịch và lưu trữ vĩnh viễn vào hệ thống tệp tin vật lý (B+ Tree) và Danh mục hệ thống (Catalog).

### 2.3. Hủy bỏ và Khôi phục (ROLLBACK)
```kbql
ROLLBACK;
```
Lệnh `ROLLBACK` thực hiện việc hủy bỏ toàn bộ các thao tác kể từ thời điểm `BEGIN TRANSACTION`, đưa cơ sở tri thức quay về trạng thái ổn định gần nhất trước khi giao dịch bắt đầu.

## 3. Vai trò của Giao dịch trong Hệ quản trị Tri thức

1.  **Tính Nguyên tử (Atomicity):** Đảm bảo tập hợp các Sự kiện ([Fact](../../00-glossary/01-glossary.md#fact)) liên quan được nạp vào hệ thống một cách trọn vẹn (ví dụ: Thông tin định danh thực thể và các triệu chứng chẩn đoán kèm theo).
2.  **Tính Nhất quán (Consistency):** Ngăn chặn việc các Luật dẫn (Rules) thực hiện các biến đổi tri thức không đồng bộ giữa các Khái niệm ([Concept](../../00-glossary/01-glossary.md#concept)) khác nhau.
3.  **Tính Cách ly (Isolation):** Các biến động dữ liệu trong một giao dịch chưa xác nhận sẽ không ảnh hưởng đến các tiến trình truy vấn và suy diễn song hành khác cho tới khi `COMMIT` thành công.
