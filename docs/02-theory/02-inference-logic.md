# 02.2. Cơ chế Suy luận và Giải quyết vấn đề

Dựa trên cấu trúc mô hình COKB, quá trình giải quyết bài toán thực chất là quá trình mở rộng tập sự thật thông qua cơ chế lan truyền dữ kiện trên mạng lưới thực thi phi tuần tự, cho phép hệ thống tự động phát sinh tri thức dẫn xuất từ tập giả thiết ban đầu.

## 1. Các Quy tắc Suy luận (Reasoning Rules)

Hệ thống KBMS vận hành dựa trên 6 loại quy tắc suy luận chính (RC1 - RC6), được ánh xạ trực tiếp vào các nút trong mạng lưới suy diễn [6]:

*   **RC1 (Vốn có)**: Dẫn xuất sự kiện từ các thuộc tính định nghĩa của đối tượng.
*   **RC2 (Mặc nhiên)**: Các phép biến đổi đồng nhất và bắc cầu giữa các thực thể tri thức.
*   **RC3 (Thay thế quan hệ)**: Sử dụng các quan hệ tính toán để xác định giá trị biến thông qua các nút so khớp điều kiện.
*   **RC4 (Luật dẫn)**: Thực thi các luật logic dạng mệnh đề thông qua cấu trúc nốt Terminal.
*   **RC5 (Giải hệ phương trình)**: Phối hợp các ràng buộc toán học để giải quyết các hệ phương trình phi tuyến đa biến.
*   **RC6 (Hành vi nội bộ)**: Suy diễn dựa trên cấu trúc thành phần (PART-OF) và phân bậc tri thức.

## 2. Thuật toán Bao đóng Rete (Rete-based Closure Algorithm) [9]

Đây là cơ chế thực thi trọng tâm của KBMS, thay thế phương pháp lặp tuần tự bằng mô hình lan truyền Token trên đồ thị có hướng nhằm tìm kiếm tập bao đóng tri thức lớn nhất từ tập giả thiết **GT**.

**Nguyên lý vận hành**:
1.  **Biên dịch Mạng (Rete Compilation)**: Toàn bộ tri thức (Rules, Equations, Constraints) được chuyển đổi thành cấu trúc nốt Alpha (Lọc đơn) và nốt Beta (So khớp liên hợp).
2.  **Lan truyền Dữ kiện (Token Propagation)**: Khi một dữ kiện mới được nạp vào, một Token sẽ được tạo ra và lan truyền qua mạng lưới. Chỉ những nhánh chịu ảnh hưởng trực tiếp mới được kích hoạt tính toán.
3.  **Quản lý Agenda**: Các luật đã thỏa mãn điều kiện sẽ được đưa vào Agenda. Engine thực hiện kích hoạt các luật này cho đến khi mạng lưới đạt trạng thái điểm dừng (Fixed-point).

## 3. Chiến lược Tối ưu hóa Hình học Mạng

Để tối ưu hóa hiệu năng suy diễn trong các không gian tri thức quy mô lớn, KBMS áp dụng các kỹ thuật tối ưu hóa hình học mạng lưới:

*   **Chia sẻ nốt Alpha (Alpha Node Sharing)**: Gom nhóm các kiểm tra điều kiện trùng lặp để giảm thiểu chi phí tính toán dư thừa.
*   **Tối ưu hóa thứ tự tham gia (Join Ordering)**: Sắp xếp các nốt Beta dựa trên độ chọn lọc của dữ liệu nhằm giảm kích thước bộ nhớ trung gian.
*   **Kích hoạt Gia tăng (Incremental Activation)**: Chỉ thực hiện tính toán trên những phần của mạng lưới thực sự có sự thay đổi dữ liệu đầu vào.

Việc áp dụng kiến trúc mạng Rete giúp hệ thống đạt được tốc độ phản hồi thời gian thực và đảm bảo tính nhất quán của tri thức dẫn xuất ngay cả với các mô hình ràng buộc cực kỳ phức tạp.