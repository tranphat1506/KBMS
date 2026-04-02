# Đặc tả Kịch bản Thực thi Tri thức Nâng cao

Tài liệu này trình bày các tình huống vận dụng ngôn ngữ **KBQL** ở mức độ chuyên sâu, khai thác tối đa sức mạnh của bộ máy suy diễn và giải toán tích hợp trong hệ quản trị **KBMS**.

## 1. Cơ chế Giải toán Ngược (Inverse Problem Solving)

Hệ thống KBMS không chỉ thực hiện tính toán thuận mà còn có khả năng giải ngược các tham số đầu vào khi biết kết quả đầu ra, thông qua các thuật toán **Newton-Raphson** và **Brent**.

### 1.1. Kịch bản: Phân tích Mạch điện hình thức

Dựa trên Định luật Ohm: `U = I * R`.

```kbql
CREATE CONCEPT Resistor (
    VARIABLES (u: DECIMAL, i: DECIMAL, r: DECIMAL),
    EQUATIONS (u = i * r)
);

-- Kịch bản 1: Biết I và R, xác định U (Tính toán thuận)
SOLVE ON CONCEPT Resistor GIVEN i = 2, r = 10 FIND u; -- Kết quả: u = 20

-- Kịch bản 2: Biết U và I, xác định R (Tính toán nghịch)
SOLVE ON CONCEPT Resistor GIVEN u = 220, i = 2 FIND r; -- Kết quả: r = 110
```

## 2. Suy diễn Chùm và Lan truyền Tri thức (Chain Reasoning)

Kịch bản thể hiện khả năng lan truyền tri thức thông qua nhiều phân lớp luật dẫn (**Forward Chaining**).

### 2.1. Kịch bản: Hệ thống Cảnh báo và Phản ứng Sự cố

```kbql
CREATE RULE R1 SCOPE Sensor IF temp > 100 THEN SET status = 'Overheat';
CREATE RULE R2 SCOPE Sensor IF status = 'Overheat' AND pressure > 50 THEN SET alert = 'Critical';
CREATE RULE R3 SCOPE Sensor IF alert = 'Critical' THEN SET system_action = 'EMERGENCY_SHUTDOWN';

-- Thực thi nạp dữ liệu:
INSERT INTO Sensor ATTRIBUTE (110, 60);

-- Phản ứng chuỗi: R1 kích hoạt -> status='Overheat' -> R2 kích hoạt -> alert='Critical' -> R3 kích hoạt -> system_action='EMERGENCY_SHUTDOWN'.
```

## 3. Cơ chế Kế thừa Tri thức Đa tầng

Khi thiết lập hệ thống Phân cấp (**Hierarchy**), các luật dẫn tại Khái niệm (**Concept**) cha sẽ tự động được kế thừa và áp dụng cho toàn bộ các thực thể thuộc các khái niệm con và cháu.

### 3.1. Kịch bản: Phân loại Hình học và Đặc tính Kế thừa

```kbql
CREATE CONCEPT Shape (VARIABLES (color: STRING));
CREATE CONCEPT Polygon (...);
CREATE CONCEPT Triangle (...);

ADD HIERARCHY Polygon IS_A Shape;
ADD HIERARCHY Triangle IS_A Polygon;

CREATE RULE ShapeColor SCOPE Shape IF color = 'Red' THEN SET is_hot = true;

-- Thực thi nạp dữ liệu cho Triangle:
INSERT INTO Triangle ATTRIBUTE ('Red', ...);

-- Kết quả: Triangle kế thừa luật từ Shape và tự động gán is_hot = true.
```

## 4. Ràng buộc Logic Toàn vẹn (Complex Constraints)

Sử dụng khối `CONSTRAINTS` để bảo vệ tính toàn vẹn của dữ liệu ở mức độ tri thức hình thức, ngăn chặn việc nạp các Sự kiện (**Fact**) mâu thuẫn.

### 4.1. Kịch bản: Quản lý Quy tắc Nhân sự

```kbql
CREATE CONCEPT Employee (
    VARIABLES (age: INT, experience: INT),
    CONSTRAINTS (
        age >= experience + 18
    )
);

-- Lệnh thực thi thất bại do vi phạm ràng buộc logic:
INSERT INTO Employee ATTRIBUTE (20, 5); -- (20 >= 5 + 18) => FALSE => Lỗi hệ thống.
```

## 5. Giải Hệ phương trình Đa biến (Newton-Raphson 2D)

KBMS hỗ trợ giải đồng thời các hệ thức toán học phức tạp để xác định các biến số chưa biết.

### 5.1. Kịch bản: Xác định Giao điểm Hình học

```kbql
CREATE CONCEPT Intersection (
    VARIABLES (x: DECIMAL, y: DECIMAL),
    EQUATIONS (
        y = 2 * x + 1,
        y = -x + 4
    )
);

SOLVE ON CONCEPT Intersection FIND x, y;
-- Kết quả: x = 1, y = 3 (Hệ thống tự giải hệ phương trình tuyến tính).
```

## 6. Phối hợp Trigger và Luật dẫn (The Chain Reaction)

Sử dụng cơ chế **Trigger** để kích hoạt luồng suy diễn tri thức tại các Khái niệm liên quan khác.

### 6.1. Kịch bản: Đồng bộ Kho vận và Kiểm soát Tồn kho

```kbql
CREATE TRIGGER SyncStock ON (INSERT OF Order) 
DO (UPDATE Product ATTRIBUTE (SET stock: stock - 1) WHERE id = new.product_id);

-- Cập nhật tồn kho tự động kích hoạt luật kiểm tra trạng thái:
CREATE RULE OutOfStock SCOPE Product IF stock < 0 THEN SET status = 'Refill';
```
