# Ngôn ngữ Truy vấn Tri thức (KQL)

**KQL** (Knowledge Query Language) tập hợp các lệnh để truy xuất thông tin, thực hiện truy vấn dữ liệu và yêu cầu hệ thống thực hiện các phép suy diễn tri thức phức tạp.

## 1. Cơ chế Truy vấn Tri thức

Lệnh `SELECT` trong KBQL tương đương với tiêu chuẩn SQL nhưng được tối ưu hóa để tương tác với các Khái niệm (Concept) và cấu trúc tri thức.

### 1.1. Cấu trúc Lệnh Truy vấn Toàn phần

```kbql
SELECT [<columns> | * | AGGREGATE(<var>)]
FROM <concept> [AS <alias>]
[JOIN <concept> ON <condition>]
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

## 2. Macro Giải quyết Tri thức SOLVE()

Macro `SOLVE()` được tích hợp trực tiếp vào danh sách truy xuất (projection) của lệnh `SELECT`. Nó kích hoạt bộ máy giải quyết vấn đề (Problem Solver) nội suy các biến số chưa biết ngay tại thời điểm truy vấn (on-the-fly) dựa trên cơ sở tri thức hiện hành (các công thức, luật dẫn, phân cấp).

### 2.1. Cú pháp thực thi

```kbql
SELECT <columns>, SOLVE(<target_variable>) 
FROM <concept>
[WHERE <conditions>];
```

### 2.2. Phân tích Hoạt động Suy diễn On-the-Fly

1.  **Thu thập dữ liệu (Fetch):** KBMS truy xuất các Sự kiện (**Facts**) từ bộ nhớ lưu trữ dựa trên mệnh đề `FROM` và `WHERE`.
2.  **Kích hoạt Engine (Trigger):** Macro `SOLVE()` sẽ lấy toàn bộ các thuộc tính của dòng hiện tại (row attributes) làm **Sự kiện ban đầu (Initial Facts)**.
3.  **Suy diễn (Inference):** Hệ thống áp dụng thuật toán Suy diễn tiến (**Forward Chaining**) kết hợp Giải phương trình (**Equation Solving**) trong bộ nhớ tạm mà không làm biến đổi dữ liệu đĩa vật lý.
4.  **Tích hợp Kết quả (Projection):** Trả về giá trị của biến `<target_variable>` ngay trong bảng kết quả của lệnh `SELECT`.

*Ví dụ:*
```kbql
-- Yêu cầu hệ thống chẩn đoán biến 'is_hypertension' dựa trên huyết áp đo được.
SELECT name, sys, dia, SOLVE(is_hypertension) FROM Patient WHERE age > 60;
```

## 3. Quản trị và Giám sát Hệ thống

Cung cấp các công cụ để liệt kê và kiểm tra các thành phần trong cơ sở tri thức hiện tại:

*   **SHOW CONCEPTS**: Liệt kê danh mục các Khái niệm.
*   **SHOW RULES**: Hiển thị các luật suy diễn đã định nghĩa.
*   **SHOW RELATIONS**: Liệt kê các quan hệ ngữ nghĩa giữa các khái niệm.
*   **SHOW HIERARCHIES**: Hiển thị cấu trúc cây phân cấp tri thức.
*   **SHOW OPERATORS / FUNCTIONS**: Liệt kê các toán tử và hàm số tùy biến.

## 4. Phân tích và Đặc tả Kỹ thuật

Các công cụ hỗ trợ nhà phát triển nắm bắt cấu trúc và phương thức thực thi của hệ thống:

*   **DESCRIBE {CONCEPT | RULE | ...} <name>**: Hiển thị chi tiết cấu trúc định nghĩa của đối tượng tri thức.
*   **EXPLAIN (<kbql_statement>)**: Đặc tả kế hoạch thực thi (**Execution Plan**) của câu lệnh, phục vụ việc tối ưu hóa hiệu năng truy vấn.

## 5. Truy cập Dữ liệu Siêu dữ liệu (Metadata)

Hệ thống cho phép truy vấn trực tiếp vào danh mục siêu dữ liệu để lấy thông tin về cấu trúc:
*Ví dụ:* `SELECT * FROM <concept_name>.variables;` trả về danh sách thuộc tính và kiểu dữ liệu tương ứng.

## 6. Ví dụ Thực tế - Truy vấn Hệ Tri thức

Dưới đây là các ví dụ phức tạp về truy vấn dữ liệu trong KBMS:

### 6.1. Truy vấn Cơ bản

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

### 6.2. Hàm Tổng hợp (Aggregate Functions)

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

### 6.3. Sắp xếp và Phân trang

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

### 6.4. Truy vấn với JOIN

```kbql
-- Thiết lập: Tạo Concept Appointment (Lịch hẹn)
CREATE CONCEPT Appointment (
    VARIABLES (
        appointmentId: STRING,
        patientId: STRING,
        doctorId: STRING,
        appointmentDate: DATETIME,
        reason: STRING,
        status: STRING
    )
);

