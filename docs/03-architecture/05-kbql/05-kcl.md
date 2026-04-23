# Ngôn ngữ Kiểm soát và Quản trị Tri thức (KCL)

**KCL** (Knowledge Control Language) tập hợp các lệnh để quản lý hệ thống bảo mật, tài khoản người dùng và phân quyền truy cập thông tin trong cơ sở tri thức.

## 1. Cơ chế Quản lý Tài khoản Người dùng

Hệ thống cho phép cấu hình định danh và vai trò để bảo vệ dữ liệu tri thức khỏi các truy cập không hợp lệ.

### 1.1. Khởi tạo Người dùng mới (CREATE USER)

```kbql
CREATE USER <username> 
PASSWORD '<password>' 
[ROLE {ADMIN | SERVICE | USER}];
```

*Ví dụ:*
```kbql
CREATE USER medic_nlp 
PASSWORD 'securepass123' 
ROLE SERVICE;
```

### 1.2. Hiệu chỉnh Thông tin Tài khoản (ALTER USER)

Lệnh `ALTER USER` hỗ trợ thay đổi mật khẩu hoặc trạng thái quản trị của tài khoản:

```kbql
ALTER USER <username> (
    SET (PASSWORD: '<new_password>', ADMIN: true)
);
```

*Ví dụ:*
```kbql
ALTER USER medic_nlp (
    SET (PASSWORD: 'new_pass_456', ADMIN: false)
);
```

### 1.3. Loại bỏ Tài khoản (DROP USER)

```kbql
DROP USER <username>;
```

*Ví dụ:*
```kbql
DROP USER old_employee;
```

## 2. Quản trị Quyền hạn và Phân quyền

Cơ chế phân quyền cho phép giới hạn khả năng thao tác của người dùng trên các Khái niệm (Concept) và thực thể tri thức cụ thể.

### 2.1. Cấp quyền (GRANT)

```kbql
GRANT {SELECT, INSERT, UPDATE, DELETE, ...} 
ON CONCEPT <concept_name> 
TO <username>;
```

*Ví dụ:*
```kbql
GRANT SELECT, INSERT 
ON CONCEPT Patient 
TO medic_nlp;
```

### 2.2. Thu hồi quyền (REVOKE)

```kbql
REVOKE {SELECT, INSERT, UPDATE, DELETE, ...} 
ON CONCEPT <concept_name> 
FROM <username>;
```

*Ví dụ:*
```kbql
REVOKE DELETE 
ON CONCEPT Patient 
FROM medic_nlp;
```

## 3. Hệ thống Vai trò và Quyền hạn Đặc quyền

Hệ thống phân cấp quyền hạn dựa trên ba nhóm vai trò chính:

*   **ADMIN**: Nhóm quyền quản trị tối cao, có khả năng thao tác trên tất cả các cơ sở tri thức, khái niệm và tài khoản người dùng.
*   **USER**: Nhóm quyền mặc định của người dùng cuối, hành vi thao tác cần được cấp phép cụ thể cho từng thực thể.

## 4. Ví dụ Thực tế - Quản trị Bảo mật Hệ thống Y tế

Dưới đây là kịch bản hoàn chỉnh về việc thiết lập bảo mật cho hệ thống KBMS trong bệnh viện:

### 4.1. Thiết lập Người dùng và Vai trò

```kbql
-- Tạo tài khoản Quản trị hệ thống
CREATE USER admin
PASSWORD 'Admin@2026!Secure'
ROLE ADMIN;

-- Tạo tài khoản Bác sĩ
CREATE USER dr_nguyen
PASSWORD 'DrNguyen@Med123'
ROLE USER;

-- Tạo tài khoản Y tá
CREATE USER nurse_trinh
PASSWORD 'NurseTrinh@456'
ROLE USER;

-- Tạo tài khoản Kế toán
CREATE USER accountant_lan
PASSWORD 'Accountant@789'
ROLE USER;

-- Tạo tài khoản Dịch vụ tự động (cho ứng dụng)
CREATE USER emr_service
PASSWORD 'EmrService@ApiKey2026'
ROLE SERVICE;

-- Hiệu chỉnh thông tin tài khoản
ALTER USER dr_nguyen (
    SET (PASSWORD: 'NewDrNguyen@2026', ADMIN: false)
);

-- Xóa tài khoản nhân viên nghỉ việc
DROP USER old_employee;
```

### 4.2. Phân quyền Chi tiết cho từng Vai trò

