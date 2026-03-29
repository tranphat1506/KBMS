# Gói Tri thức Mẫu (Sample Data Pack)

Dưới đây là các bộ mẫu tri thức hoàn chỉnh cho các lĩnh vực phổ biến. Bạn có thể sao chép và thực thi trực tiếp trong KBMS Studio để bắt đầu nhanh nhất.

## 1. Lĩnh vực Tài chính (Finance & Credit Scoring)

Hệ thống tính toán lãi suất dựa trên điểm tín dụng của khách hàng.

```kbql
CREATE CONCEPT Customer (
    VARIABLES (
        name: STRING,
        credit_score: INT,
        loan_amount: DECIMAL,
        interest_rate: DECIMAL
    )
);

CREATE RULE CreditScoring
SCOPE Customer
IF credit_score >= 750
THEN SET interest_rate = 5.5;

CREATE RULE LowCreditScoring
SCOPE Customer
IF credit_score < 600
THEN SET interest_rate = 12.0;
```

---

## 2. Hệ chuyên gia Phân loại Động vật (Animal Taxonomy)

Sử dụng đặc điểm sinh học để xác định lớp động vật.

```kbql
CREATE CONCEPT Animal (
    VARIABLES (
        has_feathers: BOOLEAN,
        lays_eggs: BOOLEAN,
        gives_milk: BOOLEAN,
        class: STRING
    )
);

CREATE RULE BirdInference
SCOPE Animal
IF has_feathers = true AND lays_eggs = true
THEN SET class = 'Bird';

CREATE MammalInference
SCOPE Animal
IF gives_milk = true
THEN SET class = 'Mammal';
```

---

## 3. Hệ thống Quản lý Kho (Inventory Management)

Tự động cảnh báo khi hàng tồn kho xuống mức thấp.

```kbql
CREATE CONCEPT Product (
    VARIABLES (
        code: STRING,
        stock_quantity: INT,
        min_threshold: INT,
        needs_reorder: BOOLEAN
    )
);

CREATE RULE ReorderRule
SCOPE Product
IF stock_quantity < min_threshold
THEN SET needs_reorder = true;
```

---

## 4. Cách sử dụng Sample Pack

1.  Mở **KBMS Studio**.
2.  Tạo một Knowledge Base mới (ví dụ: `SamplesKB`).
3.  Copy các đoạn mã trên và nhấn **RUN**.
4.  Thực hiện lệnh `INSERT` dữ liệu thực tế để kiểm tra kết quả suy diễn (ví dụ: `INSERT INTO Customer ATTRIBUTE ('John', 800, 10000, 0);`).
5.  Xem kết quả `SELECT *` để thấy các biến được hệ thống tự động điền giá trị.
