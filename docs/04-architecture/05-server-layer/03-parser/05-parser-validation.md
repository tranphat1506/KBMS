# 4.5.3.5. Xác thực Bộ biên dịch (Parser Validation)

Bộ phân tích cú pháp (Parser) là thành phần nhạy cảm nhất đối với dữ liệu đầu vào không cấu trúc từ người dùng. Do đó, việc xác thực tính đúng đắn của Parser là yêu cầu tiên quyết để đảm bảo sự ổn định của toàn bộ hệ thống tri thức.

## 1. Chỉ số Bao phủ Kiểm thử (Testing Coverage Metrics)

Tầng Parser được bảo vệ bởi một hệ thống kiểm thử tự động toàn diện với hơn **21,000 dòng mã kịch bản test** tại `ParserTests.cs` và `LexerTests.cs`, bao phủ hơn **300 kịch bản chuyên biệt**:

*   **Trường hợp chuẩn (Positive Cases)**: Kiểm chứng khả năng bóc tách chính xác 100% các từ khóa và cấu trúc lệnh thuộc cả 5 hệ phả ngôn ngữ (KDL, KQL, KML, KCL, TCL).
*   **Trường hợp biên và lỗi (Negative Cases)**: Thử nghiệm khả năng từ chối và báo lỗi đối với các câu lệnh phá vỡ quy tắc cú pháp (thiếu dấu phân tách, sai kiểu dữ liệu, hoặc sai cấu trúc khối lệnh).

### Minh chứng Thực thi:
![Kiểm thử độ bao phủ Parser](../../../assets/diagrams/code_test_parser.png)
*Hình 4.xx: Mã nguồn các kịch bản kiểm thử đơn vị (Unit Test) cho bộ phân tích cú pháp.*

![Kết quả vượt qua kiểm thử](../../../assets/diagrams/result_test_parser.png)
*Hình 4.xx: Kết quả thực thi thành công toàn bộ bộ kiểm thử cú pháp trên môi trường phát triển.*

## 2. Cơ chế Đặc tả Lỗi Cú pháp (Error Reporting)

Một Parser học thuật không chỉ dừng lại ở việc phát hiện lỗi mà còn phải cung cấp thông tin định vị chính xác (Dòng, Cột) và gợi ý khắc phục. Điều này được thực hiện thông qua việc lưu giữ metadata của Token trong quá trình quét của Lexer.

```sql
-- Ví dụ về lỗi cú pháp: Thiếu dấu đóng ngoặc
CREATE CONCEPT ModelX ( VARIABLES ( id: INT );
-- Kết quả trả về: [ERROR] Unexpected token ';', expected ')' at Line 1, Col 35.
```

### Minh chứng Hiệu quả:
![Xác thực Parser xử lý biểu thức phức tạp](../../../assets/diagrams/terminal_test_parser_nested.png)
*Hình 4.xx: Chứng minh khả năng bóc tách và xác thực thành công các cấu trúc biểu thức lồng nhau với độ phức tạp cao.*

Việc hoàn thiện Phân hệ II (Language Parser) đánh dấu sự chuyển đổi thành công từ ngôn ngữ văn bản sang cấu trúc cây logic AST. Tại giai đoạn tiếp theo, các nút AST này sẽ được chuyển giao cho Phân hệ III (System Core) — "hệ điều hành" của KBMS — để thực hiện các quy trình điều phối đa luồng, kiểm soát truy cập và ghi nhật ký kiểm toán hệ thống.


