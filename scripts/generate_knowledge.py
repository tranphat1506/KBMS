import csv
import uuid
import random
import os
from faker import Faker

fake = Faker()
Faker.seed(42)

# Configuration
DATASETS_DIR = "datasets"
DS_S_DIR = os.path.join(DATASETS_DIR, "ds_small_geometry")
DS_M_DIR = os.path.join(DATASETS_DIR, "ds_medium_medical")
DS_L_DIR = os.path.join(DATASETS_DIR, "ds_large_smart_city")

os.makedirs(DS_S_DIR, exist_ok=True)
os.makedirs(DS_M_DIR, exist_ok=True)
os.makedirs(DS_L_DIR, exist_ok=True)

def generate_geometry(count=100):
    print("Generating DS-S (Advanced Geometry)...")
    points = []
    triangles = []
    
    # [Advanced Headers]
    header = """-- [COKB Advanced Package] Geometry (DS-S)
DROP KNOWLEDGE BASE Geometry_KB;
CREATE KNOWLEDGE BASE Geometry_KB;
USE Geometry_KB;

-- [C] Concepts with Advanced Blocks
CREATE CONCEPT Point (
    VARIABLES (id: STRING, x: FLOAT, y: FLOAT),
    PROPERTIES (domain: 'Geometry_2D', precision: 'float64')
);

CREATE CONCEPT Triangle (
    VARIABLES (id: STRING, side_a: FLOAT, side_b: FLOAT, side_c: FLOAT, t_type: STRING),
    CONSTRAINTS (
        C1: side_a > 0 AND side_b > 0 AND side_c > 0,
        C2: (side_a + side_b) > side_c
    ),
    RULES (
        RULE R_RightTriangle: IF (side_a*side_a + side_b*side_b) = (side_c*side_c) THEN SET t_type = 'RightTriangle'
    ),
    PROPERTIES (author: 'Nguyen An', version: '1.0')
);

-- [Relation] vertex-poly mapping
CREATE RELATION HasVertex FROM Triangle TO Point;

-- [Funcs] Geometric Calculations
CREATE FUNCTION GetDistance(x1: FLOAT, y1: FLOAT, x2: FLOAT, y2: FLOAT) RETURNS FLOAT 
BODY {
    RETURN SQRT((x1-x2)*(x1-x2) + (y1-y2)*(y1-y2));
}

-- [Data] Bulk Ingestion
"""
    # Generate Points
    for i in range(count * 3):
        pid = f"P{i:03d}"
        x = round(random.uniform(0, 100), 2)
        y = round(random.uniform(0, 100), 2)
        points.append({"id": pid, "x": x, "y": y})
    
    # Generate Triangles
    insert_bulks = []
    for i in range(count):
        tid = f"T{i:03d}"
        p1, p2, p3 = points[i*3], points[i*3+1], points[i*3+2]
        d12 = round(((p1['x']-p2['x'])**2 + (p1['y']-p2['y'])**2)**0.5, 2)
        d23 = round(((p2['x']-p3['x'])**2 + (p2['y']-p3['y'])**2)**0.5, 2)
        d31 = round(((p3['x']-p1['x'])**2 + (p3['y']-p1['y'])**2)**0.5, 2)
        triangles.append({"id": tid, "side_a": d12, "side_b": d23, "side_c": d31})
        insert_bulks.append(f"('{tid}', {d12}, {d23}, {d31}, 'Normal')")

    # Save setup_kb.kbql
    with open(os.path.join(DS_S_DIR, "setup_kb.kbql"), 'w') as f:
        f.write(header)
        f.write("INSERT BULK INTO Triangle ATTRIBUTE\n")
        f.write(",\n".join(insert_bulks) + ";\n")

    save_csv(os.path.join(DS_S_DIR, "points.csv"), points)

