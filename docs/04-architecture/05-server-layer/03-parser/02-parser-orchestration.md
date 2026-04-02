# 4.5.3.2. Điều phối Phân tích Cú pháp (Parser Orchestration)

Lớp `Parser.cs` đóng vai trò là trung tâm điều hướng ngôn ngữ của máy chủ [KBMS](../../../00-glossary/01-glossary.md#kbms). Với cấu trúc hơn 3400 dòng mã nguồn, thành phần này thực hiện quy trình phân tích cú pháp hiệu năng cao dựa trên thuật toán **[Recursive Descent](../../../00-glossary/01-glossary.md#recursive-descent) (Phân tích đệ quy đi xuống)** để chuyển hóa Token Stream thành cây cú pháp trừu tượng (AST).

## 1. Cơ chế Điều hướng Phân tích (Parsing Dispatch)

Mỗi khi hệ thống tiếp nhận một chuỗi Tokens từ Module II, `Parser.cs` thực hiện duyệt tuần tự thông qua phương thức `ParseAll()`. Quy trình này lặp lại cho đến khi gặp Token `EOF`, đảm bảo mọi câu lệnh trong một chuỗi truy vấn (Query Batch) đều được xử lý triệt để.

### Hàm điều phối `ParseStatement()`
Đây là bộ khung logic quyết định loại câu lệnh tiếp theo thuộc nhóm ngôn ngữ nào (DDL, DML, TCL hay DCL). Khi nhận diện được từ khóa khởi đầu (ví dụ: `CREATE`, `SELECT`), Parser sẽ ủy quyền cho các phương thức con chuyên biệt như `ParseCreate()` hoặc `ParseSelect()`.

---

## 2. Kỹ thuật Phân tích LL(k) và Cấu trúc AST

Hệ thống sử dụng ngữ pháp **LL(k)**, cho phép Parser nhìn trước ([Look-ahead](../../../00-glossary/01-glossary.md#look-ahead)) $k$ Tokens để quyết định nhánh thực thi mà không cần chi phí quay lui ([Backtracking](../../../00-glossary/01-glossary.md#backtracking)).

### Minh họa cấu trúc AST đầu ra:
Giả sử câu lệnh `SELECT a, b FROM Concept_X;` được thực thi, Parser sẽ trả về một cấu trúc cây logic như sau:

```text
SelectAstNode
├── Source: "Concept_X"
├── Fields:
│   ├── IdentNode: "a"
│   └── IdentNode: "b"
├── Where: NULL
└── Limit: -1 (All)
```
*Hình 4.xx: Minh họa cấu trúc phân cấp của một nút AST điển hình.*

### Các nguyên tử điều khiển Parser:
*   **`Consume(type)`**: Kiểm tra tính đúng đắn của Token hiện tại. Nếu khớp, tiến tới Token tiếp theo; nếu sai, ngay lập tức ném ra `ParserException` kèm tọa độ `Line/Column` để báo lỗi cho người dùng.
*   **`Peek()` & `Check(type)`**: Thực hiện kiểm tra mà không tiêu thụ Token, cho phép Parser dự đoán cấu trúc tiếp theo của câu lệnh.

---

## 3. Phân tích Khối lệnh phức tạp (Nested Descent)

Một minh chứng điển hình cho sức mạnh của thuật toán đệ quy đi xuống trong KBMS là việc bóc tách lệnh `CREATE CONCEPT`. Thay vì một bộ phân tích phẳng, Parser thực hiện lồng ghép nhiều cấp:

1.  **Cấp 1**: `ParseCreate` nhận định danh từ khóa `CREATE` và chuyển cho `ParseCreateConcept`.
2.  **Cấp 2**: `ParseCreateConcept` quét khối `{...}` và xác định các phân đoạn `VARIABLES`, `CONSTRAINTS`, `RULES`.
3.  **Cấp 3**: Tùy vào phân đoạn, Parser gọi đệ quy các hàm con như `ParseVariableDefinition` hoặc `ParseExpression` để xây dựng các nhánh con cho nút Concept.

```csharp
// Minh họa thuật toán: Recursive Descent Parser bóc tách lệnh CREATE CONCEPT
private AstNode ParseCreateConcept() {
    Consume(TokenType.CREATE); // Xác thực từ khóa
    Consume(TokenType.CONCEPT);
    var conceptName = Consume(TokenType.IDENTIFIER).Text;
    var conceptNode = new AstNode.Concept(conceptName);
    
    Consume(TokenType.LBRACE); // Nhận diện dấu {
    while (!Check(TokenType.RBRACE) && !IsAtEnd()) {
        if (Match(TokenType.VARIABLES)) {
            conceptNode.Variables = ParseVariablesList(); // Gọi đệ quy nhánh 1
        } else if (Match(TokenType.CONSTRAINTS)) {
            conceptNode.Constraints = ParseConstraintsList(); // Gọi đệ quy nhánh 2
        } else {
            throw new ParseException("Cú pháp khối lệnh không hợp lệ.");
        }
    }
    Consume(TokenType.RBRACE); // Nhận diện dấu }
    return conceptNode;
}
```

Kiến trúc đệ quy này cho phép KBMS mở rộng ngôn ngữ một cách linh hoạt mà không làm ảnh hưởng đến tính ổn định của các bộ phân tích cũ. Sau khi cấu trúc AST được hình thành, chúng được phân loại vào các lớp đối tượng chuyên biệt để chuẩn bị cho giai đoạn thực thi.