# Đặc tả Giao diện Dòng lệnh (KBMS CLI)

**KBMS-CLI** là công cụ quản trị và khai thác tri thức trực tiếp dành cho kỹ sư phần mềm và quản trị viên hệ thống. Thay vì thông qua giao diện đồ họa phức hợp, CLI thiết lập kết nối trực tiếp với máy chủ thông qua giao thức nhị phân, cung cấp khả năng kiểm soát hệ thống với độ trễ tối thiểu.

## 1. Các Tính năng

Giao diện dòng lệnh được thiết kế với các cơ chế tương tác nhằm tối ưu hóa hiệu quả làm việc của người dùng trong môi trường console:

-   **Chu trình REPL**: Hệ thống thực hiện tiếp nhận câu lệnh tri thức, truyền tải tới máy chủ, tiếp nhận phản hồi và kết xuất kết quả tức thời ra màn hình điều khiển.
-   **Cơ chế Hiệu chỉnh Dòng lệnh**: Tích hợp các phím chức năng điều hướng và thao tác nhanh thông qua lớp `LineEditor.cs`:
    -   **Duyệt Lịch sử**: Sử dụng phím mũi tên Lên/Xuống để truy xuất các câu lệnh đã thực thi trước đó.
    -   **Điều hướng vị trí**: Các phím Home/End để di chuyển nhanh con trỏ tới đầu hoặc cuối dòng lệnh.
    -   **Quản lý Bộ đệm**: Phím Escape để xóa bộ đệm nhập liệu hiện hành.
-   **Hỗ trợ Nhập liệu Đa dòng**: CLI cho phép nhập các khối lệnh tri thức dài và phức tạp. Chế độ thụt đầu dòng tự động với ký hiệu `->` giúp phân biệt rõ giữa dòng khởi tạo và dòng tiếp nối của câu lệnh.
-   **Các hình thức Hiển thị Dữ liệu**: Thông qua `ResponseParser.cs`, CLI cung cấp hai chế độ hiển thị:
    -   **Chế độ Bảng**: Kết xuất dữ liệu dưới dạng bảng chuẩn hóa.
    -   **Chế độ Dọc**: Hiển thị dữ liệu theo cặp thuộc tính - giá trị trên từng hàng dọc, tự động kích hoạt cho các lệnh mô tả cấu trúc để tối ưu hóa khả năng đọc các thực thể tri thức phức hợp.

## 2. Các Nhóm Lệnh Hệ thống

Bên cạnh ngôn ngữ truy vấn tri thức, CLI cung cấp tập hợp các lệnh điều phối hệ thống:

*Bảng 4.10: Danh mục các lệnh điều khiển trong giao diện CLI*
| Lệnh điều khiển | Đặc tả Chức năng |
| :--- | :--- |
| **`LOGIN <user> <pass>`** | Thực hiện đăng nhập bảo mật. |
| **`SOURCE <path>`** | Thực thi tệp tin kịch bản tri thức từ hệ thống tệp tin cục bộ. |
| **`CONNECT`** | Thiết lập lại kết nối vật lý tới máy chủ KBMS. |
| **`CLEAR`** | Xóa sạch màn hình điều khiển. |

## 3. Cơ chế Vận hành và Quản trị

Để đảm bảo hiệu quả vận hành, CLI được tích hợp các cơ chế tự động hóa:

-   **Thực thi Kịch bản**: Xử lý các tệp tin chứa nhiều lệnh tri thức, báo lỗi chính xác tại dòng lệnh phát sinh sự cố.
-   **Kết nối tự động**: CLI duy trì cơ chế giám sát trạng thái kết nối và tự động thử lại tiến trình kết nối khi phát hiện sự gián đoạn mạng.
-   **Phân tích Phản hồi**: Hệ thống bóc tách các gói tin lỗi từ máy chủ để chỉ ra vị trí dòng và cột phát sinh lỗi.

## 4. Luồng Xử lý và Phân tích Phản hồi của CLI

