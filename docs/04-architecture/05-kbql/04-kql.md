# Ngôn ngữ Truy vấn Tri thức (KQL)

**KQL** (Knowledge Query Language) tập hợp các lệnh để truy xuất thông tin, thực hiện truy vấn dữ liệu và yêu cầu hệ thống thực hiện các phép suy diễn tri thức phức tạp.

## 1. Cơ chế Truy vấn Tri thức

Lệnh `SELECT` trong KBQL tương đương với tiêu chuẩn SQL nhưng được tối ưu hóa để tương tác với các Khái niệm (Concept) và cấu trúc tri thức.

### 1.1. Cấu trúc Lệnh Truy vấn Toàn phần

```kbql
SELECT [<columns> | * | AGGREGATE(<var>)]
FROM <concept> [AS <alias>] | (<subquery>) [AS <alias>]
[<join_type> JOIN <concept> [AS <alias>] ON <condition>]
[WHERE <filter_conditions>]
[GROUP BY <variables>]
[HAVING <filter_conditions>]
[ORDER BY <variables> {ASC | DESC}]
[LIMIT <n> OFFSET <m>];
```

### 1.2. Các Tính năng Mở rộng

*   **Hàm Tổng hợp (Aggregate):** Tích hợp các hàm `COUNT`, `SUM`, `AVG`, `MIN`, `MAX` trên các tập thuộc tính của Concept.
*   **Mệnh đề lọc sau nhóm (HAVING):** Cho phép lọc dữ liệu sau khi đã thực hiện gom nhóm tri thức.
*   **Biểu thức Tính toán CALC():** Hỗ trợ thực thi các công thức toán học ngay trong tiến trình truy vấn.
    *Ví dụ:* `SELECT name, CALC(price * 1.1) AS price_tax FROM Product;`
*   **Sub-query (Truy vấn con):** Hỗ trợ sub-query trong mệnh đề FROM và WHERE.
*   **Outer Join:** Hỗ trợ LEFT JOIN, RIGHT JOIN, FULL OUTER JOIN.

---

## 2. Các Loại JOIN

KQL hỗ trợ đầy đủ các loại JOIN theo tiêu chuẩn SQL:

### 2.1. INNER JOIN (Mặc định)

Trả về các dòng có kết quả khớp ở cả hai bảng:

```kbql
SELECT p.name, a.appointmentDate
FROM Patient p
JOIN Appointment a ON p.patientId = a.patientId;
```

### 2.2. LEFT [OUTER] JOIN

Trả về tất cả các dòng từ bảng bên trái, và các dòng khớp từ bảng bên phải. Nếu không khớp, giá trị bên phải sẽ là NULL:

```kbql
-- Liệt kê tất cả bệnh nhân, bao gồm cả những người chưa có lịch hẹn
SELECT p.name, a.appointmentDate
FROM Patient p
LEFT JOIN Appointment a ON p.patientId = a.patientId;

-- Sử dụng LEFT OUTER JOIN (tương tự)
SELECT p.name, a.appointmentDate
FROM Patient p
LEFT OUTER JOIN Appointment a ON p.patientId = a.patientId;
```

### 2.3. RIGHT [OUTER] JOIN

Trả về tất cả các dòng từ bảng bên phải, và các dòng khớp từ bảng bên trái:

```kbql
-- Liệt kê tất cả lịch hẹn, bao gồm cả những lịch hẹn chưa gán bệnh nhân
SELECT p.name, a.appointmentDate
FROM Patient p
RIGHT JOIN Appointment a ON p.patientId = a.patientId;
```

### 2.4. FULL [OUTER] JOIN

Trả về tất cả các dòng từ cả hai bảng, điền NULL cho bên không khớp:

```kbql
-- Liệt kê tất cả bệnh nhân và tất cả lịch hẹn
SELECT p.name, a.appointmentDate
FROM Patient p
FULL OUTER JOIN Appointment a ON p.patientId = a.patientId;
```

### 2.5. CROSS JOIN

Tạo tích Descartes của hai bảng:

```kbql
SELECT p1.label AS point1, p2.label AS point2
FROM Point p1
CROSS JOIN Point p2
WHERE p1.label < p2.label;
```

