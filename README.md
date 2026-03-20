# KBMS (Knowledge Base Management System) 1.0

KBMS là một hệ quản trị cơ sở tri thức (Knowledge Base Management System) hiện đại, hỗ trợ mô hình tri thức tính toán (COKB). Hệ thống này cho phép bạn định nghĩa, lưu trữ, truy vấn và thực hiện các suy luận tri thức phức tạp thông qua ngôn ngữ KBQL (Knowledge Base Query Language).

## Tính năng nổi bật của v1.0
- **Kiến trúc 4 tầng (4-tier Architecture)**: Tách biệt rõ ràng giữa ứng dụng, máy chủ, quản lý lưu trữ và lưu trữ vật lý.
- **Cơ chế WAL (Write-Ahead Logging)**: Đảm bảo toàn vẹn dữ liệu khi có sự cố hệ thống (Crash Recovery).
- **Buffer Pool (RAM Cache)**: Tối ưu hóa hiệu năng đọc/ghi dữ liệu thông qua bộ đệm RAM thông minh.
- **Active Logic (Triggers)**: Hỗ trợ tự động hóa các phản ứng dữ liệu dựa trên sự kiện (INSERT, UPDATE, DELETE).
- **Tối ưu hóa Truy vấn**: Hệ thống Indexing (.kif) giúp tăng tốc tìm kiếm lên hàng triệu đối tượng.
- **ACID Transactions**: Hỗ trợ giao dịch đầy đủ với `BEGIN`, `COMMIT`, `ROLLBACK`.

## Hệ thống Tài liệu (Documentation)

Dưới đây là các hướng dẫn chi tiết về KBMS 1.0 (Tiếng Việt):

1.  **[Tổng quan Kiến trúc](docs/01-architecture-overview.md)**: Chi tiết về mô hình 4 tầng và luồng xử lý thông tin.
2.  **[Hướng dẫn Cú pháp KBQL](docs/02-kbql-syntax-guide.md)**: Tham khảo đầy đủ nhất về ngôn ngữ truy vấn tri thức (KDL, KML, KQL, TCL, KCL, KHL).
3.  **[Cơ chế Lưu trữ Nội bộ](docs/03-storage-engine-internals.md)**: Giải thích về WAL, Buffer Pool và định dạng file `.kmf`, `.kdf`, `.klf`, `.kif`.
4.  **[Thiết kế Parser & Lexer](docs/04-parser-and-lexer-design.md)**: Cách KBMS phân tích và dịch các câu lệnh phức tạp.
5.  **[Cài đặt & Hướng dẫn Sử dụng](docs/05-installation-and-usage-guide.md)**: Cách triển khai Server/Client và 5 kịch bản sử dụng thực tế.

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

---
*© 2026 Phát triển bởi GeminiCanCode.*
