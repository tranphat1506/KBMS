# 03.1. Khảo sát Hiện trạng & Mục tiêu

Dự án [KBMS](../00-glossary/01-glossary.md#kbms) (Knowledge Base Management System) được xây dựng nhằm thu hẹp khoảng cách giữa các hệ quản trị CSDL truyền thống và các hệ thống chuyên gia dựa trên tri thức.

## 1. Khảo sát Hiện trạng

Dựa trên phân tích so sánh với các hệ thống hiện có, chúng tôi nhận thấy các giới hạn sau:

*   **CSDL Quan hệ ([RDBMS](../00-glossary/01-glossary.md#rdbms)):** Rất mạnh về lưu trữ đĩa (Persistence) và giao dịch ([ACID](../00-glossary/01-glossary.md#acid)) nhưng hoàn toàn thiếu khả năng suy diễn logic tự động [5]. Việc xử lý tri thức phải thực hiện ở tầng ứng dụng, gây khó khăn cho việc bảo trì luật.
*   **Hệ thống Logic (Prolog):** Có khả năng suy diễn cực mạnh nhưng chủ yếu hoạt động trên bộ nhớ RAM, thiếu cơ chế quản lý dữ liệu nhị phân quy mô lớn trên đĩa cứng và khả năng quản lý đa người dùng thực thụ [8].
*   **Công cụ Ontologies (Protégé):** Hỗ trợ mô hình hóa tri thức tốt nhưng hiệu năng truy vấn và khả năng tích hợp vào các hệ thống phần mềm thương mại còn hạn chế [7].

### Bảng so sánh Tính năng Chi tiết

*Bảng 3.1: So sánh Tính năng Chi tiết giữa [KBMS](../00-glossary/01-glossary.md#kbms) và các hệ thống khác*
| Đặc tính | CSDL Quan hệ [5] | Ngôn ngữ Logic [8] | Công cụ Tri thức [7] | **[KBMS](../00-glossary/01-glossary.md#kbms) [1]** |
| :--- | :--- | :--- | :--- | :--- |
| **Kiểu dữ liệu** | Structured Tables | Logic Predicates | OWL / RDF | **Knowledge Concepts** |
| **Suy diễn** | Không (Chỉ Join) | Rất mạnh (Backward) | Reasoner Plugin | **Mạnh (Forward)** |
| **Lưu trữ đĩa** | [B+ Tree](../00-glossary/01-glossary.md#b-tree), [ACID](../00-glossary/01-glossary.md#acid) | Chủ yếu RAM | Files (OWL/XML) | **[B+ Tree](../00-glossary/01-glossary.md#b-tree) nhị phân, WAL** |
| **Đa người dùng** | Rất tốt | Không | Hạn chế | **Tốt (Socket/Auth)** |
| **Giao diện** | Workbench / [CLI](../00-glossary/01-glossary.md#cli) | [CLI](../00-glossary/01-glossary.md#cli) đơn giản | Desktop phức tạp | **Studio [IDE](../00-glossary/01-glossary.md#ide) + [CLI](../00-glossary/01-glossary.md#cli)** |

### Ưu thế vượt trội của [KBMS]

1.  **Sự kết hợp giữa Persistence và Reasoning**: [KBMS](../00-glossary/01-glossary.md#kbms) tích hợp **[Inference Engine](../00-glossary/01-glossary.md#inference-engine)** trực tiếp trên tầng **Physical Storage**, cho phép suy diễn trên hàng triệu thực thể được lưu trữ bền vững.
2.  **Giao diện Phát triển (Studio [IDE](../00-glossary/01-glossary.md#ide))**: Cung cấp môi trường trực quan hóa đồ thị tri thức và hỗ trợ [IntelliSense](../00-glossary/01-glossary.md#intellisense), giúp rút ngắn thời gian thiết kế bài toán.
3.  **Hiệu năng Truyền tin (Network Layer)**: Sử dụng giao thức nhị phân ([Binary Protocol](../00-glossary/01-glossary.md#binary-protocol)) giúp đạt tốc độ xử lý cao với độ trễ tối thiểu.

**Kết luận:** Cần có một hệ thống kết hợp được cả **Hiệu năng lưu trữ (Indexing/WAL)** và **Khả năng suy diễn ([Inference Engine](../00-glossary/01-glossary.md#inference-engine))**.

---

## 2. Mục tiêu Nghiên cứu

Dự án [KBMS](../00-glossary/01-glossary.md#kbms) hướng tới việc xây dựng một hệ quản trị tri thức toàn diện với các mục tiêu cụ thể:
1.  **Storage Engine:** Phát triển cấu trúc cây [B+ Tree](../00-glossary/01-glossary.md#b-tree) nhị phân và cơ chế ghi nhật ký phục hồi (WAL) để quản lý hàng triệu thực thể tri thức bền vững.
2.  **Reasoning Engine:** Xây dựng bộ máy suy diễn tiến ([Forward Chaining](../00-glossary/01-glossary.md#forward-chaining)) dựa trên điểm đóng [F-Closure](../00-glossary/01-glossary.md#f-closure) cho phép tự động tính toán dữ liệu bổ sung.
3.  **Language Compiler:** Thiết kế ngôn ngữ [KBQL](../00-glossary/01-glossary.md#kbql) (Knowledge Base Query Language) và bộ biên dịch ([Parser](../00-glossary/01-glossary.md#parser)/[Lexer](../00-glossary/01-glossary.md#lexer)) hỗ trợ [KDL](../00-glossary/01-glossary.md#kdl) (Định nghĩa) và [KQL](../00-glossary/01-glossary.md#kql) (Truy vấn).
4.  **Integrated [IDE](../00-glossary/01-glossary.md#ide):** Phát triển môi trường Studio [IDE](../00-glossary/01-glossary.md#ide) chuyên nghiệp giúp trực quan hóa và thiết kế tri thức dễ dàng.