---

## 3. Sub-query (Truy vấn Con)

### 3.1. Derived Table (Sub-query trong FROM)

Sử dụng kết quả của một truy vấn làm nguồn dữ liệu cho truy vấn bên ngoài:

```kbql
-- Truy vấn từ một derived table
SELECT * FROM (
    SELECT name, age, sys FROM Patient WHERE age > 50
) AS elderly_patients
WHERE sys > 140;

-- Kết hợp derived table với JOIN
SELECT e.name, e.avg_sys
FROM (
    SELECT patientId, name, AVG(sys) AS avg_sys
    FROM Patient
    GROUP BY patientId, name
) AS e
JOIN Appointment a ON e.patientId = a.patientId;
```

### 3.2. Scalar Sub-query (trong WHERE)

Sử dụng sub-query trả về một giá trị duy nhất để so sánh:

```kbql
-- Tìm bệnh nhân có huyết áp cao nhất
SELECT * FROM Patient
WHERE sys = (SELECT MAX(sys) FROM Patient);

-- Tìm bệnh nhân có tuổi lớn hơn tuổi trung bình
SELECT name, age FROM Patient
WHERE age > (SELECT AVG(age) FROM Patient);
```

### 3.3. EXISTS Sub-query

Kiểm tra sự tồn tại của bản ghi thỏa mãn điều kiện:

```kbql
-- Tìm bệnh nhân đã có lịch hẹn
SELECT name FROM Patient p
WHERE EXISTS (
    SELECT 1 FROM Appointment a
    WHERE a.patientId = p.patientId
);

-- Tìm bệnh nhân chưa có lịch hẹn
SELECT name FROM Patient p
WHERE NOT EXISTS (
    SELECT 1 FROM Appointment a
    WHERE a.patientId = p.patientId
);
```

### 3.4. IN Sub-query

Kiểm tra giá trị có nằm trong tập kết quả của sub-query:

```kbql
-- Tìm bệnh nhân có trong danh sách lịch hẹn hôm nay
SELECT * FROM Patient
WHERE patientId IN (
    SELECT patientId FROM Appointment
    WHERE appointmentDate = '2026-04-03'
);

-- Sử dụng NOT IN
SELECT * FROM Patient
WHERE patientId NOT IN (
    SELECT patientId FROM Appointment
    WHERE status = 'Cancelled'
);
```

---

## 4. Macro Giải quyết Tri thức SOLVE()

Macro `SOLVE()` kích hoạt bộ máy giải quyết vấn đề (Problem Solver) nội suy các biến số chưa biết dựa trên cơ sở tri thức hiện hành.

### 4.1. SOLVE trong Projection (Danh sách truy xuất)

```kbql
SELECT <columns>, SOLVE(<target_variable>)
FROM <concept>
[WHERE <conditions>];
```

**Ví dụ:**
```kbql
-- Chẩn đoán biến 'is_hypertension' dựa trên huyết áp
SELECT name, sys, dia, SOLVE(is_hypertension)
FROM Patient
WHERE age > 60;
```

### 4.2. SOLVE trong WHERE Clause

Sử dụng SOLVE để lọc dữ liệu dựa trên kết quả suy diễn:

```kbql
-- Tìm các tam giác có diện tích > 100
SELECT * FROM Triangle
WHERE SOLVE(area) > 100;

-- Tìm bệnh nhân có mức nguy cơ cao
SELECT * FROM Patient
WHERE SOLVE(risk_level) = 'high';

-- Kết hợp với các điều kiện khác
SELECT name, sys, dia FROM Patient
WHERE SOLVE(is_hypertension) = true AND age > 50;
```

### 4.3. Phân tích Hoạt động Suy diễn

1.  **Thu thập dữ liệu (Fetch):** KBMS truy xuất các Sự kiện (**Facts**) từ bộ nhớ lưu trữ.
2.  **Kích hoạt Engine (Trigger):** Macro `SOLVE()` lấy các thuộc tính của dòng hiện tại làm **Sự kiện ban đầu**.
3.  **Suy diễn (Inference):** Hệ thống áp dụng Forward Chaining kết hợp Equation Solving.
4.  **Tích hợp Kết quả:** Trả về giá trị của biến mục tiêu.

