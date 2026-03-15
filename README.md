# KBMS - Knowledge Base Management System

Một hệ thống quản trị cơ sở tri thức dựa trên mô hình COKB với kiến trúc 4 tầng tương tự MySQL.

## Trạng thái hiện tại

| Phase | Công việc | Trạng thái |
|-------|-----------|------------|
| **Phase 1** | Project Setup & Data Models | ✅ Hoàn thành |
| **Phase 2** | Storage Engine (Encryption, Binary, WAL, Index) | ✅ Hoàn thành |
| **Phase 3** | Network Protocol (TCP, Message) | ✅ Hoàn thành |
| **Phase 3** | Server Components (ConnectionManager, AuthManager, KbmsServer) | ✅ Hoàn thành |
| **Phase 3** | Parser (Lexer, QueryParser) | ✅ Hoàn thành |
| **Phase 5** | CLI Client | ✅ Hoàn thành |
| **Phase 4** | Knowledge Manager & Reasoning | ⚠️ Cơ bản (chưa có suy luận) |
| **Phase 5** | Testing | ⚠️ Chưa hoàn thành |

## Cấu trúc project

```
KBMS/
├── KBMS.sln                    # Solution file
├── KBMS.Models/                # Data Models ✅
│   ├── User.cs
│   ├── KnowledgeBase.cs
│   ├── Concept.cs
│   ├── Relation.cs
│   ├── Operator.cs
│   ├── Function.cs
│   ├── Rule.cs
│   └── ObjectInstance.cs
├── KBMS.Storage/               # Storage Engine ✅
│   ├── Encryption.cs
│   ├── BinaryFormat.cs
│   ├── IndexManager.cs
│   ├── WalManager.cs
│   └── Engine.cs
├── KBMS.Network/               # Network Protocol ✅
│   └── Protocol.cs
├── KBMS.Server/                # Server ✅
│   ├── Program.cs
│   ├── KbmsServer.cs
│   ├── ConnectionManager.cs
│   ├── AuthenticationManager.cs
│   ├── KnowledgeManager.cs
│   ├── Lexer.cs
│   └── QueryParser.cs
├── KBMS.CLI/                  # CLI Client ✅
│   ├── Program.cs
│   └── Cli.cs
└── data/                      # Physical Storage
    ├── users/
    │   └── users.bin
    └── <kb_name>/
        ├── metadata.bin
        ├── objects.bin
        ├── index.bin
        └── wal.log
```

## Chạy project

### Build
```bash
dotnet build KBMS.sln
```

### Chạy Server
```bash
dotnet run --project KBMS.Server
# Server chạy trên port 3307
# Default ROOT user: username='root', password='admin'
```

### Chạy CLI
```bash
dotnet run --project KBMS.CLI
```

## Các lệnh hỗ trợ

| Lệnh | Cú pháp | Chức năng |
|-------|---------|-----------|
| LOGIN | `LOGIN <username> <password>` | Đăng nhập vào server |
| CREATE KB | `CREATE KNOWLEDGE BASE <name>` | Tạo cơ sở tri thức mới |
| CREATE DATABASE | `CREATE DATABASE <name>` | Tên khác cho CREATE KB |
| DROP KB | `DROP KNOWLEDGE BASE <name>` | Xóa KB |
| USE | `USE <name>` | Chọn KB hiện tại |
| SELECT | `SELECT <concept> WHERE <conditions>` | Truy vấn đối tượng |
| INSERT | `INSERT INTO <concept> VALUES (key=value, ...)` | Thêm đối tượng |
| UPDATE | `UPDATE <concept> SET key=value ... WHERE ...` | Cập nhật đối tượng |
| DELETE | `DELETE FROM <concept> WHERE ...` | Xóa đối tượng |
| CREATE USER | `CREATE USER <name> PASSWORD <password>` | Tạo user mới |
| GRANT | `GRANT <privilege> ON <kb> TO <user>` | Cấp quyền cho user |
| SHOW DATABASES | `SHOW DATABASES` | Hiển thị tất cả KB |

## Phân quyền

### User Roles
- **ROOT**: Full quyền truy cập, KHÔNG cần kiểm tra permission
- **USER**: Cần check permission theo danh sách privilege

### Privileges
- **READ**: Đọc tri thức (SELECT)
- **WRITE**: Đọc và ghi (SELECT, INSERT, UPDATE, DELETE)
- **ADMIN**: Quản trị KB (tất cả các quyền + CREATE CONCEPT/RULE)

### Quy tắc phân quyền
| Action | Quyền cần thiết |
|--------|---------------|
| CREATE KNOWLEDGE BASE | SystemAdmin = true |
| DROP KNOWLEDGE BASE | ADMIN trên KB đó |
| SELECT, SOLVE | Ít nhất READ trên KB |
| INSERT, UPDATE, DELETE | WRITE trên KB |
| CREATE CONCEPT, CREATE RULE | ADMIN trên KB |
| GRANT | Chỉ ROOT hoặc SystemAdmin = true |

## Ví dụ sử dụng

```
login> LOGIN root admin
Logged in as root (ROOT)
kbms> CREATE KNOWLEDGE BASE geometry
kbms/geometry> USE geometry
Using knowledge base: geometry
kbms/geometry> SHOW DATABASES
kbms/geometry> INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5, S=0)
kbms/geometry> SELECT TAMGIAC WHERE a=3
kbms/geometry> CREATE USER test PASSWORD test123
kbms/geometry> GRANT READ ON geometry TO test
```

## Công việc tiếp theo

1. **Phase 4 - Knowledge Manager & Reasoning**
   - [ ] Forward Chaining algorithm
   - [ ] Backward Chaining algorithm
   - [ ] SOLVE command implementation
   - [ ] Concept definition (CREATE CONCEPT)
   - [ ] Rule definition (CREATE RULE)

2. **Phase 5 - Testing & Advanced Features**
   - [ ] Unit tests
   - [ ] Aggregation functions (COUNT, SUM, AVG, MAX, MIN)
   - [ ] GROUP BY, HAVING
   - [ ] ORDER BY, LIMIT/OFFSET
   - [ ] JOIN support

## Công nghệ

- **.NET 8.0** (C# 10.0+)
- **Network**: System.Net.Sockets (TCP)
- **Encryption**: System.Security.Cryptography (AES-256)
- **Password Hashing**: BCrypt.Net-Next
- **Serialization**: System.Text.Json

## License

Academic project - Knowledge Base Management System for graduation thesis.
