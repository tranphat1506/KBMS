# 02.2. Cơ chế Suy luận và Giải quyết vấn đề

Dựa trên cấu trúc mô hình [COKB](../00-glossary/01-glossary.md#cokb), quá trình giải quyết bài toán thực chất là quá trình mở rộng tập sự thật dựa trên các quy tắc suy luận cưỡng bức, cho phép hệ thống "hiểu" và tự động phát sinh nội dung mà không cần người dùng nhập thủ công.

## 1. Các Quy tắc Suy luận

Hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) vận hành dựa trên 6 loại quy tắc suy luận chính (RC1 - RC6) [2]:

*   **RC1 (Vốn có)**: Sinh ra sự kiện từ thuộc tính mặc định của đối tượng (ví dụ: Tam giác cân có 2 góc đáy bằng nhau).
*   **RC2 (Mặc nhiên)**: Các phép toán gán, thay thế và bắc cầu cơ bản giữa các biến và hằng số.
*   **RC3 (Thay thế quan hệ)**: Sử dụng các quan hệ tính toán đã biết để tìm giá trị biến còn thiếu (Ohm's Law: $U, I \implies R$).
*   **RC4 (Luật dẫn)**: Kích hoạt các luật dạng `IF <Hypothesis> THEN <Goal>`.
*   **RC5 (Giải hệ phương trình)**: Kết hợp nhiều biến và phương trình để giải đồng thời bằng phương pháp [Newton-Raphson](../00-glossary/01-glossary.md#newton-raphson) hoặc Brent [2].
*   **RC6 (Hành vi nội bộ)**: Suy diễn dựa trên các đặc tính cấu thành (PART-OF) [2].

## 2. Thuật toán Tìm bao đóng (F-Closure Algorithm)

Đây là thuật toán trọng tâm của [KBMS](../00-glossary/01-glossary.md#kbms) dùng để tìm tập đóng tri thức lớn nhất có thể suy ra được từ tập giả thiết **GT**.

**Mô tả thuật thuật giải**:
1.  Khởi tạo tập `KnownFacts = GT`.
2.  Lặp lại cho đến khi không còn sự thật mới ([Fixed-point](../00-glossary/01-glossary.md#fixed-point)):
    *   Quét qua toàn bộ tập luật (Rules) và phương trình (Equations).
    *   Nếu điều kiện của luật thỏa mãn trong `KnownFacts`, đẩy kết luận vào tập đóng.
    *   Cập nhật `KnownFacts`.
3.  Kết quả: `FClosure(GT)` là tập hợp chứa toàn bộ tri thức tường minh và dẫn xuất.

## 3. Chiến lược Giải bài toán:

Để tăng tốc độ suy diễn trong các không gian tri thức khổng lồ, [KBMS](../00-glossary/01-glossary.md#kbms) sử dụng các quy tắc [heuristics](../00-glossary/01-glossary.md#heuristics) dựa trên lớp bài toán [2]:
*   **Heuristic 1**: Ưu tiên các luật có phần kết luận chứa biến mục tiêu (**Goal-directed**).
*   **Heuristic 2**: Ưu tiên giải quyết các đối tượng có mức độ xác định (Confidence Level) cao nhất trước.
*   **Heuristic 3**: Sử dụng đồ thị phụ thuộc biến để giới hạn phạm vi quét luật.

- Việc tích hợp các luật [heuristics](../00-glossary/01-glossary.md#heuristics) giúp hệ thống giảm thiểu các bước suy diễn thừa, từ đó đạt được thời gian đáp ứng thời gian thực ngay cả trên các bộ dữ liệu lớn.