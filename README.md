# Hệ Quản Trị Cơ Sở Tri Thức (KBMS V2)
**Knowledge Base Management System**

KBMS là một hệ quản trị tri thức mạnh mẽ, cung cấp mô hình lưu trữ ObjectInstance & Concept linh hoạt kết hợp với Inference Engine (Bộ suy diễn chuyên gia) tối ưu. 

Phiên bản V2 đánh dấu sự lột xác kiến trúc, biến KBMS từ một công cụ đơn lẻ thành hệ cấu trúc Data Engine Chuẩn Thương Mại (Commercial Grade) thông qua **Kiến trúc CSDL Đa Tầng (Multi-Tier Architecture)**. Nó gánh vác cả năng lực truyền thống của hệ cơ sở dữ liệu (DBMS) lẫn quyền năng lập luận Logic mờ.

---

## ⚡ Các Tính Năng Đỉnh Cao Của V2

### 1. Kiến Trúc Lưu Trữ Ổ Cứng (Physical Custom Engine)
Thay thế định dạng nhị phân rác chung chung, KBMS V2 sở hữu hệ sinh thái file mở rộng định hình thương hiệu:
- **`.kmf` (Knowledge Meta File)**: Chứa cấu trúc Schema, Metadata, Constraints cố định.
- **`.kdf` (Knowledge Data File)**: Lưu dạng B-Tree các thực thể đối tượng (hàng tỷ ObjectInstances).
- **`.klf` (Knowledge Log File)**: Nhật ký tĩnh `WAL` (Write-Ahead-Log). Xóa tan nỗi lo sập nguồn, duy trì tính ACID, phục hồi nguyên trạng bộ nhớ RAM siêu tốc khi khởi động lại ứng dụng.

### 2. Bộ Đệm Truy Xuất Ánh Sáng (RAM Buffer Pool)
Chấm dứt hoàn toàn tình trạng nghẽn cổ chai (I/O Bottleneck) vì phải đọc ổ đĩa cứng khi truy vấn. KBMS cung cấp hệ thống RAM Cache tự trị cho từng Knowledge Base.
Mọi truy vấn toán học, KQL `SELECT` hay `SOLVE` đều lướt qua mảng List trên bộ nhớ động của môi trường C# với tốc độ phản xạ micro-seconds (O(1)).

### 3. Vòng Đời Phiên Giao Dịch (TCL Shadow Paging)
- `BEGIN TRANSACTION`: Kích hoạt Sandbox RAM ẩn danh. Mọi thao tác đều thao tác vào rác RAM, không bao giờ gây hỏng dữ liệu hệ thống bên dưới nếu lỗi văng giữa chừng.
- `COMMIT`: Tính năng duy nhất đổ RAM đã xác nhận Flush xuống ổ cứng đĩa vật lý của hệ thống.
- `ROLLBACK`: Nút thắt xóa mảng rác để đảo ngược quá trình (Undo) mượt mà bằng hệ thống dọn rác (GC).

---

## 🔍 KBQL (Knowledge Base Query Language) V2

Bộ Parser C# tự viết được phân nhánh thành **5 Dòng Ngôn Ngữ riêng rẽ** sử dụng cú pháp đóng gói **Block Ngoặc Tròn `()`**, nâng cấp cấu trúc dễ đọc đáng sợ.

1. **KDL (Định Nghĩa Mạng Lưới Khái Niệm)**
   - Khởi tạo: `CREATE CONCEPT <TAM> ( VARIABLES(...) RULES(...) )`
   - Tiến hóa CSDL không mất Data: `ALTER CONCEPT <TAM> ( ADD ( RULES(...) ) )`

2. **KML (Sự Kiện Thực Thể)**
   - Thay đổi các sự kiện trên RAM: `INSERT INTO <TAMGIAC> ATTRIBUTE ( A: 1 );`
   - Cập nhật linh hoạt: `UPDATE <TAMGIAC> ATTRIBUTE ( SET A: 10 ) WHERE A = 5;`

3. **KQL (Chắt Lọc & Đi Cửa Sau Não Bộ Toán Học)**
   - Truy vấn CSDL tĩnh nhúng hàm Join đa cấp: `SELECT * FROM <TAMGIAC>;`
   - Giao việc tự động cho Não Bộ Máy (Inference): `SOLVE ON CONCEPT <TAMGIAC> GIVEN A=1, B=2 FIND CanhC;`

4. **KCL (Luật Quyền Hệ Thống)**
   - Phân mảnh quyền User bằng hệ thống Role Base Access System (RBAS): `GRANT READ ON <ToanHoc> TO admin;`

5. **TCL (Khóa An Toàn)**
   - Bộ 3 từ khóa quyền lực: `BEGIN TRANSACTION`, `COMMIT`, `ROLLBACK`.

---

## 🚀 Hướng Dẫn Kịch Bản Test (Benchmarking)
Với cấu hình Buffer Array RAM Cache, KBMS thách thức mọi giới hạn phần cứng nhỏ nhất. Dưới đây là chiến lược thử tải cho dự án này:

- **Giới hạn Hệ Thống OOM (Out Of Memory OOM)**: Viết script đẩy 1.000.000 sự kiện bằng `KML INSERT` lên RAM Pool để nhận diện khả năng thu gom hệ điều hành (Garbage Collector Eviction) trước và sau khi kích hoạt `ROLLBACK` hoặc xả đĩa cứng bằng `COMMIT`.
- **Đồ thị Giới Hạn Minimal SysReqs**: Tìm ngưỡng RAM giới hạn cuối cùng từ mốc `50MB RAM Idle` của C#. 

---
_Đây là hệ tư tưởng và đỉnh cao Đồ Án Kỹ Thuật Hệ Quản Trị CSDL Tri Thức!_
