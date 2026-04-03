# Hướng dẫn Thuyết trình và Bảo vệ Đồ án KBMS

---

## TỔNG QUAN CẤU TRÚC BÀI THUYẾT TRÌNH

```
1. Giới thiệu (5 phút)
2. Cơ sở lý thuyết COKB (10 phút)
3. Phân tích & Thiết kế (10 phút)
4. Kiến trúc hệ thống (20 phút) - TRỌNG TÂM
5. Thử nghiệm & Đánh giá (10 phút)
6. Kết luận (5 phút)
```

---

## PHẦN 1: GIỚI THIỆU (5 phút)

### Nội dung cần nói

**Lý do chọn đề tài:**
- DBMS truyền thống chỉ lưu trữ dữ liệu, KHÔNG TỰ ĐỘNG SUY DIỄN ra tri thức mới
- Ví dụ: Database lưu `age=60`, `bloodSugar=150` nhưng KHÔNG tự kết luận được "bệnh nhân có nguy cơ cao"
- KBMS = Kết hợp khả năng lưu trữ của DBMS + khả năng suy diễn của AI

**Mục tiêu:**
1. Biểu diễn tri thức chuyên sâu (COKB model)
2. Suy diễn tự động (Rete algorithm)
3. Lưu trữ bền vững (B+ Tree, WAL)
4. Giao diện trực quan (Studio IDE)

### Thuật ngữ cần nhớ

| Thuật ngữ | Viết tắt | Giải thích |
|-----------|----------|------------|
| Knowledge Base Management System | KBMS | Hệ quản trị cơ sở tri thức |
| Computational Objects Knowledge Base | COKB | Mô hình đối tượng tính toán |
| Knowledge Base Query Language | KBQL | Ngôn ngữ truy vấn tri thức |

### Câu hỏi có thể bị hỏi

**Q: Tại sao không dùng DBMS truyền thống + AI layer?**
> A: DBMS + AI layer tách biệt gây chậm trễ (latency), dữ liệu phải di chuyển giữa 2 hệ thống. KBMS tích hợp suy diễn NGAY TẠI TẦNG LƯU TRỮ, giúp truy vấn tri thức thời gian thực.

**Q: KBMS khác gì với Expert System?**
> A: Expert System chỉ có bộ máy suy diễn, thiếu khả năng lưu trữ bền vững. KBMS = Expert System + DBMS (có transaction, index, recovery).

---

## PHẦN 2: CƠ SỞ LÝ THUYẾT COKB (10 phút)

### Nội dung cần nói

**Công thức COKB:**
```
COKB = (C, H, R, Ops, Funcs, Rules)
```

Giải thích từng thành phần:
- **C (Concepts)**: Khái niệm - giống "Class" trong OOP. Ví dụ: Patient, Triangle
- **H (Hierarchy)**: Quan hệ IS-A (kế thừa). Ví dụ: Doctor IS-A Person
- **R (Relations)**: Quan hệ ngữ nghĩa. Ví dụ: SongSong, VuongGoc
- **Ops (Operators)**: Toán tử tùy chỉnh. Ví dụ: + cho Vector, * cho Matrix
- **Funcs (Functions)**: Hàm tính toán. Ví dụ: BMI(weight, height)
- **Rules**: Luật dẫn. Ví dụ: IF age > 60 AND bloodSugar > 140 THEN risk = 'high'

**Mô hình đối tượng:**
```
O = (Attrs, Facts, Rules)
```
- Attrs: Thuộc tính (có thể là object khác → đệ quy)
- Facts: Sự thật đã biết
- Rules: Luật nội tại

**Phân cấp khái niệm:**
- Cấp 0: Kiểu cơ bản (Số, Điểm)
- Cấp 1: Từ cấp 0 (Đoạn thẳng, Góc)
- Cấp n: Phức tạp (Tam giác, Đa giác)

### Thuật ngữ cần nhớ

