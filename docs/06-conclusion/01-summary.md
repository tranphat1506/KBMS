# 7.1. Tổng kết kết quả đạt được

Dựa trên quá trình nghiên cứu, thiết kế và thực nghiệm hệ thống KBMS, đề tài đã đạt được các kết quả trọng tâm sau:

- **Về mặt lý thuyết**: Hiện thực hóa thành công mô hình đối tượng tính toán COKB vào cấu trúc lưu trữ nhị phân, đảm bảo tính linh hoạt trong biểu diễn tri thức.
- **Về mặt công nghệ**: Xây dựng được công cụ lưu trữ dạng client/server, có bộ máy lưu trữ cũng như giao thức mạng cơ bản giữa server và client, tích hợp mạng Rete tối ưu hóa suy diễn và tối ưu được lưu trữ dưới ổ đĩa.
- **Về mặt ứng dụng**: Cung cấp bộ công cụ Studio IDE và CLI, cho phép người dùng cuối tiếp cận tri thức một cách trực quan.

# 7.2. Hạnh chế và Hướng phát triển tương lai

Mặc dù đạt được những chỉ số hiệu năng trên, hệ thống vẫn tồn tại một số điểm cần cải thiện:

- **Hạn chế**: Chưa hỗ trợ phân tán dữ liệu (Sharding) trên nhiều nốt mạng độc lập. Cơ chế giải quyết xung đột luật vẫn còn ở mức cơ bản.
- **Hướng phát triển**: Nghiên cứu tích hợp các mô hình học sâu (Deep Learning) để tự động sinh luật từ dữ liệu thô. Chuyển đổi kiến trúc sang hướng Cloud-native hỗ trợ Scaling tự động.
