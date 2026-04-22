# 4.7. Kiến trúc Tầng Suy luận

Tầng Suy luận của hệ quản trị KBMS chịu trách nhiệm thực thi các tiến trình nội suy tri thức, giải hệ thức toán học và lan truyền luật dẫn tự động. Phân hệ này được thiết kế dựa trên sự kết hợp giữa thuật toán **Rete** cổ điển và bộ máy **InferenceEngine** hướng mục tiêu để tối ưu hóa hiệu năng và độ chính xác của tri thức [1], [6].

## 4.7.1. Thuật toán Rete và Nguyên lý So khớp Mẫu

Thuật toán **Rete** (tiếng Latinh có nghĩa là "mạng lưới") là một giải thuật so khớp mẫu hiệu năng cao được sử dụng trong các hệ chuyên gia. Nguyên lý cốt lõi của Rete dựa trên hai kỹ thuật tối ưu hóa sau:

1.  **Lưu trữ Trạng thái (Persistence)**: Các kết quả so sánh cục bộ sẽ được lưu lại (cached) tại các nốt trong mạng lưới. Khi có một dữ kiện (Fact) mới được đưa vào, hệ thống không cần đánh giá lại toàn bộ các luật mà chỉ cần kích hoạt các nhánh bị ảnh hưởng.
2.  **Chia sẻ Cấu trúc (Sharing)**: Những thành phần điều kiện giống nhau giữa các luật khác nhau sẽ dùng chung các nốt xử lý, giúp tiết kiệm bộ nhớ và giảm số lượng phép toán logic cần lặp lại.

## 4.7.2. Đặc tả các Loại Nốt trong Mạng Rete

KBMS triển khai mạng Rete thông qua ba loại nốt chính:

-   **Alpha Node (Bộ lọc)**: Chịu trách nhiệm thẩm định các điều kiện đơn lẻ trên một thuộc tính (Ví dụ: `Patient.sys > 140`).
-   **Beta Node (Bộ nối)**: Thực hiện phép nối tri thức (Join) giữa các nhánh khác nhau để kiểm tra sự thỏa mãn của các tổ hợp điều kiện đa biến.
-   **P-Node (Nút thực thi)**: Đại diện cho các luật dẫn đã được thỏa mãn hoàn toàn, sẵn sàng kích hoạt hành động kết luận hoặc gán dữ liệu.

## 4.7.3. Tổng quan Hệ thống Suy luận KBMS

Bên cạnh mạng Rete, bộ máy suy luận của KBMS tích hợp các thành phần điều phối hạt nhân nhằm mở rộng khả năng giải toán nội suy:

-   **Inference Engine**: Phân hệ trung tâm điều phối toàn bộ chu kỳ sống của một phiên suy luận.
-   **Fact Memory**: Bộ nhớ lưu trữ các sự kiện tạm thời được sinh ra trong quá trình suy luận, đảm bảo tính cách ly và tốc độ truy xuất nhanh.
-   **Equation Resolver**: Bộ giải hệ thức sử dụng các phương pháp xấp xỉ số học (như Newton-Raphson) để tính toán các biến số chưa biết trong mô hình toán học.

![Sơ đồ Kiến trúc Tầng Suy luận KBMS](../../assets/diagrams/reasoning_architecture.png)
*Hình 4.24: Sơ đồ kiến trúc tầng suy luận và quy trình lan truyền tri thức.*