Giao diện dòng lệnh (CLI) thực hiện chu trình điều phối dữ liệu khép kín, từ giai đoạn thu thập dữ liệu đầu vào tới giai đoạn truyền tải nhị phân và kết xuất kết quả trực quan cho người dùng cuối.

### 4.1. Quy trình Nhận lệnh và Truyền tải

Khi người dùng thực thi một câu lệnh, CLI thực hiện quy trình theo các giai đoạn sau:

![Sơ đồ Luồng Xử lý CLI | width=1.05](../../../assets/diagrams/cli_processing_flow.png)
*Hình 4.29: Sơ đồ tuần tự mô tả luồng xử lý câu lệnh và phản hồi từ Server của CLI.*

1.  **Kiểm tra và Thu thập Dữ liệu Đầu vào**: Hệ thống thực hiện kiểm tra và thu thập các dòng nội dung của câu lệnh từ người dùng cho đến khi tiếp nhận ký hiệu kết thúc câu lệnh (dấu `;`).
2.  **Tạo Gói tin**: Đóng gói nội dung lệnh thành cấu trúc nhị phân `Message` theo định dạng `QUERY` hoặc `LOGIN` phù hợp với tầng mạng.
3.  **Truyền tải Nhị phân**: Gửi gói tin qua Socket (`KBMS.Network`) và duy trì trạng thái chờ đợi phản hồi từ máy chủ.

### 4.2. Phân tích và Kết xuất Phản hồi

Thành phần trọng yếu của CLI nằm ở lớp `ResponseParser.cs`. Do kết quả từ máy chủ có thể là một luồng dữ liệu liên tục (**Streaming Rows**), CLI phải thực hiện xử lý và phân tách từng gói tin nhị phân để hiển thị ra màn hình điều khiển:

-   **Siêu dữ liệu (METADATA)**: Xác lập định nghĩa các cột dữ liệu.
-   **Dữ liệu Bản ghi (ROW)**: Chứa dữ liệu thực tế cho từng thực thể tri thức trong tập kết quả.
-   **Kết quả Tổng quát (RESULT)**: Các thông báo xác nhận trạng thái thực thi thành công.
-   **Thông báo Lỗi (ERROR)**: Chứa thông tin chẩn đoán bao gồm nội dung lỗi và tọa độ phát sinh sai lệch (Dòng, Cột).

### 4.3. Quy trình Hiển thị Bảng Dữ liệu Động

Lớp `ResponseParser` thực hiện vẽ biểu đồ bảng theo thuật toán tối ưu hóa không gian:

1.  **Dựng khung Tiêu đề (Header Rendering)**: Ngay khi tiếp nhận Siêu dữ liệu, CLI tính toán độ rộng cột lớn nhất dựa trên tên thuộc tính để thiết lập khung tiêu đề chuẩn hóa.
2.  **Hỗ trợ Ô dữ liệu đa dòng**: Nếu giá trị trong một ô chứa ký hiệu xuống dòng, hệ thống tự động phân tách và vẽ đường kẻ phân cách hàng để đảm bảo tính mỹ thuật và cân đối của bảng dữ liệu.
3.  **Chuyển đổi Chế độ Hiển thị**: Đối với các phản hồi thuộc nhóm `EXPLAIN` hoặc `DESCRIBE`, hệ thống tự động chuyển sang chế độ hiển thị theo cặp thuộc tính - giá trị trên từng hàng dọc để tối ưu hóa khả năng đọc.

## 5. Cơ chế Thực thi Hàng loạt và Quản lý Luồng

CLI hỗ trợ thực thi khối lượng lớn lệnh thông qua tệp tin kịch bản tri thức. Luồng xử lý được thực hiện tuần tự nhằm đảm bảo tính nhất quán của mạng lưới tri thức hệ thống.

