# 06.8. Biểu thức và Hàm số (Expressions)

Biểu thức là thành phần cốt lõi xuất hiện trong các mệnh đề `WHERE`, `IF`, `SET` và hàm `CALC()`. KBQL tích hợp một bộ máy đánh giá biểu thức mạnh mẽ hỗ trợ đầy đủ các phép toán và hàm học thuật.

---

## 1. Toán tử Cơ bản (Operators)

### Toán tử Số học
*   `+`, `-`, `*`, `/`: Các phép toán cơ bản.
*   `^`: Lũy thừa.
*   `%`: Chia lấy dư.

### Toán tử So sánh
*   `=`, `!=` (hoặc `<>`): So sánh bằng và không bằng.
*   `<`, `<=`, `>`, `>=`: So sánh thứ tự.

### Toán tử Logic
*   `AND`, `OR`, `NOT`: Các phép logic Boolean.

---

## 2. Hàm Tích hợp (Built-in Functions)

KBMS tích hợp sẵn các hàm toán học sau đây để phục vụ tính toán tri thức:

*Bảng 6.4: Danh mục Hàm tích hợp (Built-in Functions) trong KBQL*
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

## 3. Cách sử dụng trong CALC()

Hàm `CALC()` cho phép nhúng trực tiếp biểu thức vào kết quả trả về của `SELECT`.
*Ví dụ:* `SELECT name, CALC(Sqrt(a^2 + b^2)) AS hypotenuse FROM Triangles;`

---

## 4. Tương tác với Biến (Variables)

Bên trong các biểu thức, bạn có thể tham chiếu trực tiếp đến tên các biến của Concept hiện tại. Nếu có `JOIN`, hãy sử dụng Alias để phân biệt: `e.salary * 1.1`.

---

## 5. Lưu ý về Hiệu năng

Các biểu thức phức tạp trong `WHERE` có thể làm chậm tốc độ truy vấn nếu không có Index phù hợp. Hãy sử dụng lệnh `EXPLAIN` để kiểm tra cách hệ thống đánh giá các biểu thức của bạn.
