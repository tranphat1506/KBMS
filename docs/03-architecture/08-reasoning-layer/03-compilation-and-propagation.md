# 4.7.5. Đóng khép Tri thức và Quy trình Lan truyền (Forward Closure)

Lan truyền tri thức (Knowledge Propagation) là tiến trình vận hành cốt lõi nhằm đưa cơ sở tri thức đạt tới trạng thái hội tụ, gọi là **Đóng khép Tri thức (Forward Closure - F-Closure)**. Trong trạng thái này, mọi luật dẫn thỏa mãn đều đã được kích hoạt và không còn thông tin mới nào có thể được sinh ra.

## 1. Thuật toán FindClosure (Fixed-point Iteration)

`InferenceEngine` thực hiện tiến trình suy diễn thông qua một vòng lặp liên tục cho đến khi đạt được điểm cố định (**Fixed-point**). Quy trình này gồm các bước chi tiết sau:

1.  **Thiết lập Dữ kiện Gốc (Seed Facts)**: Hệ thống nạp các thuộc tính ban đầu từ mạng cơ sở dữ liệu hoặc thông qua khối `GIVEN`.
2.  **Đánh giá Luật dẫn (Rule Firing Cycle)**: `RuleEvaluator` duyệt qua toàn bộ danh sách các luật dẫn hiện có. Nếu các điều kiện `IF` được khớp bởi tập dữ kiện hiện tại (thông qua mạng Rete), các hành động `SET` sẽ được thực hiện để sinh ra dữ kiện mới.
3.  **Lan truyền Biến đồng nhất (SameVariables Propagation)**: Khi một biến số được cập nhật, hệ thống tự động lan truyền giá trị đó qua các quan hệ đồng nhất (**SameVariables**), đảm bảo tính đồng bộ tri thức trên toàn hệ thống.
4.  **Kiểm tra Hội tụ (Convergence Check)**: Nếu sau một lượt quét (Pass), có ít nhất một dữ kiện mới được sinh ra, hệ thống quay trở lại Bước 2. Nếu không có dữ kiện nào mới, hệ thống tuyên bố đạt trạng thái **F-Closure** và kết thúc tiến trình.

## 2. Kiểm soát Tính ổn định và Giải quyết Xung đột

Trong quá trình lan truyền tri thức, KBMS áp dụng các cơ chế quản trị để duy trì tính nhất quán:

-   **Ngăn chặn Vòng lặp Vô hạn (Infinite Loop Prevention)**: Hệ thống duy trì một bộ đếm bước suy luận. Nếu số lượt quét vượt qua ngưỡng cho phép (mặc định là 100), hệ thống sẽ tự động ngắt tiến trình để bảo vệ tài nguyên máy chủ.
-   **Độ ưu tiên Chuyên biệt hóa (Specialization Principle)**: Các luật dẫn tại Khái niệm con (Concept cụ thể) sẽ được ưu tiên xem xét trước các luật dẫn chung tại Khái niệm cha. Điều này giúp hệ thống đưa ra kết luận sát nhất với thực tế dữ liệu.
-   **Nguyên tắc Bất biến Tri thức**: Một dữ kiện sau khi đã được xác lập (Confirmed Fact) sẽ được bảo vệ, trừ khi có lệnh `UPDATE` hoặc `DELETE` rõ ràng từ người dùng, giúp duy trì tính ổn định của chu trình đóng khép.
