# Giải Phẫu Lớp Lưu Trữ Vật Lý và Bộ Nhớ Tạm (Storage Layer Internals)

KBMS V2 hoạt động dựa trên cơ chế hai nếp gấp (Two-tier Storage System) tương tự Oracle Memory Architecture và MySQL InnoDB. Khái niệm `Đĩa cứng tĩnh File (Persistency)` và `RAM động (In-memory Engine)` được vạch rõ ràng.

## 1. Trái Tim Của CSDL: RAM Buffer Pool (Vùng Nhớ Đệm)
Mục đích tối thượng của CSDL không phải đem dữ liệu đi cất, mà là **đọc dữ liệu nhanh nhất có thể**. Việc gọi phương thức `File.ReadAllBytes` mỗi khi gõ `SELECT` là tối kỵ.
Thay vào đó, Kiến trúc Storage khởi tạo một `CacheManager/BufferPool`.
- Khi CSDL bật lên, Buffer Pool rỗng ruột. Gọi là **Cold Cache**.
- Query đầu tiên chạm vào hệ thống, Cache Manager sẽ lôi file `.kdf` nặng kịch trần 1-2GB lên và Parse thành mảng List trên RAM. Mất vài giây đầu tiên, tạo thành **Warm Cache**.
- Từ Query thứ 2 trở đi, mọi luồng xử lý toán học (Forward Chaining / SOLVE) từ KnowledgeManager lướt thẳng vào mảng List này với tốc độ vi giây. Không có bất kỳ lệnh đĩa cứng chậm chạp nào được sử dụng. Tốc độ C# Dictionary Lookup `O(1)` là trùm cuối.

## 2. Hệ Quản lý Giao Dịch (Transaction Shadow Paging)
- **Vấn đề**: Khi nhiều User cùng nhập dữ liệu, nếu họ cùng chạy `UPDATE` lên 1 mảng List trên RAM, mảng sẽ nổ hoặc rác (Race condition). Hoặc khi 1 user đang `INSERT` 1 triệu dòng thì bị lỗi gõ nhầm... văng dở dang giữa đường, 1 nửa Data đã nằm "bẩn" trên mảng Memory.
- **Giải pháp - Ngôn ngữ TCL**: Lệnh `BEGIN TRANSACTION` đánh thức Căn phòng tối ảo thuật. StorageEngine Copy sâu/Mở Luồng bộ lọc riêng biệt (Shadow Page/Session Buffer) cho ID Connection đó. 
- Mọi thao tác `INSERT` của người này lúc này chỉ làm căng RAM ảo của phiên làm việc đó. Bảng dữ liệu chính gốc (Master Buffer) bất động!
- User hô lệnh `ROLLBACK`: Ném Session Buffer vào máy hủy rác Garbage Collection C#. Tự động bay sạch bách.
- User hô lệnh `COMMIT`: Session Buffer được dập xác nhận "Sạch". Cập nhật chép đè list mới vô Master Buffer. Đồng thời ra lệnh cho Cấp dưới ghi đứt xuống ổ đĩa vật lý cứng `Flush`.

## 3. Tệp Vật Lý Bảo Lưu (Physical Disk Persistence `.kdf` & `.kmf`)
Hệ thống sử dụng Serialization Binary siêu nhẹ. Đuôi file đã được thay đổi từ `.bin` sang thương hiệu chuẩn ngầu:
*   `.kmf` (Knowledge Meta File): Ổ cứng chứa Metadata Ràng buộc, Object Type, Concept Definitions.
*   `.kdf` (Knowledge Data File): Cứa B-Tree của hàng tỷ ObjectInstance / Vụ việc. Lưu ý file này chỉ được Flush xuống khi lệnh lệnh `Commit()` vung trượng.

## 4. WAL (Write-Ahead-Log) - File Chống Cháy `.klf`
*   **Vấn đề lớn nhất**: Khốn khiếp thay, RAM thì bốc hơi khi mất điện. Nếu user `UPDATE` rồi, nhưng chưa kịp gõ `COMMIT` (Trạng thái RAM Buffer chưa ghim xuống đĩa `.kdf`)... thì Phụp, Sever Cúp Điện (Power Outage)! Bao nhiêu thay đổi cồng kềnh đi đâu?
*   **Cứu Cánh Oai Hùng - `.klf`**: Mọi chữ User gõ thay đổi data (KML) ĐỀU MÀ Âm thầm Nối Thêm Chữ (Append Log) vào cuối một file text tuyến tính có tên `transactions.klf`. Do file log xả dạng raw text đệm cực nhanh nên tốc độ append là ngang RAM.
*   **Màn Hồi Sinh Điển Hình**: Lúc bật lại App (Restarted!). KBMS check file `.klf` có chữ nào không. Nhắm mắt đọc từ trên chữ đầu tiên phân tích và "Bơm ngược" lệnh KML lên bộ nhớ RAM. Bộ Buffer Pool lại sống dậy y nguyên thời điểm trước khi cúp điện. Sau đó Server chép mẻ RAM sạch này thay cái `.kdf` bị cũ kia đi. Rất Vi Diệu!