Để duy trì trạng thái vận hành ổn định, CLI thực thi hai luồng xử lý đồng thời:
-   **Luồng Chính (Main Thread)**: Chịu trách nhiệm tương tác và tiếp nhận dữ liệu đầu vào từ người dùng.
-   **Luồng Giám sát (Heartbeat Thread)**: Duy trì tín hiệu định kỳ tới máy chủ để đảm bảo kết nối không bị ngắt quãng do các chính sách về thời gian chờ (Timeout).

## 6. Các Kịch bản Sử dụng CLI

Chương này trình bày các tình huống sử dụng thực tế của phân hệ CLI, minh họa quy trình tương tác giữa người dùng và hệ thống thông qua các sơ đồ luồng dữ liệu.

### 6.1. Kịch bản 1: Đăng nhập và quản lý phiên

Đây là bước khởi đầu để thiết lập kết nối an toàn tới máy chủ.

![Luồng logic: Xác thực hệ thống | width=1.1](../../../assets/diagrams/uc_cli_auth_flow.png)
*Hình 4.26: Luồng xác thực và thiết lập phiên làm việc trên CLI.*

-   **Mục tiêu**: Xác thực quyền truy cập của người dùng.
-   **Quy trình**: Người dùng cung cấp danh tính và mật khẩu; hệ thống thực hiện kiểm tra và cấp mã định danh phiên nếu thông tin hợp lệ.

### 6.2. Kịch bản 2: Thiết kế cấu trúc tri thức

Sử dụng CLI để định nghĩa các Khái niệm và Luật dẫn trong cơ sở tri thức.

![Luồng logic: Định nghĩa cấu trúc | width=0.5](../../../assets/diagrams/uc_cli_kdl_flow.png)
*Hình 4.27: Quy trình xử lý câu lệnh định nghĩa cấu trúc.*

-   **Mục tiêu**: Xây dựng mô hình tri thức hình thức.
-   **Quy trình**: Nhập mã nguồn tri thức; CLI thực hiện gửi gói tin tới máy chủ để biên dịch và cập nhật vào bộ nhớ lưu trữ.

### 6.3. Kịch bản 3: Truy vấn và khai thác dữ liệu

Thực hiện các câu lệnh tìm kiếm dữ kiện và lựa chọn hình thức hiển thị kết quả.

![Luồng logic: Truy vấn dữ liệu | width=0.8](../../../assets/diagrams/uc_cli_kql_flow.png)
*Hình 4.28: Quy trình truy vấn và điều phối hiển thị.*

-   **Mục tiêu**: Truy xuất các đối tượng tri thức có trong hệ thống.
-   **Quy trình**: Thực hiện câu lệnh truy vấn; người dùng có thể lựa chọn hiển thị dạng bảng hoặc dạng dọc tùy theo mã lệnh.

### 6.4. Kịch bản 4: Thực thi và truy vết suy luận

Sử dụng lệnh tìm kiếm lời giải và theo dõi các bước logic đã thực hiện.

![Luồng logic: Truy vết suy luận | width=0.4](../../../assets/diagrams/uc_cli_solve_flow.png)
*Hình 4.29: Chu trình xử lý suy luận và trích xuất cây truy vết.*

-   **Mục tiêu**: Giải quyết bài toán tri thức dựa trên các luật dẫn có sẵn.
-   **Quy trình**: Gửi yêu cầu giải quyết mục tiêu; hệ thống trả về kết luận kèm theo danh sách các bước logic đã kích hoạt.

### 6.5. Kịch bản 5: Xử lý tập lệnh hàng loạt

Thực thi các tệp tin kịch bản chứa tập hợp nhiều câu lệnh tri thức.

![Luồng logic: Xử lý tập lệnh | width=1.2](../../../assets/diagrams/uc_cli_source_flow.png)
*Hình 4.30: Quy trình thực thi tập lệnh từ tệp tin nguồn.*

-   **Mục tiêu**: Tự động hóa quá trình nạp hoặc cập nhật tri thức quy mô lớn.
-   **Quy trình**: Chỉ định đường dẫn tới tệp tin nguồn; hệ thống thực hiện tuần tự các khối lệnh và báo cáo tiến độ.

## 7. Giao diện ứng dụng KBMS-CLI