---

## 5. Semantic Validation

Hệ thống tự động kiểm tra ngữ nghĩa của truy vấn trước khi thực thi:

*   **Kiểm tra Concept tồn tại:** Xác minh concept trong FROM/INSERT/UPDATE có tồn tại.
*   **Kiểm tra Variable:** Xác minh các biến/cột có thuộc concept đúng.
*   **Kiểm tra Type Compatibility:** Cảnh báo khi kiểu dữ liệu không tương thích.
*   **Kiểm tra Hierarchy Cycle:** Phát hiện vòng lặp trong phân cấp.
*   **Kiểm tra Rule Scope:** Xác minh scope concept của rule tồn tại.

Khi có lỗi validation, hệ thống trả về lỗi `ValidationError` với chi tiết:

```json
{
    "Type": "ValidationError",
    "Message": "Concept 'Patientt' not found in knowledge base 'clinic'."
}
```

---

## 6. Quản trị và Giám sát Hệ thống

Cung cấp các công cụ để liệt kê và kiểm tra các thành phần trong cơ sở tri thức:

*   **SHOW CONCEPTS**: Liệt kê danh mục các Khái niệm.
*   **SHOW RULES**: Hiển thị các luật suy diễn đã định nghĩa.
*   **SHOW RELATIONS**: Liệt kê các quan hệ ngữ nghĩa giữa các khái niệm.
*   **SHOW HIERARCHIES**: Hiển thị cấu trúc cây phân cấp tri thức.
*   **SHOW OPERATORS / FUNCTIONS**: Liệt kê các toán tử và hàm số tùy biến.

## 7. Phân tích và Đặc tả Kỹ thuật

*   **DESCRIBE {CONCEPT | RULE | ...} <name>**: Hiển thị chi tiết cấu trúc định nghĩa.
*   **EXPLAIN (<kbql_statement>)**: Đặc tả kế hoạch thực thi (**Execution Plan**).

## 8. Truy cập Dữ liệu Siêu dữ liệu (Metadata)

Truy vấn trực tiếp vào danh mục siêu dữ liệu:

```kbql
SELECT * FROM <concept_name>.variables;
```

---

## 9. Ví dụ Thực tế - Truy vấn Hệ Tri thức

### 9.1. Truy vấn Cơ bản

```kbql
-- Truy vấn tất cả bệnh nhân
SELECT * FROM Patient;

-- Truy vấn cột cụ thể
SELECT patientId, name, age FROM Patient;

-- Truy vấn có điều kiện
SELECT name, sys, dia FROM Patient WHERE sys > 120;

-- Sử dụng các toán tử so sánh
SELECT * FROM Patient WHERE age BETWEEN 30 AND 50;
SELECT * FROM Patient WHERE bloodType IN ('A+', 'B+', 'O+');
SELECT * FROM Patient WHERE name LIKE '%Nguyen%';
```

### 9.2. Hàm Tổng hợp (Aggregate Functions)

```kbql
-- Đếm số lượng bệnh nhân
SELECT COUNT(*) AS total_patients FROM Patient;

-- Tính tuổi trung bình
SELECT AVG(age) AS average_age FROM Patient;

-- Tìm giá trị huyết áp cao nhất/thấp nhất
SELECT MAX(sys) AS max_systolic, MIN(dia) AS min_diastolic
FROM Patient;

-- Tổng hợp theo nhóm
SELECT bloodType, COUNT(*) AS patient_count, AVG(age) AS avg_age
FROM Patient
GROUP BY bloodType
HAVING COUNT(*) > 5;

-- Nhiều hàm tổng hợp trong một truy vấn
SELECT
    COUNT(*) AS total,
    AVG(sys) AS avg_sys,
    MAX(dia) AS max_dia,
    MIN(heartRate) AS min_hr
FROM Patient
WHERE age > 50;
```

### 9.3. Sắp xếp và Phân trang

