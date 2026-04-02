# Kiến trúc Tầng Suy luận

Tầng Suy luận của KBMS được thiết kế để thực hiện việc so khớp mẫu và nội suy tri thức tự động dựa trên thuật toán Rete [9]. Phân hệ này cho phép hệ thống tự động phát hiện các sự thật mới từ các dữ kiện hiện có thông qua một mạng lưới các nốt xử lý logic [1], [6].

## 4.7.1. Các thành phần Hạt nhân của Bộ máy Suy luận

Bộ máy suy luận bao gồm 3 thành phần chính hoạt động phối hợp:

1.  **Bộ máy suy diễn**: Thành phần điều phối trung tâm, tiếp nhận các yêu cầu giải quyết tri thức từ nhân tri thức và quản lý vòng đời của phiên suy luận.
2.  **Mạng lưới Rete**: Một đồ thị các nốt (Alpha, Beta, P-Node) lưu trữ trạng thái khớp cục bộ và thực hiện lan truyền dữ kiện.
3.  **Hàng đợi thực thi**: Nơi lưu trữ các luật dẫn đã thỏa mãn điều kiện nhưng chưa được thực hiện hành động kết luận.

![Sơ đồ Điều phối Tầng Suy luận | width=1.05](../../assets/diagrams/reasoning_orchestration.png)
*Hình 4.24: Quy trình điều phối suy luận từ bộ phân tích cú pháp đến kết quả cuối.*

## 4.7.2. Đặc điểm của Cơ chế Suy luận Rete

Hệ thống sử dụng mạng nốt Rete để tối ưu hóa hiệu năng dựa trên hai nguyên tắc:
-   **Lưu trữ Trạng thái**: Các kết quả khớp một phần được lưu lại tại các nốt, giúp tránh việc tính toán lại từ đầu khi có dữ kiện mới.
-   **Chia sẻ Cấu trúc**: Các phần điều kiện giống nhau giữa nhiều luật dẫn sẽ được dùng chung các nốt xử lý, giúp tiết kiệm bộ nhớ RAM.

Kiến trúc này đảm bảo KBMS có thể xử lý các hệ luật phức tạp với hàng ngàn dữ kiện mà vẫn duy trì được tốc độ phản hồi nhanh chóng.
