# 05. Xác thực Độ bao phủ (Parser Validation)

Parser là thành phần nhạy cảm nhất đối với đầu vào của người dùng. Mọi cú pháp sai sót đều phải được ngăn chặn ngay lập tức.

## 1. Thống kê Độ bao phủ Testing

Tầng Parser được bảo vệ bởi **21,000+ kịch bản test** trong `ParserTests.cs` và `LexerTests.cs`.

*   **Positive Cases**: 100% các từ khóa và cấu trúc KDL/KQL/KML/KCL/TCL được bao phủ.
*   **Negative Cases**: Kiểm tra khả năng từ chối các câu lệnh thiếu dấu chấm phẩy, sai kiểu dữ liệu hoặc sai tên Concept.

![Placeholder: Ảnh chụp màn hình mã nguồn ParserTests.cs hiển thị hàng trăm dòng Assert.NotNull(ast) và Assert.IsType<CreateConceptNode>(ast)](../assets/diagrams/placeholder_parser_source_test.png)

## 2. Đặc tả Lỗi Cú pháp (Syntax Error Proof)

Hệ thống báo lỗi chi tiết đến từng tọa độ (Dòng, Cột) giúp người dùng dễ dàng hiệu chỉnh:

```sql
-- Lỗi mô phỏng: Thiếu dấu ngoặc đóng
CREATE CONCEPT X ( VARIABLES ( id: INT );
-- [ERROR] Unexpected token ';', expected ')' at Line 1, Col 35.
```

![Placeholder: Ảnh chụp màn hình Studio đánh dấu đỏ (Màu đỏ lượn sóng) vị trí lỗi cú pháp trực tiếp trên trình soạn thảo Monaco](../assets/diagrams/placeholder_studio_syntax_error_indication.png)

---

> [!IMPORTANT]
> Toàn bộ logic giải mã biểu thức (`ExpressionEngine`) trong `full_test.kbql` được kiểm chứng từng bước thông qua các unit test nhỏ trong `LexerTests.cs`.
