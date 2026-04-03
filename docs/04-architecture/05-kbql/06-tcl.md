# Ngôn ngữ Kiểm soát Giao dịch (TCL)

**TCL** (Transaction Control Language) tập hợp các lệnh quản lý việc thực thi đồng nhất của chuỗi câu lệnh KBQL, nhằm đảm bảo tính toàn vẹn của tri thức theo tiêu chuẩn ACID.

## 1. Định nghĩa về Giao dịch Tri thức

Giao dịch là một đơn vị công việc logic bao gồm một hoặc nhiều thao tác thực thi trên hệ quản trị KBMS. Cơ chế này đảm bảo rằng nếu bất kỳ thành phần nào của giao dịch thất bại, toàn bộ tiến trình sẽ được hủy bỏ để duy trì trạng thái nhất quán của tri thức hiện tại.

## 2. Các Lệnh Thực thi Giao dịch

### 2.1. Khởi tạo Giao dịch (BEGIN)
```kbql
BEGIN TRANSACTION;
```
*Ví dụ:*
```kbql
BEGIN TRANSACTION;
INSERT INTO Patient ATTRIBUTE ('John Doe', 30, 120, 80);
```
Sau lệnh này, mọi thay đổi về dữ liệu thực thể hoặc định nghĩa cấu trúc tri thức sẽ được thực thi tạm thời trong bộ đệm giao dịch (Transaction Buffer).

### 2.2. Xác nhận và Lưu trữ (COMMIT)
```kbql
COMMIT;
```
*Ví dụ:*
```kbql
-- Hoàn tất các thay đổi và ghi xuống đĩa
COMMIT;
```
Lệnh `COMMIT` thực hiện việc xác thực toàn bộ các thay đổi trong giao dịch và lưu trữ vĩnh viễn vào hệ thống tệp tin vật lý (B+ Tree) và Danh mục hệ thống (Catalog).

### 2.3. Hủy bỏ và Khôi phục (ROLLBACK)
```kbql
ROLLBACK;
```
*Ví dụ:*
```kbql
-- Hủy bỏ nếu phát hiện lỗi logic hoặc dữ liệu sai
ROLLBACK;
```
Lệnh `ROLLBACK` thực hiện việc hủy bỏ toàn bộ các thao tác kể từ thời điểm `BEGIN TRANSACTION`, đưa cơ sở tri thức quay về trạng thái ổn định gần nhất trước khi giao dịch bắt đầu.

## 3. Vai trò của Giao dịch trong Hệ quản trị Tri thức

1.  **Tính Nguyên tử (Atomicity):** Đảm bảo tập hợp các Sự kiện (Fact) liên quan được nạp vào hệ thống một cách trọn vẹn (ví dụ: Thông tin định danh thực thể và các triệu chứng chẩn đoán kèm theo).
2.  **Tính Nhất quán (Consistency):** Ngăn chặn việc các Luật dẫn (Rules) thực hiện các biến đổi tri thức không đồng bộ giữa các Khái niệm (Concept) khác nhau.
3.  **Tính Cách ly (Isolation):** Các biến động dữ liệu trong một giao dịch chưa xác nhận sẽ không ảnh hưởng đến các tiến trình truy vấn và suy diễn song hành khác cho tới khi `COMMIT` thành công.

## 4. Ví dụ Thực tế - Quản lý Giao dịch trong Bệnh viện

Dưới đây là các kịch bản thực tế về sử dụng giao dịch trong hệ thống KBMS:

### 4.1. Giao dịch Đăng ký Bệnh nhân Mới

```kbql
-- Bắt đầu giao dịch
BEGIN TRANSACTION;

-- Thêm bệnh nhân mới
INSERT INTO Patient ATTRIBUTE (
    'P006', 'Hoang Van F', 35, 'B+', 125, 82, 72, 36.6, '2026-04-03'
);

-- Tạo hồ sơ khám bệnh
INSERT INTO MedicalRecord ATTRIBUTE (
    'MR006', 'P006', 'Khai thác bệnh sử ban đầu', '2026-04-03'
);

-- Đặt lịch hẹn với bác sĩ
INSERT INTO Appointment ATTRIBUTE (
    'APT006', 'P006', 'D001', '2026-04-05 09:00', 'Tái khám', 'Scheduled'
);

-- Nếu mọi thứ thành công, xác nhận giao dịch
COMMIT;
-- Nếu có lỗi, sử dụng ROLLBACK để hoàn tác
```

