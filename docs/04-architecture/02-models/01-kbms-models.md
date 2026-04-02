# Đặc tả Mô hình Tri thức Hình thức COKB

Mô hình **[COKB](../../00-glossary/01-glossary.md#cokb) (Computational Objects Knowledge Base)** là sự giao thoa giữa mô hình lập trình hướng đối tượng và hệ thống logic toán học, cho phép biểu diễn các thực thể tri thức có khả năng tự tính toán và suy luận logic. Hệ quản trị cơ sở tri thức **[KBMS](../../00-glossary/01-glossary.md#kbms)** được xây dựng dựa trên hạt nhân là bộ sáu thành phần hình thức cốt lõi:

![Cấu trúc cốt lõi của KnowledgeBase](../../assets/diagrams/kbms_core_v5.png)
*Hình 4.6: Cấu trúc bộ sáu thành phần hình thức của cơ sở tri thức (C, H, R, Ops, Funcs, Rules).*

Mô hình toán học của cơ sở tri thức được định nghĩa như sau:
$$COKB = (C, H, R, Ops, Funcs, Rules)$$

## 1. Thành phần Khái niệm (C - Concepts)

Khái niệm (**Concept**) là thành phần quan trọng nhất trong hệ thống, đóng vai trò định nghĩa cấu trúc cho các lớp đối tượng tri thức. Mỗi khái niệm $c \in C$ được đặc tả bởi một bộ thành phần cấu trúc nội tại phức hợp:

![Sơ đồ cấu trúc chi tiết của Concept](../../assets/diagrams/kbms_concept_v5.png)
*Hình 4.7: Sơ đồ lớp đặc tả cấu trúc nội tại của một khái niệm ([Concept](../../00-glossary/01-glossary.md#concept)).*

1.  **Biến số (Variables)**: Tập hợp các thuộc tính xác định đặc tính của đối tượng. Mỗi biến bao gồm tên định danh, kiểu dữ liệu, độ dài và mức độ chính xác thập phân.
2.  **Ràng buộc (Constraints)**: Các điều kiện logic (**Expression**) mà đối tượng phải thỏa mãn để đảm bảo tính hợp lệ và toàn vẹn của tri thức hình thức.
3.  **Phương trình (Equations)**: Các công thức toán học xác định mối liên hệ định lượng giữa các biến số bên trong phạm vi khái niệm.
4.  **Quan hệ Tính toán (Computation Relations)**: Đặc tả khả năng tính toán của khái niệm thông qua các tham số về thứ tự (**Rank**), trạng thái (**Flag**) và chi phí thực thi tính toán (**Cost**).
5.  **Luật dẫn Nội tại (Concept Rules)**: Các quy tắc suy diễn cục bộ (Giả thiết $\rightarrow$ Kết luận) có phạm vi áp dụng giới hạn trong nội bộ khái niệm.
6.  **Cấu trúc Mở rộng**: Bao gồm các định danh thay thế (**Aliases**), đối tượng cơ sở (**BaseObjects**), biến tương đương (**SameVariables**) và các quan hệ tạo lập (**ConstructRelations**).

## 2. Thành phần Phân cấp (H - Hierarchy)

Thành phần $H$ đảm nhiệm vai trò quản lý các mối quan hệ cấu trúc giữa các khái niệm thông qua thực thể **Hierarchy**:

![Sơ đồ phân cấp khái niệm](../../assets/diagrams/kbms_hierarchy_v5.png)
*Hình 4.8: Sơ đồ minh họa quan hệ cha-con thông qua các loại hình phân cấp tri thức.*

-   **Khái niệm Cha và Khái niệm Con**: Xác định điểm đầu và điểm cuối của liên kết phân cấp trong không gian không gian tri thức.
-   **Loại hình Phân cấp (Hierarchy Type)**: Bao gồm quan hệ kế thừa tri thức (**IsA**) và quan hệ cấu trúc thành phần (**PartOf**).

## 3. Thành phần Quan hệ Ngữ nghĩa (R - Relations)

Quan hệ ngữ nghĩa $R$ trong hệ thống KBMS không chỉ đơn thuần là các liên kết tĩnh mà còn mang đặc tính toán học và logic thực thi:

![Sơ đồ cấu trúc Quan hệ ngữ nghĩa](../../assets/diagrams/kbms_relation_v5.png)
*Hình 4.9: Sơ đồ lớp đặc tả quan hệ ngữ nghĩa với miền xác định, miền giá trị và tri thức nội tại.*

-   **Miền xác định (Domain) và Miền giá trị (Range)**: Xác định phạm vi tác động và biên giới của quan hệ giữa các thực thể tri thức.
-   **Tính chất Quan hệ**: Các đặc tính toán học hình thức như đối xứng (Symmetry), phản xạ (Reflexivity) và tính bắc cầu (Transitivity).
-   **Hợp nhất Tri thức**: Mỗi quan hệ có khả năng tích hợp các phương trình và luật dẫn độc lập để hỗ trợ các quy trình suy diễn phức tạp.

## 4. Thành phần Luật dẫn và Hệ thống Logic (Rules & Logic)

Bộ máy suy diễn sử dụng các luật dẫn toàn cục để thực hiện bao đóng tri thức (Closure). Mỗi luật dẫn (**Rule**) được cấu tạo từ các tham số kỹ thuật chặt chẽ:

![Sơ đồ bộ máy suy diễn Logic](../../assets/diagrams/kbms_logic_v5.png)
*Hình 4.10: Sơ đồ lớp đặc tả cấu trúc luật dẫn ([Rule](../../00-glossary/01-glossary.md#rule)) và cấu trúc đệ quy của biểu thức logic.*

-   **Phân loại Luật (Rule Type)**: Bao gồm các nhóm luật suy diễn (Deduction), luật mặc định (Default), luật ràng buộc (Constraint) và luật tính toán (Computation).
-   **Phạm vi (Scope)**: Xác định danh mục các khái niệm chịu tác động trực tiếp của luật dẫn.
-   **Giả thiết và Kết luận**: Tập hợp các biểu thức logic (**Expression**). Cấu trúc đệ quy của biểu thức cho phép biểu diễn các công thức toán học và logic với độ phức tạp không giới hạn.

## 5. Thành phần Thực thi (Ops & Funcs)

Đây là các thành phần trực tiếp đảm nhiệm vai trò thực thi các tính toán động trong chu trình vận hành hệ thống:

![Sơ đồ bộ máy thực thi Executables](../../assets/diagrams/kbms_executables_v4.png)
*Hình 4.11: Sơ đồ lớp đặc tả thành phần hàm số và toán tử hệ thống.*

-   **Toán tử (Operators)**: Được đặc tả qua biểu tượng định danh, kiểu tham số đầu vào và khối mã nguồn thực thi tương ứng.
-   **Hàm số (Functions)**: Bao gồm tập hợp tham số, kiểu dữ liệu trả về và logic xử lý nội tại của hàm.

## 6. Thực thể Đối tượng (Object Instances)

Thực thể (**[ObjectInstance](../../00-glossary/01-glossary.md#objectinstance)**) là các thể hiện cụ thể mang giá trị dữ liệu thực tế của một khái niệm trong quá trình vận hành thực tế:

![Sơ đồ cấu trúc Thực thể Đối tượng](../../assets/diagrams/kbms_instance_v5.png)
*Hình 4.12: Sơ đồ lớp đặc tả thực thể đối tượng và cơ chế lưu trữ dữ liệu động.*

-   **Định danh Khái niệm**: Tên của khái niệm gốc mà thực thể được khởi tạo.
-   **Tập giá trị (Values)**: Sử dụng cấu trúc từ điển dữ liệu để lưu trữ tập hợp các cặp (**Thuộc tính, Giá trị**). Cơ chế này đảm bảo tính linh hoạt tối đa trong việc quản trị dữ liệu thực thể và tối ưu hóa tài nguyên bộ nhớ đệm.

## 7. Kịch bản Minh họa Thực nghiệm

Để cụ thể hóa các khái niệm lý thuyết, xét mô hình tri thức **Hình học phẳng** tập trung vào thực thể hình thức là **Tam giác**.

### 7.1. Đặc tả Khái niệm (Concept)
Định nghĩa khái niệm `Triangle` trong hệ thống:
- **Biến số**: `a, b, c` (độ dài ba cạnh), `p` (nửa chu vi), `S` (diện tích).
- **Ràng buộc**: $a + b > c, a + c > b, b + c > a$ (Điều kiện tồn tại hình học).
- **Phương trình**: $p = (a + b + c) / 2$ và $S = \sqrt{p(p-a)(p-b)(p-c)}$ (Công thức thực thi Heron).

### 7.2. Đặc tả Phân cấp (Hierarchy)
- **Kế thừa (IsA)**: `RightTriangle` kế thừa `Triangle` (Sở hữu toàn bộ thuộc tính và phương trình của lớp cha nhưng được bổ sung ràng buộc $a^2 + b^2 = c^2$).
- **Thành phần (PartOf)**: Khái niệm `Vertex` (Đỉnh) được xác định là một thành phần cấu trúc của khái niệm `Triangle`.

### 7.3. Đặc tả Quan hệ Ngữ nghĩa (Relation)
- **Quan hệ**: `Similarity(t1: Triangle, t2: Triangle)`.
- **Tính chất**: Đối xứng và Bắc cầu.
- **Logic suy diễn**: Nếu tỷ lệ giữa các cạnh tương ứng đạt mức tương đương thì hệ thống xác lập kết luận đồng dạng giữa $t1$ và $t2$.

### 7.4. Đặc tả Luật dẫn (Rule)
- **Luật**: `R1: Triangle(a==b, b==c) -> Triangle{Type="Equilateral"}`.
- **Ý nghĩa**: Nếu các cạnh có giá trị tương đương, hệ thống tự động xác lập nhãn thực thể là tam giác đều.

### 7.5. Thực thể Đối tượng (Object Instance)
 Một thể hiện cụ thể của khái niệm `Triangle` trong bộ nhớ hệ thống:
 - **ID**: `550e8400-e29b-41d4-a716-446655440000`
- **Khái niệm gốc**: `Triangle`
- **Giá trị thực tế**: `{ "a": 3, "b": 4, "c": 5 }`.
