# Giới thiệu về Ngôn ngữ Truy vấn Tri thức

Ngôn ngữ **KBQL (Knowledge Base Query Language)** là phương thức giao tiếp chính để tương tác với hệ quản trị cơ sở tri thức KBMS. KBQL được thiết kế dựa trên sự kế thừa các cú pháp tiêu chuẩn của SQL trong thao tác dữ liệu, đồng thời mở rộng các khả năng suy diễn tri thức dựa trên hệ thống logic vị từ và tập luật [6].

## 1. Triết lý Thiết kế Hệ thống

Ngôn ngữ KBQL được xây dựng dựa trên ba nguyên tắc cốt lõi:
1.  **Tính kế thừa:** Cú pháp tiệm cận với tiêu chuẩn SQL giúp tối ưu hóa tiến trình tiếp cận hệ thống của người dùng.
2.  **Định hướng Tri thức:** Tích hợp sâu các thực thể hình thức như Concept, Fact và Rule, vượt xa giới hạn của mô hình Bảng - Bản ghi truyền thống.
3.  **Tự động Suy diễn:** Kết quả truy vấn có khả năng tự cập nhật và suy luận thông qua bộ máy suy diễn (Inference Engine) tích hợp, giảm thiểu việc triển khai logic thủ công tại lớp ứng dụng.

## 2. Các Phân hệ Thành phần của Ngôn ngữ

*Bảng 4.3: Phân loại nhóm lệnh và từ khóa dành riêng trong ngôn ngữ KBQL*
| Nhóm Lệnh | Chức năng | Các lệnh tiêu biểu |
| :--- | :--- | :--- |
| **KDL** (Knowledge Definition Language) | Định nghĩa cấu trúc tri thức, luật, phân cấp | `CREATE KB`, `CONCEPT`, `RULE`, `HIERARCHY`, `RELATION` |
| **KML** (Knowledge Maintenance Language) | Thao tác trên tập các sự kiện (Facts) | `INSERT`, `UPDATE`, `DELETE`, `IMPORT`, `EXPORT` |
| **KQL** (Knowledge Query Language) | Truy vấn và yêu cầu suy diễn | `SELECT` (với macro `SOLVE()`), `SHOW`, `EXPLAIN`, `DESCRIBE` |
| **KCL** (Knowledge Control Language) | Quản lý người dùng và quyền truy cập | `GRANT`, `REVOKE`, `CREATE/ALTER/DROP USER` |
| **TCL** (Transaction Control Language) | Quản lý giao dịch và tính toàn vẹn | `BEGIN`, `COMMIT`, `ROLLBACK` |
| **Admin** (Maintenance) | Bảo trì và tối ưu hóa hệ thống | `MAINTENANCE (VACUUM, REINDEX, CHECK)` |

## 3. Khả năng Hiệu chỉnh Cấu trúc Tri thức

Trong hệ thống KBMS, các đối tượng mang tính cấu trúc logic được quản lý chặt chẽ thông qua lệnh hiệu chỉnh `ALTER`. Dưới đây là đặc tả khả năng hỗ trợ sửa đổi của các thực thể tri thức:

*Bảng 4.4: Đặc tả khả năng hỗ trợ sửa đổi (ALTER) cho các thực thể tri thức*
| Đối tượng | Hỗ trợ ALTER | Ghi chú |
| :--- | :--- | :--- |
| **Concept** | ✅ Có | Hỗ trợ thêm/xóa biến, luật, ràng buộc, quan hệ nội bộ. |
| **Knowledge Base** | ✅ Có | Hỗ trợ thay đổi mô tả (Description). |
| **User** | ✅ Có | Hỗ trợ đổi mật khẩu và vai trò quản trị. |
| **Relation** | ❌ Không | Cần `DROP` và `CREATE` lại. |
| **Hierarchy** | ❌ Không | Sử dụng `ADD/REMOVE HIERARCHY`. |
| **Rule** (Toàn cục) | ❌ Không | Cần `DROP` và `CREATE` lại (Khác với Rule nội bộ Concept). |
| **Operator/Function**| ❌ Không | Cần `DROP` và `CREATE` lại. |

