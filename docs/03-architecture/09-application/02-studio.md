# Giao diện Quản trị tri thức (KBMS Studio)

**KBMS Studio** được phát triển như một môi trường phát triển tích hợp (IDE) hiện đại, giúp chuyên gia tri thức có thể thiết kế, kiểm thử và giám sát cơ sở tri thức một cách trực quan.

## 1. Các Tính năng Chính

-   **Trình soạn thảo Tri thức Thông minh**: Tích hợp Monaco Editor với khả năng hỗ trợ cú pháp KBQL.
-   **Trực quan hóa Phả hệ Tri thức**: Hiển thị cấu trúc cây của các Concepts và Rules.
-   **Giám sát Hệ thống**: Dashboard hiển thị tài nguyên CPU, RAM và trạng thái kết nối thời gian thực.
-   **Trình truy vết Suy luận**: Minh bạch hóa quá trình suy luận thông qua sơ đồ cây logic.

## 2. Kiến trúc và Các tầng Xử lý của Studio

Ứng dụng Studio được xây dựng trên nền tảng React, tuân thủ kiến trúc phân lớp để đảm bảo tính mở rộng và khả năng bảo trì:

![Kiến trúc Phân lớp của Ứng dụng Studio | width=1.1](../../../assets/diagrams/studio_internal_arch.png)
*Hình 4.34: Kiến trúc thành phần và luồng dữ liệu nội bộ của KBMS Studio.*

-   **Tầng Giao diện (Presentation Layer)**: Sử dụng mô hình Component-based của React để xây dựng các khu vực chức năng như Editor, Explorer, và Monitor.
-   **Tầng Điều phối (Coordination Layer)**: Quản lý trạng thái ứng dụng thông qua các Context và Reducers, điều phối dữ liệu giữa giao diện và các dịch vụ mạng.
-   **Tầng Giao tiếp (Communication Layer)**: Hiện thực hóa giao thức nhị phân trên WebSocket hoặc TCP Proxy để trao đổi dữ liệu với máy chủ KBMS.

### 2.1. Cơ chế Cập nhật Dữ liệu Thời gian thực

Studio sử dụng cơ chế truyền tải bất đồng bộ để cập nhật trạng thái hệ thống mà không làm gián đoạn trải nghiệm người dùng:

![Cơ chế Server Push | width=1.05](../../../assets/diagrams/4_tier_notification_flow.png)
*Hình 4.35: Cơ chế Server Push cho các thông báo hệ thống và an ninh thời gian thực.*

1.  **Kích hoạt Sự kiện (Trigger)**: Một sự kiện an ninh hoặc hệ thống được phát hiện tại tầng máy chủ.
2.  **Đẩy tin (Push)**: Máy chủ đóng gói thông điệp và truyền tải trực tiếp qua Socket.
3.  **Điều hướng (Dispatch)**: Ứng dụng Studio tiếp nhận gói tin và cập nhật trạng thái thông báo tới giao diện người dùng.

### 2.2. Quy trình Xác lập Phiên làm việc (Authentication Flow)

Tiến trình đăng nhập bảo mật được thực hiện qua chuỗi các bước xác thực hình thức:

1.  **Bắt tay Xác thực (Handshake)**: Studio truyền tải gói tin `LOGIN` chứa thông tin định danh được mã hóa bảo mật.
2.  **Kiểm chứng Máy chủ**: Máy chủ thực hiện đối soát thông tin trong phân hệ quản trị người dùng (Tầng 4).
3.  **Xác lập Ngữ cảnh**: Khi thông tin khớp, máy chủ khởi tạo một ngữ cảnh phiên làm việc (**SessionContext**) trong RAM và phản hồi trạng thái thành công, cho phép Studio bắt đầu các thao tác tương tác tri thức.

## 3. Các Kịch bản Sử dụng Studio

Chương này trình bày các tình huống sử dụng thực tế của phân hệ Studio, minh họa quy trình tương tác phối hợp giữa các công cụ đồ họa thông qua các sơ đồ luồng dữ liệu.

### 3.1. Kịch bản 1: Thiết kế cấu trúc tri thức

Sử dụng trình thiết kế tri thức để xây dựng cấu trúc các Khái niệm và Luật dẫn.

![Luồng logic: Thiết kế tri thức](../../../assets/diagrams/uc_studio_designer_flow.png)
*Hình 4.31: Quy trình soạn thảo và biên dịch tri thức trên giao diện Studio.*

-   **Mục tiêu**: Xây dựng mô hình tri thức hình thức thông qua giao diện đồ họa.
-   **Quy trình**: Người dùng thực hiện lệnh soạn thảo; hệ thống cung cấp các gợi ý cú pháp và phản hồi lỗi tức thời từ máy chủ.

### 3.2. Kịch bản 2: Giải quyết bài toán và truy vết suy luận

Tìm kiếm lời giải cho mục tiêu tri thức và theo dõi sơ đồ suy luận.

![Luồng logic: Giải thuật suy luận](../../../assets/diagrams/uc_studio_trace_flow.png)
*Hình 4.32: Chu trình thực thi suy luận và hiển thị cây bước logic.*

