# 12.4. Xác thực CLI (CLI Validation)

[KBMS](../../../00-glossary/01-glossary.md#kbms) [CLI](../../../00-glossary/01-glossary.md#cli) là giao diện dòng lệnh mạnh mẽ, cho phép tương tác trực tiếp và chạy các kịch bản tri thức quy mô lớn.

## 1. Kiểm thử Phiên làm việc

Mọi thao tác trong CLI đều được ghi nhận và xác thực thông qua log kết nối tới Server.

*   **[Handshake](../../../00-glossary/01-glossary.md#handshake) nhị phân**: CLI gửi bản tin định danh và nhận SESSION_ID.
*   **Batch execution**: Chạy script qua lệnh `SOURCE`.

### Minh chứng Mã nguồn (CLI System):
![Minh chứng mã nguồn kiểm thử giao diện dòng lệnh CLI](../../../assets/diagrams/code_test_cli.png)
*Hình: Minh chứng mã nguồn kiểm thử giao diện dòng lệnh (CLI).*

### Minh chứng Kết quả (Session Log):
![Giao diện tương tác CLI thực thi câu lệnh truy vấn tri thức](../../../assets/diagrams/terminal_test_cli_query.png)
*Hình: Giao diện tương tác CLI thực thi câu lệnh truy vấn tri thức.*

## 2. Kiểm thử Định dạng Kết quả

Xác thực khả năng căn chỉnh bảng và hiển thị kết quả truy vấn dưới dạng trực quan.

*Bảng: Kiểm thử định dạng kết quả truy vấn trong CLI*
| Query | Kết quả mô phỏng | Hiển thị bảng |
| :--- | :--- | :--- |
| `SELECT *` | 3 Rows | Bảng có header lam (Cyan) |
| `DESCRIBE` | Metadata | Bảng thông tin Concept |

![Kết quả lệnh DESCRIBE hiển thị bảng biến và kiểu dữ liệu trong CLI](../../../assets/diagrams/placeholder_cli_describe_output.png)
*Hình: Hiển thị kết quả truy vấn dưới dạng bảng (Table View) trong CLI.*

---

