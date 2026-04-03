# Đặc tả Biểu thức và Hệ thống Hàm số

Biểu thức là tập hợp các đơn vị tính toán cốt lõi trong các mệnh đề `WHERE`, `IF`, `SET` và hàm `CALC()`. **KBQL** tích hợp bộ máy đánh giá biểu thức hỗ trợ đầy đủ các phép toán học thuật và hàm số hình thức [6].

## 1. Hệ thống Toán tử Cơ sở

Hệ thống toán tử trong KBQL bao gồm các phép toán tiêu chuẩn cho các kiểu dữ liệu số, chuỗi và logic.

### 1.1. Toán tử Số học
*   `+`, `-`, `*`, `/`: Các phép toán cơ sở.
*   `^`: Lũy thừa.
*   `%`: Chia lấy dư.

### 1.2. Toán tử So sánh
*   `=`, `!=` (hoặc `<>`): So sánh tương đương và phi tương đương.
*   `<`, `<=`, `>`, `>=`: Các phép so sánh thứ tự.

### 1.3. Toán tử Logic (Boolean)
*   `AND`, `OR`, `NOT`: Các phép logic Boolean phục vụ việc tổ hợp các điều kiện ràng buộc.

## 2. Danh mục Hàm số Tích hợp

Hệ thống **KBMS** tích hợp sẵn các hàm toán học chuyên sâu để phục vụ việc tính toán tri thức:

*Bảng: Danh mục Hàm số Tích hợp (Built-in Functions) trong KBQL*
| Hàm | Đặc tả Chức năng | Ví dụ Thực thi |
| :--- | :--- | :--- |
| `Abs(x)` | Giá trị tuyệt đối | `Abs(-10)` $\rightarrow$ 10 |
| `Sqrt(x)` | Căn bậc hai | `Sqrt(16)` $\rightarrow$ 4 |
| `Pow(x, y)` | Lũy thừa | `Pow(2, 3)` $\rightarrow$ 8 |
| `Round(x, n)` | Làm tròn đến n chữ số thập phân | `Round(3.1415, 2)` $\rightarrow$ 3.14 |
| `Floor(x)` / `Ceiling(x)` | Làm tròn xuống / Làm tròn lên | `Floor(2.9)` $\rightarrow$ 2 |
| `Sin`, `Cos`, `Tan` | Các hàm lượng giác | `Sin(0)` $\rightarrow$ 0 |
| `Factorial(n)` | Tính giai thừa | `Factorial(5)` $\rightarrow$ 120 |

## 3. Ứng dụng trong Hàm Truy vấn CALC()

Hàm `CALC()` cho phép nhúng trực tiếp biểu thức vào kết quả của lệnh `SELECT` để thực hiện tính toán tại thời điểm truy xuất:

*Ví dụ:* `SELECT name, CALC(Sqrt(a^2 + b^2)) AS hypotenuse FROM Triangles;`

## 4. Tương tác với Thuộc tính và Biến số

Bên trong các biểu thức, người dùng có thể tham chiếu trực tiếp đến định danh của các biến thuộc **Concept** hiện tại. Đối với các truy vấn liên kết (**JOIN**), cần sử dụng bí danh (**Alias**) để phân định rõ ràng các thực thể liên quan.

## 5. Tối ưu hóa Hiệu năng Thực thi

Các biểu thức logic phức tạp trong mệnh đề `WHERE` có thể ảnh hưởng đến tốc độ truy vấn nếu không có chỉ mục (**Index**) hỗ trợ phù hợp. Nhà phát triển được khuyến nghị sử dụng lệnh `EXPLAIN` để kiểm tra kế hoạch đánh giá biểu thức của hệ thống.

## 6. Ví dụ Thực tế - Biểu thức và Hàm số trong KBQL

Dưới đây là các ví dụ chi tiết về sử dụng biểu thức và hàm số trong KBMS:

### 6.1. Biểu thức Số học Cơ bản

```kbql
-- Các phép toán cơ bản
SELECT
    price,
    CALC(price * 1.1) AS price_with_tax,
    CALC(price * 0.9) AS discounted_price
FROM Product;

-- Phép toán lũy thừa
SELECT
    base,
    exponent,
    CALC(base ^ exponent) AS power_result
FROM MathTable;

-- Chia lấy dư
SELECT
    value,
    CALC(value % 2) AS is_even
FROM Numbers;

-- Kết hợp nhiều phép toán
SELECT
    length,
    width,
    height,
    CALC(length * width * height) AS volume,
    CALC(2 * (length*width + width*height + height*length)) AS surface_area
FROM BoxDimensions;
```

### 6.2. Hàm số Toán học

