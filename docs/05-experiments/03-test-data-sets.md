## 1. DS-S: Gói Tri thức Hình học (Advanced)
- **Kịch bản**: `datasets/ds_small_geometry/setup_kb.kbql`
- **Mã nguồn KML chuẩn**:
```kbql
-- Reset môi trường thực nghiệm
DROP KNOWLEDGE BASE Geometry_KB;
CREATE KNOWLEDGE BASE Geometry_KB;
USE Geometry_KB;

CREATE CONCEPT Triangle (
    VARIABLES (id: STRING, side_a: FLOAT, side_b: FLOAT, side_c: FLOAT, t_type: STRING),
    CONSTRAINTS (
        C1: side_a > 0 AND side_b > 0 AND side_c > 0,
        C2: (side_a + side_b) > side_c  -- Ràng buộc bất đẳng thức tam giác
    ),
    RULES (
        R_RightTriangle: IF (side_a*side_a + side_b*side_b) = (side_c*side_c) 
                          THEN SET t_type = 'RightTriangle'
    ),
    PROPERTIES (author: 'Nguyen An', version: '1.0', domain: 'Math_2D')
);
```

## 2. DS-M: Gói Tri thức Y tế (Advanced Features)
- **Mục tiêu**: Quản trị thực thể "Dày" với Ràng buộc (Constraints) và Bí danh (Aliases).
- **Kịch bản**: `datasets/ds_medium_medical/setup_kb.kbql`
```kbql
CREATE CONCEPT Patient (
    VARIABLES (
        p_id: STRING, p_name: STRING, p_age: INT,
        p_bmi: FLOAT, cholesterol: INT, blood_pressure: INT, 
        p_risk: STRING, createdAt: DATETIME
    ),
    ALIASES (BenhNhan, Subject), -- Bí danh định danh tri thức
    CONSTRAINTS (
        C_ValidAge: p_age >= 0 AND p_age < 120, -- Kiểm soát chất lượng dữ liệu
        C_ValidBMI: p_bmi > 10 AND p_bmi < 60
    ),
    RULES (
        RULE R_Stroke: IF p_age > 70 AND is_smoker = TRUE AND blood_pressure > 160 
                  THEN SET p_risk = 'High_Stroke_Risk'
    ),
    PROPERTIES (domain: 'Cardiology', expert_system: 'KBMS_V3_Medical_Core')
);
```

## 3. DS-L: Gói Tri thức Đô thị Thông minh (Scale context)
- **Mục tiêu**: Thử nghiệm bao đóng tri thức trên 1 triệu cảm biến với cấu trúc vật lý.
- **Kịch bản**: `datasets/ds_large_smart_city/setup_kb.kbql`
```kbql
CREATE CONCEPT Sensor (
    VARIABLES (s_id: STRING, speed: INT, zone_id: STRING),
    BASE_OBJECTS (Zone), -- Khai báo thành phần cấu thành vật lý
    CONSTRAINTS (C_ValidSpeed: speed >= 0),
    RULES (
        R_TrafficJam: IF speed < 10 THEN SET zone_id.status = 'JAMMED'
    ),
    PROPERTIES (source: 'Telemetry_API', version: '4.0')
);

CREATE HIERARCHY Zone PART_OF City;
```
