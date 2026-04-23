# Phương pháp kiểm chứng

Chương này trình bày các phương thức xác thực và đánh giá được áp dụng tại máy chủ KBMS, tập trung vào tính đúng đắn của logic điều phối, khả năng chịu tải và an ninh hệ thống.

## 4.6.16. Nhật ký Kiểm toán và Truy vết Hoạt động

Mọi hành động từ truy vấn cơ sở tri thức đến các thao tác cập nhật dữ kiện đều được ghi lại qua bộ phận kiểm toán. Dữ liệu này bao gồm:
-   **Hoạt động**: Nội dung câu lệnh đã thực hiện.
-   **Trạng thái**: Kết quả thực thi (Thành công, Thất bại, Từ chối quyền hạn).
-   **Thời gian Thực hiện**: Tổng thời gian đo được tại máy chủ.

Các thông tin này được lưu trữ tập trung, cho phép thực hiện việc thẩm tra lịch sử truy cập của hệ thống.

## 4.6.17. Phân quyền và Bảo mật dựa trên Vai trò

Hệ thống triển khai kiểm soát quyền hạn người dùng dựa trên vai trò thông qua bộ quản lý xác thực. Các kịch bản kiểm chứng bao gồm:
1.  **Quyền Quản trị**: Tài khoản có toàn quyền truy xuất tệp tin và cấu hình máy chủ.
2.  **Quyền Người dùng**: Cấp quyền thao tác trên các cơ sở tri thức nhất định. Mọi truy cập vượt quyền hạn sẽ bị chặn ngay tại giai đoạn xử lý cây AST.

![Xác thực An ninh và Phiên làm việc | width=1.05](../../../assets/diagrams/kbms_security_diagnostics_flow.png)
*Hình 4.23: Sơ đồ luồng chẩn đoán an ninh và xác thực trạng thái phiên.*

## 4.6.18. Đánh giá Hiệu năng Thực nghiệm

Hiệu năng của bộ phân phối được đo lường trực tiếp tại máy chủ. Các số liệu cho thấy chi phí xử lý cây AST và quản lý phiên là rất thấp:

*Bảng 4.13: Đặc tả hiệu năng điều phối tác vụ tại Tầng Server*
| Loại công việc | Thời gian Điều phối (ms) | Thời gian Thực thi (ms) | Tổng (ms) |
| :--- | :--- | :--- | :--- |
| **Truy vấn Dữ kiện** | 0.82 | 11.45 | 12.27 |
| **Suy luận Logic** | 1.15 | 34.20 | 35.35 |
| **Quản trị Giao dịch** | 0.45 | 2.10 | 2.55 |

Các kết quả này chứng minh rằng mô hình xử lý bất đồng bộ của KBMS đảm bảo thời gian phản hồi nhanh, đáp ứng tốt việc xử lý khối lượng tri thức lớn.