### 4.2. Giao dịch Chuyển Kho

```kbql
-- Tạo Concept InventoryMovement
CREATE CONCEPT InventoryMovement (
    VARIABLES (
        movementId: STRING,
        productId: STRING,
        fromLocation: STRING,
        toLocation: STRING,
        quantity: INT,
        movementDate: DATETIME
    )
);

-- Giao dịch chuyển thuốc từ kho chính đến khoa dược
BEGIN TRANSACTION;

-- Giảm số lượng tại kho chính
UPDATE Product
ATTRIBUTE (SET stock: stock - 100)
WHERE productId = 'MED001';

-- Tăng số lượng tại khoa dược
INSERT INTO PharmacyStock ATTRIBUTE (
    'PH001', 'MED001', 'Khoa Dược', 100, '2026-04-03'
);

-- Ghi lại lịch sử chuyển kho
INSERT INTO InventoryMovement ATTRIBUTE (
    'MOV001', 'MED001', 'Kho Chính', 'Khoa Dược', 100, '2026-04-03 10:30'
);

-- Xác nhận giao dịch
COMMIT;
```

### 4.3. Giao dịch Xử lý Thanh toán

```kbql
-- Tạo Concept Payment
CREATE CONCEPT Payment (
    VARIABLES (
        paymentId: STRING,
        billId: STRING,
        patientId: STRING,
        amount: DECIMAL,
        paymentMethod: STRING,
        paymentDate: DATETIME,
        status: STRING
    )
);

-- Giao dịch thanh toán viện phí
BEGIN TRANSACTION;

-- Cập nhật trạng thái thanh toán
UPDATE Billing
ATTRIBUTE (SET status: 'Paid', paymentDate: '2026-04-03')
WHERE billId = 'BILL001';

-- Ghi nhận giao dịch thanh toán
INSERT INTO Payment ATTRIBUTE (
    'PAY001', 'BILL001', 'P001', 2500000, 'Cash', '2026-04-03 14:30', 'Completed'
);

-- Cập nhật công nợ bệnh nhân (nếu có)
UPDATE Patient
ATTRIBUTE (SET outstandingBalance: outstandingBalance - 2500000)
WHERE patientId = 'P001';

-- Xác nhận giao dịch
COMMIT;
```

### 4.4. Giao dịch với Xử lý Lỗi

```kbql
-- Giao dịch nhập hàng mới
BEGIN TRANSACTION;

-- Thêm lô thuốc mới
INSERT INTO Product ATTRIBUTE (
    'PRD004', 'Paracetamol 500mg', 'Thuốc', 50000, 1000, 100, 'PharmaCo', '2026-04-03'
);

-- Cập nhật kho
INSERT INTO PharmacyStock ATTRIBUTE (
    'PH004', 'PRD004', 'Kho Chính', 1000, '2026-04-03'
);

-- Giả sử có lỗi: số lượng âm
-- INSERT INTO Product ATTRIBUTE ('PRD005', 'Invalid', 'Thuốc', -1000, -100, 0, 'X', '2026-04-03');

-- Kiểm tra lỗi và rollback nếu cần
-- ROLLBACK;

-- Nếu không có lỗi
COMMIT;
```

### 4.5. Giao dịch Phức tạp - Nhiều Bước