-   **Mục tiêu**: Thực hiện các bài toán suy luận và minh bạch hóa quá trình giải quyết.
-   **Quy trình**: Nhập yêu cầu giải quyết mục tiêu; Studio hiển thị kết quả dưới dạng lưới dữ liệu và sơ đồ truy vết các bước logic đã thực hiện.

### 3.3. Kịch bản 3: Giám sát và bảo trì hệ thống

Theo dõi trạng thái vận hành và thực hiện các thao tác bảo trì cơ sở tri thức.

![Luồng logic: Giám sát hệ thống](../../../assets/diagrams/uc_studio_monitor_flow.png)
*Hình 4.33: Quy trình thu tập chỉ số và điều phối bảo trì.*

-   **Mục tiêu**: Đảm bảo trạng thái ổn định của hệ thống quản trị tri thức.
-   **Quy trình**: Theo dõi các biểu đồ tài nguyên trên giao diện; thực hiện các lệnh tối ưu hóa hoặc làm sạch dữ liệu khi cần thiết.

## 4. Giao diện ứng dụng KBMS Studio

Phân hệ giao diện đồ họa (**KBMS Studio**) được thiết kế như một môi trường tích hợp (IDE) giúp tối ưu hóa quy trình quản trị và phát triển tri thức. Dưới đây là đặc tả chi tiết các khu vực chức năng chính của ứng dụng:

### 4.1. Giao diện quản lý dự án và phả hệ tri thức

Cung cấp cái nhìn tổng quát về cấu trúc tổ chức của cơ sở tri thức hiện hành. Giao diện bao gồm:

*   **Cây Explorer**: Hiển thị danh sách phân cấp của các Concepts, Relations và Rules. Người dùng có thể nhanh chóng định vị các đối tượng tri thức thông qua cấu trúc thư mục logic.
*   **Thanh điều hướng nhanh**: Cho phép chuyển đổi nhanh giữa các tập tin tri thức (`.kbql`) đang mở.
*   **Trình đơn ngữ cảnh**: Cung cấp các thao tác nhanh như tạo mới, xóa hoặc đổi tên các thực thể tri thức trực tiếp trên cây phả hệ.

![Giao diện quản lý dự án và Explorer Studio](../../../assets/diagrams/studio_interface_explorer.png)
*Hình 4.39: Giao diện quản lý cây phả hệ và điều phối tập tin tri thức.*

### 4.2. Giao diện soạn thảo mã nguồn tích hợp

Đây là khu vực tương tác trọng tâm dành cho việc định nghĩa tri thức hình thức. Giao diện bao gồm:

*   **Vùng soạn thảo Monaco**: Hỗ trợ tô màu cú pháp chuyên sâu cho ngôn ngữ KBQL, hiển thị số dòng và hỗ trợ thu gọn khối lệnh (Code Folding).
*   **Hệ thống IntelliSense**: Tự động gợi ý các từ khóa đặc quyền và tên các Khái niệm đã được định nghĩa, giúp tăng tốc độ soạn thảo và giảm sai sót.
*   **Chỉ báo lỗi trực tiếp**: Các lỗi biên dịch được gạch chân và hiển thị thông báo chi tiết khi di chuột qua, giúp hiệu chỉnh mã nguồn tức thời.

![Giao diện soạn thảo mã nguồn và IntelliSense Studio](../../../assets/diagrams/studio_interface_designer.png)
*Hình 4.40: Giao diện thiết kế tri thức với hỗ trợ cú pháp và kiểm lỗi.*

### 4.3. Giao diện giám sát hiệu năng hệ thống

Cung cấp các thông số vận hành thời gian thực của máy chủ KBMS. Giao diện bao gồm:

*   **Biểu đồ tài nguyên**: Trực quan hóa mức độ chiếm dụng CPU và RAM theo thời gian.
*   **Chỉ số Disk I/O**: Giám sát tốc độ đọc/ghi dữ liệu vào tệp tin cơ sở tri thức, hỗ trợ phát hiện các điểm nghẽn hiệu năng.
*   **Trạng thái Kết nối**: Hiển thị số lượng phiên làm việc đang hoạt động và băng thông đang sử dụng.

![Giao diện giám sát tài nguyên hệ thống Studio | width=1.1](../../../assets/diagrams/studio_interface_monitor.png)
*Hình 4.41: Giao diện Dashboard giám sát sức khỏe và hiệu năng máy chủ.*

### 4.4. Giao diện thực thi và trực quan hóa kết quả

Khu vực hiển thị phản hồi từ máy chủ sau khi thực thi các yêu cầu tri thức. Giao diện bao gồm:

*   **Data Grid tương tác**: Kết quả truy vấn sự kiện được trình bày dưới dạng lưới dữ liệu, hỗ trợ sắp xếp và lọc trực tiếp trên các cột.
*   **Bảng điều khiển Trace**: Hiển thị sơ đồ cây các bước suy luận dành riêng cho lệnh `SOLVE`, giúp giải thích tường tận cách máy rút ra kết luận.
*   **Console Log**: Ghi nhận lịch sử các gói tin nhị phân đã trao đổi, phục vụ mục đích chẩn đoán hệ thống.

![Giao diện kết quả truy vấn và truy vết suy luận Studio | width=1.1](../../../assets/diagrams/studio_interface_results.png)
*Hình 4.42: Giao diện hiển thị kết quả và trực quan hóa tiến trình suy cứu.*