## 4. Hệ thống Kiểu Dữ liệu Đặc tả

KBQL cung cấp hệ thống kiểu dữ liệu đa dạng để phục vụ việc định nghĩa cấu trúc khái niệm:

*Bảng 4.5: Danh mục các kiểu dữ liệu nguyên thủy được hỗ trợ trong KBQL*
| Nhóm | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| **Số học** | `INT`, `BIGINT`, `DECIMAL`, `FLOAT`, `DOUBLE` | Các kiểu số nguyên và số thực. |
| **Chuỗi** | `VARCHAR(n)`, `CHAR(n)`, `TEXT`, `STRING` | Lưu trữ văn bản (hỗ trợ độ dài tùy chỉnh). |
| **Logic** | `BOOLEAN` | Giá trị `true` hoặc `false`. |
| **Thời gian** | `DATE`, `DATETIME`, `TIMESTAMP` | Quản lý thời gian và sự kiện. |
| **Tri thức** | `OBJECT`, `<ConceptName>` | Tham chiếu đến một đối tượng hoặc một Khái niệm khác. |
| **Đặc biệt** | `NULL` | Trạng thái rỗng. |

## 5. Ví dụ Quickstart - Hệ Tri thức Hình học

Để minh họa sức mạnh của KBQL, dưới đây là một ví dụ hoàn chỉnh về xây dựng hệ tri thức hình học:

```kbql
-- Bước 1: Tạo cơ sở tri thức
CREATE KNOWLEDGE BASE GeometryDB
DESCRIPTION "Hệ tri thức Hình học Phẳng";

-- Bước 2: Định nghĩa khái niệm Điểm
CREATE CONCEPT Point (
    VARIABLES (x: DECIMAL, y: DECIMAL, name: STRING),
    CONSTRAINTS (x IS NOT NULL AND y IS NOT NULL)
);

-- Bước 3: Định nghĩa khái niệm Đoạn thẳng từ 2 điểm
CREATE CONCEPT LineSegment (
    VARIABLES (p1: Point, p2: Point, length: DECIMAL),
    EQUATIONS ('length = Sqrt((p2.x - p1.x)^2 + (p2.y - p1.y)^2)')
);

-- Bước 4: Định nghĩa khái niệm Tam giác
CREATE CONCEPT Triangle (
    VARIABLES (a: LineSegment, b: LineSegment, c: LineSegment,
               area: DECIMAL, perimeter: DECIMAL),
    EQUATIONS (
        'perimeter = a.length + b.length + c.length',
        'area = Sqrt(perimeter/2 * (perimeter/2 - a.length) *
                     (perimeter/2 - b.length) * (perimeter/2 - c.length))'
    ),
    CONSTRAINTS (a.length + b.length > c.length)
);

-- Bước 5: Thêm sự kiện (Facts)
INSERT INTO Point ATTRIBUTE (0, 0, 'O');
INSERT INTO Point ATTRIBUTE (3, 0, 'A');
INSERT INTO Point ATTRIBUTE (0, 4, 'B');

-- Bước 6: Truy vấn với suy diễn tự động
SELECT SOLVE(length) FROM LineSegment
WHERE p1.name = 'O' AND p2.name = 'A';
-- Kết quả: length = 3.0

-- Bước 7: Suy diễn phức tạp - Tính diện tích tam giác
SELECT SOLVE(area), SOLVE(perimeter) FROM Triangle
WHERE a.p1.name = 'O' AND a.p2.name = 'A'
  AND b.p1.name = 'O' AND b.p2.name = 'B';
-- Kết quả: area = 6.0, perimeter = 12.0
```

### 5.1. So sánh với SQL Truyền thống

| Đặc điểm | SQL Truyền thống | KBQL |
|:---|:---|:---|
| **Lưu trữ** | Bảng hàng cột | Khái niệm (Concept) với luật nội tại |
| **Truy vấn** | Chỉ trả về dữ liệu đã có | Tự động suy diễn dữ liệu mới |
| **Tính toán** | Cần ứng dụng xử lý | Tích hợp SOLVE() giải phương trình |
| **Quan hệ** | JOIN bảng | Kế thừa (IS_A) và Thành phần (PART_OF) |

---
