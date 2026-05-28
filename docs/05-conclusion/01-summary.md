# 5.1. Tổng kết những kết quả đạt được

Trải qua quá trình tìm hiểu lý thuyết và tiến hành xây dựng, đồ án đã hoàn thành mục tiêu ban đầu là thiết kế và phát triển một Hệ quản trị cơ sở tri thức (KBMS) cơ bản [9]. Các kết quả đạt được của đồ án bao gồm ba điểm chính:

Thứ nhất, về mặt cấu trúc lưu trữ, đồ án đã chuyển đổi thành công mô hình COKB (Computational Object Knowledge Base) từ lý thuyết thành hệ thống lưu trữ thực tế [10]. Dữ liệu về Khái niệm (Concepts) và Luật (Rules) được tổ chức thành các khối nhị phân trên cấu trúc Slotted Page và cây B+ Tree. Cách làm này giúp việc tìm kiếm (Look-up) dữ liệu nhanh hơn, giải quyết được tình trạng đọc ghi chậm trên ổ đĩa thường gặp ở các bài toán lưu trữ tĩnh.

Thứ hai, về mặt xử lý suy diễn, đồ án đã xây dựng được một Inference Engine (Động cơ suy diễn) riêng. Điểm nổi bật là việc cài đặt thuật toán mạng Rete chạy trên bộ nhớ trong (In-Memory), hỗ trợ cả suy diễn tiến (Forward Chaining) và suy diễn lùi (Backward Chaining) [11]. Nhờ biểu diễn các điều kiện dưới dạng đồ thị Rete, quá trình khớp lệnh (Pattern Matching) diễn ra nhanh chóng, cho phép hệ thống giải quyết các bài toán có nhiều Khái niệm liên kết với nhau như định giá khách hàng hay tính toán hình học.

Thứ ba, về mặt ứng dụng, đồ án đã hoàn thiện hệ thống theo mô hình Client-Server. Nhóm đã tự thiết kế ngôn ngữ truy vấn KBQL (Knowledge Base Query Language), đi kèm với trình phân tích cú pháp (Parser) để Client có thể tương tác với Server qua giao thức TCP/IP. Các công cụ hỗ trợ như KBMS CLI và Studio IDE cũng được phát triển giúp người dùng dễ dàng thao tác, kiểm tra lỗi và trực quan hóa cơ sở tri thức.

# 5.2. Những mặt hạn chế còn tồn tại

Bên cạnh những kết quả đạt được, do giới hạn về thời gian và kiến thức, hệ thống KBMS hiện tại vẫn còn một số điểm chưa hoàn thiện:

Hạn chế lớn nhất là khả năng xử lý phân tán. Hiện tại, kiến trúc của KBMS V3 chỉ được thiết kế để chạy trên một máy chủ duy nhất (Single-node). Nếu lượng truy cập từ Client quá lớn hoặc dữ liệu vượt quá dung lượng ổ cứng, hệ thống không thể tự động chia nhỏ dữ liệu (Sharding) hay nhân bản (Replication) sang các máy khác để giảm tải. 

Tiếp đến là vấn đề xử lý xung đột trong luật suy diễn (Conflict Resolution). Khi có nhiều Luật cùng thỏa mãn điều kiện nhưng đưa ra kết luận trái ngược nhau, hệ thống hiện tại mới chỉ giải quyết cứng nhắc dựa trên mức độ ưu tiên (Priority) do người dùng thiết lập sẵn. Hệ thống chưa có khả năng tự đánh giá hoặc gỡ rối tự động khi dữ liệu đầu vào phức tạp.

# 5.3. Hướng phát triển

Từ những hạn chế trên, nhóm đề ra các hướng phát triển tiếp theo để hoàn thiện đồ án:

Trước tiên, nâng cấp kiến trúc lưu trữ để hỗ trợ phân tán dữ liệu trên nhiều máy chủ (Cluster). Việc áp dụng các thuật toán đồng bộ cơ bản có thể giúp hệ thống mở rộng ngang (Horizontal Scaling), từ đó xử lý được lượng dữ liệu lớn hơn và tăng khả năng chịu lỗi nếu một máy chủ gặp sự cố [12].

Thứ hai, nghiên cứu tích hợp các kỹ thuật học máy (Machine Learning) vào hệ thống. Thay vì bắt buộc người dùng phải gõ từng câu lệnh KBQL để định nghĩa Luật một cách thủ công, hệ thống có thể phân tích dữ liệu cũ để tự động gợi ý hoặc tự sinh ra các Luật mới. Việc này sẽ giúp KBMS trở nên thông minh hơn và dễ tiếp cận hơn đối với người sử dụng.
