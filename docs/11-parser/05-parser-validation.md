# 11.5. Xác thực Bộ biên dịch (Parser Validation)

Parser là thành phần nhạy cảm nhất đối với đầu vào của người dùng. Mọi cú pháp sai sót đều phải được ngăn chặn ngay lập tức.

## 1. Thống kê Độ bao phủ Testing

Tầng Parser được bảo vệ bởi **21,000+ kịch bản test** trong `ParserTests.cs` và `LexerTests.cs`.

*   **Positive Cases**: 100% các từ khóa và cấu trúc KDL/KQL/KML/KCL/TCL được bao phủ.
*   **Negative Cases**: Kiểm tra khả năng từ chối các câu lệnh thiếu dấu chấm phẩy, sai kiểu dữ liệu hoặc sai tên Concept.

### Minh chứng Mã nguồn (Parser):
![Minh chứng mã nguồn kiểm thử độ bao phủ của bộ phân tích](../assets/diagrams/code_test_parser.png)
*Hình 11.1: Minh chứng mã nguồn kiểm thử độ bao phủ của bộ phân tích (Parser).*

### Minh chứng Kết quả (21k Tests):
![Kết quả vượt qua toàn bộ 21000+ kịch bản kiểm thử cú pháp](../assets/diagrams/result_test_parser.png)
*Hình 11.2: Kết quả vượt qua toàn bộ 21,000+ kịch bản kiểm thử cú pháp.*

## 2. Đặc tả Lỗi Cú pháp (Syntax Error Proof)

Hệ thống báo lỗi chi tiết đến từng tọa độ (Dòng, Cột) giúp người dùng dễ dàng hiệu chỉnh:

```sql
-- Lỗi mô phỏng: Thiếu dấu ngoặc đóng
CREATE CONCEPT X ( VARIABLES ( id: INT );
-- [ERROR] Unexpected token ';', expected ')' at Line 1, Col 35.
```

### Minh chứng Lỗi (Studio Editor):
![Chứng minh Parser xử lý thành công các biểu thức lồng nhau phức tạp](../assets/diagrams/terminal_test_parser_nested.png)
*Hình 11.3: Chứng minh Parser xử lý thành công các biểu thức lồng nhau phức tạp.*

---

> [!IMPORTANT]
> Toàn bộ logic giải mã biểu thức (`ExpressionEngine`) trong `full_test.kbql` được kiểm chứng từng bước thông qua các unit test nhỏ trong `LexerTests.cs`.
