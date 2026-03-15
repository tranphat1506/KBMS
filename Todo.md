
CÔNG VIỆC
STT
1
XÁC ĐỊNH YÊU CẦU CỦA HỆ QUẢN TRỊ CƠ SỞ TRI THỨC TRÊN COKB
2
3
4
1,1 Nghiên cứu mô hình COKB: Nghiên cứu 6 thành phần COKB: Concept, Hierarchy, Relation, Operator, Function, Rule
1,2
2,1
Xác định yêu cầu quản trị: Tổ chức tri thức, quản trị thêm/xóa/sửa, truy vấn và khai thác tri thức
THIẾT KẾ KIẾN TRÚC HỆ QUẢN TRỊ CƠ SỞ TRI THỨC DẠNG COKB
Thiết kế kiến trúc tổng thể hệ thống theo mô hình nhiều tầng (Application Layer, KBMS Server Layer, Knowledge Storage Engine, Physical Storage)
Thiết kế cấu trúc dữ liệu tri thức dạng COKB và ngôn ngữ truy vần (KBDDL, KBDML)
- Mô hình tri thức dạng COKB bao gồm Concept, Hierachy, Relation, Ops, Funcs, Object
- Ngôn ngữ truy vấn được chia thành 2 loại:
+KBDDL (Các câu lệnh tạo sửa xoá các database (KB) có cấu trúc tri thức dạng COKB) KnowledgeBase Data
Definition: CREATE, TRUNCATE, DROP,...
+KBDML (Các câu lệnh tạo, sửa xoá các dữ liệu trong một kh cụ thể) KnowledgeBase Manipulation Language:Select, 2,2 Insert, Update,...
3,1
CÀI ĐẶT VÀ TRIỂN KHAI HỆ QUẢN TRỊ CƠ SỞ TRI THỨC
Cài đặt Storage Engine và Physical Storage Layer (quản lý, thêm/xóa/sửa tri thức, load/save disk)
- Cài đặt phân quyền ROOT, và quyền hạn cho các user khác (READ, WRITE)
- Lưu log, metadata, .. của hệ thống trong do hệ thống. User có thể xem chi tiết
- Mỗi KB là một folder riêng, mỗi KB riêng đều có mã hoá dữ liệu trong file.
- Mỗi file trong KB đều được mã hoá và giải mã dữ liệu, phân quyền, và có cấu trúc dạng file .bin.
3,2 Cài đặt và thiết kế giao thức network giữa Application Layer và tầng KBMS Server. Chạy và kiểm thử giao diện
TỔNG KẾT & VIẾT BÁO CÁO
4,1 Viết bài báo khoa học tóm tắt đề tài và hoàn thiện quyền báo cáo khóa luận.