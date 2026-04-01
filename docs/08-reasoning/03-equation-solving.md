# 08.3. Giải thuật Tính toán và Hệ Phương trình (Numerical Solvers)

Hệ quản trị tri thức [KBMS](../00-glossary/01-glossary.md#kbms) V3 phân biệt với các hệ chuyên gia truyền thống nhờ khả năng giải quyết các ràng buộc toán học phi tuyến tính thông qua các phương pháp giải số (**Numerical Methods**). Bộ máy suy diễn tích hợp các toán tử giải (**Solvers**) để tìm nghiệm của các biến ẩn số trong quá trình thực thi bao đóng tri thức.

---

## 1. Giải phương trình Một ẩn (1D Root Finding)

Trong quá trình suy diễn, nếu một phương trình $f(x) = 0$ chỉ còn duy nhất một biến $x$ chưa xác định giá trị, [KBMS](../00-glossary/01-glossary.md#kbms) sẽ kích hoạt bộ giải **[Brent's Method](../00-glossary/01-glossary.md#brents-method)**.

### Đặc tính Kỹ thuật:
- **Nguyên lý**: Kết hợp giữa phương pháp chia đôi ([Bisection](../00-glossary/01-glossary.md#bisection)) để đảm bảo tính hội tụ, phương pháp cát tuyến (Secant) và nội suy bậc hai ngược (Inverse Quadratic Interpolation) để tối ưu tốc độ.
- **Tính Thích nghi (Adaptive Scanning)**: Nếu khoảng tìm kiếm ban đầu không chứa nghiệm ($f(a) \cdot f(b) > 0$), hệ thống sẽ tự động mở rộng dải quét theo hàm số mũ để xác định vùng hội tụ tiềm năng.

---

## 2. Giải Hệ phương trình Phi tuyến (2D Equation Systems)

Đối với các ràng buộc phức tạp hơn, nơi hai phương trình chia sẻ chung hai ẩn số chưa biết:
$$
\begin{cases}
f_1(x, y) = 0 \\
f_2(x, y) = 0
\end{cases}
$$
[KBMS](../00-glossary/01-glossary.md#kbms) áp dụng giải thuật **[Newton-Raphson](../00-glossary/01-glossary.md#newton-raphson)** đa biến kết hợp với ma trận **Jacobian**.

### Quy trình Giải số:
1.  **Xấp xỉ Đạo hàm**: Sử dụng phương pháp sai phân hữu hạn (**Finite Difference**) để tính các đạo hàm riêng $\frac{\partial f}{\partial x}$ và $\frac{\partial f}{\partial y}$ ngay tại thời điểm thực thi.
2.  **Cập nhật Nghiệm ([Jacobian Matrix](../00-glossary/01-glossary.md#jacobian-matrix))**: Nghiệm mới được tính toán thông qua công thức hiệu chỉnh ma trận:
    $$\Delta X = -J^{-1} \cdot F(X)$$
3.  **Hội tụ**: Quá trình lặp dừng lại khi sai số cực đại $\max(|f_1|, |f_2|)$ nhỏ hơn ngưỡng $\epsilon = 10^{-7}$.

---

## 3. Tích hợp trong Chu trình Suy diễn (Inference Integration)

Các bộ giải toán học không hoạt động độc lập mà được triệu gọi trực tiếp bởi `InferenceEngine` trong mỗi chu kỳ của thuật toán [F-Closure](../00-glossary/01-glossary.md#f-closure):

- **Kích hoạt Tự động**: Hệ thống liên tục giám sát số lượng "ẩn số" trong từng ràng buộc. Ngay khi số lượng ẩn số bằng năng lực của bộ giải (1 hoặc 2), toán tử tương ứng sẽ được đẩy vào hàng đợi thực thi.
- **Phản hồi Tri thức**: Nghiệm số sau khi tìm được sẽ được đưa ngược lại vào tập **[Ground Truth](../00-glossary/01-glossary.md#ground-truth)**, từ đó có khả năng kích hoạt thêm các luật logic hoặc phương trình khác, tạo nên hiệu ứng lan truyền tri thức toàn cục.

Cơ chế này cho phép [KBMS](../00-glossary/01-glossary.md#kbms) giải quyết các bài toán kỹ thuật, hình học và tài chính phức tạp mà không yêu cầu người dùng phải xác định thứ tự tính toán thủ công.
