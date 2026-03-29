# Giới thiệu về Ngôn ngữ Truy vấn KBQL

KBQL (Knowledge Base Query Language) là ngôn ngữ truy vấn chính được sử dụng trong hệ quản trị KBMS. KBQL không chỉ kế thừa các cú pháp SQL tiêu chuẩn để thao tác với dữ liệu mà còn mở rộng các khả năng suy diễn tri thức (Reasoning) dựa trên logic vị từ và tập luật (Rules).

## 1. Triết lý Thiết kế

KBQL được thiết kế dựa trên ba trụ cột chính:
1.  **Sự quen thuộc (Familiarity):** Cú pháp gần gũi với SQL giúp người dùng dễ dàng tiếp cận.
2.  **Tính tri thức (Knowledge-Driven):** Tích hợp sâu các khái niệm về Concept, Fact và Rule thay vì chỉ dừng lại ở Table/Row.
3.  **Tự động suy diễn (Automatic Inference):** Kết quả truy vấn có thể được tự động cập nhật hoặc suy luận thông qua bộ máy Suy diễn (Inference Engine) mà người dùng không cần viết logic thủ công.

## 2. Các Thành phần Chính của KBQL

Hệ thống lệnh của KBQL được chia thành 4 nhóm chính:

| Nhóm Lệnh | Chức năng | Các lệnh tiêu biểu |
| :--- | :--- | :--- |
| **DDL** (Data Definition) | Định nghĩa cấu trúc tri thức | `CREATE KB`, `CREATE CONCEPT`, `CREATE RULE` |
| **DML** (Data Manipulation) | Thao tác trên tập các sự kiện | `INSERT`, `UPDATE`, `DELETE` |
| **DQL** (Data Query) | Truy vấn và yêu cầu suy diễn | `SELECT`, `INFER`, `ASK` |
| **DCL/System** | Quản lý hệ thống và metadata | `USE`, `DESCRIBE`, `EXPLAIN`, `SHOW` |

## 3. Khái niệm Cốt lõi

### Concept (Khái niệm)
Thay vì dùng "Table", KBQL sử dụng **Concept**. Một Concept đại diện cho một thực thể hoặc một lớp đối tượng trong thế giới thực, bao gồm các biến (Variables) định nghĩa thuộc tính của nó.

### Fact (Sự kiện)
Mỗi bản ghi dữ liệu trong một Concept được coi là một **Fact**. Tập hợp các Fact tạo nên cơ sở dữ liệu hiện tại.

### Rule (Luật)
**Rule** định nghĩa mối quan hệ logic giữa các Fact. Khi dữ liệu (Fact) thay đổi, các Rule liên quan có thể được kích hoạt để tạo ra Fact mới hoặc cập nhật Fact hiện có (Forward Chaining).

---

Tiếp theo, chúng ta sẽ đi sâu vào chi tiết các lệnh định nghĩa (DDL).
