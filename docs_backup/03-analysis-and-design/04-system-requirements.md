# 03.4. Đặc tả Yêu cầu Hệ thống KBMS

Tài liệu này là bản tổng hợp toàn diện nhất về mọi chức năng và yêu cầu kỹ thuật của [KBMS](../00-glossary/01-glossary.md#kbms), được đối soát trực tiếp với mã nguồn hệ thống (C# Server, [Parser](../00-glossary/01-glossary.md#parser), Studio). Đây là "nền tảng xương sống" cho việc thiết kế và phát triển toàn bộ hệ thống.

---

## 1. Yêu cầu Chức năng

*Bảng 3.2: Đặc tả Yêu cầu Chức năng Hệ thống Master*
| ID | Nhóm Chức năng | Mô tả Chi tiết | Mức ưu tiên |
|----|----------------|----------------|-------------|
| 01 | App Layer | KBMS Studio, CLI, REPL | High |
| 02 | Network Layer | Binary Protocol, Streaming | High |
| 03 | Server Layer | Reasoning Engine, Parser | Critical |
| 04 | Storage Layer | WAL, B+ Tree, Encryption | Critical |

---

## 1. Yêu cầu Chức năng theo Tầng Kiến trúc

### a. Tầng Ứng dụng

*   **KBMS Studio ([IDE](../00-glossary/01-glossary.md#ide) Chuyên sâu)**:
    *   **[Monaco](../00-glossary/01-glossary.md#monaco) Engine**: Soạn thảo [KBQL](../00-glossary/01-glossary.md#kbql) với Syntax Highlighting, [IntelliSense](../00-glossary/01-glossary.md#intellisense) và [Squiggles](../00-glossary/01-glossary.md#squiggles) báo lỗi thời gian thực.
    *   **[Knowledge Designer](../00-glossary/01-glossary.md#knowledge-designer) (VGE)**: Trực quan hóa đồ thị tri thức (Graph Nodes & Edges), thiết kế [KDL](../00-glossary/01-glossary.md#kdl) kéo thả.
    *   **[Management Dashboard](../00-glossary/01-glossary.md#management-dashboard)**: Giám sát RAM/CPU/Disk/Network, quản lý User, Session và [Log Analyzer](../00-glossary/01-glossary.md#log-analyzer).
*   **KBMS [CLI](../00-glossary/01-glossary.md#cli) (Interactive Tool)**:
    *   **[REPL](../00-glossary/01-glossary.md#repl) & [LineEditor](../00-glossary/01-glossary.md#lineeditor)**: Hỗ trợ phím tắt (`Home/End`, `Escx2`), lịch sử lệnh và thụt đầu dòng tự động.
    *   **Response Parser**: Vẽ bảng động, hỗ trợ Multi-line Cell và chế độ hiển thị dọc (`\G`).
    *   **[Batch Processing](../00-glossary/01-glossary.md#batch-processing)**: Lệnh `SOURCE` thực thi tệp tin kịch bản `.kbql`.

### b. Tầng Mạng (Network Layer - Binary Protocol)

*   **Custom [Binary Protocol](../00-glossary/01-glossary.md#binary-protocol)**: Mã hóa nhị phân tối ưu (1 byte Type, 4 byte Length).
*   **Message Types (12+ loại)**: 
    *   `1-LOGIN / 5-LOGOUT`: Xác thực và kết thúc phiên.
    *   `2-QUERY`: Gửi lệnh thực thi.
    *   `6-METADATA / 7-ROW / 8-FETCH_DONE`: Luồng kết quả [streaming](../00-glossary/01-glossary.md#streaming).
    *   `4-ERROR`: Báo lỗi line-accurate (Line/Column).
    *   `10-STATS / 11-LOGS_STREAM / 12-SESSIONS`: Dữ liệu quản trị hệ thống.
    *   `13-MANAGEMENT_CMD`: Lệnh điều khiển Server từ xa.

### c. Tầng Máy chủ

*   **KnowledgeManager**: Điều phối luồng giữa Parser, Engine và Storage.
*   **Reasoning Engine (Bộ máy Suy diễn)**:
    *   **Thuật toán [F-Closure](../00-glossary/01-glossary.md#f-closure)**: Suy diễn tiến đến điểm đóng.
    *   **Recursive Sub-closure**: Suy diễn tổ hợp các thành phần con.
    *   **[SameVariables](../00-glossary/01-glossary.md#samevariables) Propagation**: Truyền thụ tính chất giữa các biến trùng tên.
*   **Parser & Compiler**:
    *   **[Lexer](../00-glossary/01-glossary.md#lexer)**: Nhận diện 190+ từ khóa.
    *   **Parser**: 50+ loại [AST](../00-glossary/01-glossary.md#ast) Nodes hỗ trợ KDL, [KQL](../00-glossary/01-glossary.md#kql), [KML](../00-glossary/01-glossary.md#kml), [TCL](../00-glossary/01-glossary.md#tcl), [KCL](../00-glossary/01-glossary.md#kcl).
*   **Security & Auth**: Xác thực Base64, Role-Based Access Control (ROOT, ADMIN, RESEARCHER).
*   **System Services**: `Bootstrapper`, `SystemUpdater`, `V2ToV3Converter`.

### d. Tầng Lưu trữ

*   **Physical Paging**: Trang dữ liệu 8KB cố định.
*   **Indexing**: [B+ Tree](../00-glossary/01-glossary.md#b-tree) tối ưu tìm kiếm $O(\log n)$.
*   **WAL (Write-Ahead Logging)**: Nhật ký phục hồi dữ liệu đảm bảo [ACID](../00-glossary/01-glossary.md#acid).
*   **Encryption**: Mã hóa tĩnh [AES-256](../00-glossary/01-glossary.md#aes-256) đối với tệp `.dat` và `.kbf`.

---

## 2. Bản đồ Chức năng KBQL

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

## 3. Yêu cầu Phi chức năng

*   **ACID Compliance**: Đảm bảo an toàn dữ liệu 100% thông qua WAL.
*   **High Performance**: Độ trễ < 10ms trên LAN, hỗ trợ [Asynchronous I/O](../00-glossary/01-glossary.md#asynchronous-io).
*   **Security**: Mã hóa AES-256, [Master Key](../00-glossary/01-glossary.md#master-key) Security.
*   **Scalability**: [Stateless](../00-glossary/01-glossary.md#stateless) socket handling, hỗ trợ hàng trăm kết nối đồng thời.
