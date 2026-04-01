# 06.10. Xác thực Ngôn ngữ

Để đảm bảo mọi câu lệnh [KBQL](../00-glossary/01-glossary.md#kbql) đều được diễn dịch chính xác trước khi gửi tới Engine, hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) trải qua quy trình kiểm thử cú pháp nghiêm ngặt.

## 1. Kiểm thử Cú pháp

Tầng [Parser](../00-glossary/01-glossary.md#parser) được kiểm soát bởi hệ thống kiểm thử tự động với hơn **21,000 dòng mã kịch bản** (Test Scripts), bao phủ hơn **300 trường hợp kiểm thử** (Test Cases) chuyên biệt từ đơn giản đến phức tạp.

*Bảng 6.5: Phân bổ kịch bản kiểm thử theo nhóm ngôn ngữ*
| Nhóm Kiểm thử | Mô tả | Trạng thái |
| :--- | :--- | :--- |
| **[KDL](../00-glossary/01-glossary.md#kdl) Validation** | Kiểm tra định nghĩa [Concept](../00-glossary/01-glossary.md#concept), Hierarchy, Relation. | **Passed** |
| **[KQL](../00-glossary/01-glossary.md#kql) Validation** | Kiểm tra các phép toán Join, Filter, Order By, Limit. | **Passed** |
| **Expression Eval** | Kiểm tra các biểu thức toán học phức tạp (`CALC()`). | **Passed** |
| **Error Handling** | Kiểm tra khả năng báo lỗi đúng dòng, đúng cột. | **Passed** |

## 2. Bằng chứng Thực nghiệm

Kết quả chạy bộ kiểm thử `ParserTests`:

![Kết quả chạy bộ kiểm thử ParserTests](../assets/diagrams/placeholder_parser_test_log.png)
*Hình 6.1: Kết quả kiểm thử bộ phân tích cú pháp ([Parser](../00-glossary/01-glossary.md#parser)) với hàng trăm kịch bản kiểm thử trên 21,000 dòng mã kịch bản.*

## 3. Nhật ký [Lexer]

Trước khi phân tích cú pháp, [Lexer](../00-glossary/01-glossary.md#lexer) thực hiện tách từ tố ([Tokenization](../00-glossary/01-glossary.md#tokenization)). Tệp `LexerTests.cs` đảm bảo mọi ký tự đặc biệt và từ khóa đều được nhận diện đúng.

![Nhật ký Token Stream của câu lệnh SELECT phức tạp](../assets/diagrams/placeholder_lexer_token_stream.png)
*Hình 6.2: Luồng [Token](../00-glossary/01-glossary.md#token) được phân tách bởi [Lexer](../00-glossary/01-glossary.md#lexer) trước khi đưa vào [Parser](../00-glossary/01-glossary.md#parser).*

---

