# Use Case: Hệ chuyên gia Chẩn đoán Y tế

KBMS có thể phục vụ như một hệ chuyên gia mạnh mẽ (Expert System) để hỗ trợ chẩn đoán bệnh dựa trên các triệu chứng và tiền sử của bệnh nhân thông qua các luật logic `IF-THEN`.

## 1. Bài toán: Chẩn đoán Cúm và Viêm họng

Giả sử chúng ta muốn xây dựng một bộ luật để phân biệt giữa Cúm và Viêm họng dựa trên nhiệt độ cơ thể và các triệu chứng đi kèm.

### Bước 1: Định nghĩa Concept Bệnh nhân

```kbql
CREATE CONCEPT PatientRecord (
    VARIABLES (
        id: INT,
        name: STRING,
        temperature: DECIMAL,
        sore_throat: BOOLEAN, -- Đau họng
        cough: BOOLEAN, -- Ho
        diagnosis: STRING, -- Chẩn đoán
        severity: STRING -- Mức độ
    )
);
```

### Bước 2: Thiết lập Bộ luật Chẩn đoán

Chúng ta sử dụng Forward Chaining Rules để đưa ra kết luận:

```kbql
-- Luật 1: Chẩn đoán Sốt
CREATE RULE FeverRule
SCOPE PatientRecord
IF temperature > 37.5
THEN SET severity = 'Moderate';

-- Luật 2: Chẩn đoán Cúm
CREATE RULE FluRule
SCOPE PatientRecord
IF temperature > 38.5 AND cough = true
THEN SET diagnosis = 'Flu' AND SET severity = 'High';

-- Luật 3: Chẩn đoán Viêm họng
CREATE RULE ThroatRule
SCOPE PatientRecord
IF temperature > 37.5 AND sore_throat = true
THEN SET diagnosis = 'Sore Throat';
```

---

## 2. Thực thi Chẩn đoán

### Nhập dữ liệu bệnh nhân
Giả sử có một bệnh nhân tên Alice với các triệu chứng: nhiệt độ 39 độ và ho.

```kbql
INSERT INTO PatientRecord ATTRIBUTE (101, 'Alice', 39.0, false, true, 'Unknown', 'None');

-- Xem kết quả chẩn đoán
SELECT name, diagnosis, severity FROM PatientRecord;
```

**Kết quả mong đợi:**
*   `diagnosis` = 'Flu'
*   `severity` = 'High' (Luật FluRule ghi đè mức độ 'Moderate' của FeverRule).

---

## 3. Suy diễn nâng cao: Chuỗi kết luận

KBMS hỗ trợ suy diễn theo chuỗi (Inference Chain):

1.  **Fact mới:** `Alice.temperature = 39.0` nạp vào GT.
2.  **Fire Rule 1:** `Alice.severity = 'Moderate'` (Vì 39.0 > 37.5).
3.  **Fire Rule 2:** `Alice.diagnosis = 'Flu'` và `Alice.severity` được cập nhật thành `'High'` (Vì 39.0 > 38.5 và ho = true).
4.  **Kết thúc:** Hệ thống không tìm thấy luật nào khác để kích hoạt.

---

## 4. Lợi ích khi dùng KBMS cho Y tế

*   **Tính minh bạch (Explainability):** Bác sĩ có thể sử dụng `Reasoning Trace` để biết tại sao hệ thống kết luận bệnh nhân bị Cúm (Dựa trên triệu chứng cụ thể nào).
*   **Tính linh hoạt:** Có thể thêm các biến mới (như huyết áp, nhịp tim) và các luật mới mà không cần lập trình lại toàn bộ hệ thống.
*   **Ràng buộc (Constraints):** Bạn có thể thêm các ràng buộc như `CONSTRAINT (age > 0)` để đảm bảo tính hợp lệ của hồ sơ bệnh án.
