## 4.2. Kiểm định Trình phân dịch và Cấu trúc lưu trữ (Unit Testing)

Để đảm bảo các suy diễn ở tầng cao (Reasoning Layer) hoạt động chính xác, nền tảng vật lý và trình phân dịch ngôn ngữ KBQL (Knowledge Base Query Language) phải đảm bảo độ tin cậy ở cấp độ byte [3]. Các bài kiểm thử đơn vị đóng vai trò như một màng lọc, ngăn chặn mọi sai sót cú pháp hoặc hỏng hóc dữ liệu trước khi chúng tiến sâu vào hệ thống.

![Sơ đồ Luồng Kiểm thử Đơn vị](../assets/diagrams/eval_unit_test_flow.png)
*Hình 4.3 Luồng kiểm thử độc lập cho Language Parser và Storage Engine.*

Về phía trình phân dịch (Parser), bộ test `LexerTests.cs` và `ParserTests.cs` xác thực khả năng chuyển đổi câu lệnh dạng chuỗi thành cây cú pháp trừu tượng (AST). Các kịch bản giả lập hàng loạt lỗi cú pháp phổ biến (ví dụ: thiếu dấu phẩy, đóng ngoặc sai vị trí) để kiểm chứng khả năng bắt lỗi (Error Handling) của Lexer. Chỉ khi AST được xây dựng thành công và hợp lệ, khối lệnh mới được cấp phép chuyển giao cho Inference Engine.

Chuyển sang bộ máy lưu trữ (Storage Engine), dữ liệu được tổ chức dưới dạng cấu trúc B+ Tree trên các Slotted Page [4]. Bài test `BPlusTreeTests.cs` kiểm tra thao tác tách nút (Node Splitting) khi dung lượng của một trang (thường là 4KB hoặc 8KB) đạt mức bão hòa. Hàng ngàn thao tác chèn và xóa dữ liệu nhị phân ngẫu nhiên được thực hiện liên tục. Sau mỗi thao tác, bộ kiểm thử sẽ quét lại toàn bộ offset trong Page Header để khẳng định không có byte dữ liệu nào bị đè lấp sai quy tắc.

Đặc biệt, tính toàn vẹn dữ liệu (ACID) khi xảy ra sự cố hệ thống được xác minh thông qua kịch bản `TransactionRollbackTests.cs`. Kịch bản này cố ý ngắt luồng thực thi (Throw Exception) giữa một tiến trình Bulk Insert. Cơ chế Write-Ahead Logging (WAL) của KBMS được kích hoạt, tự động khôi phục (Rollback) toàn bộ dữ liệu đang ghi dở về trạng thái nguyên thủy. Sự vượt qua kịch bản này là tiền đề kỹ thuật vững chắc để hệ thống tiến tới các bài kiểm thử phức tạp hơn về kết nối đa Khái niệm ở mục tiếp theo.
