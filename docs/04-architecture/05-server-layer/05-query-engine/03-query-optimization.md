# 4.5.5.2. Chiến lược Tối ưu hóa Truy vấn (Query Optimization)

Bộ tối ưu hóa truy vấn (**[Query Optimizer](../../../00-glossary/01-glossary.md#query-optimizer)**) đóng vai trò là thực thể trí tuệ của Phân hệ IV, chịu trách nhiệm chuyển đổi các yêu cầu tri thức trừu tượng thành các kế hoạch thực thi (Execution Plans) đạt hiệu suất cao nhất trên hạ tầng lưu trữ vật lý.

---

## 1. Cơ chế Tối ưu hóa trong Kiến trúc Máy chủ

Trong hệ thống [KBMS](../../../00-glossary/01-glossary.md#kbms) V3, Query Optimizer tiếp nhận đầu vào là cây cú pháp [AST](../../../00-glossary/01-glossary.md#ast) từ Phân hệ II và thực hiện quy trình chuẩn hóa qua ba giai đoạn then chốt:
*   **Ánh xạ Vật lý (Logical to Physical Mapping)**: Thực hiện chuyển đổi các định danh Concept trừu tượng sang danh sách các trang dữ liệu (`Slotted Pages`) cụ thể trên đĩa cứng.
*   **Ước lượng Chi phí (Cost Estimation)**: Sử dụng các mô hình toán học để dự báo mức độ chiếm dụng tài nguyên (CPU, I/O) cho từng lộ trình thực thi khả thi.
*   **Lựa chọn Kế hoạch Tối ưu (Plan Selection)**: Quyết định chiến lược truy cập dữ liệu hiệu quả nhất, ví dụ cân nhắc giữa việc sử dụng chỉ mục B+ Tree hay quét tuần tự dựa trên quy mô dữ liệu thực tế.

---

## 2. Mô hình Ước lượng dựa trên Chi phí (Cost-Based Model)

Để định lượng hiệu quả của một kế hoạch, KBMS áp dụng mô hình chi phí dựa trên kỳ vọng về tài nguyên phần cứng:

### 4.5.5.2.2.1. Đơn vị Chi phí Cơ sở (I/O Cost)
Thao tác đọc một trang dữ liệu 16KB tuần tự từ đĩa cứng được xác định là đơn vị chi phí cơ bản (**Standard Cost = 1.0**). Mọi phép toán khác đều được quy đổi dựa trên đơn vị này.

### 4.5.5.2.2.2. Hệ số Trọng số Xử lý (CPU Penalty)
Các toán tử tiêu tốn chu kỳ xử lý của CPU được gán các hệ số "phạt" nhằm phản ánh độ phức tạp thuật toán:
*   **Đánh giá Biểu thức (Predicate Evaluation)**: Chi phí **1.1** cho mỗi bản ghi, phản ánh quá trình biên dịch và thực thi các logic điều kiện.
*   **Xây dựng Bảng băm (Hash Construction)**: Chi phí **10.0**, tương ứng với độ phức tạp $O(N+M)$ khi thiết lập cấu trúc dữ liệu tạm thời trong bộ nhớ để phục vụ các phép kết nối (Join) quy mô lớn.

---

## 3. Khung thực thi Pipeline (Volcano Model)

Sau khi kế hoạch được phê duyệt, hệ thống chuyển sang giai đoạn thực thi theo mô hình **Pipeline** ([Volcano Model](../../../00-glossary/01-glossary.md#volcano-model)). Cơ chế này cho phép luân chuyển dữ liệu theo từng bản ghi giữa các toán tử vật lý, giúp tối ưu hóa không gian bộ nhớ đệm:

*Bảng: Danh mục các Toán tử vật lý trong mô hình thực thi Pipeline*
| Toán tử Vật lý | Vai trò Kỹ thuật |
| :--- | :--- |
| **SequentialScan** | Thực hiện quét đĩa tuần tự để truy xuất các Slotted Pages liên quan đến thực thể. |
| **HashJoin** | Kết hợp hai luồng thực thể dựa trên thuật toán băm, đặc biệt hiệu quả với tập dữ liệu vượt ngưỡng 10.000 bản ghi. |
| **Filter (Pushdown)** | Áp dụng chiến lược **Predicate Pushdown** để thực hiện lọc dữ liệu ngay tại tầng truy xuất thấp nhất, giảm thiểu lượng dữ liệu cần chuyển lên các tầng trên. |

---

## 4. Chỉ số Hiệu năng Thực nghiệm

Kiến trúc tối ưu hóa này đã được kiểm chứng thông qua các bài thử nghiệm áp lực (Stress Test), mang lại những kết quả định lượng ấn tượng:
*   **Thông lượng ghi nhận**: Đạt mức $30,000$ thao tác trên giây trong điều kiện tải cao.
*   **Tốc độ Truy vấn**: Thực hiện kết nối hai tập thực thể quy mô $10^4 \times 10^4$ chỉ trong **18 ms**, chứng minh tính hiệu quả của chiến lược lựa chọn toán tử vật lý.