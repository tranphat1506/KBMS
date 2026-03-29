# Biểu thức & Hàm (Expressions & Functions)

KBQL hỗ trợ một hệ thống biểu thức phong phú, từ các toán tử logic cơ bản đến các hàm toán học phức tạp cho việc suy diễn và tính toán.

## 1. Toán tử (Operators)

Các toán tử được sử dụng rộng rãi trong các mệnh đề `WHERE`, `IF` và trong lệnh `CALC()`.

### a. Toán tử Toán học
*   `+`, `-`, `*`, `/`: Các phép toán cơ bản.
*   `^`: Phép nâng lên lũy thừa (ví dụ: `x^2`).
*   `%`: Phép chia lấy dư (Modulus).

### b. Toán tử So sánh
*   `=`, `!=`: So sánh bằng và không bằng.
*   `<`, `<=`, `>`, `>=`: So sánh thứ tự.

### c. Toán tử Logic
*   `AND`, `OR`, `NOT`: Các phép logic Boolean.

---

## 2. Các Hàm Tích hợp (Built-in Functions)

KBQL tích hợp sẵn các hàm toán học phổ biến (thông qua bộ máy NCalc):

| Hàm | Giải thích | Ví dụ |
| :--- | :--- | :--- |
| `Abs(x)` | Giá trị tuyệt đối | `Abs(-10)` $\rightarrow$ 10 |
| `Sqrt(x)` | Căn bậc hai | `Sqrt(16)` $\rightarrow$ 4 |
| `Pow(x, y)` | Lũy thừa | `Pow(2, 3)` $\rightarrow$ 8 |
| `Round(x, n)` | Làm tròn đến n chữ số | `Round(3.1415, 2)` $\rightarrow$ 3.14 |
| `Floor(x)` / `Ceiling(x)` | Làm tròn xuống / lên | `Floor(2.9)` $\rightarrow$ 2 |
| `Sin`, `Cos`, `Tan` | Các hàm lượng giác | `Sin(0)` $\rightarrow$ 0 |
| `Factorial(n)` | Tính giai thừa | `Factorial(5)` $\rightarrow$ 120 |

---

## 3. Hàm Đặc biệt của KBQL

### CALC(expression)
Hàm này là một phần mở rộng đặc biệt của KBQL cho phép nhúng một biểu thức toán học vào kết quả trả về của `SELECT`.
*   **Skeleton:** `SELECT CALC(<formula>) AS <alias> FROM ...`
*   **Ý tưởng:** Tại mỗi dòng dữ liệu (Fact) được trích xuất, KBMS sẽ nạp giá trị các biến của dòng đó vào tham số của `expression` và tính toán kết quả ngay lập tức.

### SCRIPT / CUSTOM (Nếu có)
KBQL trong các phiên bản mở rộng có thể hỗ trợ các Script tùy biến của người dùng để thực hiện các phép suy diễn phức tạp không thể biểu diễn bằng toán học thuần túy.

---

## 4. Ví dụ tổng hợp

```kbql
-- Kết hợp nhiều hàm và toán tử
SELECT 
    name, 
    CALC(Sqrt(Pow(x, 2) + Pow(y, 2))) AS Distance 
FROM Points 
WHERE status = 'Active' AND type != 'Hidden';
```
