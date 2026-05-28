# 4.4. Đánh giá giới hạn chịu tải vật lý (Stress Testing)

Mục tiêu cuối cùng của quá trình kiểm thử là phác họa giới hạn chịu tải (Scalability) của cấu trúc dữ liệu vật lý khi dung lượng tri thức phình to [6]. Các bài test thuộc nhóm `LoadAndStressTests.cs` tiến hành chèn liên tục 1,000,000 bản ghi ngẫu nhiên vào hệ thống, qua đó ghi nhận các chỉ số về Thông lượng (Throughput) và Độ trễ (Latency).

![Biểu đồ Thông lượng Ghi theo Buffer Pool](../assets/diagrams/eval_throughput_chart.png)
*Hình 4.5: Mối tương quan giữa Kích thước Buffer Pool và Thông lượng ghi.*

Phân tích dữ liệu từ biểu đồ (Hình 4.5) cho thấy một sự đánh đổi (Trade-off) rõ rệt giữa dung lượng bộ nhớ RAM (Buffer Pool) và tốc độ ghi đĩa. Khi tắt hoàn toàn bộ nhớ đệm (No Buffer), hệ thống phải ghi trực tiếp từng bản ghi xuống đĩa cứng vật lý (Direct I/O). Tốc độ lúc này bị nghẽn ở mức 15,000 thao tác/giây (ops/sec) do độ trễ cơ học của thiết bị lưu trữ. 

Ngược lại, khi cấp phát cho hệ thống 256MB RAM để quản lý Buffer Pool, sự kết hợp giữa thuật toán thay thế trang LRU (Least Recently Used) và cơ chế ghi hoãn (Dirty Pages) đã loại bỏ hoàn toàn nút thắt cổ chai I/O. Tại mốc này, thông lượng hệ thống đạt cực đại hơn 200,000 ops/sec, hoàn tất việc nạp 1 triệu bản ghi chỉ trong xấp xỉ 5 giây. Cơ chế Write-Ahead Logging đóng vai trò đồng bộ ngầm các trang thay đổi (Flushing) xuống đĩa mà không làm gián đoạn luồng thực thi chính của CPU.

Tác động của bộ nhớ đệm (Buffer Pool) đến thông lượng I/O được định lượng thông qua phép thử chèn dữ liệu hàng loạt (Bulk Insert). Quá trình đo lường tiến hành đối chiếu thông lượng ghi đĩa thực tế trên ba quy mô tập dữ liệu khác nhau, tương ứng với ba mức cấu hình dung lượng bộ nhớ đệm. Dữ liệu tại Bảng 4.3 cho thấy tốc độ ghi (Ops/sec) duy trì ổn định khi dung lượng RAM cấp phát lớn hơn kích thước tập dữ liệu cần chèn. Điều này khẳng định thuật toán LRU Cache đã hấp thụ hiệu quả độ trễ vật lý của ổ đĩa, nâng cao thông lượng tổng thể lên xấp xỉ 14 lần so với cấu hình ghi trực tiếp (Direct I/O).

| Quy mô Tập dữ liệu (Bản ghi) | Chế độ Direct I/O (0 MB Buffer) | Chế độ LRU Cache (64 MB Buffer) | Chế độ LRU Cache (256 MB Buffer) | Hệ số Cải thiện |
|---|---|---|---|---|
| 10,000 | 14,500 Ops/sec | 210,000 Ops/sec | 215,000 Ops/sec | 14.8 lần |
| 100,000 | 14,800 Ops/sec | 165,000 Ops/sec | 210,000 Ops/sec | 14.1 lần |
| 1,000,000 | 15,000 Ops/sec | 85,000 Ops/sec | 215,000 Ops/sec | 14.3 lần |
*Bảng 4.3: Thông lượng chèn dữ liệu theo quy mô bản ghi và dung lượng bộ nhớ đệm.*

Bên cạnh tốc độ ghi, thời gian đáp ứng (Response Time) của Inference Engine cũng được đo lường trong tình huống xấu nhất (Worst-case Scenario). Trong bài test Hash Join giữa hai tập dữ liệu lớn (10,000 phần tử mỗi tập), thuật toán Rete vẫn duy trì thời gian thực thi trung bình ở mức ~7.0 ms. Khả năng này có được nhờ việc ứng dụng cây nhị phân B+ Tree tại lớp Storage, giúp việc dò tìm (Look-up) các khóa ngoại (Foreign Keys) tại *Beta Node* chỉ tiêu tốn chi phí thời gian logarithm ($O(\log N)$).

# 4.5. Tổng kết Kết quả Thực nghiệm

Tổng hợp các số liệu đo lường từ 418 kịch bản kiểm thử, có thể khẳng định Hệ quản trị cơ sở tri thức KBMS đáp ứng toàn diện các tiêu chuẩn kỹ thuật thiết yếu [7]. Hệ thống không chỉ xử lý trơn tru các giải thuật suy diễn phức tạp (như Forward/Backward Chaining) với sự hỗ trợ của mạng Rete, mà còn sở hữu một động cơ lưu trữ bền bỉ. Mức thông lượng 200,000 ops/sec trên quy mô triệu bản ghi cùng khả năng tự phục hồi dữ liệu thông qua cơ chế WAL là những minh chứng cụ thể cho tính ứng dụng thực tiễn của kiến trúc COKB, mở ra triển vọng triển khai trên môi trường Client-Server thực tế.
