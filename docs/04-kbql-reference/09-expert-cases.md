# Tài liệu Chuyên gia: Các trường hợp Cực hạn (Expert Case Studies)

Tài liệu này trình bày các tình huống sử dụng KBQL ở mức độ nâng cao, khai thác tối đa sức mạnh của bộ máy suy diễn và giải toán của KBMS.

---

## 1. Giải ngược Phương trình (Inverse Problem Solving)

Hệ thống KBMS không chỉ tính toán một chiều mà còn có khả năng giải ngược các tham số đầu vào khi biết kết quả đầu ra thông qua các thuật toán **Newton-Raphson** và **Brent**.

### Kịch bản: Tính toán mạch điện
Chúng ta biết luật Ohm `U = I * R`. Nếu biết `U` và `I`, hệ thống phải tự tìm `R`.

```kbql
CREATE CONCEPT Resistor (
    VARIABLES (u: DECIMAL, i: DECIMAL, r: DECIMAL),
    EQUATIONS (u = i * r)
);

-- Trường hợp 1: Biết I và R, tìm U (Tính xuôi)
SOLVE ON CONCEPT Resistor GIVEN i = 2, r = 10 FIND u; -- Kết quả: u = 20

-- Trường hợp 2: Biết U và I, tìm R (Tính ngược - Expert)
SOLVE ON CONCEPT Resistor GIVEN u = 220, i = 2 FIND r; -- Kết quả: r = 110
```

---

## 2. Suy diễn Chùm (Chained Inference)

Thể hiện khả năng lan truyền tri thức qua nhiều lớp luật (Forward Chaining).

### Kịch bản: Hệ thống Cảnh báo sớm
```kbql
CREATE RULE R1 SCOPE Sensor IF temp > 100 THEN SET status = 'Overheat';
CREATE RULE R2 SCOPE Sensor IF status = 'Overheat' AND pressure > 50 THEN SET alert = 'Critical';
CREATE RULE R3 SCOPE Sensor IF alert = 'Critical' THEN DO (PRINT 'EMERGENCY SHUTDOWN');

-- Khi chèn dữ liệu thỏa mãn chuỗi:
INSERT INTO Sensor ATTRIBUTE (110, 60);
-- Kết quả: R1 kích hoạt -> status='Overheat' -> R2 kích hoạt -> alert='Critical' -> R3 kích hoạt -> In ra thông báo.
```

---

## 3. Phân cấp Kế thừa Đa tầng (Recursive Property Propagation)

Khi định nghĩa Hierarchy, các luật ở Concept cha sẽ tự động "chảy" xuống toàn bộ các con, cháu.

### Kịch bản: Phân loại Hình học
```kbql
CREATE CONCEPT Shape (VARIABLES (color: STRING));
CREATE CONCEPT Polygon (...);
CREATE CONCEPT Triangle (...);

ADD HIERARCHY Polygon IS_A Shape;
ADD HIERARCHY Triangle IS_A Polygon;

CREATE RULE ShapeColor SCOPE Shape IF color = 'Red' THEN SET is_hot = true;

-- Khi chèn một Triangle màu đỏ:
INSERT INTO Triangle ATTRIBUTE ('Red', ...);
-- Kết quả: Mặc dù luật định nghĩa ở Shape, nhưng Triangle vẫn nhận được is_hot = true.
```

---

## 4. Ràng buộc nội bộ phức tạp (Internal Constraints)

Sử dụng khối `CONSTRAINTS` để bảo vệ dữ liệu ở mức tri thức, không cho phép các Fact mâu thuẫn tồn tại.

### Kịch bản: Quản lý nhân sự
```kbql
CREATE CONCEPT Employee (
    VARIABLES (age: INT, experience: INT),
    CONSTRAINTS (
        IF age < experience + 18 THEN DO (ERROR 'Tuổi không hợp lệ so với kinh nghiệm')
    )
);

-- Lệnh này sẽ bị Parser/Engine chặn lại:
INSERT INTO Employee ATTRIBUTE (20, 5); -- (20 < 5 + 18) => Lỗi.
```

---

## 5. Giải hệ phương trình 2 ẩn (Newton-Raphson 2D)

KBMS hỗ trợ giải đồng thời các hệ thức toán học.

### Kịch bản: Tìm giao điểm đường thẳng
```kbql
CREATE CONCEPT Intersection (
    VARIABLES (x: DECIMAL, y: DECIMAL),
    EQUATIONS (
        y = 2 * x + 1,
        y = -x + 4
    )
);

SOLVE ON CONCEPT Intersection FIND x, y;
-- Kết quả: x = 1, y = 3 (Hệ thống tự giải hệ phương trình).
```

---

## 6. Kết hợp Trigger và Rule (The Chain Reaction)

Dùng Trigger để kích hoạt một luồng suy diễn ở một Concept hoàn toàn khác.

### Kịch bản: Đồng bộ kho và đơn hàng
```kbql
CREATE TRIGGER SyncStock ON (INSERT OF Order) 
DO (UPDATE Product ATTRIBUTE (SET stock: stock - 1) WHERE id = new.product_id);

-- Khi cập nhật kho, các luật về 'Out of stock' sẽ tự động chạy:
CREATE RULE OutOfStock SCOPE Product IF stock < 0 THEN SET status = 'Refill';
```
