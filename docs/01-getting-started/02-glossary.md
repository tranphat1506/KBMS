# Thuật ngữ & Khái niệm Cốt lõi (Glossary)

Để hiểu rõ cách vận hành của KBMS, người dùng cần nắm vững các thuật ngữ chuyên môn sau đây:

### 1. GT (Ground Truth - Tập sự kiện cơ sở)
GT là tập hợp tất cả các sự kiện (Facts) đã biết hoặc đã được xác minh là đúng tại một thời điểm cụ thể. Đây là "nguyên liệu" đầu vào cho mọi quá trình suy diễn.

### 2. KL (Knowledge List / Goal - Tập mục tiêu)
KL là danh sách các biến hoặc các câu hỏi mà người dùng muốn tìm lời giải. Quá trình suy diễn sẽ cố gắng tìm giá trị cho các mục tiêu trong KL bằng cách sử dụng GT và các luật logic.

### 3. FClosure (Forward Closure - Điểm đóng suy diễn)
FClosure là trạng thái mà tại đó, việc áp dụng thêm bất kỳ luật (Rule) hay phương trình (Equation) nào cũng không sinh ra thêm sự kiện mới trong GT. Nói cách khác, đây là kết quả cuối cùng của quá trình suy diễn tiến.

### 4. Fact (Sự kiện)
Một Fact là một đơn vị tri thức cụ thể, tương ứng với một dòng dữ liệu (Tuple) trong một Concept. Ví dụ: "Bệnh nhân A có nhiệt độ 39 độ" là một Fact.

### 5. Concept (Khái niệm)
Một lớp đối tượng hoặc một phạm trù tri thức (giống như Class trong lập trình hoặc Table trong CSDL). Concept định nghĩa các thuộc tính (Variables) mà các Fact của nó sẽ có.

### 6. Rule (Luật)
Một quy tắc logic dạng `IF-THEN`. Nếu các Fact trong GT thỏa mãn phần `IF`, hệ thống sẽ suy ra kết luận ở phần `THEN`.

### 7. Equation (Phương trình)
Một ràng buộc toán học mô tả mối liên hệ định lượng giữa các biến. KBMS có thể giải phương trình này để tìm ra giá trị của các biến chưa biết.

### 8. Derivation Trace (Truy vết suy luận)
Quá trình ghi lại từng bước mà hệ thống đã thực hiện để đi đến một kết luận (Ví dụ: "Biến X được tính từ Rule R1 dựa trên các sự kiện F1, F2"). Điều này giúp đảm bảo tính minh bạch của tri thức.

### 9. IS-A & PART-OF
*   **IS-A:** Quan hệ kế thừa (Ví dụ: Square IS-A Rectangle).
*   **PART-OF:** Quan hệ thành phần (Ví dụ: Engine PART-OF Car).
