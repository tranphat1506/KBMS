# 11.5. Xác thực Bộ biên dịch (Parser Validation)

[Parser](../00-glossary/01-glossary.md#parser) là thành phần nhạy cảm nhất đối với đầu vào của người dùng. Mọi cú pháp sai sót đều phải được ngăn chặn ngay lập tức.

## 1. Thống kê Độ bao phủ Testing

Tầng [Parser](../00-glossary/01-glossary.md#parser) được bảo vệ bởi hơn **21,000 dòng mã kịch bản test** trải dài trong `ParserTests.cs` và `LexerTests.cs`, bao phủ hơn **300 kịch bản test** chuyên biệt.

*   **Positive Cases**: 100% các từ khóa và cấu trúc [KDL](../00-glossary/01-glossary.md#kdl)/[KQL](../00-glossary/01-glossary.md#kql)/[KML](../00-glossary/01-glossary.md#kml)/[KCL](../00-glossary/01-glossary.md#kcl)/[TCL](../00-glossary/01-glossary.md#tcl) được bao phủ.
*   **Negative Cases**: Kiểm tra khả năng từ chối các câu lệnh thiếu dấu chấm phẩy, sai kiểu dữ liệu hoặc sai tên [Concept](../00-glossary/01-glossary.md#concept).

### Minh chứng Mã nguồn (Parser):
![Minh chứng mã nguồn kiểm thử độ bao phủ của bộ phân tích](../assets/diagrams/code_test_parser.png)
*Hình 11.1: Minh chứng mã nguồn kiểm thử độ bao phủ của bộ phân tích ([Parser](../00-glossary/01-glossary.md#parser)).*

### Minh chứng Kết quả (300+ Test Cases):
![Kết quả vượt qua toàn bộ các kịch bản kiểm thử cú pháp](../assets/diagrams/result_test_parser.png)
*Hình 11.2: Kết quả vượt qua bộ kiểm thử với hàng trăm kịch bản trên nền tảng 21,000 dòng mã kịch bản.*

## 2. Đặc tả Lỗi Cú pháp

Hệ thống báo lỗi chi tiết đến từng tọa độ (Dòng, Cột) giúp người dùng dễ dàng hiệu chỉnh:

```sql
-- Lỗi mô phỏng: Thiếu dấu ngoặc đóng
CREATE CONCEPT X ( VARIABLES ( id: INT );
-- [ERROR] Unexpected token ';', expected ')' at Line 1, Col 35.
```

### Minh chứng Lỗi (Studio Editor):
![Chứng minh Parser xử lý thành công các biểu thức lồng nhau phức tạp](../assets/diagrams/terminal_test_parser_nested.png)
*Hình 11.3: Chứng minh [Parser](../00-glossary/01-glossary.md#parser) xử lý thành công các biểu thức lồng nhau phức tạp.*

---