Phân hệ giao diện dòng lệnh (**KBMS-CLI**) được thiết kế để cung cấp khả năng tương tác trực tiếp với máy chủ tri thức. Dưới đây là đặc tả chi tiết cho từng khu vực giao diện và các chế độ hoạt động chính:

### 7.1. Giao diện khởi tạo và thiết lập phiên

Đây là giao diện đầu tiên người dùng tiếp cận khi khởi động công cụ. Hệ thống cung cấp cơ chế đăng nhập bảo mật và xác lập kết nối nhị phân. Giao diện bao gồm:

*   **Dòng lệnh chào mừng**: Hiển thị phiên bản hệ thống và trạng thái sẵn sàng của bộ điều phối.
*   **Thanh nhập liệu Login**: Cho phép nhập danh tính và mật khẩu (mật khẩu được mã hóa và ẩn trên màn hình).
*   **Trạng thái kết nối**: Hiển thị địa chỉ IP máy chủ và mã định danh phiên làm việc đã được cấp.

![Giao diện khởi tạo và đăng nhập CLI](../../../assets/diagrams/cli_interface_init.png)
*Hình 4.32: Giao diện khởi tạo và xác lập phiên làm việc trên console.*

### 7.2. Giao diện soạn thảo cấu trúc tri thức

Hỗ trợ chuyên gia tri thức định nghĩa các Khái niệm và Luật dẫn thông qua cơ chế nhập liệu đa dòng. Giao diện bao gồm:

*   **Con trỏ lệnh đa cấp**: Tự động chuyển đổi sang ký hiệu thụt đầu dòng khi phát hiện câu lệnh chưa kết thúc.
*   **Bảng định vị lỗi**: Khi phát sinh lỗi cú pháp, CLI chỉ ra chính xác vị trí dòng/cột kèm theo gợi ý sửa lỗi.
*   **Bộ nhớ lịch sử (History)**: Cho phép truy xuất nhanh các khối luật đã soạn thảo trước đó để tinh chỉnh.

![Giao diện soạn thảo tri thức đa dòng CLI](../../../assets/diagrams/cli_interface_designer.png)
*Hình 4.33: Giao diện soạn thảo và kiểm soát cú pháp tri thức.*

### 7.3. Giao diện truy vấn và kết xuất dữ liệu

Hiển thị kết quả khai thác tri thức dưới các hình thức chuẩn hóa. Giao diện bao gồm:

*   **Chế độ Bảng (Table Mode)**: Tự động căn chỉnh độ rộng cột dựa trên nội dung sự kiện tri thức.
*   **Chế độ Dọc (Vertical Mode)**: Kích hoạt thông qua mã lệnh đặc biệt để xem chi tiết từng thuộc tính trên các nốt tri thức phức hợp.
*   **Thanh trạng thái ResultSet**: Thông báo tổng số bản ghi tìm thấy và thời gian xử lý tại máy chủ.

![Giao diện kết xuất dữ liệu dạng bảng và dọc CLI](../../../assets/diagrams/cli_interface_query.png)
*Hình 4.34: Các chế độ hiển thị kết quả truy vấn tri thức trên console.*

### 7.4. Giao diện truy vết và giải thuật suy luận

Hiển thị quy trình tư duy của hệ thống khi giải quyết một mục tiêu tri thức. Giao diện bao gồm:

*   **Cây truy vết logic (Trace Tree)**: Cấu trúc phân cấp các luật đã kích hoạt để dẫn tới kết luận.
*   **Danh sách sự kiện nguồn**: Hiển thị các dữ kiện cơ bản đã được máy sử dụng làm tiền đề.
*   **Kết luận cuối cùng**: Hiển thị rõ ràng trạng thái mục tiêu (Thành công/Thất bại) và giá trị tìm được.

![Giao diện truy vết suy luận logic CLI](../../../assets/diagrams/cli_interface_solve.png)
*Hình 4.35: Kết quả thực thi solver và trích xuất tiến trình suy luận.*