```kbql
-- Giá trị tuyệt đối
SELECT
    temperature,
    CALC(Abs(temperature - 37)) AS deviation_from_normal
FROM VitalSigns;

-- Căn bậc hai
SELECT
    sideA,
    sideB,
    CALC(Sqrt(sideA^2 + sideB^2)) AS hypotenuse
FROM Triangle;

-- Làm tròn
SELECT
    price,
    CALC(Round(price, 2)) AS price_rounded,
    CALC(Floor(price)) AS price_floor,
    CALC(Ceiling(price)) AS price_ceiling
FROM Product;

-- Hàm lượng giác
SELECT
    angle_degrees,
    CALC(Sin(angle_degrees * 3.14159/180)) AS sin_value,
    CALC(Cos(angle_degrees * 3.14159/180)) AS cos_value,
    CALC(Tan(angle_degrees * 3.14159/180)) AS tan_value
FROM AngleTable;

-- Giai thừa
SELECT
    n,
    CALC(Factorial(n)) AS factorial
FROM Numbers
WHERE n <= 10;
```

### 6.3. Biểu thức Logic trong WHERE

```kbql
-- Toán tử AND
SELECT * FROM Patient
WHERE age >= 18 AND age <= 65;

-- Toán tử OR
SELECT * FROM Patient
WHERE sys >= 140 OR dia >= 90;

-- Toán tử NOT
SELECT * FROM Patient
WHERE NOT (is_critical = true);

-- Kết hợp phức tạp
SELECT * FROM Patient
WHERE (age >= 40 AND (sys > 140 OR dia > 90))
   OR (age >= 60 AND heartRate > 100);

-- Sử dụng NOT với các điều kiện phức tạp
SELECT * FROM Patient
WHERE NOT ((sys < 120 AND dia < 80) AND heartRate < 100);
```

### 6.4. Biểu thức So sánh

```kbql
-- So sánh bằng
SELECT * FROM Patient WHERE bloodType = 'A+';

-- So sánh khác
SELECT * FROM Patient WHERE bloodType != 'O+';

-- So sánh lớn hơn/bé hơn
SELECT * FROM Patient
WHERE age > 30 AND age < 50;

-- So sánh lớn hơn hoặc bằng
SELECT * FROM Product
WHERE stock >= minStock;

-- So sánh chuỗi (theo thứ tự bảng chữ cái)
SELECT name FROM Patient
WHERE name >= 'Nguyen' AND name < 'Tran';

-- Kết hợp nhiều điều kiện so sánh
SELECT * FROM Patient
WHERE sys BETWEEN 110 AND 140
  AND dia BETWEEN 70 AND 90
  AND age >= 18;
```

### 6.5. Biểu thức trong UPDATE

```kbql
-- Cập nhật với biểu thức tính toán
UPDATE Product
ATTRIBUTE (SET price: price * 1.05)
WHERE category = 'Electronics';

-- Cập nhật nhiều thuộc tính với biểu thức
UPDATE Patient
ATTRIBUTE (
    SET bmi: CALC(weight / (height/100)^2),
        healthRisk: CALC(CASE WHEN age > 60 AND sys > 140 THEN 'High' ELSE 'Normal' END)
)
WHERE patientId = 'P001';

-- Cập nhật với biểu thức phức tạp
UPDATE Inventory
ATTRIBUTE (
    SET totalValue: CALC(quantity * unitPrice),
        taxAmount: CALC(quantity * unitPrice * 0.1),
        finalValue: CALC(quantity * unitPrice * 1.1)
);
```

### 6.6. Biểu thức trong Constraints

```kbql
-- Ràng buộc với biểu thức số học
CREATE CONCEPT Triangle (
    VARIABLES (a: DECIMAL, b: DECIMAL, c: DECIMAL),
    CONSTRAINTS (
        a + b > c,
        b + c > a,
        a + c > b
    )
);

-- Ràng buộc với biểu thức logic
CREATE CONCEPT Employee (
    VARIABLES (
        hireDate: DATE,
        birthDate: DATE,
        retirementDate: DATE
    ),
    CONSTRAINTS (
        hireDate >= birthDate + 18*365,
        retirementDate > hireDate,
        retirementDate <= birthDate + 65*365
    )
);

-- Ràng buộc với hàm số
CREATE CONCEPT Circle (
    VARIABLES (radius: DECIMAL, area: DECIMAL),
    CONSTRAINTS (
        radius > 0,
        area = CALC(3.14159 * radius^2)
    )
);
```

### 6.7. Biểu thức trong Equations

