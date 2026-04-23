# Cơ sở lý thuyết

## Mô hình Đối tượng Tính toán

Mô hình **COKB (Computational Objects Knowledge Base)** [1] là sự mở rộng của các hệ thống logic truyền thống, tích hợp khả năng tính toán mạnh mẽ vào các cấu trúc đối tượng, phục vụ như là định dạng lưu trữ và biểu diễn bộ não cốt lõi của hệ thống KBMS.

### 1. Thành phần Hệ thống tri thức

Một cơ sở tri thức COKB được xác định bởi bộ 6 thành phần [1], [3]:
$$COKB = (C, H, R, Ops, Funcs, Rules)$$

Trong đó:
- **C (Concepts)**: Tập hợp các khái niệm hoặc lớp đối tượng tính toán.
- **H (Hierarchy)**: Các quan hệ phân cấp đặc biệt hóa giữa các khái niệm (quan hệ IS-A).
- **R (Relations)**: Tập các quan hệ ngữ nghĩa giữa các lớp đối tượng (ví dụ: song song, vuông góc).
- **Ops (Operators)**: Các toán tử tính toán trên các miền giá trị (Số thực, Vector, Ma trận).
- **Funcs (Functions)**: Các hàm xác định ánh xạ giữa các thuộc tính.
- **Rules (Rules)**: Tập hợp các luật dẫn để suy diễn ra tri thức mới.

### 2. Mô hình Đối tượng Tính toán

Mỗi thực thể (Object) trong hệ thống được biểu diễn bởi bộ ba thành phần [2]:
$$O = (Attrs, Facts, Rules)$$

- **Attrs (Attributes)**: Tập các thuộc tính của đối tượng. Mỗi thuộc tính bản thân nó cũng có thể là một đối tượng tính toán khác (cấu trúc đệ quy) [4].
- **Facts**: Các sự thật, giá trị hoặc tính chất vốn có của đối tượng đã được xác định.
- **Rules (Internal Rules)**: Các quy tắc, phương trình nội tại ràng buộc mối quan hệ giữa các Attrs bên trong đối tượng đó.

### 3. Phân cấp Khái niệm (Concept Levels)

Trong mô hình COKB, các khái niệm được phân tầng dựa theo độ phức tạp của cấu trúc attributes:
- **Cấp 0 (Base Concepts)**: Các kiểu dữ liệu cơ sở (Số thực - ℝ, Điểm - Point).
- **Cấp 1**: Các khái niệm xây dựng trực tiếp từ cấp 0 (Đoạn thẳng, Góc).
- **Cấp n**: Các khái niệm phức tạp hình thành từ các lớp thấp hơn (Tam giác, Tứ giác, Động cơ).

Việc phân cấp này giúp hệ thống quản lý tri thức theo hướng mô-đun hóa và hỗ trợ lan truyền kế thừa tri thức một cách tự động.


## Cơ chế Suy luận và Giải quyết vấn đề

Dựa trên cấu trúc mô hình COKB, quá trình giải quyết bài toán thực chất là quá trình mở rộng tập sự thật thông qua cơ chế lan truyền dữ kiện trên mạng lưới thực thi phi tuần tự, cho phép hệ thống tự động phát sinh tri thức dẫn xuất từ tập giả thiết ban đầu.

### 1. Các Quy tắc Suy luận (Reasoning Rules)

Hệ thống KBMS vận hành dựa trên 6 loại quy tắc suy luận chính (RC1 - RC6), được ánh xạ trực tiếp vào các nút trong mạng lưới suy diễn [6]:

*   **RC1 (Vốn có)**: Dẫn xuất sự kiện từ các thuộc tính định nghĩa của đối tượng.
*   **RC2 (Mặc nhiên)**: Các phép biến đổi đồng nhất và bắc cầu giữa các thực thể tri thức.
*   **RC3 (Thay thế quan hệ)**: Sử dụng các quan hệ tính toán để xác định giá trị biến thông qua các nút so khớp điều kiện.
*   **RC4 (Luật dẫn)**: Thực thi các luật logic dạng mệnh đề thông qua cấu trúc nốt Terminal.
*   **RC5 (Giải hệ phương trình)**: Phối hợp các ràng buộc toán học để giải quyết các hệ phương trình phi tuyến đa biến.
*   **RC6 (Hành vi nội bộ)**: Suy diễn dựa trên cấu trúc thành phần (PART-OF) và phân bậc tri thức.