```kbql
-- Sắp xếp theo tuổi tăng dần
SELECT name, age FROM Patient ORDER BY age ASC;

-- Sắp xếp theo nhiều cột
SELECT name, age, sys, dia
FROM Patient
ORDER BY age DESC, sys ASC;

-- Phân trang (Limit/Offset)
SELECT * FROM Patient ORDER BY patientId LIMIT 10 OFFSET 20;

-- Lấy Top N bệnh nhân có chỉ số nguy hiểm nhất
SELECT name, sys, dia
FROM Patient
WHERE sys > 140 OR dia > 90
ORDER BY sys DESC, dia DESC
LIMIT 5;
```

### 9.4. Truy vấn với JOINs

```kbql
-- INNER JOIN: Lấy danh sách lịch hẹn kèm thông tin bệnh nhân
SELECT
    a.appointmentId,
    p.name AS patient_name,
    d.name AS doctor_name,
    a.appointmentDate
FROM Appointment a
JOIN Patient p ON a.patientId = p.patientId
JOIN Doctor d ON a.doctorId = d.doctorId
WHERE a.appointmentDate >= '2026-04-01'
ORDER BY a.appointmentDate DESC;

-- LEFT JOIN: Bao gồm cả bệnh nhân chưa có lịch hẹn
SELECT
    p.name,
    COUNT(a.appointmentId) AS appointment_count
FROM Patient p
LEFT JOIN Appointment a ON p.patientId = a.patientId
GROUP BY p.patientId, p.name
ORDER BY appointment_count DESC;

-- FULL OUTER JOIN: Tất cả bệnh nhân và tất cả lịch hẹn
SELECT p.name, a.appointmentDate
FROM Patient p
FULL OUTER JOIN Appointment a ON p.patientId = a.patientId;

-- Multiple JOINs với điều kiện phức tạp
SELECT
    p.name,
    p.sys,
    p.dia,
    d.name AS attending_doctor
FROM Patient p
JOIN Appointment a ON p.patientId = a.patientId
JOIN Doctor d ON a.doctorId = d.doctorId
WHERE p.sys > 140 AND a.status = 'Scheduled';
```

### 9.5. Sub-query Phức tạp

```kbql
-- Derived table với filtering
SELECT name, risk_score
FROM (
    SELECT name, SOLVE(risk_level) AS risk_score
    FROM Patient
    WHERE age > 40
) AS at_risk_patients
WHERE risk_score = 'high';

-- Nested sub-query
SELECT * FROM Patient
WHERE patientId IN (
    SELECT patientId FROM Appointment
    WHERE doctorId IN (
        SELECT doctorId FROM Doctor
        WHERE specialty = 'Cardiology'
    )
);

-- Correlated EXISTS
SELECT p.name FROM Patient p
WHERE EXISTS (
    SELECT 1 FROM Appointment a
    JOIN Doctor d ON a.doctorId = d.doctorId
    WHERE a.patientId = p.patientId
    AND d.specialty = 'Cardiology'
);
```

### 9.6. Hàm Tính toán CALC()

```kbql
-- Tính chỉ số BMI (Body Mass Index)
SELECT
    weight,
    height,
    CALC(weight / (height/100)^2) AS bmi_value
FROM HealthMetrics
WHERE height > 0;

-- Tính chi phí với bảo hiểm
SELECT
    examinationFee,
    medicineFee,
    roomFee,
    CALC(examinationFee + medicineFee + roomFee) AS calculated_total,
    CALC((examinationFee + medicineFee + roomFee) * 0.8) AS insurance_amount,
    CALC((examinationFee + medicineFee + roomFee) * 0.2) AS patient_amount
FROM MedicalBill;

-- Tính khoảng cách giữa hai điểm
SELECT
    p1.label AS point1,
    p2.label AS point2,
    CALC(Sqrt((p2.x - p1.x)^2 + (p2.y - p1.y)^2)) AS distance
FROM Point p1
CROSS JOIN Point p2
WHERE p1.label < p2.label;
```

### 9.7. SOLVE() - Suy diễn Tri thức

