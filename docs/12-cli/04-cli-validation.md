# 12.4. Xác thực CLI (CLI Validation)

KBMS CLI là giao diện dòng lệnh mạnh mẽ, cho phép tương tác trực tiếp và chạy các kịch bản tri thức quy mô lớn.

## 1. Kiểm thử Phiên làm việc (Session Logs)

Mọi thao tác trong CLI đều được ghi nhận và xác thực thông qua log kết nối tới Server.

*   **Handshake nhị phân**: CLI gửi bản tin định danh và nhận SESSION_ID.
*   **Batch execution**: Chạy script qua lệnh `SOURCE`.

### Minh chứng Mã nguồn (CLI System):
![code_test_cli.png](../assets/diagrams/code_test_cli.png)
*Hình 12.1: Minh chứng mã nguồn kiểm thử giao diện dòng lệnh (CLI).*

### Minh chứng Kết quả (Session Log):
![terminal_test_cli_query.png](../assets/diagrams/terminal_test_cli_query.png)
*Hình 12.1: Giao diện tương tác CLI thực thi câu lệnh truy vấn tri thức.*

## 2. Kiểm thử Định dạng Kết quả

Xác thực khả năng căn chỉnh bảng và hiển thị kết quả truy vấn dưới dạng trực quan.

| Query | Kết quả mô phỏng | Hiển thị bảng |
| :--- | :--- | :--- |
| `SELECT *` | 3 Rows | Bảng có header lam (Cyan) |
| `DESCRIBE` | Metadata | Bảng thông tin Concept |

![Placeholder: Ảnh chụp màn hình kết quả lệnh DESCRIBE (CONCEPT: Product) trong CLI, hiển thị bảng variables, types và descriptions](../assets/diagrams/placeholder_cli_describe_output.png)
*Hình 10.3: Hiển thị kết quả truy vấn dưới dạng bảng (Table View) trong CLI.*

---

> [!NOTE]
> Bạn có thể chạy `full_test.kbql` trực tiếp từ CLI để kiểm chứng toàn bộ 96 kịch bản thử nghiệm chỉ trong tích tắc.
