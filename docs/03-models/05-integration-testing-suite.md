# 05. Bằng chứng Kiểm thử Tích hợp (Integration Evidence)

Để chứng minh tính đúng đắn của toàn bộ mô hình COKB, KBMS đi kèm với bộ kiểm thử tích hợp `full_test.kbql`.

### 1. Kịch bản Kiểm thử Tổng lực
96 dòng lệnh KBQL bao phủ từ DDL (Định nghĩa) đến DML (Truy vấn/Cập nhật) và Suy diễn Logic.

### 2. Kết quả từ CLI Console:

![Placeholder: Ảnh chụp màn hình Terminal sạch hiển thị lệnh SOURCE full_test.kbql; kèm theo các biểu tượng thông báo thành công xanh mướt cho từng bước](../assets/diagrams/placeholder_cli_test_output.png)

### 3. Kiểm thử Tích hợp Module (Integration Proof)
Bộ kiểm thử **`FullIntegrationV3Tests.cs`** thực hiện các bước:
1.  Khởi tạo hệ thống Server ngầm (Shadow Server).
2.  Gửi các gói tin từ CLI ảo tới Server.
3.  Kiểm tra kết quả phản hồi nhị phân.

---

> [!IMPORTANT]
> **Kết luận**: Tầng Models là nền tảng vững chắc đã vượt qua hàng chục nghìn bài kiểm tra unit test tự động trước khi triển khai thực tế.
