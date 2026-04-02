# Kiến trúc Phân lớp và Luồng Điều phối Studio

**KBMS Studio** vận hành dựa trên một kiến trúc phân tầng chặt chẽ, từ giao diện người dứng tới các khối dữ liệu nhị phân tại tầng lưu trữ vật lý.

## 1. Sơ đồ Luồng Xử lý Hợp nhất 4 Tầng

Sơ đồ dưới đây mô tả cách thức một yêu cầu tri thức hình thức (như lệnh **`SOLVE`**) được điều hướng xuyên suốt hệ thống:

![Luồng Xử lý Studio](../../../assets/diagrams/4_tier_studio_flow.png)
*Hình 4.34: Quy trình điều phối yêu cầu tri thức xuyên suốt 4 tầng chức năng từ giao diện Studio.*

### 1.1. Phân tầng Chiến lược

-   **Tầng 1: Giao diện Ứng dụng (React/Electron)**: Thành phần frontend (React) quản lý trạng thái luồng dữ liệu thông qua cơ chế Redux. Khi người dùng xác nhận thực thi, yêu cầu được chuyển tải tới tiến trình chính của Electron để xử lý hạ tầng.
-   **Tầng 2: Truyền vận và Giao thức (Network & Protocol)**: Chuyển đổi yêu cầu thành các gói tin nhị phân chuẩn hóa. Hệ thống duy trì các tín hiệu nhịp tim (**Heartbeat**) định kỳ để đảm bảo sự ổn định của kết nối Socket giữa Studio và Máy chủ.
-   **Tầng 3: Bộ máy Tri thức (Knowledge Engine)**: Thành phần `KnowledgeManager` điều phối việc phân tích mã nguồn tri thức. Nếu là lệnh suy diễn, bộ máy thực thi giải thuật F-Closure trên các trang dữ liệu được nạp vào bộ nhớ tạm thời.
-   **Tầng 4: Lưu trữ Vật lý (Storage Layer)**: Sử dụng các cấu trúc chỉ mục **B+ Tree** để định vị dữ liệu thực thể. Cơ chế **WAL** (Write-Ahead Logging) đảm bảo tính toàn vẹn của tri thức ngay cả khi xảy ra các sự cố ngắt quãng nguồn điện.

## 2. Cơ chế Thông báo Thời gian thực (Real-time Notification)

Hệ thống thông báo của Studio sử dụng mô hình đẩy dữ liệu từ phía máy chủ (**Server Push**) thay vì mô hình yêu cầu - phản hồi truyền thống:

![Cơ chế Server Push | width=1.05](../../../assets/diagrams/4_tier_notification_flow.png)
*Hình 4.35: Cơ chế Server Push cho các thông báo hệ thống và an ninh thời gian thực.*

1.  **Kích hoạt Sự kiện (Trigger)**: Một sự kiện an ninh hoặc hệ thống được phát hiện tại tầng máy chủ.
2.  **Đẩy tin (Push)**: Máy chủ đóng gói thông điệp và truyền tải trực tiếp qua Socket.
3.  **Điều hướng (Dispatch)**: Ứng dụng Studio tiếp nhận gói tin và cập nhật trạng thái thông báo tới giao diện người dùng.

## 3. Quy trình Xác lập Phiên làm việc (Authentication Flow)

Tiến trình đăng nhập bảo mật được thực hiện qua chuỗi các bước xác thực hình thức:

1.  **Bắt tay Xác thực (Handshake)**: Studio truyền tải gói tin `LOGIN` chứa thông tin định danh được mã hóa bảo mật.
2.  **Kiểm chứng Máy chủ**: Máy chủ thực hiện đối soát thông tin trong phân hệ quản trị người dùng (Tầng 4).
3.  **Xác lập Ngữ cảnh**: Khi thông tin khớp, máy chủ khởi tạo một ngữ cảnh phiên làm việc (**SessionContext**) trong RAM và phản hồi trạng thái thành công, cho phép Studio bắt đầu các thao tác tương tác tri thức.
