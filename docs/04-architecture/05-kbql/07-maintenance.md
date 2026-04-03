# Quản trị và Bảo trì Cơ sở Tri thức

**MAINTENANCE** là một lệnh chuyên biệt trong **KBQL**, được thiết kế để thực thi các tác vụ dọn dẹp, tối ưu hóa hiệu năng lưu trữ và kiểm chứng tính nhất quán của hệ thống tri thức.

## 1. Đặc tả Lệnh Bảo trì

Lệnh `MAINTENANCE` được thiết kế để thực thi các tác vụ dọn dẹp và sửa đổi hệ thống ở cấp độ vật lý:

```kbql
MAINTENANCE (
    {VACUUM | REINDEX | CHECK CONSISTENCY} [ON CONCEPT <name>],
    ...
);
```

*Ví dụ:*
```kbql
MAINTENANCE (VACUUM ON CONCEPT Patient, CHECK CONSISTENCY);
```

## 2. Đặc tả các Hoạt động Quản trị

### 2.1. Giải phóng và Tối ưu hóa Bộ nhớ (VACUUM)

Khi một Sự kiện (**Fact**) bị xóa, không gian lưu trữ trong cấu trúc cây **B+ Tree** sẽ không được giải phóng ngay lập tức để tiết kiệm chi phí I/O. Lệnh `VACUUM` thực hiện việc tái cơ cấu các Trang dữ liệu (**Page**) và thu hồi dung lượng đĩa cứng dư thừa.

### 2.2. Tái thiết lập Chỉ mục (REINDEX)

Hoạt động cập nhật toàn diện các cấu trúc chỉ mục (**Index**) liên kết với Khái niệm (**Concept**). Hoạt động này được khuyến nghị thực hiện sau khi hệ thống gặp sự cố mất điện hoặc phát hiện dấu hiệu sai lệch dữ liệu chỉ mục.

### 2.3. Kiểm tra Tính nhất quán Tri thức (CHECK CONSISTENCY)

Tiến trình kiểm chứng các liên kết logic giữa Sự kiện, Luật dẫn (**Rule**) và Hệ thống Phân cấp (**Hierarchy**). Hoạt động này đảm bảo mạng lưới tri thức không tồn tại các mâu thuẫn hình thức hoặc đứt gãy tham chiếu.

## 3. Khuyến nghị Về Chu kỳ Bảo trì

1.  **VACUUM:** Thực thi sau các đợt xóa dữ liệu hàng loạt (Batch deletion) để tối ưu hóa không gian lưu trữ.
2.  **REINDEX:** Thực thi định kỳ hoặc sau các thao tác thay đổi Siêu dữ liệu (Metadata) phức tạp.
3.  **CHECK CONSISTENCY:** Thực thi bắt buộc trước khi triển khai hệ thống suy diễn vào các kịch bản kiểm thử hoặc môi trường vận hành thực tế.

## 4. Ví dụ Thực tế - Quy trình Bảo trì Hệ thống

Dưới đây là các kịch bản thực tế về bảo trì hệ thống KBMS:

### 4.1. Bảo trì Hàng ngày

```kbql
-- Kịch bản: Bảo trì hàng ngày vào lúc 2:00 AM

-- Bước 1: Kiểm tra tính nhất quán của dữ liệu
MAINTENANCE (CHECK CONSISTENCY);

-- Bước 2: Tối ưu hóa các Concept hoạt động nhiều
MAINTENANCE (VACUUM ON CONCEPT Patient);
MAINTENANCE (VACUUM ON CONCEPT Appointment);
MAINTENANCE (VACUUM ON CONCEPT Diagnosis);

-- Bước 3: Cập nhật thống kê hệ thống
MAINTENANCE (UPDATE STATISTICS ON CONCEPT Patient);
MAINTENANCE (UPDATE STATISTICS ON CONCEPT Billing);
```

### 4.2. Bảo trì Tuần hoàn

```kbql
-- Kịch bản: Bảo trì định kỳ vào Chủ nhật hàng tuần

-- Bước 1: Dọn dẹp tất cả các Concept
MAINTENANCE (
    VACUUM ON CONCEPT Patient,
    VACUUM ON CONCEPT Appointment,
    VACUUM ON CONCEPT Diagnosis,
    VACUUM ON CONCEPT Billing,
    VACUUM ON CONCEPT Pharmacy,
    VACUUM ON CONCEPT Inventory
);

-- Bước 2: Tái tạo chỉ mục
MAINTENANCE (
    REINDEX ON CONCEPT Patient,
    REINDEX ON CONCEPT Appointment,
    REINDEX ON CONCEPT Diagnosis
);

-- Bước 3: Kiểm tra toàn bộ tính nhất quán
MAINTENANCE (CHECK CONSISTENCY);

-- Bước 4: Tối ưu hóa không gian bảng điều khiển (Catalog)
MAINTENANCE (VACUUM FULL);
```

### 4.3. Bảo trì Sau Xóa dữ liệu Lớn

```kbql
-- Kịch bản: Sau khi xóa dữ liệu bệnh nhân cũ (hồ sơ > 10 năm)

-- Xóa dữ liệu cũ
DELETE FROM Patient WHERE lastVisit < '2016-04-01';
DELETE FROM Appointment WHERE appointmentDate < '2016-04-01';
DELETE FROM Diagnosis WHERE diagnosisDate < '2016-04-01';

-- Dọn dẹp không gian lưu trữ
MAINTENANCE (VACUUM FULL ON CONCEPT Patient);
MAINTENANCE (VACUUM FULL ON CONCEPT Appointment);
MAINTENANCE (VACUUM FULL ON CONCEPT Diagnosis);

-- Tái tạo chỉ mục sau khi xóa
MAINTENANCE (REINDEX ON CONCEPT Patient);
MAINTENANCE (REINDEX ON CONCEPT Appointment);

-- Kiểm tra tính nhất quán
MAINTENANCE (CHECK CONSISTENCY);
```

