# 4.5.3.4. Bộ máy Đánh giá Biểu thức (Expression Engine)

Bộ máy đánh giá biểu thức (Expression Engine) là thành phần hạt nhân xử lý logic và toán học bên trong các câu lệnh tri thức. Thông qua lớp cơ sở `ExpressionNode.cs` và các lớp con chuyên biệt, hệ thống thực hiện giải mã các phép toán phức tạp từ cây AST để phục vụ cho cả tầng lưu trữ phân tích và tầng suy diễn.

## 1. Phân tích Biểu thức Nhị phân (Binary Expressions)

Các biểu thức chứa hai toán hạng được xử lý thông qua `BinaryExpressionNode`. Hệ thống hỗ trợ đầy đủ các nhóm toán tử chuẩn:

*   **Toán tử Số học**: Bao gồm các phép tính cơ bản (`+`, `-`, `*`, `/`) và các phép toán nâng cao (`^` - lũy thừa, `%` - chia lấy dư).
*   **Toán tử So sánh**: Thiết lập các mệnh đề quan hệ (`=`, `!=`, `<`, `>`, `<=`, `>=`).
*   **Toán tử Logic**: Kết hợp các biểu thức Boolean thông qua `AND` và `OR`.

### Thứ tự ưu tiên Toán tử (Operator Precedence)
Để đảm bảo tính chính xác tuyệt đối trong các bài toán tri thức, Parser tuân thủ nghiêm ngặt bảng thứ tự ưu tiên quốc tế, được thực hiện thông qua cấu trúc phân tầng của các phương thức đệ quy:

1.  **Cấp 1 (Primary)**: Biểu thức trong ngoặc `(...)`, truy cập thuộc tính `.` và các hằng số.
2.  **Cấp 2 (Unary)**: Phép phủ định `NOT` và dấu âm `-`.
3.  **Cấp 3 (Factor)**: Các phép toán nhân/chia `*`, `/`.
4.  **Cấp 4 (Term)**: Các phép toán cộng/trừ `+`, `-`.
5.  **Cấp 5 (Comparison)**: Các phép so sánh quan hệ.
6.  **Cấp 6 (Equality)**: Kiểm tra tính bằng nhau (`=`, `!=`).
7.  **Cấp 7 (Logic)**: Các phép kết hợp `AND`, `OR`.

---

## 2. Các Thành phần Biểu thức Học thuật

Trong cấu trúc cây AST, các biểu thức được phân tách thành các nút nút lá (leaf) và nút nhánh (branch) với chức năng cụ thể:

### 4.5.3.4.2.1. Nút Biến (`VariableNode`)
Đại diện cho các thuộc tính định danh của một Concept (ví dụ: `age`, `velocity`). Hệ thống hỗ trợ cơ chế truy cập thuộc tính phân cấp thông qua toán tử chấm (`p.x`), cho phép xử lý các mô hình tri thức đa tầng và quan hệ lồng nhau.

### 4.5.3.4.2.2. Nút Hằng số (`LiteralNode`)
Lưu trữ các giá trị dữ liệu thực tế như số học (`100`), chuỗi văn bản (`"Active"`), hoặc giá trị logic (`true`/`false`). Mỗi nút không chỉ chứa giá trị thô mà còn lưu giữ định danh kiểu dữ liệu (`TokenType`) để phục vụ kiểm tra kiểu tại runtime.

### 4.5.3.4.2.3. Nút Gọi hàm (`FunctionCallNode`)
Điều phối việc thực thi các hàm tri thức tích hợp (ví dụ: `Sqrt(x)`, `Abs(y)`). Parser bóc tách định danh hàm và danh sách các tham số đầu vào, cho phép xây dựng các biểu thức lồng nhau với độ phức tạp cao.

---

## 3. Xử lý Biểu thức Đơn phân (Unary Expressions)

Được đại diện bởi `UnaryExpressionNode`, thành phần này xử lý các toán tử tác động lên một toán hạng duy nhất. Hai trường hợp phổ biến nhất là phép phủ định logic (`NOT`) và đảo dấu số học (`-`), đóng vai trò quan trọng trong việc thiết lập các ràng buộc tri thức loại trừ.

Sau khi các biểu thức và cấu trúc câu lệnh đã được Parser bóc tách thành cây AST hoàn chỉnh, hệ thống cần trải qua một quy trình kiểm định cuối cùng để đảm bảo tính đúng đắn của dữ liệu trước khi bàn giao cho tầng điều phối Core.
