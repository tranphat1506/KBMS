# KBMS (Knowledge Base Management System) 1.1

KBMS là một hệ quản trị cơ sở tri thức (Knowledge Base Management System) hiện đại, hỗ trợ mô hình tri thức thực thể và tính toán (COKB). Hệ thống này cho phép bạn định nghĩa, cung cấp, truy vấn và thực hiện các suy luận tri thức phức toán thông qua ngôn ngữ **KBQL (Knowledge Base Query Language)**.

## Tính năng mới nổi bật (v1.1)
- **Row-based Streaming Protocol**: Truyền tải dữ liệu dòng cho `SELECT`, `SHOW` và `DESCRIBE`, tối ưu bộ nhớ.
- **True Typing & Precision**: Hỗ trợ kiểu `INT`, `DECIMAL`, `DOUBLE` chuẩn xác cao.
- **Multi-statement Execution**: Thực thi chuỗi câu lệnh cách nhau bằng `;`.
- **Hệ thống Metadata Thông minh**: Truy vấn cấu trúc tri thức (`system.concepts`, `Person.rules`).
- **Cải tiến Giao diện CLI**: Hiển thị dạng bảng (Tabular), dạng dọc (Vertical \G) cho mô tả chi tiết, và Execution Pipeline cho giải trình.

## Hệ thống Tài liệu (Documentation)

1.  **[Tổng quan Kiến trúc](docs/01-architecture-overview.md)**: Chi tiết về mô hình 4 tầng và luồng xử lý thông tin.
2.  **[Hướng dẫn Cú pháp KBQL](docs/02-kbql-syntax-guide.md)**: Tham khảo đầy đủ nhất về ngôn ngữ truy vấn v1.1.
3.  **[Hệ thống Kiểu dữ liệu & Độ chính xác](docs/07-typing-and-precision.md)**: Cách KBMS bảo toàn độ chính xác cho số liệu.
4.  **[Cơ chế Lưu trữ Nội bộ](docs/03-storage-engine-internals.md)**: Giải thích về WAL, Buffer Pool và định dạng file.
5.  **[Thiết kế Parser & Lexer](docs/04-parser-and-lexer-design.md)**: Cách KBMS phân tích và dịch các câu lệnh phức tạp.
6.  **[Giao thức Mạng & Truyền tin Dòng](docs/06-networking-and-protocol.md)**: Đặc tả chi tiết về Row-based Streaming.
7.  **[Cài đặt & Hướng dẫn Sử dụng](docs/05-installation-and-usage-guide.md)**: Cách triển khai Server/Client và ví dụ thực tế.

## Cài đặt nhanh
Yêu cầu: .NET 8.0 SDK.

```bash
# Clone repository
git clone https://github.com/tranphat1506/KBMS.git

# Chạy Server
cd KBMS.Server
dotnet run

# Chạy Client
cd KBMS.CLI
dotnet run
```

## Ví dụ KBQL v1.1
```sql
-- Tạo khái niệm với kiểu dữ liệu chính xác
CREATE CONCEPT Product (
    VARIABLES ( id: INT, price: DECIMAL(10,2), tax: DOUBLE ),
    CONSTRAINTS ( total = price * (1 + tax) )
);

-- Thêm đối tượng
INSERT INTO Product ATTRIBUTE ( id: 1, price: 100.00, tax: 0.1 );

-- Truy vấn metadata
SELECT * FROM system.concepts WHERE Name = 'Product';

-- Truy vấn dữ liệu
SELECT id, total FROM Product;
```

---
*© 2026 Phát triển bởi GeminiCanCode.*