```kbql
-- Phương trình bậc 1
CREATE CONCEPT LinearEquation (
    VARIABLES (x: DECIMAL, m: DECIMAL, c: DECIMAL, y: DECIMAL),
    EQUATIONS ('y = m * x + c')
);

-- Phương trình bậc 2
CREATE CONCEPT QuadraticEquation (
    VARIABLES (a: DECIMAL, b: DECIMAL, c: DECIMAL, x: DECIMAL, y: DECIMAL),
    EQUATIONS ('y = a*x^2 + b*x + c')
);

-- Hệ phương trình
CREATE CONCEPT PhysicsProblem (
    VARIABLES (
        velocity: DECIMAL,
        time: DECIMAL,
        acceleration: DECIMAL,
        distance: DECIMAL
    ),
    EQUATIONS (
        'velocity = acceleration * time',
        'distance = 0.5 * acceleration * time^2'
    )
);

-- Phương trình lượng giác
CREATE CONCEPT TrigonometryProblem (
    VARIABLES (angle: DECIMAL, sin_val: DECIMAL, cos_val: DECIMAL, tan_val: DECIMAL),
    EQUATIONS (
        'sin_val = Sin(angle)',
        'cos_val = Cos(angle)',
        'tan_val = sin_val / cos_val'
    )
);
```

### 6.8. Biểu thức với Hàm Tự định nghĩa

```kbql
-- Tạo hàm tính chỉ số khối cơ thể (BMI)
CREATE FUNCTION CalculateBMI
PARAMS (DECIMAL weight, DECIMAL height)
RETURNS DECIMAL
BODY '(weight * 10000) / (height * height)';

-- Sử dụng hàm trong truy vấn
SELECT
    name,
    weight,
    height,
    CALC(CalculateBMI(weight, height)) AS bmi
FROM Patient;

-- Tạo hàm tính chi phí vận chuyển
CREATE FUNCTION CalculateShippingCost
PARAMS (DECIMAL weight, DECIMAL distance)
RETURNS DECIMAL
BODY '(weight * 5000) + (distance * 2000) + 50000';

-- Sử dụng trong UPDATE
UPDATE Order
ATTRIBUTE (SET shippingCost: CALC(CalculateShippingCost(packageWeight, shippingDistance)))
WHERE orderId = 'ORD001';
```

### 6.9. Biểu thức Phức tạp - Kết hợp Nhiều Hàm

```kbql
-- Tính khoảng cách giữa hai điểm trong không gian 3D
SELECT
    p1.x AS x1, p1.y AS y1, p1.z AS z1,
    p2.x AS x2, p2.y AS y2, p2.z AS z2,
    CALC(Sqrt((p2.x-p1.x)^2 + (p2.y-p1.y)^2 + (p2.z-p1.z)^2)) AS distance_3d
FROM Point3D p1
CROSS JOIN Point3D p2
WHERE p1.pointId < p2.pointId;

-- Tính độ lệch chuẩn
SELECT
    category,
    COUNT(*) AS sample_count,
    AVG(value) AS mean_value,
    CALC(Sqrt(SUM((value - AVG(value))^2) / COUNT(*))) AS std_deviation
FROM Measurements
GROUP BY category;

-- Biểu thức CASE phức tạp
SELECT
    patientId,
    name,
    sys,
    dia,
    CALC(CASE
        WHEN sys < 120 AND dia < 80 THEN 'Normal'
        WHEN sys >= 140 OR dia >= 90 THEN 'Hypertension Stage 2'
        WHEN sys >= 130 OR dia >= 85 THEN 'Hypertension Stage 1'
        ELSE 'Elevated'
    END) AS bp_category
FROM Patient;

-- Tính điểm tín dụng
SELECT
    customerId,
    income,
    debt,
    paymentHistory,
    CALC(
        (income / 10000000 * 30) +
        ((1 - debt/income) * 40) +
        (paymentHistory * 30)
    ) AS credit_score
FROM CreditApplication;
```

### 6.10. Tối ưu hóa Biểu thức với Index

```kbql
-- Tạo chỉ mục cho các cột thường dùng trong biểu thức
CREATE INDEX idx_patient_age ON Patient (age);
CREATE INDEX idx_patient_sys ON Patient (sys);
CREATE INDEX idx_product_price ON Product (price);

-- Sử dụng EXPLAIN để kiểm tra kế hoạch thực thi
EXPLAIN (
    SELECT * FROM Patient
    WHERE age >= 40 AND (sys > 140 OR dia > 90)
);

-- Biểu thức không sử dụng index (không tối ưu)
-- SELECT * FROM Patient WHERE CALC(age + 1) > 40;

-- Biểu thức sử dụng index (tối ưu)
-- SELECT * FROM Patient WHERE age > 39;
```