| Thuật ngữ | Giải thích |
|-----------|------------|
| Concept | Khái niệm - đơn vị tri thức cơ bản |
| Fact | Sự thật - giá trị đã biết của biến |
| Rule | Luật dẫn - quy tắc IF...THEN |
| Forward Chaining | Suy diễn tiến - từ giả thiết → kết luận |
| Backward Chaining | Suy diễn lùi - từ mục tiêu → tìm bằng chứng |

### Câu hỏi có thể bị hỏi

**Q: COKB khác gì với Ontology/OWL?**
> A: COKB tập trung vào TÍNH TOÁN (computational), có thể giải phương trình. Ontology tập trung vào mô tả quan hệ, không có khả năng tính toán số học.

**Q: Tại sao cần phân cấp khái niệm?**
> A: Để kế thừa tri thức. Tam giác kế thừa từ Đa giác → không cần định nghĩa lại thuộc tính chung.

**Q: Attrs có thể là object khác - ý nghĩa?**
> A: Cho phép cấu trúc lồng nhau. Tam giác có 3 cạnh (LineSegment), mỗi cạnh có 2 điểm (Point). Đây là tính đệ quy của COKB.

---

## PHẦN 3: KIẾN TRÚC HỆ THỐNG (20 phút) - TRỌNG TÂM

### 3.1 Kiến trúc 4 lớp

```
┌─────────────────────────────────────────┐
│         Lớp Ứng dụng (Application)       │
│   KBMS Studio (React/Electron) | CLI     │
├─────────────────────────────────────────┤
│         Lớp Mạng (Network)               │
│   Socket | AES-256 | Binary Protocol     │
├─────────────────────────────────────────┤
│         Lớp Server (Engine)              │
│   Parser | AST | Knowledge Manager       │
├─────────────────────────────────────────┤
│         Lớp Lưu trữ (Storage)            │
│   B+ Tree | Slotted Page | WAL           │
└─────────────────────────────────────────┘
```

### 3.2 Luồng xử lý (QUAN TRỌNG)

```
1. User nhập: "SELECT SOLVE(area) FROM Triangle WHERE a=3, b=4, c=5"
                    ↓
2. Network Layer: Đóng gói thành binary packet, gửi đến server
                    ↓
3. Parser:
   - Lexer: Tokenize → [SELECT, SOLVE, (, area, ), FROM, Triangle, ...]
   - Parser: Tạo AST (Abstract Syntax Tree)
                    ↓
4. Knowledge Manager:
   - Kiểm tra: Triangle concept tồn tại?
   - Lấy definition: EQUATIONS, CONSTRAINTS
                    ↓
5. Inference Engine:
   - Nạp facts: a=3, b=4, c=5
   - Build Rete Network từ rules/equations
   - Lan truyền token → tính area = 6.0
                    ↓
6. Storage Layer (nếu cần):
   - Truy xuất dữ liệu từ B+ Tree
   - Đọc page từ Buffer Pool
                    ↓
7. Response: Trả về client {area: 6.0}
```

### 3.3 Tầng Suy diễn (Rete Algorithm)

**Nguyên lý:**
1. **Biên dịch (Compile-time)**: Rules → Rete Network (Alpha + Beta nodes)
2. **Lan truyền (Run-time)**: Facts → Token → propagate qua network
3. **Agenda**: Rules thỏa mãn → Queue → Fire

**Ví dụ Rete Network:**
```
Rule: IF age > 60 AND bloodSugar > 140 THEN risk = 'high'

Alpha Node 1: age > 60  ──┐
                          ├──→ Beta Node ──→ Terminal (Fire Rule)
Alpha Node 2: bloodSugar > 140 ──┘
```

### 3.4 Tầng Lưu trữ

**Slotted Page Structure:**
```
┌────────────────────────────────────────┐
│ Header (32 bytes)                       │
│ - pageId, freeSpaceOffset, slotCount    │
├────────────────────────────────────────┤
│ Slot Directory (growing ↓)              │
│ - [offset, length] cho mỗi record       │
├────────────────────────────────────────┤
│ Free Space                              │
├────────────────────────────────────────┤
│ Records (growing ↑)                     │
│ - Record 1 | Record 2 | ...             │
└────────────────────────────────────────┘
```

