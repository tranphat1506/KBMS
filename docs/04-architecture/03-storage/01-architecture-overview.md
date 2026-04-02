# 4.3.1 Tổng quan Kiến trúc và Luồng dữ liệu (Architecture Overview)

Kiến trúc lưu trữ của hệ thống KBMS [K00] phiên bản V3 được thiết kế dựa trên mô hình phân trang (Page-based storage model). Đây là sự thay thế cho mô hình hướng đối tượng (Object-based) nhằm tối ưu hóa hiệu suất xử lý và khả năng mở rộng quy mô dữ liệu. Tầng lưu trữ chịu trách nhiệm thực thi các cơ chế duy trì tính bền vững, quản lý vòng đời của các thực thể tri thức từ trạng thái xử lý trong bộ nhớ tạm thời (RAM) đến trạng thái lưu trữ vĩnh viễn trên thiết bị vật lý dưới định dạng nhị phân đã mã hóa.

## 4.3.1.1 Mô hình Phân tầng Chức năng

Hệ thống lưu trữ được tổ chức theo cấu trúc phân tầng (Layered Architecture) nhằm đảm bảo sự độc lập giữa logic quản lý tri thức và các đặc tính kĩ thuật của lớp lưu trữ vật lý. Sơ đồ dưới đây mô tả cấu trúc phân tầng và sự tương tác giữa các thành phần trong hệ thống:

![storage_overview_v3.png | width=0.7](../../assets/diagrams/storage_overview_v3.png)
*Hình 4.11: Mô hình phân tầng từ lớp Ứng dụng đến lớp Lưu trữ vật lý của hệ thống KBMS.*

1.  **Application Layer**: Cung cấp giao diện tương tác và tiếp nhận các yêu cầu truy vấn ngôn ngữ KBQL [K01] từ phía người dùng.
2.  **Server Layer**: Chịu trách nhiệm thực hiện các quy trình phân tích cú pháp, xác thực dữ liệu và điều phối các thuật toán suy diễn tri thức.
3.  **Storage Layer (V3)**: Thực hiện chức năng điều phối tài nguyên thông qua bộ quản lý bộ nhớ đệm (Buffer Pool) và đảm bảo tính toàn vẹn dữ liệu bằng giao thức Write-Ahead Logging (WAL).
4.  **Hardware Abstraction Layer (HAL)**: Thực hiện các thao tác nhập/xuất (I/O) trực tiếp với hệ điều hành thông qua bộ quản lý thiết bị lưu trữ (`DiskManager`) ở cấp độ khối (Block).

## 4.3.1.2 Nguyên lý Thiết kế và Các Yêu cầu Kĩ thuật

Việc triển khai hệ thống quản trị tri thức yêu cầu giải quyết các vấn đề kĩ thuật liên quan đến quản lý dữ liệu hiệu năng cao:

1.  **Khả năng Mở rộng (Scalability)**: Hệ thống sử dụng kiến trúc phân trang để nạp dữ liệu theo yêu cầu (On-demand loading), hỗ trợ xử lý các cơ sở tri thức có dung lượng lớn hơn bộ nhớ vật lý khả dụng của máy chủ.
2.  **Tính Bền vững và Nhất quán (Durability & Consistency)**: Hệ thống thực thi giao thức Write-Ahead Logging (WAL) để phòng ngừa rủi ro mất dữ liệu trong trường hợp xảy ra sự cố ngừng hoạt động đột ngột. Giao thức này đảm bảo mọi giao dịch đã được xác nhận (Commit) được lưu trữ vĩnh viễn trước khi áp dụng thay đổi vào tệp tin cơ sở dữ liệu chính.
3.  **Hiệu năng Truy xuất (Performance)**: Thông qua việc ứng dụng cấu trúc chỉ mục B+ Tree và tổ chức dữ liệu Slotted Page, hệ thống đạt được độ phức tạp thời gian truy xuất bản ghi ở mức $O(\log n)$ và hỗ trợ truy cập ngẫu nhiên trang với độ phức tạp $O(1)$.

## 4.3.1.3 Cấu trúc Chương

Các thành phần kĩ thuật của tầng lưu trữ được trình bày chi tiết theo các nội dung sau:
-   **Mục 4.3.2**: Phân hệ quản lý trang và điều phối bộ nhớ đệm.
-   **Mục 4.3.3**: Đặc tả cấu trúc tổ chức dữ liệu Slotted Page.
-   **Mục 4.3.4**: Hệ thống chỉ mục B+ Tree và quy trình truy xuất.
-   **Mục 4.3.5**: Cơ chế đảm bảo tính bền vững và giao thức WAL.
-   **Mục 4.3.6**: Quy trình tuần tự hóa dữ liệu tri thức sang định dạng nhị phân.
