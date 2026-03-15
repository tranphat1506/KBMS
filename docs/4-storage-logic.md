# Storage Engine và Tính Toán / Suy Luận

## 1. Storage Engine
Tầng cơ sở dữ liệu vật lý xây dựng trên tập tin dạng nhị phân (`binary format`) nhằm đảm bảo:
- **Tốc độ đọc / ghi**: Serialize/Deserialize struct sang Base64/Bytes cực nhanh.
- **Bảo mật**: Sử dụng khóa AES-256 mã hóa block dữ liệu.
- **Index Manager**: Sử dụng thuật toán B+Tree làm giảm O(N) tìm kiếm về O(log N) đối với query lệnh SELECT theo conditions lớn.
- **WAL.log (Write-Ahead Logging)**: Hạn chế rủi ro Crash, đảm bảo ACID cho Database Engine tự tự như PostgreSQL/MySQL.

## 2. Hệ Ngữ Nghĩa KBDML/KBDDL
Được sử dụng cho quá trình tạo Index / Cập nhật và Xóa siêu dữ liệu trong Storage:
* `INSERT`: WAL -> Serialize byte -> B+Tree Update -> Disk Write -> Commit.
* `SELECT`: Regex Parser -> Load Metadata -> Compare -> Cache B+Tree Node -> Trả về JSON Results.

## 3. Khối suy luận Engine (Reasoning Engine)
Hoạt động bằng việc áp dụng Forward và Backward Chaining duyệt AST Rule.
1. Khởi tạo Known Matrix từ Object data.
2. Tìm kiếm Goal Tree từ Unkown Target (Backward Chaining).
3. Đánh giá tính sẵn sàng của Variables, áp dụng Formal Deduction Rules để Forward Chaining điền Unknown Variables.
4. Output Solution Steps thay vì chỉ Result thuần.
