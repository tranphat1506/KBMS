# Mô hình COKB (Computational Object Knowledge Base)

COKB là mô hình cơ sở tri thức tính toán hỗ trợ tư duy và suy luận, đóng vai trò cốt lõi trong nền tảng hệ thống KBMS.

## 6 Thành phần COKB

### 1. Concept (Khái niệm)
Gồm các đặc trưng của đối tượng và mô hình ứng dụng thông tin về đối tượng đó.
* Các biến (Variables: bao gồm kiểu nguyên thủy và Concept Con).
* Ràng buộc (Constraints).
* Phương trình (Equations): Các cấu trúc hình thức hỗ trợ phương trình 1D, 2D.
* Quan hệ cấu thành (Construct Relations): Kế thừa phương trình, luật từ Relation vào Concept phụ thuộc.
* Khai báo sự tương đương (Same Variables) giúp lan truyền biến đồng bộ.
* Gọi khác (Aliases).

### 2. Hierarchy (Phân cấp)
Tạo quan hệ cấu trúc giữa các Concepts qua kế thừa kế trúc.
* IS_A: Đặc điểm kế thừa. Sẽ tự động gộp (merge) Variables, Rules, Equations từ đối tượng cha.
* PART_OF: Quan hệ cấu thành phần. Tự sinh ra Implicit Variables đại diện thành phần con.

### 3. Relation (Quan hệ)
Tạo ánh xạ tĩnh và ngữ nghĩa toán học. Hỗ trợ Properties và nhúng thẳng Equations + Rules.
Domain và Range. Các thuộc tính như Transitive, Reflexive, Symmetric, v.v.

### 4. Operator (Toán tử)
Là phương thức toán tử logic tích hợp (như cộng, trừ, nhân, chia, pow, v.v.)
Cấu trúc có tính giao hoán, phân phối, kết hợp...

### 5. Function (Hàm)
Định nghĩa một phép tính cụ thể (như tính khoảng cách điểm, chu vi).

### 6. Rule (Luật)
Thành phần quyết định sức mạnh của Hệ Expert System với các mệnh đề "Nếu - Thì".
* Dạng Deduction: Truyền logic thông thường.
* Dạng Constraint: Kiểm tra ràng buộc.
* Dạng Computation: Luật hỗ trợ rút ra kết quả cho hệ tính toán.