```kbql
-- Kịch bản: Chẩn đoán bệnh lý từ triệu chứng
CREATE CONCEPT Symptom (
    VARIABLES (
        patientId: STRING,
        fever: BOOLEAN,
        cough: BOOLEAN,
        headache: BOOLEAN,
        fatigue: BOOLEAN
    )
);

-- Luật chẩn đoán
CREATE RULE DiagnoseFlu SCOPE Symptom
IF fever = true AND cough = true AND fatigue = true
THEN SET disease = 'Influenza', confidence = 0.85;

-- Sử dụng SOLVE() trong projection
SELECT
    s.patientId,
    p.name AS patient_name,
    SOLVE(disease) AS diagnosed_disease,
    SOLVE(confidence) AS diagnosis_confidence
FROM Symptom s
JOIN Patient p ON s.patientId = p.patientId
WHERE s.fever = true;

-- Sử dụng SOLVE() trong WHERE clause
SELECT * FROM Symptom
WHERE SOLVE(disease) = 'Influenza';

-- Kịch bản: Giải bài toán hình học
CREATE CONCEPT Triangle (
    VARIABLES (
        sideA: DECIMAL,
        sideB: DECIMAL,
        angleC: DECIMAL,
        sideC: DECIMAL,
        area: DECIMAL
    ),
    EQUATIONS (
        'sideC = Sqrt(sideA^2 + sideB^2 - 2*sideA*sideB*Cos(angleC*3.14159/180))',
        'area = 0.5 * sideA * sideB * Sin(angleC*3.14159/180)'
    )
);

INSERT INTO Triangle ATTRIBUTE (5, 7, 60);

-- Tìm các tam giác có diện tích > 100
SELECT sideA, sideB, angleC
FROM Triangle
WHERE SOLVE(area) > 100;

-- Hiển thị cả giá trị đã giải
SELECT
    sideA,
    sideB,
    angleC,
    SOLVE(sideC) AS calculated_side_c,
    SOLVE(area) AS calculated_area
FROM Triangle;
```

### 9.8. Truy vấn Phức tạp - Kết hợp Nhiều Tính năng

```kbql
-- Báo cáo thống kê bệnh nhân với nhiều tính năng
SELECT
    bloodType,
    COUNT(*) AS patient_count,
    ROUND(AVG(sys), 2) AS avg_systolic,
    ROUND(AVG(dia), 2) AS avg_diastolic,
    MAX(sys) AS max_systolic,
    MIN(dia) AS min_diastolic,
    COUNT(CASE WHEN sys > 140 THEN 1 END) AS hypertension_count
FROM Patient
WHERE age >= 40
GROUP BY bloodType
HAVING COUNT(*) >= 3
ORDER BY hypertension_count DESC;

-- Truy vấn với JOINs, SOLVE() và derived table
SELECT
    p.name AS patient_name,
    p.age,
    p.sys,
    p.dia,
    CALC(p.sys / p.dia) AS pulse_pressure,
    d.name AS doctor_name,
    a.appointmentDate,
    SOLVE(diagnosis) AS predicted_diagnosis
FROM Patient p
JOIN Appointment a ON p.patientId = a.patientId
JOIN Doctor d ON a.doctorId = d.doctorId
WHERE p.sys > 130 OR p.dia > 85
ORDER BY p.sys DESC
LIMIT 20;

-- Truy vấn phức tạp với sub-query và SOLVE trong WHERE
SELECT name, age, sys, dia
FROM (
    SELECT * FROM Patient
    WHERE age > (SELECT AVG(age) FROM Patient)
) AS older_patients
WHERE SOLVE(is_hypertension) = true;

-- Tìm bệnh nhân có chỉ số bất thường
SELECT
    patientId,
    name,
    sys,
    dia,
    heartRate,
    temperature,
    CASE
        WHEN sys >= 140 OR dia >= 90 THEN 'Hypertension'
        WHEN heartRate > 100 THEN 'Tachycardia'
        WHEN temperature > 37.5 THEN 'Fever'
        ELSE 'Normal'
    END AS health_status
FROM Patient
WHERE sys >= 140 OR dia >= 90 OR heartRate > 100 OR temperature > 37.5
ORDER BY health_status, sys DESC;
```
