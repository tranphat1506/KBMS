# 06.1. Giới thiệu về Ngôn ngữ Truy vấn KBQL

KBQL (Knowledge Base Query Language) là ngôn ngữ truy vấn chính được sử dụng trong hệ quản trị KBMS. KBQL không chỉ kế thừa các cú pháp SQL tiêu chuẩn để thao tác với dữ liệu mà còn mở rộng các khả năng suy diễn tri thức (Reasoning) dựa trên logic vị từ và tập luật (Rules).

## 1. Triết lý Thiết kế

KBQL được thiết kế dựa trên ba trụ cột chính:
1.  **Sự quen thuộc (Familiarity):** Cú pháp gần gũi với SQL giúp người dùng dễ dàng tiếp cận.
2.  **Tính tri thức (Knowledge-Driven):** Tích hợp sâu các khái niệm về Concept, Fact và Rule thay vì chỉ dừng lại ở Table/Row.
3.  **Tự động suy diễn (Automatic Inference):** Kết quả truy vấn có thể được tự động cập nhật hoặc suy luận thông qua bộ máy Suy diễn (Inference Engine) mà người dùng không cần viết logic thủ công.

## 2. Các Thành phần Chính của KBQL

*Bảng 6.1: Phân loại Từ khóa dành riêng (Reserved Keywords)*
| Nhóm Lệnh | Chức năng | Các lệnh tiêu biểu |
| :--- | :--- | :--- |
| **KDL** (Knowledge Definition) | Định nghĩa cấu trúc tri thức, luật, phân cấp | `CREATE KB`, `CONCEPT`, `RULE`, `HIERARCHY`, `RELATION` |
| **KML** (Knowledge Manipulation) | Thao tác trên tập các sự kiện (Facts) | `INSERT`, `UPDATE`, `DELETE`, `IMPORT`, `EXPORT` |
| **KQL** (Knowledge Query) | Truy vấn và yêu cầu suy diễn | `SELECT`, `SOLVE`, `SHOW`, `EXPLAIN`, `DESCRIBE` |
| **KCL** (Knowledge Control) | Quản lý người dùng và quyền truy cập | `GRANT`, `REVOKE`, `CREATE/ALTER/DROP USER` |
| **TCL** (Transaction Control) | Quản lý giao dịch và tính toàn vẹn | `BEGIN`, `COMMIT`, `ROLLBACK` |
| **Admin** (Maintenance) | Bảo trì và tối ưu hóa hệ thống | `MAINTENANCE (VACUUM, REINDEX, CHECK)` |

## 3. Hỗ trợ Chỉnh sửa (ALTER Support)

Không phải tất cả các đối tượng trong KBMS đều hỗ trợ lệnh `ALTER`. Dưới đây là bảng tra cứu:

*Bảng 6.2: Khả năng hỗ trợ lệnh ALTER cho từng đối tượng KBQL*
| Đối tượng | Hỗ trợ ALTER | Ghi chú |
| :--- | :--- | :--- |
| **Concept** | ✅ Có | Hỗ trợ thêm/xóa biến, luật, ràng buộc, quan hệ nội bộ. |
| **Knowledge Base** | ✅ Có | Hỗ trợ thay đổi mô tả (Description). |
| **User** | ✅ Có | Hỗ trợ đổi mật khẩu và vai trò quản trị. |
| **Relation** | ❌ Không | Cần `DROP` và `CREATE` lại. |
| **Hierarchy** | ❌ Không | Sử dụng `ADD/REMOVE HIERARCHY`. |
| **Rule** (Toàn cục) | ❌ Không | Cần `DROP` và `CREATE` lại (Khác với Rule nội bộ Concept). |
| **Operator/Function**| ❌ Không | Cần `DROP` và `CREATE` lại. |

## 4. Kiểu Dữ liệu Hỗ trợ (Data Types)

KBQL cung cấp bộ kiểu dữ liệu phong phú để định nghĩa Concept:

*Bảng 6.3: Phân loại Kiểu dữ liệu nguyên thuỷ trong KBQL*
| Nhóm | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| **Số học** | `INT`, `BIGINT`, `DECIMAL`, `FLOAT`, `DOUBLE` | Các kiểu số nguyên và số thực. |
| **Chuỗi** | `VARCHAR(n)`, `CHAR(n)`, `TEXT`, `STRING` | Lưu trữ văn bản (hỗ trợ độ dài tùy chỉnh). |
| **Logic** | `BOOLEAN` | Giá trị `true` hoặc `false`. |
| **Thời gian** | `DATE`, `DATETIME`, `TIMESTAMP` | Quản lý thời gian và sự kiện. |
| **Tri thức** | `OBJECT`, `<ConceptName>` | Tham chiếu đến một đối tượng hoặc một Khái niệm khác. |
| **Đặc biệt** | `NULL` | Trạng thái rỗng. |

## 4. Khái niệm Cốt lõi

### Concept (Khái niệm)
Thay vì dùng "Table", KBQL sử dụng **Concept**. Một Concept đại diện cho một thực thể hoặc một lớp đối tượng trong thế giới thực, bao gồm các biến (Variables) định nghĩa thuộc tính của nó.

### Fact (Sự kiện)
Mỗi bản ghi dữ liệu trong một Concept được coi là một **Fact**. Tập hợp các Fact tạo nên cơ sở dữ liệu hiện tại.

### Rule (Luật)
**Rule** định nghĩa mối quan hệ logic giữa các Fact. Khi dữ liệu (Fact) thay đổi, các Rule liên quan có thể được kích hoạt để tạo ra Fact mới hoặc cập nhật Fact hiện có (Forward Chaining).

---

Tiếp theo, chúng ta sẽ đi sâu vào chi tiết các lệnh định nghĩa (DDL).
