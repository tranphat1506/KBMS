# Kiến trúc hệ thống KBMS

Hệ thống quản trị cơ sở tri thức (KBMS - Knowledge Base Management System) được thiết kế theo kiến trúc 4 tầng (4-Tier Architecture), đảm bảo tính module hóa, dễ bảo trì, và mở rộng.

## Sơ đồ tổng thể

```mermaid
graph TD
    subgraph App Layer [Application Layer]
        CLI[CLI Client / Console]
        GUI[Client Applications]
    end

    subgraph Server Layer [Server Layer]
        CM[Connection Manager]
        AM[Auth Manager]
        QP[Query Parser]
        KM[Knowledge Manager]
        RE[Reasoning Engine]
    end

    subgraph Storage Eng Layer [Storage Engine]
        CE[Crypto Engine]
        IX[Index Manager]
        WAL[WAL Manager]
    end

    subgraph File System [Physical Files]
        Bin[Binary Files .bin]
        Log[Log Files .log]
    end

    App Layer -- TCP/IP --> Server Layer
    Server Layer -- Object I/O --> Storage Eng Layer
    Storage Eng Layer -- Bytes I/O --> File System
```

## Các Tầng (Tiers)

### 1. Application Layer (Tầng Ứng dụng)
- Chứa các Client giao tiếp với hệ thống.
- **KBMS.CLI**: Cung cấp giao diện command-line. Người dùng gõ lệnh KBQL trực tiếp.
- Các app tương lai (Web/Desktop) cũng thuộc tầng này, giao tiếp qua Giao thức TCP.

### 2. Server Layer (Tầng Máy chủ)
- **KBMS.Network**: Xử lý TCP connections.
- **KBMS.Server**: Core loop nhận request, kiểm tra quyền (Auth).
- **KBMS.Parser**: Phân tích câu lệnh KBQL (Lexer -> Parser -> AST). Kiểm tra lỗi cú pháp và trả về cấu trúc cây phân tích.
- **KBMS.Knowledge**: Xử lý logic nghiệp vụ cho siêu dữ liệu (thêm/xóa Concept, Rule).
- **KBMS.Reasoning**: Động cơ suy luận chính. Thực thi các lệnh `SOLVE`, áp dụng luật (Forward/Backward Chaining), duyệt cây tri thức để giải quyết bài toán.

### 3. Storage Engine Layer (Tầng Lưu trữ)
- **KBMS.Storage**: Xử lý lưu trữ vật lý.
- **Encryption**: Dữ liệu mã hóa AES-256 để bảo mật.
- **Index Manager**: Quản lý Data Indexing (B+ Tree) giúp truy vấn nhanh `SELECT` và `WHERE`.
- **Wal Manager (Write-Ahead Log)**: Đảm bảo ACID, ghi log trước khi commit xuống file.

### 4. Physical Storage (Tầng Vật lý)
- Cấu trúc thư mục `data/` chứa các KB, mỗi KB là một sub-directory (vd: `data/geometry`).
- Mỗi thành phần lưu ra một file binary riêng: `concepts.bin`, `rules.bin`, `index.bin`, `objects.bin`.
