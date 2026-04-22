# 03.1. Khảo sát Hiện trạng & Mục tiêu

Dự án KBMS (Knowledge Base Management System) được xây dựng nhằm thu hẹp khoảng cách giữa các hệ quản trị CSDL truyền thống và các hệ thống chuyên gia dựa trên tri thức.

## 1. Khảo sát Hiện trạng

Dựa trên phân tích so sánh với các hệ thống hiện có, chúng tôi nhận thấy các giới hạn sau:

### 2.3.2 Jess (Java Expert System Shell)
Jess [22] là phiên bản Java của CLIPS. Dự án đã ngừng phát triển tích cực và không còn được cập nhật cho các nền tảng hiện đại.

### 2.3.3 Drools (JBoss Rules Engine)
Drools [23] sử dụng giải thuật Rete cải tiến (Phreak), được ứng dụng rộng rãi trong quản lý quy tắc kinh doanh (Business Rule Management). Drools được thiết kế chủ yếu cho business logic, không tối ưu cho suy luận toán học.

### 2.3.4 SWI-Prolog
SWI-Prolog [24] hỗ trợ mạnh suy luận lùi (Backward Chaining) thông qua cơ chế unification. Nhược điểm là thiếu kiến trúc Client-Server và thiếu lưu trữ vĩnh cửu.

### 2.3.5 Protégé
Protégé [25] là công cụ của Đại học Stanford dùng để chỉnh sửa Ontology theo chuẩn OWL. Protégé chủ yếu là trình soạn thảo, không phải hệ quản trị tri thức hoàn chỉnh.

### 2.3.6 Cyc
Cyc [40] là dự án AI tham vọng nhất trong lịch sử, khởi động từ năm 1984 bởi Douglas Lenat nhằm mã hóa toàn bộ tri thức thường thức (common sense). Cyc sử dụng ngôn ngữ CycL dựa trên logic vị từ bậc nhất. Mặc dù quy mô tri thức rất lớn (hơn 1.5 triệu assertions), Cyc thiếu khả năng tính toán định lượng trên đối tượng và không được thiết kế cho các bài toán kỹ thuật có tính toán phức tạp.

### 2.3.7 Bảng Tổng hợp So sánh Các Hệ thống Quản trị Tri thức

| Tiêu chí | CLIPS [21] | Jess [22] | Drools [23] | SWI-Prolog [24] | Protégé [25] | Cyc [40] | **KBMS COKB** |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Suy luận tiến** | Có | Có | Có | Không | Hạn chế | Có | **Có** |
| **Suy luận lùi** | Không | Không | Không | Có | Hạn chế | Có | **Có** |
| **Tính toán số học** | Hạn chế | Hạn chế | Hạn chế | Hạn chế | Không | Không | **Mạnh** |
| **Lưu trữ** | Không | Không | DBMS ngoài | Không | File OWL | Có | **B+ Tree + WAL** |
| **Mã hóa dữ liệu** | Không | Không | Không | Không | Không | Không | **AES-256** |
| **Client-Server** | Không | Không | Có | Không | Không | Có | **Có (TCP)** |
| **Ngôn ngữ truy vấn** | CLIPS DSL | Jess DSL | DRL | Prolog | SPARQL | CycL | **KBQL** |


### Ưu thế vượt trội của KBMS

1.  **Sự kết hợp giữa Persistence và Reasoning**: KBMS tích hợp **Rete Network** trực tiếp trên tầng **Physical Storage**, cho phép suy diễn thời gian thực trên hàng triệu thực thể được lưu trữ bền vững thông qua cơ chế lan truyền dữ kiện gia tăng.
2.  **Giao diện Phát triển (Studio IDE)**: Cung cấp môi trường trực quan hóa đồ thị tri thức và hỗ trợ IntelliSense, giúp rút ngắn thời gian thiết kế bài toán.
3.  **Hiệu năng Truyền tin (Network Layer)**: Sử dụng giao thức nhị phân (Binary Protocol) giúp đạt tốc độ xử lý cao với độ trễ tối thiểu.

**Kết luận:** Cần có một hệ thống kết hợp được cả **Hiệu năng lưu trữ (Indexing/WAL)** và **Khả năng suy diễn (Inference Engine) dựa trên mô hình sự kiện**.

---

## 2. Mục tiêu Nghiên cứu

Dự án KBMS hướng tới việc xây dựng một hệ quản trị tri thức toàn diện với các mục tiêu cụ thể:
1.  **Storage Engine:** Phát triển cấu trúc cây B+ Tree nhị phân và cơ chế ghi nhật ký phục hồi (WAL) để quản lý hàng triệu thực thể tri thức bền vững [5], [10].
2.  **Reasoning Engine:** Xây dựng bộ máy suy diễn tiến (Forward Chaining) tiên tiến dựa trên mạng lưới Rete và thuật toán bao đóng F-Closure, đảm bảo tốc độ phản hồi tối ưu thông qua so khớp luật phi tuần tự [1], [6], [9].
3.  **Language Compiler:** Thiết kế ngôn ngữ KBQL (Knowledge Base Query Language) và bộ biên dịch (Parser/Lexer) hỗ trợ KDL (Định nghĩa) và KQL (Truy vấn) [6].
4.  **Integrated IDE:** Phát triển môi trường Studio IDE chuyên nghiệp giúp trực quan hóa và thiết kế tri thức dễ dàng.