**B+ Tree Index:**
- Chiều cao h = 4 cho 1 triệu bản ghi
- Tìm kiếm O(log n)

### Thuật ngữ cần nhớ

| Thuật ngữ | Giải thích |
|-----------|------------|
| AST | Abstract Syntax Tree - Cây cú pháp trừu tượng |
| Alpha Node | Nút lọc điều kiện đơn (age > 60) |
| Beta Node | Nút nối nhiều điều kiện (AND, OR) |
| Token | Đối tượng mang facts lan truyền trong Rete |
| Agenda | Hàng đợi các rule sẵn sàng kích hoạt |
| Slotted Page | Cấu trúc trang với slot directory |
| WAL | Write-Ahead Logging - Ghi log trước khi ghi dữ liệu |
| B+ Tree | Cây chỉ mục, tối ưu cho range query |
| Buffer Pool | Vùng đệm RAM giữ các page hay dùng |

### Câu hỏi có thể bị hỏi

**Q: Tại sao dùng Rete thay vì thuật toán đơn giản (vòng lặp)?**
> A: Rete có 2 tối ưu:
> 1. **State persistence**: Lưu kết quả tính toán cũ, không tính lại
> 2. **Node sharing**: Nhiều rule dùng chung điều kiện → dùng chung node
>
> Với 1000 rules và 100 facts, vòng lặp cần 100,000 phép kiểm tra. Rete chỉ kích hoạt các nhánh bị ảnh hưởng.

**Q: Giải thích Slotted Page - tại sao không lưu tuần tự?**
> A: Slotted Page cho phép:
> - Xóa record ở giữa mà không di chuyển data khác
> - Cập nhật record với kích thước mới
> - Truy xuất random O(1) qua slot index

**Q: WAL là gì và tại sao cần?**
> A: WAL = Write-Ahead Logging. Ghi log TRƯỚC khi ghi data. Khi crash, replay log để phục hồi. Đảm bảo Durability (D trong ACID).

**Q: B+ Tree khác B Tree ở đâu?**
> A: B+ Tree chỉ lưu data ở leaf nodes, internal nodes chỉ chứa keys. Leaf nodes liên kết với nhau (linked list) → tối ưu cho range query.

---

## PHẦN 4: NGÔN NGỮ KBQL (5 phút)

### Các nhóm lệnh

| Nhóm | Tên | Ví dụ lệnh |
|------|-----|------------|
| KDL | Definition | CREATE KB, CREATE CONCEPT, CREATE RULE |
| KML | Maintenance | INSERT, UPDATE, DELETE |
| KQL | Query | SELECT, SELECT SOLVE(), SHOW |
| KCL | Control | GRANT, REVOKE, CREATE USER |
| TCL | Transaction | BEGIN, COMMIT, ROLLBACK |

### Điểm khác SQL

| SQL | KBQL |
|-----|------|
| SELECT * FROM Table | SELECT SOLVE(x, y) FROM Concept |
| Chỉ trả về data đã có | Tự động suy diễn data mới |
| JOIN bảng | Kế thừa (IS_A), Thành phần (PART_OF) |
| Stored Procedures | Rules nội tại |

### Câu hỏi có thể bị hỏi

**Q: SOLVE() hoạt động như thế nào?**
> A: SOLVE(x) tìm giá trị x từ các equations/constraints đã định nghĩa:
> - Nếu 1 biến未知: Dùng Newton-Raphson tìm nghiệm
> - Nếu nhiều biến: Giải hệ phương trình (Newton multi-dimensional)

**Q: Concept khác Table ở đâu?**
> A: Concept có thêm:
> - EQUATIONS: Phương trình nội tại
> - CONSTRAINTS: Ràng buộc
> - RULES: Luật nội tại
> - HIERARCHY: Kế thừa

---

## PHẦN 5: THỬ NGHIỆM & ĐÁNH GIÁ (5 phút)

### Kết quả chính

