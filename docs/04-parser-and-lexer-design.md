# Bộ Công Cụ Dịch Toán Học (Parser & Lexer Architecture)

Lexer và Parser của KBMS đóng chung một vai là Compiler (Trình Biên dịch siêu nhỏ). Thay vì dịch ra mã máy Assemble, chúng dịch các ký tự Text thành Cấu trúc Cây Đối tượng Toán học C# gọi là AST (Abstract Syntax Tree).

## 1. Trình Phân Tính Ký Tự Từ Vựng (Lexer)
Lexer không hiểu ngôn ngữ KBQL, nó chỉ làm nhiệm vụ Cảm Nhận Ký Tự.
Ví dụ:
`BEGIN TRANSACTION;`
- Nó quét gặp chữ `B, E, G, I, N`. Dò từ điển Token. Biết đây là **`TokenType.BEGIN`**.
- Nó quét gặp không gian trống (Space). Cắt đứt lệnh, chuyển qua chuỗi mới.
- Nó tiếp tục cho đến tận dấu `;`. Chốt thẻ kết thúc `EOF`.
- **Thế mạnh V2**: Khả năng phân tách linh hoạt ngoặc trái `LPAREN` `(` và ngoặc phải `RPAREN` `)` đếm số lớp (Nested Levels). Dấu ngoặc tròn là công cụ rào luồng code tốt nhất hiện tại thay vì chữ `begin...end`.

## 2. Quá trình Biến hình Thành Cây 5 Nhánh (Parser -> AST)
Trình Parser mới chia cắt nhiệm vụ của cái hàm Update khủng long, thành 5 Trình Điều Phối riêng biệt:
Cây gọi hàm (Call Stack Function):
1. `ParseStatement()`
   - Đọc Keyword đầu (VD: `ALTER`, `CREATE`, `INSERT`).
   - Rẽ nhánh dựa trên đó xuống Tầng thấp hơn -> Kéo dãn tốc độ phản xạ của cây Switch-case.
2. `ParseKDL() - Kiến Trúc Định Nghĩa`
   - Bắt Token lệnh: `ALTER CONCEPT` -> Nuốt `(` -> Đọc `ADD` -> Nuốt `(` -> Chui xuống `ParseRuleList()` hoặc `ParseVariableList()`. Nuốt `)` kết thúc Block. Trả về đối tượng `AlterConceptStmt` béo mầm Data.
3. `ParseKML() - Sự Kiện Manipulation`
   - Bắt Token lệnh: `UPDATE` -> Đánh cắp Keyword `ATTRIBUTE` (Dùng để xác định sửa dữ liệu thay vì sửa Concept Schema KDL) -> Trả về đối tượng `UpdateAttributeStmt`.
4. `ParseTCL() - Giao Dịch Buffer`
   - Nhận định quá dễ: Không bắt Ngoặc hay Giá Trị gì ráo trọi. Nhận một chữ `COMMIT` -> Nhè về Obejct `CommitStmt`.

Đầu ra cuối ròng của tầng thư mục CLI sẽ gọi `Server.ExecuteNode(AST_Object)`. Giao thoa sức mạnh với Engine Backend (Hầm ngầm Logical/Buffer Memory).
