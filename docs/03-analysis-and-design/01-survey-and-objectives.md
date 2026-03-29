# 03.1. Khảo sát Hiện trạng & Mục tiêu (Survey & Objectives)

Dự án KBMS (Knowledge Base Management System) được xây dựng nhằm thu hẹp khoảng cách giữa các hệ quản trị CSDL truyền thống và các hệ thống chuyên gia dựa trên tri thức.

## 1. Khảo sát Hiện trạng (Current State Survey)

Dựa trên phân tích so sánh với các hệ thống hiện có, chúng tôi nhận thấy các giới hạn sau:

*   **CSDL Quan hệ (RDBMS):** Rất mạnh về lưu trữ đĩa (Persistence) và giao dịch (ACID) nhưng hoàn toàn thiếu khả năng suy diễn logic tự động [5]. Việc xử lý tri thức phải thực hiện ở tầng ứng dụng, gây khó khăn cho việc bảo trì luật.
*   **Hệ thống Logic (Prolog):** Có khả năng suy diễn cực mạnh nhưng chủ yếu hoạt động trên bộ nhớ RAM, thiếu cơ chế quản lý dữ liệu nhị phân quy mô lớn trên đĩa cứng và khả năng quản lý đa người dùng thực thụ [8].
*   **Công cụ Ontologies (Protégé):** Hỗ trợ mô hình hóa tri thức tốt nhưng hiệu năng truy vấn và khả năng tích hợp vào các hệ thống phần mềm thương mại còn hạn chế [7].

### Bảng so sánh Tính năng Chi tiết

| Đặc tính | CSDL Quan hệ [5] | Ngôn ngữ Logic [8] | Công cụ Tri thức [7] | **KBMS [1]** |
| :--- | :--- | :--- | :--- | :--- |
| **Kiểu dữ liệu** | Structured Tables | Logic Predicates | OWL / RDF | **Knowledge Concepts** |
| **Suy diễn** | Không (Chỉ Join) | Rất mạnh (Backward) | Reasoner Plugin | **Mạnh (Forward)** |
| **Lưu trữ đĩa** | B+ Tree, ACID | Chủ yếu RAM | Files (OWL/XML) | **B+ Tree nhị phân, WAL** |
| **Đa người dùng** | Rất tốt | Không | Hạn chế | **Tốt (Socket/Auth)** |
| **Giao diện** | Workbench / CLI | CLI đơn giản | Desktop phức tạp | **Studio IDE + CLI** |

### Ưu thế vượt trội của KBMS

1.  **Sự kết hợp giữa Persistence và Reasoning**: KBMS tích hợp **Inference Engine** trực tiếp trên tầng **Physical Storage**, cho phép suy diễn trên hàng triệu thực thể được lưu trữ bền vững.
2.  **Giao diện Phát triển (Studio IDE)**: Cung cấp môi trường trực quan hóa đồ thị tri thức và hỗ trợ IntelliSense, giúp rút ngắn thời gian thiết kế bài toán.
3.  **Hiệu năng Truyền tin (Network Layer)**: Sử dụng giao thức nhị phân (Binary Protocol) giúp đạt tốc độ xử lý cao với độ trễ tối thiểu.

**Kết luận:** Cần có một hệ thống kết hợp được cả **Hiệu năng lưu trữ (Indexing/WAL)** và **Khả năng suy diễn (Inference Engine)**.

---

## 2. Mục tiêu Nghiên cứu (Research Objectives)

Dự án KBMS hướng tới việc xây dựng một hệ quản trị tri thức toàn diện với các mục tiêu cụ thể:
1.  **Storage Engine:** Phát triển cấu trúc cây B+ Tree nhị phân và cơ chế ghi nhật ký phục hồi (WAL) để quản lý hàng triệu thực thể tri thức bền vững.
2.  **Reasoning Engine:** Xây dựng bộ máy suy diễn tiến (Forward Chaining) dựa trên điểm đóng F-Closure cho phép tự động tính toán dữ liệu bổ sung.
3.  **Language Compiler:** Thiết kế ngôn ngữ KBQL (Knowledge Base Query Language) và bộ biên dịch (Parser/Lexer) hỗ trợ KDL (Định nghĩa) và KQL (Truy vấn).
4.  **Integrated IDE:** Phát triển môi trường Studio IDE chuyên nghiệp giúp trực quan hóa và thiết kế tri thức dễ dàng.

---

> [!TIP]
> **Giá trị cốt lõi**: "Dữ liệu hóa Tri thức - Thông minh hóa Dữ liệu".