```kbql
-- Kịch bản: Nhập viện bệnh nhân mới (nhiều bước)
BEGIN TRANSACTION;

-- Bước 1: Đăng ký bệnh nhân
INSERT INTO Patient ATTRIBUTE (
    'P007', 'Nguyen Thi G', 42, 'O+', 138, 88, 76, 36.8, '2026-04-03'
);

-- Bước 2: Phân giường bệnh
CREATE CONCEPT Bed (
    VARIABLES (bedId: STRING, ward: STRING, room: INT, bedNumber: INT, status: STRING)
);

INSERT INTO Bed ATTRIBUTE ('B001', 'Nội khoa', 301, 5, 'Occupied');

-- Bước 3: Gán bệnh nhân vào giường
UPDATE Bed
ATTRIBUTE (SET status: 'Occupied')
WHERE bedId = 'B001';

-- Bước 4: Tạo phiếu điều trị
CREATE CONCEPT TreatmentSheet (
    VARIABLES (
        sheetId: STRING,
        patientId: STRING,
        bedId: STRING,
        admitDate: DATETIME,
        primaryDoctor: STRING,
        status: STRING
    )
);

INSERT INTO TreatmentSheet ATTRIBUTE (
    'TS007', 'P007', 'B001', '2026-04-03 16:00', 'D001', 'Active'
);

-- Bước 5: Khởi tạo biểu đồ sinh tồn
CREATE CONCEPT VitalSigns (
    VARIABLES (
        recordId: STRING,
        patientId: STRING,
        bpSys: INT,
        bpDia: INT,
        pulse: INT,
        temp: DECIMAL,
        recordedAt: DATETIME
    )
);

INSERT INTO VitalSigns ATTRIBUTE (
    'VS001', 'P007', 138, 88, 76, 36.8, '2026-04-03 16:00'
);

-- Xác nhận toàn bộ quy trình
COMMIT;
```

### 4.6. Giao dịch với Savepoint (Điểm lưu)

```kbql
-- Giao dịch phức tạp với điểm hồi cứu
BEGIN TRANSACTION;

-- Thêm bệnh nhân
INSERT INTO Patient ATTRIBUTE ('P008', 'Test User', 30, 'A+', 120, 80, 70, 36.5, '2026-04-03');

-- Tạo savepoint sau khi thêm bệnh nhân
-- SAVEPOINT after_patient_insert;

-- Thêm lịch hẹn
INSERT INTO Appointment ATTRIBUTE ('APT008', 'P008', 'D001', '2026-04-04', 'Test', 'Scheduled');

-- Nếu có lỗi ở bước này, có thể quay về savepoint
-- ROLLBACK TO after_patient_insert;

-- Thay vì rollback hoàn toàn
-- ROLLBACK;

-- Hoặc tiếp tục và commit
COMMIT;
```

### 4.7. Ví dụ về Xung đột Giao dịch

```kbql
-- Session 1: Bác sĩ A đang cập nhật bệnh án
BEGIN TRANSACTION;
UPDATE Patient
ATTRIBUTE (SET sys: 135, dia: 88)
WHERE patientId = 'P001';
-- (chưa COMMIT)

-- Session 2: Y tá B cố gắng cập nhật cùng bệnh nhân
-- BEGIN TRANSACTION;
-- UPDATE Patient
-- ATTRIBUTE (SET heartRate: 75)
-- WHERE patientId = 'P001';
-- -> Sẽ bị BLOCK chờ Session 1 COMMIT hoặc ROLLBACK

-- Session 1: Hoàn tất
COMMIT;

-- Session 2: Bây giờ có thể tiếp tục
-- COMMIT;
```

### 4.8. Giao dịch với Kiểm tra Tính toàn vẹn

```kbql
-- Giao dịch với ràng buộc dữ liệu
BEGIN TRANSACTION;

-- Kiểm tra số dư tài khoản trước khi thanh toán
CREATE CONCEPT Account (
    VARIABLES (accountId: STRING, patientId: STRING, balance: DECIMAL)
);

-- Giả sử patient P001 có số dư 5,000,000
-- Bill là 2,000,000

-- Trừ tiền
UPDATE Account
ATTRIBUTE (SET balance: balance - 2000000)
WHERE patientId = 'P001' AND balance >= 2000000;

-- Nếu balance không đủ, câu lệnh trên sẽ fail
-- Kiểm tra số hàng affected
-- Nếu = 0, rollback

-- Cập nhật thanh toán
UPDATE Billing
ATTRIBUTE (SET status: 'Paid')
WHERE billId = 'BILL001' AND patientId = 'P001';

-- Commit nếu mọi thứ OK
COMMIT;
-- hoặc ROLLBACK nếu có lỗi
```
