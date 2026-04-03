# KỊCH BẢN DEMO KBMS (15-20 phút)

---

## CHUẨN BỊ TRƯỚC KHI DEMO

### 1. Start Server
```bash
cd /Users/lechautranphat/Desktop/KBMS
dotnet run --project KBMS.Server
```

### 2. Start CLI (terminal riêng)
```bash
dotnet run --project KBMS.CLI
```

### 3. Mở sẵn Studio (nếu demo UI)
```bash
cd kbms-studio && npm start
```

---

## DEMO 1: KHỞI ĐỘNG & TẠO KB (2 phút)

### Commands:
```kbql
LOGIN root root
```
**Nói:** "Đăng nhập với tài khoản root. Hệ thống có RBAC phân quyền."

```kbql
CREATE KNOWLEDGE BASE HospitalDB
DESCRIPTION "Hệ tri thức Bệnh viện";
```
**Nói:** "Tạo cơ sở tri thức HospitalDB. Khác với CREATE DATABASE trong SQL, KB lưu cả rules và equations."

```kbql
USE HospitalDB;
```

---

## DEMO 2: ĐỊNH NGHĨA CONCEPT VỚI EQUATIONS (5 phút) - QUAN TRỌNG

### Tạo Concept Bệnh nhân:
```kbql
CREATE CONCEPT Patient (
    VARIABLES (
        patientId: STRING,
        name: STRING,
        age: INT,
        sys: DECIMAL,
        dia: DECIMAL,
        bmi: DECIMAL,
        riskLevel: STRING
    ),
    CONSTRAINTS (
        age >= 0 AND age <= 150,
        sys > 0 AND dia > 0,
        sys > dia
    )
);
```
**Nói:** "Concept giống Class trong OOP nhưng có thêm CONSTRAINTS - ràng buộc logic. Nếu nhập sai, hệ thống sẽ từ chối."

### Tạo Concept với EQUATIONS (ĐIỂM MẠNH):
```kbql
CREATE CONCEPT Triangle (
    VARIABLES (
        a: DECIMAL,
        b: DECIMAL,
        c: DECIMAL,
        perimeter: DECIMAL,
        area: DECIMAL
    ),
    EQUATIONS (
        'perimeter = a + b + c',
        'area = Sqrt(perimeter/2 * (perimeter/2 - a) * (perimeter/2 - b) * (perimeter/2 - c))'
    ),
    CONSTRAINTS (
        a + b > c,
        b + c > a,
        a + c > b
    )
);
```
**Nói:** "Đây là điểm khác biệt lớn nhất với SQL. Concept có EQUATIONS - phương trình nội tại. Hệ thống TỰ ĐỘNG GIẢI khi cần."

### Demo INSERT:
```kbql
INSERT INTO Triangle ATTRIBUTE (a: 3, b: 4, c: 5);
```
**Nói:** "Chỉ nhập 3 cạnh a, b, c. Perimeter và area CHƯA CÓ."

---

## DEMO 3: SUY DIỄN TỰ ĐỘNG VỚI SOLVE() (5 phút) - TRỌNG TÂM

### Truy vấn với SOLVE:
```kbql
SELECT a, b, c, SOLVE(perimeter), SOLVE(area) FROM Triangle;
SELECT a, b, c, SOLVE(perimeter), SOLVE(area) FROM Triangle;
```
**Nói:** "SELECT SOLVE() - Hệ thống TỰ ĐỘNG tính perimeter = 12, area = 6 từ equations đã định nghĩa. SQL KHÔNG LÀM ĐƯỢC điều này!"

### Ngược lại - Tìm cạnh từ diện tích:
```kbql
INSERT INTO Triangle ATTRIBUTE (a: 3, b: 4, area: 6);
SELECT a, b, SOLVE(c), SOLVE(perimeter) FROM Triangle WHERE area = 6;
```
**Nói:** "Nhập diện tích = 6, hệ thống GIẢI NGƯỢC để tìm c = 5. Đây là khả năng symbolic reasoning."

---

## DEMO 4: RULES VÀ FORWARD CHAINING (5 phút) - TRỌNG TÂM

### Tạo Rule y khoa:
```kbql
CREATE RULE HypertensionDetection
SCOPE Patient
IF sys >= 140 OR dia >= 90
THEN SET riskLevel = 'high'
PRIORITY 80;

CREATE RULE DiabetesRisk
SCOPE Patient
IF age > 50 AND bmi > 30
THEN SET riskLevel = 'elevated'
PRIORITY 60;
```
**Nói:** "Tạo 2 rules y khoa. PRIORITY quyết định rule nào chạy trước nếu conflict."