```kbql
-- Phân quyền cho Bác sĩ: Đọc và ghi bệnh nhân, không xóa
GRANT SELECT, INSERT, UPDATE
ON CONCEPT Patient
TO dr_nguyen;

GRANT SELECT, INSERT, UPDATE
ON CONCEPT Appointment
TO dr_nguyen;

GRANT SELECT
ON CONCEPT Diagnosis
TO dr_nguyen;

-- Phân quyền cho Y tá: Chỉ được đọc bệnh nhân và tạo lịch hẹn
GRANT SELECT, INSERT
ON CONCEPT Patient
TO nurse_trinh;

GRANT SELECT, INSERT, UPDATE
ON CONCEPT Appointment
TO nurse_trinh;

-- Y tá không được xem chẩn đoán (không cấp quyền)
-- Không được phép xóa bệnh nhân

-- Phân quyền cho Kế toán: Chỉ được đọc dữ liệu thanh toán
CREATE CONCEPT Billing (
    VARIABLES (
        billId: STRING,
        patientId: STRING,
        amount: DECIMAL,
        status: STRING,
        paymentDate: DATE
    )
);

GRANT SELECT, UPDATE
ON CONCEPT Billing
TO accountant_lan;

-- Kế toán không được xem chi tiết bệnh án
REVOKE SELECT
ON CONCEPT Diagnosis
FROM accountant_lan;

-- Phân quyền cho Service: Chỉ được INSERT và SELECT
GRANT SELECT, INSERT
ON CONCEPT Patient
TO emr_service;

GRANT SELECT
ON CONCEPT Appointment
TO emr_service;
```

### 4.3. Quản lý Quyền theo Nhóm

```kbql
-- Tạo nhóm bác sĩ khoa tim mạch
CREATE USER dr_cardio_1 PASSWORD 'pass1' ROLE USER;
CREATE USER dr_cardio_2 PASSWORD 'pass2' ROLE USER;
CREATE USER dr_cardio_3 PASSWORD 'pass3' ROLE USER;

-- Cấp quyền chung cho nhóm
GRANT SELECT, INSERT, UPDATE
ON CONCEPT CardiologyRecord
TO dr_cardio_1, dr_cardio_2, dr_cardio_3;

-- Thu hồi quyền Xóa cho tất cả
REVOKE DELETE
ON CONCEPT CardiologyRecord
FROM dr_cardio_1, dr_cardio_2, dr_cardio_3;
```

### 4.4. Kịch bản Thay đổi Vai trò

```kbql
-- Thăng chức Bác sĩ thành Trưởng khoa
ALTER USER dr_nguyen (
    SET (ADMIN: true)
);

-- Chuyển y tá sang vị trí khác (thu hồi quyền cũ)
REVOKE SELECT, INSERT, UPDATE
ON CONCEPT Patient
FROM nurse_trinh;

REVOKE SELECT, INSERT, UPDATE
ON CONCEPT Appointment
FROM nurse_trinh;

-- Cấp quyền mới cho vị trí kho dược
CREATE CONCEPT Pharmacy (
    VARIABLES (
        medicineId: STRING,
        name: STRING,
        stock: INT,
        expiryDate: DATE
    )
);

GRANT SELECT, UPDATE
ON CONCEPT Pharmacy
TO nurse_trinh;
```

### 4.5. Kiểm tra Quyền hạn

```kbql
-- Xem danh sách người dùng (chỉ Admin)
SHOW USERS;

-- Xem quyền hạn hiện tại
SHOW GRANTS FOR dr_nguyen;

-- Kiểm tra xem một user có quyền cụ thể không
EXPLAIN (SELECT * FROM Patient WHERE patientId = 'P001');
-- Hệ thống sẽ báo lỗi nếu user không có quyền SELECT
```

### 4.6. Ví dụ về Ma trận Quyền hạn

| Vai trò | Patient | Appointment | Diagnosis | Billing | Pharmacy |
|:---|:---:|:---:|:---:|:---:|:---:|
| **Admin** | ALL | ALL | ALL | ALL | ALL |
| **Bác sĩ** | SELECT, INSERT, UPDATE | SELECT, INSERT, UPDATE | SELECT | - | - |
| **Y tá** | SELECT, INSERT | SELECT, INSERT, UPDATE | - | - | - |
| **Kế toán** | - | SELECT | - | SELECT, UPDATE | - |
| **Dược sĩ** | - | - | - | - | SELECT, UPDATE |
| **Service** | SELECT, INSERT | SELECT | - | - | - |

```kbql
-- Triển khai ma trận quyền hạn trên
-- Tạo user Dược sĩ
CREATE USER pharmacist_hung PASSWORD 'hung123' ROLE USER;

-- Cấp quyền cho Dược sĩ
GRANT SELECT, UPDATE
ON CONCEPT Pharmacy
TO pharmacist_hung;

-- Kiểm tra và xác thực quyền
SELECT
    username,
    target_object,
    privileges
FROM system.privileges
WHERE username IN ('dr_nguyen', 'nurse_trinh', 'accountant_lan', 'pharmacist_hung')
ORDER BY username, target_object;
```