| Chỉ tiêu | Kết quả |
|----------|---------|
| Thông lượng ghi | 200,000+ ops/sec |
| Xử lý 1M bản ghi | ~5 giây |
| Hash Join 10k×10k | ~7ms |
| Tests passed | 377/380 (99.2%) |

### Câu hỏi có thể bị hỏi

**Q: So sánh với hệ thống khác?**
> A: KBMS lai giữa RDBMS và KBS:
> - Nhanh hơn Expert System truyền thống (có indexing, caching)
> - Chậm hơn RDBMS thuần (do overhead suy diễn)
> - Trade-off: Performance ↔ Intelligence

---

## CÂU HỎI KHÓ CÓ THỂ BỊ HỎI

### Q1: Tính ứng dụng thực tế?

**Trả lời:**
- Y tế: Chẩn đoán bệnh từ triệu chứng + lab results
- Tài chính: Đánh giá rủi ro tín dụng
- Sản xuất: Chẩn đoán lỗi máy móc
- Giáo dục: Hệ thống hỏi đáp thông minh

### Q2: Hạn chế của hệ thống?

**Trả lời thành thật:**
1. Chưa hỗ trợ distributed processing (single-node)
2. Solver giới hạn ở hệ phương trình phi tuyến (không giải được symbolically)
3. Query optimization chưa tối ưu bằng commercial DBMS
4. UI Studio còn basic

### Q3: Hướng phát triển?

**Trả lời:**
1. Distributed KBMS (sharding, replication)
2. Machine Learning integration (hybrid AI)
3. Natural Language Query (NL → KBQL)
4. Cloud deployment (Kubernetes)

### Q4: Tại sao chọn .NET/C#?

**Trả lời:**
- Performance gần C++ (AOT compilation)
- Async/await tốt cho network programming
- Cross-platform (.NET 8)
- Tooling tốt (Visual Studio, Rider)

### Q5: So sánh với Prolog/Datalog?

**Trả lời:**
| Prolog | KBMS |
|--------|------|
| Backward chaining mặc định | Forward + Backward |
| Không có storage | Có B+ Tree, WAL |
| Không có transaction | ACID transactions |
| Single-user | Multi-user concurrent |

---

## MẸO THUYẾT TRÌNH

1. **Mở đầu mạnh**: Nêu vấn đề thực tế → Giải pháp KBMS
2. **Demo ngắn**: SHOW cách hệ thống tự động suy diễn
3. **Slide ít text, nhiều diagram**: Architecture diagram quan trọng nhất
4. **Chuẩn bị backup slides**: Chi tiết thuật toán, code examples
5. **Thực hành demo trước**: Test kỹ để tránh lỗi khi present

---

## TỪ VIẾT TẮT TỔNG HỢP

| Viết tắt | Đầy đủ | Nghĩa |
|----------|--------|-------|
| KBMS | Knowledge Base Management System | Hệ quản trị cơ sở tri thức |
| COKB | Computational Objects Knowledge Base | Mô hình đối tượng tính toán |
| KBQL | Knowledge Base Query Language | Ngôn ngữ truy vấn tri thức |
| KDL | Knowledge Definition Language | Ngôn ngữ định nghĩa tri thức |
| KML | Knowledge Maintenance Language | Ngôn ngữ bảo trì tri thức |
| KQL | Knowledge Query Language | Ngôn ngữ truy vấn tri thức |
| KCL | Knowledge Control Language | Ngôn ngữ điều khiển tri thức |
| TCL | Transaction Control Language | Ngôn ngữ điều khiển giao dịch |
| AST | Abstract Syntax Tree | Cây cú pháp trừu tượng |
| WAL | Write-Ahead Logging | Ghi nhật ký trước |
| ACID | Atomicity, Consistency, Isolation, Durability | 4 tính chất giao dịch |
| B+ Tree | B-plus Tree | Cấu trúc chỉ mục |
| RBAC | Role-Based Access Control | Kiểm soát truy cập theo vai trò |
| IS-A | Is A (inheritance) | Quan hệ kế thừa |
| PART-OF | Part Of (composition) | Quan hệ thành phần |

