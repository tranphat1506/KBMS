# Đặc tả Biểu thức và Hệ thống Hàm số

Biểu thức là tập hợp các đơn vị tính toán cốt lõi trong các mệnh đề `WHERE`, `IF`, `SET` và hàm `CALC()`. **[KBQL](../../00-glossary/01-glossary.md#kbql)** tích hợp bộ máy đánh giá biểu thức hỗ trợ đầy đủ các phép toán học thuật và hàm số hình thức.

## 1. Hệ thống Toán tử Cơ sở

Hệ thống toán tử trong KBQL bao gồm các phép toán tiêu chuẩn cho các kiểu dữ liệu số, chuỗi và logic.

### 1.1. Toán tử Số học
*   `+`, `-`, `*`, `/`: Các phép toán cơ sở.
*   `^`: Lũy thừa.
*   `%`: Chia lấy dư.

### 1.2. Toán tử So sánh
*   `=`, `!=` (hoặc `<>`): So sánh tương đương và phi tương đương.
*   `<`, `<=`, `>`, `>=`: Các phép so sánh thứ tự.

### 1.3. Toán tử Logic (Boolean)
*   `AND`, `OR`, `NOT`: Các phép logic Boolean phục vụ việc tổ hợp các điều kiện ràng buộc.

## 2. Danh mục Hàm số Tích hợp

Hệ thống **[KBMS](../../00-glossary/01-glossary.md#kbms)** tích hợp sẵn các hàm toán học chuyên sâu để phục vụ việc tính toán tri thức:

*Bảng: Danh mục Hàm số Tích hợp (Built-in Functions) trong KBQL*
| Hàm | Đặc tả Chức năng | Ví dụ Thực thi |
| :--- | :--- | :--- |
| `Abs(x)` | Giá trị tuyệt đối | `Abs(-10)` $\rightarrow$ 10 |
| `Sqrt(x)` | Căn bậc hai | `Sqrt(16)` $\rightarrow$ 4 |
| `Pow(x, y)` | Lũy thừa | `Pow(2, 3)` $\rightarrow$ 8 |
| `Round(x, n)` | Làm tròn đến n chữ số thập phân | `Round(3.1415, 2)` $\rightarrow$ 3.14 |
| `Floor(x)` / `Ceiling(x)` | Làm tròn xuống / Làm tròn lên | `Floor(2.9)` $\rightarrow$ 2 |
| `Sin`, `Cos`, `Tan` | Các hàm lượng giác | `Sin(0)` $\rightarrow$ 0 |
| `Factorial(n)` | Tính giai thừa | `Factorial(5)` $\rightarrow$ 120 |

## 3. Ứng dụng trong Hàm Truy vấn CALC()

Hàm `CALC()` cho phép nhúng trực tiếp biểu thức vào kết quả của lệnh `SELECT` để thực hiện tính toán tại thời điểm truy xuất:

*Ví dụ:* `SELECT name, CALC(Sqrt(a^2 + b^2)) AS hypotenuse FROM Triangles;`

## 4. Tương tác với Thuộc tính và Biến số

Bên trong các biểu thức, người dùng có thể tham chiếu trực tiếp đến định danh của các biến thuộc **[Concept](../../00-glossary/01-glossary.md#concept)** hiện tại. Đối với các truy vấn liên kết (**JOIN**), cần sử dụng bí danh (**Alias**) để phân định rõ ràng các thực thể liên quan.

## 5. Tối ưu hóa Hiệu năng Thực thi

Các biểu thức logic phức tạp trong mệnh đề `WHERE` có thể ảnh hưởng đến tốc độ truy vấn nếu không có chỉ mục (**Index**) hỗ trợ phù hợp. Nhà phát triển được khuyến nghị sử dụng lệnh `EXPLAIN` để kiểm tra kế hoạch đánh giá biểu thức của hệ thống.
