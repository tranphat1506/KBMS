# 07.5. Độ bền & Nhật ký ghi trước (Durability & WAL)

Đảm bảo dữ liệu không bị mất mát khi hệ thống bị ngắt điện đột ngột là nhiệm vụ của `WalManager`. Thành phần này thực hiện các giao thức để đảm bảo tính **Durability** (Độ bền vững) trong mô hình ACID.

---

## 1. Giao thức Write-Ahead Logging (WAL)

KBMS tuân thủ nguyên tắc vàng của hệ quản trị cơ sở dữ liệu: **Ghi Nhật ký trước khi ghi Dữ liệu**.

1.  **Log First:** Mọi thay đổi (Insert, Update, Delete) được mô tả thành một đối tượng `LogRecord` và ghi vào tệp `.log` trên đĩa trước khi dữ liệu thực tế được cập nhật vào Buffer Pool.
2.  **Sequential I/O:** Việc ghi Log là thao tác ghi tuần tự (Sequential), giúp tận dụng tối đa băng thông của ổ cứng mà không bị trễ do tìm kiếm đầu đọc (Disk Seek).
3.  **Atomic Write:** Một giao dịch chỉ được coi là thành công khi `FileStream.Flush(true)` xác nhận dữ liệu Log đã nằm an toàn trên bề mặt vật lý của đĩa.

---

## 2. Số tuần tự nhật ký (LSN - Log Sequence Number)

LSN là "dấu vân tay thời gian" của mọi thay đổi trong hệ thống:
-   Mỗi bản ghi Log có một LSN tăng dần duy nhất.
-   Mỗi trang dữ liệu (`Page`) lưu trữ LSN của thao tác ghi cuối cùng tác động lên nó (tại Offset 4-7 trong Header).
-   **Quy tắc an toàn:** Một trang dữ liệu chỉ được phép ghi xuống đĩa (Flush) nếu LSN của trang đó nhỏ hơn hoặc bằng LSN lớn nhất đã được ghi thành công vào file Log.

---

## 3. Quy trình Phục hồi sau sự cố (Recovery)

Khi KBMS khởi động lại sau một vụ sập nguồn đột ngột (Crash), nó thực hiện quy trình **Forward Recovery**:

![Sơ đồ Vòng đời Phục hồi Dữ liệu (Recovery Flow)](../assets/diagrams/recovery_flow.png)
*Hình 7.5: Cơ chế tái hiện (Redo) dữ liệu từ nhật ký WAL để khôi phục trạng thái nhất quán.*

1.  **Phân tích (Analysis):** Xác định vị trí bản ghi Log cuối cùng và điểm kiểm tra (Checkpoint) gần nhất.
2.  **Tái hiện (Redo Phase):** Hệ thống đọc file Log và thực hiện lại các thao tác nếu `LSN_Log > LSN_Page`. Điều này đảm bảo mọi giao dịch đã được xác nhận (Committed) nhưng chưa kịp ghi xuống file `.dat` sẽ được phục hồi đầy đủ.
3.  **Nhất quán:** Sau khi hoàn tất, hệ thống sẵn sàng phục vụ các truy vấn mới.

---

## 4. Cơ chế Checkpoint

Để ngăn file Log phình to vô hạn, định kỳ KBMS thực hiện **Checkpoint**:
-   Ghi toàn bộ các trang "Dirty" từ Buffer Pool xuống đĩa.
-   Đánh dấu vị trí an toàn trong file Log.
-   Xóa bỏ các bản ghi Log cũ đã được lưu trữ an toàn trên file dữ liệu chính, giúp giải phóng không gian lưu trữ.
