# 04. Đặc tả Yêu cầu Hệ thống Master (Master Requirements)

Tài liệu này là bản tổng hợp toàn diện nhất về mọi chức năng và yêu cầu kỹ thuật của KBMS, được đối soát trực tiếp với mã nguồn hệ thống (C# Server, Parser, Studio). Đây là "nền tảng xương sống" cho việc thiết kế và phát triển toàn bộ hệ thống.

---

## 1. Yêu cầu Chức năng theo Tầng Kiến trúc (4 Tiers)

### a. Tầng Ứng dụng (App Layer)

*   **KBMS Studio (IDE Chuyên sâu)**:
    *   **Monaco Engine**: Soạn thảo KBQL với Syntax Highlighting, IntelliSense và Squiggles báo lỗi thời gian thực.
    *   **Knowledge Designer (VGE)**: Trực quan hóa đồ thị tri thức (Graph Nodes & Edges), thiết kế KDL kéo thả.
    *   **Management Dashboard**: Giám sát RAM/CPU/Disk/Network, quản lý User, Session và Log Analyzer.
*   **KBMS CLI (Interactive Tool)**:
    *   **REPL & LineEditor**: Hỗ trợ phím tắt (`Home/End`, `Escx2`), lịch sử lệnh và thụt đầu dòng tự động.
    *   **Response Parser**: Vẽ bảng động, hỗ trợ Multi-line Cell và chế độ hiển thị dọc (`\G`).
    *   **Batch Processing**: Lệnh `SOURCE` thực thi tệp tin kịch bản `.kbql`.

### b. Tầng Mạng (Network Layer - Binary Protocol)

*   **Custom Binary Protocol**: Mã hóa nhị phân tối ưu (1 byte Type, 4 byte Length).
*   **Message Types (12+ loại)**: 
    *   `1-LOGIN / 5-LOGOUT`: Xác thực và kết thúc phiên.
    *   `2-QUERY`: Gửi lệnh thực thi.
    *   `6-METADATA / 7-ROW / 8-FETCH_DONE`: Luồng kết quả streaming.
    *   `4-ERROR`: Báo lỗi line-accurate (Line/Column).
    *   `10-STATS / 11-LOGS_STREAM / 12-SESSIONS`: Dữ liệu quản trị hệ thống.
    *   `13-MANAGEMENT_CMD`: Lệnh điều khiển Server từ xa.

### c. Tầng Máy chủ (Server Layer - Core & Reasoning)

*   **KnowledgeManager**: Điều phối luồng giữa Parser, Engine và Storage.
*   **Reasoning Engine (Bộ máy Suy diễn)**:
    *   **Thuật toán F-Closure**: Suy diễn tiến đến điểm đóng.
    *   **Recursive Sub-closure**: Suy diễn tổ hợp các thành phần con.
    *   **SameVariables Propagation**: Truyền thụ tính chất giữa các biến trùng tên.
*   **Parser & Compiler**:
    *   **Lexer**: Nhận diện 190+ từ khóa.
    *   **Parser**: 50+ loại AST Nodes hỗ trợ KDL, KQL, KML, TCL, KCL.
*   **Security & Auth**: Xác thực Base64, Role-Based Access Control (ROOT, ADMIN, RESEARCHER).
*   **System Services**: `Bootstrapper`, `SystemUpdater`, `V2ToV3Converter`.

### d. Tầng Lưu trữ (Storage Layer - Physical Engine)

*   **Physical Paging**: Trang dữ liệu 8KB cố định.
*   **Indexing**: B+ Tree tối ưu tìm kiếm $O(\log n)$.
*   **WAL (Write-Ahead Logging)**: Nhật ký phục hồi dữ liệu đảm bảo ACID.
*   **Encryption**: Mã hóa tĩnh AES-256 đối với tệp `.dat` và `.kbf`.

---

## 2. Bản đồ Chức năng KBQL (Detailed Registry)

Dưới đây là danh sách đầy đủ các câu lệnh hệ thống hỗ trợ:

### Quản lý Kiểm soát (KCL - Security)
*   `CREATE/ALTER/DROP USER`: Quản lý tài khoản người dùng hệ thống.
*   `GRANT/REVOKE`: Phân quyền truy cập theo từng Cơ sở tri thức (KB).

### Định nghĩa Tri thức (KDL - Design)
*   `CREATE/DROP KNOWLEDGE BASE`: Quản lý thực thể KB.
*   `CREATE/ALTER/DROP CONCEPT`: Quản lý các lớp đối tượng và thuộc tính.
*   `CREATE/DROP RULE & EQUATION`: Định nghĩa luật logic và phương trình toán học.
*   `CREATE FUNCTION/OPERATOR`: Mở rộng khả năng tính toán.
*   `ADD/REMOVE COMPUTATION & HIERARCHY`: Thiết lập quan hệ tính toán và kế thừa.
*   `CREATE INDEX`: Tối ưu hóa hiệu năng truy vấn.

### Truy cập & Suy diễn (KQL/RE - Solve)
*   `SELECT / INSERT / UPDATE / DELETE`: Thao tác dữ liệu tri thức.
*   `SOLVE`: Kích hoạt bộ máy suy diễn tìm lời giải cho mục tiêu.
*   `EXPLAIN / DESCRIBE / SHOW`: Giải thích luồng suy diễn và xem cấu trúc tri thức.

### Bảo trì & Hệ thống (KML - Maintenance)
*   `IMPORT / EXPORT / BULK INSERT`: Di trú dữ liệu quy mô lớn.
*   `REINDEX / CHECKPOINT / VACUUM`: Tối ưu hóa và dọn dẹp lưu trữ vật lý.
*   `MIGRATION V2`: Nâng cấp dữ liệu từ phiên bản cũ.

### Quản lý Giao dịch (TCL - Transaction)
*   `BEGIN / COMMIT / ROLLBACK`: Đảm bảo tính nguyên tố (Atomicity) cho các chuỗi lệnh.

---

## 3. Yêu cầu Phi chức năng (Non-functional)

*   **ACID Compliance**: Đảm bảo an toàn dữ liệu 100% thông qua WAL.
*   **High Performance**: Độ trễ < 10ms trên LAN, hỗ trợ Asynchronous I/O.
*   **Security**: Mã hóa AES-256, Master Key Security.
*   **Scalability**: Stateless socket handling, hỗ trợ hàng trăm kết nối đồng thời.

---

> [!NOTE]
> Tài liệu này đã được đồng bộ hóa với cấu trúc mã nguồn thực tế tại các phân hệ `KBMS.Server`, `KBMS.Parser` và `KBMS.Storage`.
