# KBMS - CHEAT SHEET (Ôn nhanh trước giờ)

---

## 1. CÔNG THỨC QUAN TRỌNG

```
COKB = (C, H, R, Ops, Funcs, Rules)

Object = (Attrs, Facts, Rules)
```

---

## 2. KIẾN TRÚC 4 TẦNG

| Tầng | Chức năng | Công nghệ |
|------|-----------|-----------|
| **Application** | UI (Studio, CLI) | React, Electron |
| **Network** | Giao tiếp Client-Server | Socket, AES-256 |
| **Server** | Parser, Knowledge Manager | LL(k) Parser, AST |
| **Storage** | Lưu trữ bền vững | B+ Tree, WAL, Slotted Page |

---

## 3. LUỒNG XỬ LÝ (NHỚ THEO SƠ ĐỒ)

```
User Input → Lexer (Tokenize) → Parser (AST) →
Knowledge Manager (Route) → Inference Engine (Suy diễn) /
Storage (Đọc/ghi) → Response
```

---

## 4. RETE ALGORITHM

**2 nguyên lý:**
1. **State Persistence** - Lưu kết quả cũ, không tính lại
2. **Node Sharing** - Dùng chung node cho điều kiện trùng

**3 loại node:**
- **Alpha Node** - Lọc 1 điều kiện (age > 60)
- **Beta Node** - Nối nhiều điều kiện (AND, OR)
- **Terminal Node** - Kích hoạt rule

---

## 5. STORAGE LAYER

**Slotted Page (16KB):**
- Header (32B) + Slot Directory + Records
- Cho phép xóa/sửa record không di chuyển data

**B+ Tree:**
- Chiều cao h = 4 cho 1M records
- Tìm kiếm O(log n)
- Leaf nodes linked → range query nhanh

**WAL (Write-Ahead Logging):**
- Ghi log TRƯỚC khi ghi data
- Đảm bảo Durability (ACID)

---

## 6. NGÔN NGỮ KBQL

| Nhóm | Lệnh điển hình |
|------|----------------|
| **KDL** | CREATE KB/CONCEPT/RULE |
| **KML** | INSERT/UPDATE/DELETE |
| **KQL** | SELECT, SELECT SOLVE() |
| **KCL** | GRANT/REVOKE |
| **TCL** | BEGIN/COMMIT/ROLLBACK |

**Điểm khác SQL:**
- `SOLVE(x)` - Tự động giải phương trình
- Concept = Table + Rules + Equations
- Kế thừa (IS_A), Thành phần (PART_OF)

---

## 7. KẾT QUẢ THỬ NGHIỆM

- **200,000+** ops/sec throughput
- **~5s** xử lý 1M records
- **377/380** tests passed (99.2%)

---

## 8. CÂU HỎI KHÓ - TRẢ LỜI NGẮN

**Q: Tại sao không dùng DBMS + AI riêng?**
> A: Latency cao, data phải di chuyển. KBMS tích hợp suy diễn NGAY tại storage.

**Q: Rete tại sao nhanh?**
> A: 1) Lưu state không tính lại, 2) Share node cho điều kiện trùng.

**Q: WAL để làm gì?**
> A: Ghi log trước, crash thì replay → đảm bảo Durability.

**Q: Hạn chế hệ thống?**
> A: 1) Chưa distributed, 2) Solver giới hạn, 3) Query opt chưa tốt.

**Q: Ứng dụng thực tế?**
> A: Y tế (chẩn đoán), Tài chính (risk), Sản xuất (chẩn đoán lỗi).

---

## 9. TỪ VIẾT TẮT HAY BỊ HỎI

| Viết tắt | Nghĩa |
|----------|-------|
| COKB | Computational Objects KB |
| KBQL | Knowledge Base Query Language |
| AST | Abstract Syntax Tree |
| WAL | Write-Ahead Logging |
| ACID | Atomicity, Consistency, Isolation, Durability |
| RBAC | Role-Based Access Control |

---

## 10. TIPS TRẢ LỜI

1. **Không biết** → "Đây là hướng phát triển tương lai"
2. **Bị bắt lỗi** → "Cảm ơn góp ý, tôi sẽ cải thiện"
3. **So sánh** → Nêu ưu điểm của KBMS, thừa nhận hạn chế
4. **Demo lỗi** → Có backup screenshots/video

**CHÚC BẠN BẢO VỆ THÀNH CÔNG! 🎓**
