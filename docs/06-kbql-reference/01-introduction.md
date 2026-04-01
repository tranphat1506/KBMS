# 06.1. Giới thiệu về Ngôn ngữ Truy vấn

[KBQL](../00-glossary/01-glossary.md#kbql) (Knowledge Base Query Language) là ngôn ngữ truy vấn chính được sử dụng trong hệ quản trị [KBMS](../00-glossary/01-glossary.md#kbms). [KBQL](../00-glossary/01-glossary.md#kbql) không chỉ kế thừa các cú pháp SQL tiêu chuẩn để thao tác với dữ liệu mà còn mở rộng các khả năng suy diễn tri thức (Reasoning) dựa trên logic vị từ và tập luật (Rules).

## 1. Triết lý Thiết kế

[KBQL](../00-glossary/01-glossary.md#kbql) được thiết kế dựa trên ba trụ cột chính:
1.  **Sự quen thuộc (Familiarity):** Cú pháp gần gũi với SQL giúp người dùng dễ dàng tiếp cận.
2.  **Tính tri thức (Knowledge-Driven):** Tích hợp sâu các khái niệm về [Concept](../00-glossary/01-glossary.md#concept), [Fact](../00-glossary/01-glossary.md#fact) và [Rule](../00-glossary/01-glossary.md#rule) thay vì chỉ dừng lại ở Table/Row.
3.  **Tự động suy diễn (Automatic Inference):** Kết quả truy vấn có thể được tự động cập nhật hoặc suy luận thông qua bộ máy Suy diễn ([Inference Engine](../00-glossary/01-glossary.md#inference-engine)) mà người dùng không cần viết logic thủ công.

## 2. Các Thành phần Chính của

*Bảng 6.1: Phân loại Từ khóa dành riêng (Reserved Keywords)*
| Nhóm Lệnh | Chức năng | Các lệnh tiêu biểu |
| :--- | :--- | :--- |
| **[KDL](../00-glossary/01-glossary.md#kdl)** (Knowledge Definition Language) | Định nghĩa cấu trúc tri thức, luật, phân cấp | `CREATE KB`, `CONCEPT`, `RULE`, `HIERARCHY`, `RELATION` |
| **[KML](../00-glossary/01-glossary.md#kml)** (Knowledge Maintenance Language) | Thao tác trên tập các sự kiện (Facts) | `INSERT`, `UPDATE`, `DELETE`, `IMPORT`, `EXPORT` |
| **[KQL](../00-glossary/01-glossary.md#kql)** (Knowledge Query Language) | Truy vấn và yêu cầu suy diễn | `SELECT`, `SOLVE`, `SHOW`, `EXPLAIN`, `DESCRIBE` |
| **[KCL](../00-glossary/01-glossary.md#kcl)** (Knowledge Control Language) | Quản lý người dùng và quyền truy cập | `GRANT`, `REVOKE`, `CREATE/ALTER/DROP USER` |
| **[TCL](../00-glossary/01-glossary.md#tcl)** (Transaction Control Language) | Quản lý giao dịch và tính toàn vẹn | `BEGIN`, `COMMIT`, `ROLLBACK` |
| **Admin** (Maintenance) | Bảo trì và tối ưu hóa hệ thống | `MAINTENANCE (VACUUM, REINDEX, CHECK)` |

## 3. Hỗ trợ Chỉnh sửa

Không phải tất cả các đối tượng trong [KBMS](../00-glossary/01-glossary.md#kbms) đều hỗ trợ lệnh `ALTER`. Dưới đây là bảng tra cứu:

*Bảng 6.2: Khả năng hỗ trợ lệnh ALTER cho từng đối tượng [KBQL](../00-glossary/01-glossary.md#kbql)*
| Đối tượng | Hỗ trợ ALTER | Ghi chú |
| :--- | :--- | :--- |
| **[Concept](../00-glossary/01-glossary.md#concept)** | ✅ Có | Hỗ trợ thêm/xóa biến, luật, ràng buộc, quan hệ nội bộ. |
| **Knowledge Base** | ✅ Có | Hỗ trợ thay đổi mô tả (Description). |
| **User** | ✅ Có | Hỗ trợ đổi mật khẩu và vai trò quản trị. |
| **Relation** | ❌ Không | Cần `DROP` và `CREATE` lại. |
| **Hierarchy** | ❌ Không | Sử dụng `ADD/REMOVE HIERARCHY`. |
| **[Rule](../00-glossary/01-glossary.md#rule)** (Toàn cục) | ❌ Không | Cần `DROP` và `CREATE` lại (Khác với [Rule](../00-glossary/01-glossary.md#rule) nội bộ [Concept](../00-glossary/01-glossary.md#concept)). |
| **Operator/Function**| ❌ Không | Cần `DROP` và `CREATE` lại. |

## 4. Kiểu Dữ liệu Hỗ trợ

[KBQL](../00-glossary/01-glossary.md#kbql) cung cấp bộ kiểu dữ liệu phong phú để định nghĩa [Concept](../00-glossary/01-glossary.md#concept):

*Bảng 6.3: Phân loại Kiểu dữ liệu nguyên thuỷ trong [KBQL](../00-glossary/01-glossary.md#kbql)*
| Nhóm | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| **Số học** | `INT`, `BIGINT`, `DECIMAL`, `FLOAT`, `DOUBLE` | Các kiểu số nguyên và số thực. |
| **Chuỗi** | `VARCHAR(n)`, `CHAR(n)`, `TEXT`, `STRING` | Lưu trữ văn bản (hỗ trợ độ dài tùy chỉnh). |
| **Logic** | `BOOLEAN` | Giá trị `true` hoặc `false`. |
| **Thời gian** | `DATE`, `DATETIME`, `TIMESTAMP` | Quản lý thời gian và sự kiện. |
| **Tri thức** | `OBJECT`, `<ConceptName>` | Tham chiếu đến một đối tượng hoặc một Khái niệm khác. |
| **Đặc biệt** | `NULL` | Trạng thái rỗng. |

## 4. Khái niệm Cốt lõi

### Concept
Thay vì dùng "Table", [KBQL](../00-glossary/01-glossary.md#kbql) sử dụng **[Concept](../00-glossary/01-glossary.md#concept)**. Một [Concept](../00-glossary/01-glossary.md#concept) đại diện cho một thực thể hoặc một lớp đối tượng trong thế giới thực, bao gồm các biến (Variables) định nghĩa thuộc tính của nó.

### Fact
Mỗi bản ghi dữ liệu trong một [Concept](../00-glossary/01-glossary.md#concept) được coi là một **[Fact](../00-glossary/01-glossary.md#fact)**. Tập hợp các [Fact](../00-glossary/01-glossary.md#fact) tạo nên cơ sở dữ liệu hiện tại.

### Rule
**[Rule](../00-glossary/01-glossary.md#rule)** định nghĩa mối quan hệ logic giữa các [Fact](../00-glossary/01-glossary.md#fact). Khi dữ liệu ([Fact](../00-glossary/01-glossary.md#fact)) thay đổi, các [Rule](../00-glossary/01-glossary.md#rule) liên quan có thể được kích hoạt để tạo ra [Fact](../00-glossary/01-glossary.md#fact) mới hoặc cập nhật [Fact](../00-glossary/01-glossary.md#fact) hiện có ([Forward Chaining](../00-glossary/01-glossary.md#forward-chaining)).

---

Tiếp theo, chúng ta sẽ đi sâu vào chi tiết các lệnh định nghĩa ([DDL](../00-glossary/01-glossary.md#ddl)).
