# KBMS (Knowledge Base Management System)

![KBMS Architecture](https://img.shields.io/badge/Architecture-4--Tier-blue)
![C#](https://img.shields.io/badge/Language-C%23_.NET_8-green)
![License](https://img.shields.io/badge/License-MIT-purple)

KBMS là một Hệ Quản Trị Cơ Sở Tri Thức (Knowledge Base Management System) mạnh mẽ được thiết kế dựa trên mô hình hình thức **COKB** (Computational Object Knowledge Base). Hệ thống cung cấp giải pháp lưu trữ, truy vấn, và suy luận trên tập tri thức, đặc biệt phù hợp cho các bài toán hình học, giáo dục, và hệ chuyên gia.

## 🌟 Tính năng nổi bật

- **Kiến trúc 4 tầng chuẩn (4-Tier Architecture)**
  - Tương tự các RDBMS hiện đại như MySQL, PostgreSQL.
  - Phân tách rõ ràng giữa Application (CLI), Server, Reasoning Engine, và Storage.
- **Mô hình COKB đầy đủ (6 thành phần)**
  - Quản lý Concepts, Relations, Operators, Functions, Rules, và Hierarchies.
  - Mở rộng kiến trúc nạp: `SameVariables`, `Equations` (giải phương trình 1D, 2D phi tuyến tính).
  - Tích hợp `PART_OF` tự động sinh các biến con trỏ.
  - Cho phép `CONSTRUCT_RELATIONS` liên kết tri thức phương trình giữa các mô hình.
- **Ngôn ngữ truy vấn KBQL (KBDDL & KBDML)**
  - Cú pháp SQL-like đặc thù (VD: CREATE CONCEPT ... VARIABLES ... EQUATIONS).
  - Hỗ trợ đầy đủ DDL (Create, Drop, Use) và DML (Select, Insert, Update, Delete, Solve).
- **Suy luận mạnh mẽ (Reasoning Engine)**
  - Cơ chế suy luận kết nối ma trận Closure (FClosure).
  - Hỗ trợ giải phương trình đơn biến bằng thuật toán Brent (1D) và đa biến bằng Newton-Raphson (2D).
  - Truy vết (DerivationTrace) luồng lập luận chi tiết.
- **Storage Engine tối ưu**
  - Trình quản lý Object Data và Metadata bằng Binary Format.
  - B+ Tree Indexing.
  - WAL (Write-Ahead Logging) System.
- **Hệ thống phân quyền chi tiết**
  - ROOT, USER và cấp phép READ, WRITE, ADMIN.

## 📂 Cấu trúc dự án

```text
KBMS/
├── KBMS.CLI/          # Giao diện dòng lệnh (Command Line Interface)
├── KBMS.Server/       # Core Server xử lý kết nối và Request
├── KBMS.Knowledge/    # Quản lý cấu trúc tri thức (Knowledge Manager)
├── KBMS.Models/       # Các Entity Models (Concept, Rule, Operator...)
├── KBMS.Network/      # Tầng giao tiếp mạng TCP/IP Protocol
├── KBMS.Parser/       # Trình phân tích cú pháp KBQL (Lexer, Parser, AST)
├── KBMS.Reasoning/    # Động cơ suy luận (Reasoning Engine)
├── KBMS.Storage/      # Storage cơ sở dữ liệu vật lý (File I/O, Index, WAL)
└── KBMS.Tests/        # Unit Tests & Integration Tests
```

## 🚀 Hướng dẫn cài đặt và sử dụng

### Yêu cầu hệ thống
- .NET 8.0 SDK hoặc mới hơn
- Visual Studio 2022 hoặc JetBrains Rider

### Build và Chạy
1. Build toàn bộ solution:
   ```bash
   dotnet build KBMS.sln
   ```
2. Khởi chạy KBMS Server:
   ```bash
   cd KBMS.Server
   dotnet run
   ```
3. Khởi chạy KBMS CLI (Client):
   ```bash
   cd KBMS.CLI
   dotnet run
   ```

### Giao tiếp cơ bản (CLI)
```sql
> CREATE KNOWLEDGE BASE geometry;
> USE geometry;
> CREATE CONCEPT DIEM VARIABLES (x:INT, y:INT);
> CREATE CONCEPT TAMGIAC VARIABLES (a:INT, b:INT, c:INT, S:DOUBLE) ALIASES TRIANGLE;
> ADD COMPUTATION TO TAMGIAC VARIABLES a, b, c, S FORMULA 'sqrt(((a+b+c)/2) * (((a+b+c)/2)-a) * (((a+b+c)/2)-b) * (((a+b+c)/2)-c))';
> INSERT INTO DIEM VALUES (x=0, y=0);
> SELECT DIEM;
```

## 📚 Tài liệu
Xem chi tiết trong thư mục [docs/](./docs/).
- [1. Kiến trúc hệ thống](./docs/1-architecture.md)
- [2. Ngôn ngữ truy vấn KBQL](./docs/2-kbql-syntax.md)
- [3. Cơ sở lý thuyết COKB](./docs/3-cokb-model.md)
- [4. Lưu trữ và Tính toán](./docs/4-storage-logic.md)

---
*Phát triển cho đồ án/luận văn Quản Trị Cơ Sở Tri Thức.*
