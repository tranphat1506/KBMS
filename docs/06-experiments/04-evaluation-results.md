# 6.4. Tổng kết và Đánh giá Kết quả Thực nghiệm

Dựa trên các số liệu đo đạc thực tế từ chương 6.1 đến 6.3, chương này đưa ra các đánh giá tổng kết về mức độ hoàn thành mục tiêu của luận văn đối với hệ quản trị cơ sở tri thức KBMS V3.

## 1. Đánh giá về Tính Đúng đắn (Correctness)
Hệ thống đã vượt qua bộ kiểm thử tích hợp (Integration Tests) bao gồm **111 kịch bản** khác nhau:
- **Ngôn ngữ KBQL**: 100% các câu lệnh DDL, DML, KCL và Tcl được phân tích cú pháp và thực thi đúng theo đặc tả AST.
- **Cơ chế Suy diễn**: Công cụ Inference Engine (Rete V3) thực hiện chính xác các bao đóng tri thức (Closure) trên cả 4 kịch bản đa miền, đảm bảo tính nhất quán (Consistency) của tri thức dẫn xuất.
- **Giao dịch (ACID)**: Cơ chế Write-Ahead Logging (WAL) đảm bảo dữ liệu không bị thất thoát ngay cả khi hệ thống dừng đột ngột (Crash recovery).

## 2. Đánh giá về Hiệu năng (Performance)
KBMS V3 thể hiện sức mạnh vượt trội nhờ kiến trúc hướng trang (Page-oriented) và Query Optimizer:
- **Xử lý dữ liệu lớn**: Thông lượng ghi đạt hơn **41,200 ops/sec** trên tập dữ liệu 1 triệu thực thể (DS-L).
- **Phản hồi thời gian thực**: Độ trễ suy diễn (Reasoning Latency) trung bình dưới **10ms** cho các kịch bản thực tế, đáp ứng yêu cầu của các hệ thống IoT và chẩn đoán y khoa.
- **Truy vấn quan hệ**: Phép toán Hash Join được tối ưu hóa ở tầng Page Buffer giúp giảm 40% thời gian so với các phương pháp giải mã thực thể truyền thống.

## 3. Đánh giá về Khả năng Mở rộng (Scalability)
- **Quản lý bộ nhớ**: Buffer Pool Manager (LRU) giúp KBMS vận hành ổn định ngay cả khi kích thước cơ sở tri thức vượt quá RAM vật lý.
- **Cấu trúc lưu trữ**: B+ Tree duy trì chiều cao thấp (h=4 cho 1 triệu bản ghi), đảm bảo độ trễ truy cập dữ liệu tăng trưởng theo mức logarit cơ số n.

## 4. Kết luận
Thực nghiệm cho thấy KBMS V3 không chỉ là một hệ quản trị dữ liệu truyền thống mà còn là một nền tảng tri thức lai (Hybrid RDB/KBS) mạnh mẽ. Hệ thống đã giải quyết được bài toán cân bằng giữa tính linh hoạt của mô hình đối tượng (COKB) và hiệu năng truy xuất của cơ sở dữ liệu quan hệ, đạt được các mục tiêu khoa học đề ra trong luận văn.
