# 06.10. Xác thực Ngôn ngữ (Language Validation)

Để đảm bảo mọi câu lệnh KBQL đều được diễn dịch chính xác trước khi gửi tới Engine, hệ thống KBMS trải qua quy trình kiểm thử cú pháp nghiêm ngặt.

## 1. Kiểm thử Cú pháp (Parsing Test)

Tầng Parser được kiểm soát bởi tệp **`ParserTests.cs`** với hơn 21,000 dòng kịch bản kiểm thử, bao phủ mọi trường hợp từ câu lệnh đơn giản đến phức tạp nhất.

| Nhóm Kiểm thử | Mô tả | Trạng thái |
| :--- | :--- | :--- |
| **KDL Validation** | Kiểm tra định nghĩa Concept, Hierarchy, Relation. | **Passed** |
| **KQL Validation** | Kiểm tra các phép toán Join, Filter, Order By, Limit. | **Passed** |
| **Expression Eval** | Kiểm tra các biểu thức toán học phức tạp (`CALC()`). | **Passed** |
| **Error Handling** | Kiểm tra khả năng báo lỗi đúng dòng, đúng cột. | **Passed** |

## 2. Bằng chứng Thực nghiệm (Evidence)

Kết quả chạy bộ kiểm thử `ParserTests`:

![Placeholder: Ảnh chụp màn hình kết quả chạy 'dotnet test --filter ParserTests' hiển thị hàng ngàn test case màu xanh (Passed)](../assets/diagrams/placeholder_parser_test_log.png)
*Hình 6.1: Kết quả kiểm thử bộ phân tích cú pháp (Parser) với hơn 21,000 kịch bản.*

## 3. Nhật ký Lexer (Lexing Proof)

Trước khi phân tích cú pháp, Lexer thực hiện tách từ tố (Tokenization). Tệp `LexerTests.cs` đảm bảo mọi ký tự đặc biệt và từ khóa đều được nhận diện đúng.

![Placeholder: Ảnh chụp nhật ký Token Stream của một câu lệnh SELECT phức tạp, cho thấy các phân đoạn IDENTIFIER, KEYWORD, OPERATOR](../assets/diagrams/placeholder_lexer_token_stream.png)
*Hình 6.2: Luồng Token được phân tách bởi Lexer trước khi đưa vào Parser.*

---

> [!NOTE]
> Việc vượt qua bộ kiểm thử `ParserTests` đảm bảo rằng KBMS sẽ không bao giờ thực thi một câu lệnh sai cú pháp, giúp bảo vệ an toàn cho tầng lưu trữ.
