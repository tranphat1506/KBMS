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