### Thêm bệnh nhân:
```kbql
INSERT INTO Patient ATTRIBUTE (
    patientId: 'P001',
    name: 'Nguyen Van A',
    age: 65,
    sys: 155,
    dia: 95,
    bmi: 28
);
```

### Xem rule fire:
```kbql
SELECT patientId, name, sys, dia, SOLVE(riskLevel) FROM Patient WHERE patientId = 'P001';
```
**Nói:** "Nhập sys=155, dia=95. Hệ thống TỰ ĐỘNG suy diễn riskLevel = 'high' dựa trên rule HypertensionDetection. Không cần code IF-ELSE ở application layer!"

---

## DEMO 5: HIERARCHY (KẾ THỪA) (3 phút)

```kbql
CREATE CONCEPT Person (
    VARIABLES (id: STRING, name: STRING, age: INT)
);

CREATE CONCEPT Doctor (
    VARIABLES (specialty: STRING, licenseNumber: STRING)
);

ADD HIERARCHY Doctor IS_A Person;
```
**Nói:** "Doctor IS_A Person - kế thừa thuộc tính. Tương tự inheritance trong OOP nhưng ở mức data model."

```kbql
INSERT INTO Doctor ATTRIBUTE (id: 'D001', name: 'Dr. Smith', age: 45, specialty: 'Cardiology', licenseNumber: 'MD123');
SELECT * FROM Doctor;
```

---

## DEMO 6: JOIN VÀ MULTI-CONCEPT (3 phút)

```kbql
CREATE CONCEPT LabResult (
    VARIABLES (
        resultId: STRING,
        patientId: STRING,
        bloodSugar: DECIMAL,
        cholesterol: DECIMAL
    )
);

INSERT INTO LabResult ATTRIBUTE (resultId: 'L001', patientId: 'P001', bloodSugar: 180, cholesterol: 250);
```

### Multi-concept rule:
```kbql
CREATE RULE CardiovascularRisk
SCOPE Patient p, LabResult l
IF p.age > 50 AND l.bloodSugar > 140
THEN SET p.riskLevel = 'very_high'
PRIORITY 90;
```
**Nói:** "Rule multi-concept - kết hợp Patient và LabResult. Tương tự JOIN trong SQL nhưng ở mức RULE."

---

## DEMO 7: PERFORMANCE (2 phút)

```kbql
-- Xem thống kê hệ thống
MAINTENANCE CHECK;
```

**Nói:** "Hệ thống đạt 200,000+ ops/sec, xử lý 1 triệu records trong ~5 giây với B+ Tree indexing và WAL logging."

---

## DEMO 8: STUDIO UI (nếu có thời gian)

Mở KBMS Studio → Show:
1. **Knowledge Base Explorer** - Tree view các KB, Concepts
2. **Query Editor** - Monaco editor với syntax highlighting
3. **Result Grid** - Hiển thị kết quả
4. **Visual Diagram** - Concept relationship diagram

**Nói:** "Studio được xây dựng bằng React + Electron, cung cấp giao diện trực quan cho việc quản trị tri thức."

---

## BACKUP PLAN (nếu lỗi)

### Nếu CLI lỗi:
- Có sẵn screenshots trong `/docs/assets/screenshots/`
- Hoặc show video demo đã quay trước

### Nếu Server không start:
- Show architecture diagrams thay thế
- Giải thích bằng slides

---

## TÓM TẮT CÂU NÓI CHUYỂN KHI DEMO

| Demo | Key message |
|------|-------------|
| 1. KB Creation | "KB = Database + Rules + Equations" |
| 2. Concept | "Concept có constraints, không chỉ columns" |
| 3. SOLVE() | "Tự động giải phương trình - SQL không làm được" |
| 4. Rules | "Forward chaining tự động, không code IF-ELSE" |
| 5. Hierarchy | "Kế thừa như OOP nhưng ở data level" |
| 6. Multi-concept | "Rules có thể JOIN nhiều concepts" |
| 7. Performance | "200K ops/sec với Rete + B+Tree" |
| 8. Studio | "UI trực quan cho end-users" |

---

## CÂU HỎI THẦY CÓ THỂ HỎI KHI DEMO

**Q: Làm sao hệ thống biết giải phương trình nào?**
> A: "Khi gọi SOLVE(area), Inference Engine tìm equation có area vế trái, dùng Newton-Raphson solver."

**Q: Nếu có nhiều rules conflict?**
> A: "PRIORITY quyết định. Rule priority cao hơn fire trước. Nếu cùng priority → order of creation."

**Q: Demo real-time hay pre-recorded?**
> A: "Real-time. Tôi có thể nhập commands mới để chứng minh."

**Q: Scale như thế nào?**
> A: "Hiện tại single-node. Hướng phát triển: distributed với sharding."

