# Tầng Parser: Cơ chế Đệ quy Đi xuống

Lớp `Parser.cs` là "ngã tư" điều hướng mọi câu lệnh. Với hơn 3400 dòng mã nguồn, nó thực hiện một quy trình phân tích cú pháp hiệu năng cao dựa trên thuật toán **[Recursive Descent](../00-glossary/01-glossary.md#recursive-descent) (Đệ quy đi xuống)**.

## 1. Cơ chế Điều hướng

Mỗi khi máy chủ nhận được một chuỗi Tokens từ [Lexer](../00-glossary/01-glossary.md#lexer), nó khởi tạo `Parser.cs` và gọi hàm `ParseAll()`.

```csharp
// Luồng xử lý đa câu lệnh trong Parser.cs
public List<AstNode> ParseAll() {
    var statements = new List<AstNode>();
    while (!IsAtEnd()) {
        var stmt = ParseStatement(); // "Ngã tư" điều hướng
        statements.Add(stmt);
        if (Check(TokenType.SEMICOLON)) Advance(); // Tiêu thụ dấu ';'
    }
}
```

### Hàm `ParseStatement()`
Đây là bộ khung switch-case khổng lồ, quyết định câu lệnh tiếp theo là [DDL](../00-glossary/01-glossary.md#ddl) (Định nghĩa), DML (Truy vấn) hay [TCL](../00-glossary/01-glossary.md#tcl) (Giao dịch). Mỗi khi nhận diện được một từ khóa bắt đầu (như `CREATE`, `SELECT`), nó sẽ ủy quyền cho một hàm con chuyên biệt (ví dụ: `ParseCreate()`, `ParseSelect()`).

---

## 2. Kỹ thuật Dự đoán & Tiêu thụ

[KBMS](../00-glossary/01-glossary.md#kbms) sử dụng ngữ pháp **LL(k)**, cho phép [Parser](../00-glossary/01-glossary.md#parser) nhìn trước ([Look-ahead](../00-glossary/01-glossary.md#look-ahead)) $k$ [Token](../00-glossary/01-glossary.md#token) để quyết định nhánh thực thi mà không cần quay lui ([Backtracking](../00-glossary/01-glossary.md#backtracking)).

!Cấu trúc cây phân tích cú pháp ([AST) của KBQL](../assets/diagrams/ast_tree_layout_v2.png)
*Hình: Ví dụ cấu trúc cây [AST](../00-glossary/01-glossary.md#ast) sau khi phân tích (dàn ngang)*

*   **`Peek()`**: Xem [Token](../00-glossary/01-glossary.md#token) hiện tại mà không tiêu thụ.
*   **`Check(type)`**: Kiểm tra xem [Token](../00-glossary/01-glossary.md#token) hiện tại có đúng Type mong muốn hay không.
*   **`Consume(type)`**: Nếu [Token](../00-glossary/01-glossary.md#token) đúng Type, tiến tới [Token](../00-glossary/01-glossary.md#token) tiếp theo. Nếu sai, ngay lập tức ném ra `ParseException` kèm tọa độ `Line/Column`.
*   **`Advance()`**: Luôn tiến về phía trước, đảm bảo độ phức tạp thời gian là $O(N)$ (N là số lượng [Token](../00-glossary/01-glossary.md#token)).

---

## 3. Phân tích Khối Lệnh phức tạp

Một trong những phần xử lý tinh vi nhất trong `Parser.cs` là phân tích lệnh `CREATE CONCEPT`. Thay vì một [parser](../00-glossary/01-glossary.md#parser) phẳng, nó thực hiện đệ quy lồng nhau:

1.  **`ParseCreate`** gọi **`ParseCreateConcept`**.
2.  **`ParseCreateConcept`** lặp trong khối `(...)` và kiểm tra từ khóa.
3.  Nếu gặp `VARIABLES`, nó gọi **`ParseVariableDefinition`** để bóc tách `name: TYPE`.
4.  Nếu gặp `CONSTRAINTS`, nó gọi **`ParseConstraintList`** (đây là lúc Expression [Parser](../00-glossary/01-glossary.md#parser) được kích hoạt).
5.  Nếu gặp `RULES`, nó gọi **`ParseConceptRuleList`** (phân tích IF-THEN logic).

