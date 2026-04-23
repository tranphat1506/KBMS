# Thực thi lệnh và Giao dịch

Phân hệ Nhân tri thức là trung tâm xử lý dữ liệu vật lý và các logic tri thức đa biến. Chương này phân tích cách thức thực thi các nốt cây AST và đảm bảo tính nhất quán của dữ liệu tri thức thông qua các giao dịch.

## 4.6.19. Quá trình Điều phối dựa trên Cây AST

Sau khi nhận được cây AST, nhân tri thức đóng vai trò là bộ điều hướng thực thi. Mỗi loại nốt sẽ được chuyển giao đến các bộ phận xử lý chuyên biệt:
-   **Đọc và Ghi Dữ liệu**: Dành cho các lệnh khai báo và thao tác dữ liệu.
-   **Quản trị Giao dịch**: Xử lý các lệnh như `BEGIN`, `COMMIT`, `ROLLBACK`.
-   **Bộ máy Suy luận**: Dành riêng cho các bài toán logic thông qua lệnh `SOLVE`.

## 4.6.20. Quản lý Giao dịch và Tính Toàn vẹn

Hệ quản trị KBMS triển khai mô hình quản lý giao dịch để bảo vệ dữ liệu tri thức:
1.  **Vùng đệm Giao dịch**: Khi bắt đầu một giao dịch, các biến động dữ liệu chỉ tác động trên vùng bộ nhớ tạm thời, chưa thay đổi tệp tin tri thức gốc.
2.  **Cam kết Dữ liệu**: Khi nhận lệnh `COMMIT`, hệ thống mới thực hiện ghi các thay đổi xuống đĩa thông qua nhật ký ghi trước.
3.  **Hoàn tác**: Lệnh `ROLLBACK` sẽ xóa bỏ vùng đệm tạm thời, đưa trạng thái hệ thống về điểm an toàn trước giao dịch.

Sự kết hợp giữa điều phối cây AST linh hoạt và quản lý giao dịch giúp KBMS duy trì tính ổn định khi xử lý các kịch bản tri thức phức tạp quy mô lớn.
