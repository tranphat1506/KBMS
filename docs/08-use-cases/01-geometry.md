# Use Case: Giải bài toán Hình học phẳng

Một trong những sức mạnh lớn nhất của KBMS là khả năng giải các bài toán hình học thông qua các phương trình toán học và bộ giải số học (Equation Solver).

## 1. Bài toán: Giải tam giác vuông

Giả sử chúng ta muốn hệ thống tự động tính toán các cạnh còn lại của một tam giác vuông khi biết trước 2 cạnh.

### Bước 1: Định nghĩa Concept

```kbql
CREATE CONCEPT RightTriangle (
    VARIABLES (
        a: DECIMAL, -- Cạnh góc vuông 1
        b: DECIMAL, -- Cạnh góc vuông 2
        c: DECIMAL, -- Cạnh huyền
        area: DECIMAL, -- Diện tích
        perimeter: DECIMAL -- Chu vi
    )
);
```

### Bước 2: Thiết lập các Phương trình (Rule/Equation)

Chúng ta định nghĩa các mối liên hệ hình học của tam giác vuông:

```kbql
-- Định lý Pythagoras
CREATE RULE Pythagoras
SCOPE RightTriangle
IF a > 0 AND b > 0
THEN SET c = Sqrt(a^2 + b^2);

-- Tính diện tích
CREATE RULE CalcArea
SCOPE RightTriangle
IF a > 0 AND b > 0
THEN SET area = (a * b) / 2;

-- Tính chu vi
CREATE RULE CalcPerimeter
SCOPE RightTriangle
IF a > 0 AND b > 0 AND c > 0
THEN SET perimeter = a + b + c;
```

---

## 2. Thực thi Truy vấn

Khi đã có cấu trúc, chúng ta chỉ cần nạp dữ liệu vào và hệ thống sẽ tự động thực hiện các phần còn lại.

### Nhập dữ liệu và Suy diễn
```kbql
INSERT INTO RightTriangle ATTRIBUTE (3, 4, 0, 0, 0);

-- Truy vấn kết quả
SELECT a, b, c, area, perimeter FROM RightTriangle;
```

**Kết quả mong đợi:**
*   `c` = 5
*   `area` = 6
*   `perimeter` = 12

---

## 3. Bài toán Nâng cao: Tính ngược chiều cao từ Diện tích

Giả sử chúng ta biết diện tích và một cạnh, liệu hệ thống có tính được cạnh kia không?

### Sử dụng Equation để giải ngược
Thay vì dùng `SET`, chúng ta định nghĩa dùng **Equation** (Phương trình):

```kbql
-- Phương trình Heron: S=sqrt(p(p-a)(p-b)(p-c))
CREATE RULE HeronEquation
SCOPE RightTriangle
IF a > 0 AND c > 0 AND area > 0
THEN EQUATION area = Sqrt(((a+b+c)/2) * (((a+b+c)/2)-a) * (((a+b+c)/2)-b) * (((a+b+c)/2)-c));
```

**Kết quả:** Khi bạn nhập `area` và `a, c`, `ReasoningEngine` sẽ sử dụng thuật toán **Brent's Method** để giải phương trình trên và tìm ra giá trị của `b` một cách tự động.

---

## 4. Tóm tắt luồng xử lý

1.  **Dữ liệu:** 3, 4 $\rightarrow$ GT.
2.  **Match:** Rule `Pythagoras` thỏa mãn điều kiện IF (a=3, b=4).
3.  **Fire:** Tính `c = 5`, cập nhật GT.
4.  **Match:** Rule `CalcPerimeter` giờ đã thỏa mãn vì có đủ (a, b, c).
5.  **Fire:** Tính `perimeter = 12`, cập nhật GT.
6.  **Kết thúc:** Toàn bộ tri thức về tam giác đã được điền đầy đủ.