def generate_medical(count=10000):
    print("Generating DS-M (Advanced Medical)...")
    header = """-- [COKB Advanced Package] Medical Expert (DS-M)
DROP KNOWLEDGE BASE Medical_KB;
CREATE KNOWLEDGE BASE Medical_KB;
USE Medical_KB;

-- [C] Concepts + [H] Hierarchy
CREATE CONCEPT Disease (
    VARIABLES (d_name: STRING),
    PROPERTIES (category: 'DiagnosticCodes')
);

CREATE CONCEPT ChronicDisease (
    VARIABLES (d_name: STRING)
);

CREATE HIERARCHY ChronicDisease IS_A Disease;

CREATE CONCEPT Patient (
    VARIABLES (
        p_id: STRING, p_name: STRING, p_sex: STRING, p_age: INT, p_bmi: FLOAT, 
        cholesterol: INT, blood_pressure: INT, heart_rate: INT, glucose: FLOAT,
        is_smoker: BOOLEAN, is_alcohol: BOOLEAN, physical_activity: INT,
        stress_level: INT, sleep_hours: FLOAT, waist_circ: FLOAT,
        family_history: BOOLEAN, insurance: STRING, p_risk: STRING,
        createdAt: DATETIME, updatedAt: DATETIME
    ),
    ALIASES (BenhNhan, Subject),
    CONSTRAINTS (
        C_ValidAge: p_age >= 0 AND p_age < 120,
        C_ValidBMI: p_bmi > 10 AND p_bmi < 60
    ),
    RULES (
        -- 1. Luật xác định Hội chứng chuyển hóa (Metabolic Syndrome)
        RULE R_Metabolic: IF p_bmi > 30 AND waist_circ > 90 AND glucose > 110 AND blood_pressure > 135 THEN SET p_risk = 'Metabolic_Syndrome',
        
        -- 2. Luật cảnh báo Nguy cơ Đột quỵ (Stroke Risk)
        RULE R_Stroke: IF p_age > 70 AND is_smoker = TRUE AND blood_pressure > 160 THEN SET p_risk = 'High_Stroke_Risk',
        
        -- 3. Luật phân loại Tăng huyết áp Độ 2 (Stage 2 Hypertension)
        RULE R_Hypertension: IF blood_pressure > 160 THEN SET p_risk = 'Hypertension_Stage_2',
        
        -- 4. Luật xác định Nhóm Sức khỏe Tối ưu (Optimal Health)
        RULE R_OptimalHealth: IF blood_pressure < 120 AND cholesterol < 200 AND is_smoker = FALSE AND physical_activity > 5 THEN SET p_risk = 'Optimal_Health',
        
        -- 5. Luật cảnh báo Tiền tiểu đường (Pre-diabetes)
        RULE R_PreDiabetes: IF glucose > 100 AND glucose < 125 THEN SET p_risk = 'Pre_Diabetes',
        
        -- 6. Luật cảnh báo Nguy cơ Suy tim (Heart Failure Susceptibility)
        RULE R_HeartFailure: IF p_age > 60 AND heart_rate > 100 THEN SET p_risk = 'Heart_Failure_Susceptibility',
        
        -- 7. Luật Stress và Chất lượng giấc ngủ (Stress-Sleep Impact)
        RULE R_SleepDeprivation: IF stress_level > 8 AND sleep_hours < 6 THEN SET p_risk = 'High_Stress_Sleep_Deprived'
    ),
    PROPERTIES (domain: 'Cardiology', expert_system: 'KBMS_V3_Medical_Core')
);

CREATE CONCEPT Symptom (
    VARIABLES (name: STRING, severity: INT)
);

-- [Relation] exhibits
CREATE RELATION Exhibits FROM Patient TO Symptom;

-- [Data]
"""
    patients = []
    insert_bulks = []
    
    for i in range(count):
        uid = f"PAT{i:05d}"
        name = fake.name()
        sex = random.choice(["Male", "Female"])
        age = random.randint(20, 95)
        bmi = round(random.uniform(18, 42), 1)
        chol = random.randint(140, 350)
        bp = random.randint(90, 200)
        hr = random.randint(50, 110)
        glu = round(random.uniform(70, 180), 1)
        smoker = random.choice(["TRUE", "FALSE"])
        alcohol = random.choice(["TRUE", "FALSE"])
        activity = random.randint(0, 7)
        stress = random.randint(1, 10)
        sleep = round(random.uniform(4, 10), 1)
        waist = round(random.uniform(60, 120), 1)
        fam = random.choice(["TRUE", "FALSE"])
        ins = random.choice(["Basic", "Premium", "Gov"])
        created = fake.date_time_this_decade().strftime("%Y-%m-%d %H:%M:%S")
        updated = fake.date_time_this_year().strftime("%Y-%m-%d %H:%M:%S")
        
        patients.append({
            "id": uid, "name": name, "sex": sex, "age": age, "bmi": bmi, 
            "chol": chol, "bp": bp, "hr": hr, "glu": glu, "smoker": smoker,
            "alcohol": alcohol, "activity": activity, "stress": stress, "sleep": sleep,
            "waist": waist, "fam": fam, "ins": ins, "created": created, "updated": updated
        })
        insert_bulks.append(f"('{uid}', '{name}', '{sex}', {age}, {bmi}, {chol}, {bp}, {hr}, {glu}, {smoker}, {alcohol}, {activity}, {stress}, {sleep}, {waist}, {fam}, '{ins}', 'Normal', '{created}', '{updated}')")

    with open(os.path.join(DS_M_DIR, "setup_kb.kbql"), 'w') as f:
        f.write(header)
        f.write(f"-- Dataset contains {count} rich patient profiles (20 features).\n")
        f.write("INSERT BULK INTO Patient ATTRIBUTE\n")
        f.write(",\n".join(insert_bulks[:1000]) + ";\n")

    save_csv(os.path.join(DS_M_DIR, "patients.csv"), patients)

