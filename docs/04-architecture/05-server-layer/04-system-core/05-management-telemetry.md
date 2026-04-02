# 4.5.4.2. Quản trị và Giám sát Từ xa (Management & Telemetry)

Một hệ thống quản trị tri thức hiện đại yêu cầu khả năng tự giám sát sức khỏe vận hành và khả năng phát tán nhật ký hoạt động (Logging) theo thời gian thực. Trong kiến trúc KBMS V3, các thành phần tại `ManagementManager.cs` và `SystemLogger.cs` đảm nhiệm vai trò duy trì sự minh bạch và ổn định của máy chủ.

## 1. Cơ chế Thu thập Chỉ số Vận hành (System Metrics)

Hệ thống cung cấp giao diện lập trình cho phép các thành phần khách (đặc biệt là KBMS Studio) truy xuất dữ liệu vận hành thông qua các lệnh quản trị cấp cao:

*   **Chỉ số Đo lường (STATS)**: Cung cấp thông tin định lượng về số lượng kết nối đang hoạt động (**Active Connections**), thời gian vận hành liên tục (**Uptime**), và hiệu suất sử dụng tài nguyên của bộ nhớ đệm (**BufferPool Hit Rate**). Các chỉ số này đóng vai trò quan trọng trong việc đưa ra quyết định mở rộng tài nguyên (Scaling) hoặc tối ưu hóa truy vấn.
*   **Điều phối Phiên làm việc (SESSIONS)**: Quản trị viên có khả năng trích xuất danh sách thực thể người dùng trực tuyến, thực hiện các thao tác ngắt kết nối (Kill Session) hoặc điều chỉnh ngưỡng giới hạn kết nối đồng thời để bảo vệ tài nguyên hệ thống trước các nguy cơ tấn công từ chối dịch vụ (DoS).

---

## 2. Hệ thống Nhật ký Luồng (Live Log Streaming)

Đây là một tính năng tiên tiến của KBMS V3, cho phép giám sát mọi biến động của máy chủ mà không gây ảnh hưởng đến hiệu suất thực thi chính.

### 2.1. Kiến trúc `SystemLogger.cs`
Lớp này đóng vai trò là thực thể ghi nhận trung tâm. Khi phát sinh các sự kiện hệ thống hoặc lỗi thực thi tại tầng Lưu trữ/Suy diễn, `SystemLogger` thực hiện cơ chế ghi kép (Dual-write):
1.  **Lưu trữ Vật lý**: Ghi nhận vào tệp tin `.log` truyền thống phục vụ mục đích truy vết và hậu kiểm lâu dài.
2.  **Lưu trữ Tri thức**: Ghi trực tiếp một sự kiện (Fact) mới vào Concept `Log` bên trong cơ sở tri thức `system`. Cơ chế này biến nhật ký vận hành thành một phần của mạng lưới tri thức, cho phép truy vấn ngược bằng ngôn ngữ KBQL.

### 2.2. Giao thức `LOGS_STREAM` (Publish/Subscribe)
Khi một ứng dụng khách (với vai trò ROOT) gửi yêu cầu `LOGS_STREAM`, `ManagementManager` sẽ kích hoạt mô hình **Pub/Sub**. Mọi dòng nhật ký vừa phát sinh sẽ được đóng gói và gửi ngay lập tức tới các Client đã đăng ký. Cơ chế này cho phép xây dựng các bảng điều khiển (Dashboards) giám sát tri thức thời gian thực với độ trễ cực thấp.

---

## 3. Quyền hạn và Lệnh Quản trị Đặc quyền

Đối với các tác vụ cấu hình hệ thống hoặc khôi phục dữ liệu, máy chủ cung cấp mã lệnh `MANAGEMENT_CMD` (0x0D), được thiết kế với các ràng buộc an ninh nghiêm ngặt:
*   **Xác thực vai trò (ROOT only)**: Chỉ những tài khoản có thẩm quyền tối cao mới có thể kích hoạt các lệnh can thiệp vào tầng hạ tầng.
*   **Khả năng can thiệp**: Cho phép thay đổi nóng các tham số cấu hình như `MaxConnections`, `BufferPoolSize`, hoặc kích hoạt tiến trình dọn dẹp ghi nhật ký trước (Write-Ahead Log - WAL) thông qua lệnh `Checkpoint`.

Sự kết hợp giữa điều phối luồng bất đồng bộ và khả năng giám sát từ xa tạo nên một hạ tầng máy chủ vững chắc. Để chứng minh tính ổn định này, hệ thống cần vượt qua các bài kiểm định khắt khe về thông lượng và độ tin cậy trong môi trường thực tế.
