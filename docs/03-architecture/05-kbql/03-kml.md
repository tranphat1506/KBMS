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

## 5. Ví dụ Thực tế - Quản lý Bệnh nhân

Dưới đây là kịch bản hoàn chỉnh về việc quản lý dữ liệu bệnh nhân trong hệ thống y tế:

```kbql
-- Thiết lập: Tạo Concept Patient
CREATE CONCEPT Patient (
    VARIABLES (
        patientId: STRING,
        name: STRING,
        age: INT,
        bloodType: STRING,
        sys: INT,           -- Huyết áp tâm thu
        dia: INT,           -- Huyết áp tâm trương
        heartRate: INT,
        temperature: DECIMAL,
        lastVisit: DATE,
        is_critical: BOOLEAN
    ),
    CONSTRAINTS (
        age >= 0 AND age <= 150,
        sys > 0 AND dia > 0,
        sys > dia,
        heartRate > 30 AND heartRate < 220,
        temperature >= 35.0 AND temperature <= 42.0
    )
);

-- Kịch bản 1: Thêm bệnh nhân mới (INSERT đơn lẻ)
INSERT INTO Patient ATTRIBUTE (
    'P001', 'Nguyen Van A', 45, 'A+', 120, 80, 72, 36.5, '2026-04-01'
);

-- Kịch bản 2: Thêm hàng loạt bệnh nhân (BULK INSERT)
INSERT BULK INTO Patient ATTRIBUTE (
    ('P002', 'Tran Thi B', 32, 'B+', 115, 75, 68, 36.6, '2026-04-02'),
    ('P003', 'Le Van C', 58, 'O+', 145, 95, 88, 37.2, '2026-04-02'),
    ('P004', 'Pham Thi D', 28, 'AB+', 118, 78, 70, 36.4, '2026-04-03'),
    ('P005', 'Hoang Van E', 67, 'A+', 155, 105, 92, 38.1, '2026-04-03')
);

-- Kịch bản 3: Cập nhật thông tin bệnh nhân
-- Cập nhật chỉ số sinh tồn cho bệnh nhân P003
UPDATE Patient
ATTRIBUTE (SET sys: 140, dia: 90, heartRate: 85)
WHERE patientId = 'P003';

-- Cập nhật nhiều thuộc tính cùng lúc
UPDATE Patient
ATTRIBUTE (
    SET sys: 130,
        dia: 85,
        heartRate: 75,
        temperature: 36.7
)
WHERE patientId = 'P002';

-- Kịch bản 4: Xóa bệnh nhân khỏi hệ thống
-- Xóa bệnh nhân có dữ liệu lỗi
DELETE FROM Patient WHERE age < 0 OR sys < dia;

-- Xóa bệnh nhân đã chuyển đi
DELETE FROM Patient WHERE patientId = 'P999';

-- Kịch bản 5: Xuất báo cáo bệnh nhân高血压 (Huyết áp cao)
EXPORT (
    CONCEPT: Patient,
    FILE: '/reports/hypertension_patients.csv',
    FORMAT: CSV
);

-- Kịch bản 6: Nhập dữ liệu từ file bên ngoài
IMPORT (
    CONCEPT: Patient,
    FILE: '/data/new_patients_batch.json',
    FORMAT: JSON
);

-- Kịch bản 7: Cập nhật hàng loạt (Batch Update)
-- Đánh dấu tất cả bệnh nhân nguy cấp
UPDATE Patient
ATTRIBUTE (SET is_critical: true)
WHERE sys >= 140 OR dia >= 90 OR heartRate > 100 OR temperature >= 38.0;

-- Kịch bản 8: Xóa hàng loạt (Batch Delete)
-- Xóa các bản ghi cũ hơn 1 năm
DELETE FROM Patient
WHERE lastVisit < '2025-04-01';
```

### 5.1. Ví dụ về Quản lý Kho Hàng

```kbql
-- Tạo Concept Product
CREATE CONCEPT Product (
    VARIABLES (
        productId: STRING,
        name: STRING,
        category: STRING,
        price: DECIMAL,
        stock: INT,
        minStock: INT,
        supplier: STRING,
        lastRestock: DATE
    ),
    CONSTRAINTS (
        price > 0,
        stock >= 0,
        minStock >= 0
    )
);

-- Nhập hàng mới về kho
INSERT BULK INTO Product ATTRIBUTE (
    ('PRD001', 'Laptop Dell XPS', 'Electronics', 25000000, 50, 10, 'Dell Vietnam', '2026-04-01'),
    ('PRD002', 'Mouse Logitech', 'Accessories', 500000, 200, 20, 'Logitech', '2026-04-01'),
    ('PRD003', 'Keyboard Mechanical', 'Accessories', 1200000, 100, 15, 'Keychron', '2026-04-02')
);

-- Cập nhật số tồn kho sau khi bán
UPDATE Product
ATTRIBUTE (SET stock: stock - 5)
WHERE productId = 'PRD001';

-- Kiểm tra hàng cần nhập lại
SELECT productId, name, stock, minStock
FROM Product
WHERE stock < minStock;
```