-- Tạo Concept Doctor
CREATE CONCEPT Doctor (
    VARIABLES (
        doctorId: STRING,
        name: STRING,
        specialty: STRING,
        experience: INT
    )
);

-- JOIN: Lấy danh sách lịch hẹn kèm thông tin bệnh nhân
SELECT
    a.appointmentId,
    p.name AS patient_name,
    d.name AS doctor_name,
    a.appointmentDate,
    a.reason
FROM Appointment a
JOIN Patient p ON a.patientId = p.patientId
JOIN Doctor d ON a.doctorId = d.doctorId
WHERE a.appointmentDate >= '2026-04-01'
ORDER BY a.appointmentDate DESC;

-- JOIN với điều kiện lọc
SELECT
    p.name,
    p.sys,
    p.dia,
    d.name AS attending_doctor
FROM Patient p
JOIN Appointment a ON p.patientId = a.patientId
JOIN Doctor d ON a.doctorId = d.doctorId
WHERE p.sys > 140 AND a.status = 'Scheduled';

-- LEFT JOIN - Bao gồm cả bệnh nhân chưa có lịch hẹn
SELECT
    p.name,
    COUNT(a.appointmentId) AS appointment_count
FROM Patient p
LEFT JOIN Appointment a ON p.patientId = a.patientId
GROUP BY p.patientId, p.name
ORDER BY appointment_count DESC;
```

### 6.5. Hàm Tính toán CALC()

```kbql
-- Tính chỉ số BMI (Body Mass Index)
CREATE CONCEPT HealthMetrics (
    VARIABLES (weight: DECIMAL, height: DECIMAL, bmi: DECIMAL)
);

-- Tính BMI khi truy vấn
SELECT
    weight,
    height,
    CALC(weight / (height/100)^2) AS bmi_value
FROM HealthMetrics
WHERE height > 0;

-- Tính chi phí khám chữa bệnh
CREATE CONCEPT MedicalBill (
    VARIABLES (
        examinationFee: DECIMAL,
        medicineFee: DECIMAL,
        roomFee: DECIMAL,
        totalFee: DECIMAL,
        insurance: DECIMAL,
        patientPay: DECIMAL
    )
);

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

### 6.6. Macro SOLVE() - Suy diễn Tri thức

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

CREATE CONCEPT Diagnosis (
    VARIABLES (
        patientId: STRING,
        disease: STRING,
        confidence: DECIMAL
    )
);

-- Luật chẩn đoán
CREATE RULE DiagnoseFlu SCOPE Symptom
IF fever = true AND cough = true AND fatigue = true
THEN SET disease = 'Influenza', confidence = 0.85;

CREATE RULE DiagnoseMigraine SCOPE Symptom
IF headache = true AND fatigue = true AND fever = false
THEN SET disease = 'Migraine', confidence = 0.75;

-- Sử dụng SOLVE() để chẩn đoán tự động
SELECT
    s.patientId,
    p.name AS patient_name,
    SOLVE(disease) AS diagnosed_disease,
    SOLVE(confidence) AS diagnosis_confidence
FROM Symptom s
JOIN Patient p ON s.patientId = p.patientId
WHERE s.fever = true;

-- Kịch bản: Giải bài toán hình học
CREATE CONCEPT TriangleProblem (
    VARIABLES (
        sideA: DECIMAL,
        sideB: DECIMAL,
        angleC: DECIMAL,  -- Góc giữa sideA và sideB (độ)
        sideC: DECIMAL,   -- Cần tìm
        area: DECIMAL     -- Cần tìm
    ),
    EQUATIONS (
        'sideC = Sqrt(sideA^2 + sideB^2 - 2*sideA*sideB*Cos(angleC*3.14159/180))',
        'area = 0.5 * sideA * sideB * Sin(angleC*3.14159/180)'
    )
);

-- Giả sử có sideA = 5, sideB = 7, angleC = 60 độ
INSERT INTO TriangleProblem ATTRIBUTE (5, 7, 60);

-- Tìm sideC và area bằng SOLVE()
SELECT
    SOLVE(sideC) AS calculated_side_c,
    SOLVE(area) AS calculated_area
FROM TriangleProblem;
-- Kết quả: sideC ≈ 6.08, area ≈ 15.31
```

### 6.7. Truy vấn Phức tạp - Kết hợp Nhiều Tính năng

```kbql
-- Báo cáo thống kê bệnh nhân高血压
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

-- Truy vấn với nhiều JOIN và SOLVE()
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
