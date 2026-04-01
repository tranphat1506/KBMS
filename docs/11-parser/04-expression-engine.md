# 11.4. Bộ máy Đánh giá Biểu thức

Expression Engine là "trái tim" của mọi câu lệnh tri thức. Lớp `ExpressionNode.cs` và các lớp con trong thư mục `KBMS.Parser/Ast/Expressions` chịu trách nhiệm xử lý các phép toán và logic phức tạp.

## 1. Phân tích Biểu thức Nhị phân

Đây là các phép toán có hai vế, được xử lý thông qua `BinaryExpressionNode`.

*   **Toán tử Toán học**: `+`, `-`, `*`, `/`, `^`, `%`.
*   **Toán tử So sánh**: `=`, `!=`, `<`, `>`, `<=`, `>=`.
*   **Toán tử Logic**: `AND`, `OR`.

### Thứ tự ưu tiên (Operator Precedence)
[Parser](../00-glossary/01-glossary.md#parser) của [KBMS](../00-glossary/01-glossary.md#kbms) thực hiện phân tích theo mức độ ưu tiên chuẩn quốc tế để đảm bảo kết quả chính xác:
1.  **Cấp 1**: Dấu ngoặc `(...)`.
2.  **Cấp 2**: Hàm gọi và Truy cập thuộc tính (`.`).
3.  **Cấp 3**: Lũy thừa `^`.
4.  **Cấp 4**: Nhân/Chia `*`, `/`.
5.  **Cấp 5**: Cộng/Trừ `+`, `-`.
6.  **Cấp 6**: So sánh `=`, `!=`, `<`, `>`.
7.  **Cấp 7**: Logic `NOT`, `AND`, `OR`.

---

## 2. Các Đơn vị Biểu thức Cốt lõi

Trong cây [AST](../00-glossary/01-glossary.md#ast), biểu thức được phân rã thành các nút lá và nút nhánh:

### 2.1. `VariableNode`
Đại diện cho các thuộc tính của [Concept](../00-glossary/01-glossary.md#concept) (Ví dụ: `a`, `age`). [Parser](../00-glossary/01-glossary.md#parser) hỗ trợ truy cập thuộc tính lồng nhau thông qua dấu chấm (`p1.x`), giúp xử lý tri thức đa tầng.

### 2.2. `LiteralNode`
Chứa giá trị thực tế như số (`100`), chuỗi (`"Active"`), hoặc Boolean (`true`/`false`). Mỗi nút lưu trữ cả giá trị thô và kiểu dữ liệu (`TokenType`).

### 2.3. `FunctionCallNode`
Xử lý các hàm toán học hoặc tri thức tích hợp (v.đ: `Sqrt(x)`, `Abs(y)`). [Parser](../00-glossary/01-glossary.md#parser) bóc tách tên hàm và một danh sách các tham số (`List<ExpressionNode>`), cho phép các hàm có thể lồng nhau linh hoạt.

---

## 3. Xử lý Biểu thức Đơn phân

Được đại diện bởi `UnaryExpressionNode`, dùng cho các trường hợp chỉ có một vế:
*   **Phép phủ định**: `NOT (a > B)`.
*   **Số âm**: `-x`.