def generate_iot(count=1000000):
    print("Generating DS-L (Advanced Smart City IoT)...")
    header = f"""-- [COKB Advanced Package] Smart City IoT (DS-L)
DROP KNOWLEDGE BASE City_KB;
CREATE KNOWLEDGE BASE City_KB;
USE City_KB;

-- [C] Concepts + [H] Space Hierarchy
CREATE CONCEPT City (
    VARIABLES (c_name: STRING)
);

CREATE CONCEPT Zone (
    VARIABLES (z_id: STRING, status: STRING),
    PROPERTIES (type: 'SpatialArea')
);

CREATE CONCEPT Sensor (
    VARIABLES (s_id: STRING, speed: INT, zone_id: STRING),
    BASE_OBJECTS (Zone),
    CONSTRAINTS (C_ValidSpeed: speed >= 0),
    RULES (
        -- Luật lan truyền tắc nghẽn (Transitive Congestion)
        RULE R_TrafficJam: IF speed < 10 THEN SET zone_id.status = 'JAMMED'
    ),
    PROPERTIES (source: 'Vortex_Telemetry', version: '4.0.9')
);

CREATE HIERARCHY Zone PART_OF City;

-- [Data] Bulk Import from CSV (Production Mode)
IMPORT (CONCEPT: Sensor, FORMAT: CSV, FILE: 'sensors.csv');
"""
    with open(os.path.join(DS_L_DIR, "setup_kb.kbql"), 'w') as f:
        f.write(header)
    
    path = os.path.join(DS_L_DIR, "sensors.csv")
    with open(path, 'w', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=["id", "speed", "zone_id"])
        writer.writeheader()
        for i in range(count):
            writer.writerow({
                "id": f"S{i:07d}",
                "speed": random.randint(0, 120),
                "zone_id": f"Z{i // 1000:04d}"
            })
    print(f"Generated {path}")

def save_csv(path, data):
    if not data: return
    keys = data[0].keys()
    with open(path, 'w', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=keys)
        writer.writerows(data)

if __name__ == "__main__":
    generate_geometry(100)
    generate_medical(10000)
    generate_iot(1000000)
    print("Advanced Knowledge Pkgs Generated.")
