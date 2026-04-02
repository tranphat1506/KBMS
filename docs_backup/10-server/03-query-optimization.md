# 10.3. Bộ tối ưu hóa Truy vấn dựa trên Chi phí (Query Optimizer)

Bộ tối ưu hóa truy vấn (**[Query Optimizer](../00-glossary/01-glossary.md#query-optimizer)**) là "bộ não" điều phối của tầng Server Engine, chịu trách nhiệm chuyển đổi các yêu cầu tri thức trừu tượng thành các kế hoạch thực thi hiệu quả nhất trên dữ liệu nhị phân.

---

## 1. Vai trò trong Kiến trúc Server

Trong [KBMS](../00-glossary/01-glossary.md#kbms) V3, Query Optimizer nhận đầu vào là cây cú pháp [AST](../00-glossary/01-glossary.md#ast) từ bộ [Parser](../00-glossary/01-glossary.md#parser) và thực hiện các bước tối ưu hóa sau:
- **Logical to Physical Mapping**: Ánh xạ các [Concept](../00-glossary/01-glossary.md#concept) sang danh sách [Page](../00-glossary/01-glossary.md#page) ID vật lý.
- **Cost Estimation**: Tính toán chi phí dự kiến cho từng phương án thực thi.
- **Plan Selection**: Lựa chọn lộ trình truy cập dữ liệu tối ưu nhất (ví dụ: chọn [Hash Join](../00-glossary/01-glossary.md#hash-join) thay vì Nested Loop).

---

## 2. Mô hình Ước lượng Chi phí (Cost-Based Model)

Để lựa chọn kế hoạch tối ưu, hệ thống sử dụng một mô hình toán học định lượng dựa trên kỳ vọng tài nguyên phần cứng:

### 2.1. Đơn vị Chi phí Cơ bản (Baseline)
- **Sequential Page Read (Cost 1.0)**: Thao tác đọc một trang 16KB từ đĩa cứng được lấy làm đơn vị đo lường cơ bản cho toàn bộ hệ thống.

### 2.2. Hệ số Trọng số CPU (CPU Penalty)
Các toán tử tiêu tốn CPU được gán một hệ số "phạt" so với việc chỉ đọc dữ liệu thô:
- **Predicate Evaluation (Cost 1.1)**: Phản ánh chi phí đánh giá các biểu thức logic cho từng bản ghi thông qua `PredicateCompiler`.
- **Hash Table Construction (Cost 10.0)**: Đại diện cho chi phí $O(N+M)$ khi xây dựng bảng băm trong bộ nhớ cho phép kết nối dữ liệu quy mô lớn.

---

## 3. Khung thực thi Volcano (Execution Pipeline)

Sau khi được tối ưu, kế hoạch sẽ được thực thi theo mô hình **Pipeline** ([Volcano Model](../00-glossary/01-glossary.md#volcano-model)), cho phép xử lý dữ liệu theo từng bản ghi để tối ưu hóa bộ nhớ RAM:

| Toán tử Vật lý | Mô tả Chức năng |
| :--- | :--- |
| **SequentialScan** | Quét đĩa tuần tự, đọc các Slotted Pages liên quan đến Concept. |
| **HashJoin** | Kết hợp hai luồng thực thể dựa trên khóa băm, tối ưu cho tập dữ liệu lớn ($>10,000$ bản ghi). |
| **Filter** | Áp dụng chiến lược **Predicate Pushdown** để lọc dữ liệu ngay tại tầng thấp nhất. |

---

## 4. Hiệu năng Định lượng

Kiến trúc tối ưu hóa này cho phép KBMS V3 đạt được các chỉ số ấn tượng trong môi trường Stress Test thực tế:
- **Thông lượng ghi**: $30,000$ operations/second.
- **Truy vấn phức tạp**: Kết nối $10^4 \times 10^4$ thực thể chỉ trong **18 ms** nhờ tối ưu hóa thuật toán kết hợp.

Mô hình tối ưu hóa này đảm bảo máy chủ KBMS luôn vận hành với hiệu suất cao nhất ngay cả khi cơ sở tri thức mở rộng tới hàng triệu thực thể.
