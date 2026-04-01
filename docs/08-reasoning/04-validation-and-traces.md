# 08.4. Kiểm chứng Ràng buộc và Truy vết Suy diễn (Validation & Traces)

Hệ thống suy diễn của [KBMS](../00-glossary/01-glossary.md#kbms) V3 đảm bảo tính minh bạch và độ tin cậy của tri thức thông qua hai cơ chế hậu xử lý: Kiểm chứng ràng buộc (**Constraint Validation**) và Truy vết dẫn xuất (**Derivation Tracing**).

---

## 1. Kiểm chứng Ràng buộc Toàn vẹn (Integrity Constraints)

Sau khi thuật toán [F-Closure](../00-glossary/01-glossary.md#f-closure) đạt đến trạng thái bao đóng hoặc tìm thấy các biến mục tiêu, hệ thống thực hiện bước kiểm tra tính nhất quán cuối cùng.

- **Cơ chế**: Quét qua tất cả các biểu thức logic trong khối `CONSTRAINTS` của [Concept](../00-glossary/01-glossary.md#concept).
- **Xử lý Vi phạm**: Nếu bất kỳ ràng buộc nào trả về giá trị `False` (ví dụ: tổng hai cạnh tam giác nhỏ hơn cạnh còn lại), hệ thống sẽ ngay lập tức hủy bỏ kết quả và trả về thông báo lỗi chi tiết.
- **Ý nghĩa Học thuật**: Đảm bảo rằng tri thức được suy diễn không chỉ đúng về mặt toán học mà còn hợp lệ về mặt ngữ nghĩa và quy luật thực tế của miền tri thức.

---

## 2. Truy vết Suy diễn (Derivation Traceability)

Để đáp ứng yêu cầu của một hệ chuyên gia "Hộp trắng" (Explainable AI), [KBMS](../00-glossary/01-glossary.md#kbms) cung cấp khả năng giải trình lý do tại sao một giá trị tri thức được hình thành.

### 2.1. Cấu trúc Vết dẫn xuất (Trace Object)
Mỗi giá trị biến được suy diễn đều được gắn kèm một đối tượng `SourceTrace` bao gồm:
- **Type**: Loại hình dẫn xuất ([Rule](../00-glossary/01-glossary.md#rule), [Equation](../00-glossary/01-glossary.md#equation), hoặc Kế thừa).
- **Identifier**: Tên của luật hoặc phương trình đã kích hoạt.
- **Inputs**: Danh sách các biến và giá trị đầu vào đã trực tiếp đóng góp vào kết quả.

### 2.2. Trực quan hóa Giải trình
Dữ liệu truy vết này được Server đóng gói dưới định dạng [JSON](../00-glossary/01-glossary.md#json) và gửi về cho **[KBMS](../00-glossary/01-glossary.md#kbms) Studio** để hiển thị dưới dạng cây giải trình (Explanation Tree). Người dùng có thể nhấp vào một kết quả để xem lộ trình logic mà hệ thống đã đi qua để tìm ra nó.

---

## 3. Quản lý Ngữ cảnh và Phân cấp (Contextual Hierarchy)

Suy diễn trong [KBMS](../00-glossary/01-glossary.md#kbms) không cô lập mà diễn ra trong một hệ sinh thái phân cấp:

- **Kế thừa IS-A**: Cho phép lan truyền tri thức từ lớp cha sang lớp con, giúp tái sử dụng các bộ luật và phương trình chung.
- **Thành phần PART-OF**: Hỗ trợ suy diễn đệ quy. Một đối tượng phức tạp (ví dụ: Xe hơi) sẽ tự động kích hoạt tiến trình suy diễn cho các thành phần con (Động cơ, Bánh xe) trước khi tổng hợp tri thức lên mức độ hệ thống.
- **Quan hệ Tương quan (CONSTRUCT_RELATIONS)**: Cho phép "tiêm" các mô hình toán học từ thư viện quan hệ toàn cục vào ngữ cảnh của một đối tượng cụ thể thông qua cơ chế thay thế biến động (Dynamic Variable Substitution).