### 4.4. Bảo trì Sau Sự cố Hệ thống

```kbql
-- Kịch bản: Khôi phục sau khi mất điện

-- Bước 1: Kiểm tra tính toàn vẹn dữ liệu
MAINTENANCE (CHECK CONSISTENCY);

-- Bước 2: Tái tạo tất cả chỉ mục (có thể bị hỏng)
MAINTENANCE (
    REINDEX ON CONCEPT Patient,
    REINDEX ON CONCEPT Appointment,
    REINDEX ON CONCEPT Diagnosis,
    REINDEX ON CONCEPT Billing,
    REINDEX ON CONCEPT Pharmacy,
    REINDEX ON CONCEPT Inventory
);

-- Bước 3: Dọn dẹp các transaction chưa hoàn thành
MAINTENANCE (CLEANUP TRANSACTIONS);

-- Bước 4: Tối ưu hóa toàn bộ
MAINTENANCE (VACUUM FULL);
```

### 4.5. Bảo trì Trước khi Triển khai Production

```kbql
-- Kịch bản: Chuẩn bị triển khai hệ thống vào môi trường production

-- Bước 1: Kiểm tra toàn bộ tính nhất quán
MAINTENANCE (CHECK CONSISTENCY);

-- Bước 2: Dọn dẹp và tối ưu hóa
MAINTENANCE (
    VACUUM FULL ON CONCEPT Patient,
    VACUUM FULL ON CONCEPT Appointment,
    VACUUM FULL ON CONCEPT Diagnosis,
    VACUUM FULL ON CONCEPT Billing,
    VACUUM FULL ON CONCEPT Pharmacy,
    VACUUM FULL ON CONCEPT Inventory
);

-- Bước 3: Tạo lại tất cả chỉ mục
MAINTENANCE (REBUILD ALL INDEXES);

-- Bước 4: Cập nhật thống kê cho trình tối ưu hóa
MAINTENANCE (UPDATE ALL STATISTICS);

-- Bước 5: Xác minh cấu trúc database
MAINTENANCE (VALIDATE SCHEMA);

-- Bước 6: Tạo bản sao lưu (backup)
-- (external command, not KBQL)
```

### 4.6. Bảo trì Theo dõi Hiệu năng

```kbql
-- Kịch bản: Theo dõi và tối ưu hóa hiệu năng

-- Tạo Concept để theo dõi hiệu năng
CREATE CONCEPT PerformanceMetrics (
    VARIABLES (
        metricId: STRING,
        metricName: STRING,
        metricValue: DECIMAL,
        recordedAt: DATETIME
    )
);

-- Theo dõi kích thước các Concept
INSERT INTO PerformanceMetrics ATTRIBUTE (
    'PM001', 'Patient_Concept_Size_MB',
    (SELECT CALC(size/1024/1024) FROM system.catalog WHERE name = 'Patient'),
    '2026-04-03 10:00'
);

-- Theo dõi số lượng records
INSERT INTO PerformanceMetrics ATTRIBUTE (
    'PM002', 'Patient_Record_Count',
    (SELECT COUNT(*) FROM Patient),
    '2026-04-03 10:00'
);

-- Bảo trì dựa trên metrics
MAINTENANCE (
    VACUUM ON CONCEPT Patient,
    REINDEX ON CONCEPT Patient
);
```

### 4.7. Kịch bản Bảo trì Tự động

```kbql
-- Tạo stored procedure cho bảo trì tự động
CREATE PROCEDURE DailyMaintenance()
BEGIN
    -- Bước 1: Ghi log bắt đầu
    INSERT INTO MaintenanceLog ATTRIBUTE (
        'LOG001', 'Daily Maintenance', 'Started', '2026-04-03 02:00'
    );

    -- Bước 2: Thực hiện bảo trì
    MAINTENANCE (
        VACUUM ON CONCEPT Patient,
        VACUUM ON CONCEPT Appointment,
        VACUUM ON CONCEPT Diagnosis,
        CHECK CONSISTENCY
    );

    -- Bước 3: Ghi log hoàn thành
    INSERT INTO MaintenanceLog ATTRIBUTE (
        'LOG002', 'Daily Maintenance', 'Completed', '2026-04-03 02:15'
    );
END;

-- Gọi stored procedure
-- EXECUTE DailyMaintenance();
```

### 4.8. Lịch Bảo trì Khuyến nghị

| Tần suất | Hoạt động | Command |
|:---|:---|:---|
| **Hàng ngày** | VACUUM các Concept hoạt động nhiều | `MAINTENANCE (VACUUM ON CONCEPT Patient)` |
| **Hàng tuần** | VACUUM tất cả, REINDEX | `MAINTENANCE (VACUUM, REINDEX)` |
| **Hàng tháng** | CHECK CONSISTENCY đầy đủ | `MAINTENANCE (CHECK CONSISTENCY)` |
| **Sau xóa lớn** | VACUUM FULL, REINDEX | `MAINTENANCE (VACUUM FULL, REINDEX)` |
| **Sau sự cố** | REBUILD ALL INDEXES | `MAINTENANCE (REBUILD ALL INDEXES)` |
| **Trước deploy** | VALIDATE SCHEMA | `MAINTENANCE (VALIDATE SCHEMA)` |
