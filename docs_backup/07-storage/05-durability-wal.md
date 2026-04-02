# 07.5. Độ bền & Nhật ký ghi trước

Đảm bảo dữ liệu không bị mất mát khi hệ thống bị ngắt điện đột ngột là nhiệm vụ cốt lõi của `WalManagerV3`. Thành phần này thực hiện giao thức **Write-Ahead Logging** để đảm bảo tính **Durability** (Độ bền vững) trong mô hình [ACID](../00-glossary/01-glossary.md#acid).

---

## 1. Giao thức Write-Ahead Logging

[KBMS](../00-glossary/01-glossary.md#kbms) V3 tuân thủ nguyên tắc: **Mọi thay đổi phải được ghi vào Nhật ký trước khi được ghi vào Tệp dữ liệu chính**.

1. **Log First**: Trước khi một trang ([Page](../00-glossary/01-glossary.md#page)) bẩn được đẩy ([Flush](../00-glossary/01-glossary.md#flush)) từ [Buffer Pool](../00-glossary/01-glossary.md#buffer-pool) xuống tệp `.kdb`, hệ thống sẽ tạo một bản ghi `LogRecord` và ghi vào tệp `.wal`.
2. **Sequential Append**: Việc ghi nhật ký là thao tác ghi nối đuôi tuần tự (Sequential), giúp tối ưu hóa hiệu năng I/O trên cả ổ cứng HDD và SSD.
3. **Atomic Commit**: Một giao dịch chỉ được coi là hoàn tất khi cờ `Committed` trong file nhật ký được xác nhận là `true`.

---

## 2. Số tuần tự nhật ký (LSN - Log Sequence Number)

[LSN](../00-glossary/01-glossary.md#lsn) là chỉ số định danh thời gian cho mọi thay đổi:
- Mỗi khi một bản ghi nhật ký được tạo, `WalManagerV3` sẽ gán một LSN tăng dần.
- Mỗi trang dữ liệu lưu trữ LSN của thao tác thay đổi cuối cùng (tại Offset 4-7 trong [Header](../00-glossary/01-glossary.md#header)).
- **Quy tắc an toàn**: Một trang chỉ được phép ghi xuống đĩa nếu LSN của trang đó nhỏ hơn hoặc bằng LSN lớn nhất đã được ghi an toàn vào tệp `.wal`.

---

## 3. Quy trình Phục hồi sau sự cố

Khi KBMS khởi động lại sau một sự cố ngắt điện (Crash), `WalManagerV3` thực hiện quy trình **Undo-based Recovery** để đưa hệ thống về trạng thái nhất quán gần nhất:

![Sơ đồ Vòng đời Phục hồi Dữ liệu (Recovery Flow)](../assets/diagrams/recovery_flow.png)
*Hình 7.5: Cơ chế khôi phục tính nhất quán bằng cách hoàn tác (Undo) các giao dịch chưa hoàn tất.*

1. **Quét Nhật ký (Scanning)**: Hệ thống quét toàn bộ tệp `.wal` từ đầu đến cuối.
2. **Xác định giao dịch chưa Commit**: Với mỗi bản ghi, nếu cờ `Committed` là `false`, giao dịch đó được coi là "bị bỏ dở" do sự cố.
3. **Hoàn tác (Undo Phase)**: Hệ thống sử dụng **[Before-Image](../00-glossary/01-glossary.md#before-image)** (ảnh dữ liệu cũ trước khi thay đổi) từ bản ghi nhật ký để ghi đè ngược lại vào tệp `.kdb`.
4. **Kết quả**: Mọi thay đổi của các giao dịch chưa hoàn tất sẽ bị xóa bỏ, đảm bảo tính nguyên tử (Atomicity) của hệ thống.

---

## 4. Đặc tả nhị phân của [LogRecord]

Cấu trúc một bản ghi nhật ký trong KBMS V3 được thiết kế để chứa đủ thông tin cho cả việc **Undo** và **Redo**:

*Bảng 7.5: Cấu trúc nhị phân chi tiết của một đơn vị [LogRecord](../00-glossary/01-glossary.md#logrecord) trong file WAL.*

| Thành phần | Kiểu | Kích thước | Mô tả |
| :--- | :--- | :--- | :--- |
| **TxnId** | Guid | 16 Bytes | Định danh duy nhất cho giao dịch. |
| **PageId** | Int32 | 4 Bytes | ID của trang bị thay đổi. |
| **BeforeLen** | Int32 | 4 Bytes | Độ dài của dữ liệu Before-Image. |
| **BeforeImage** | Blob | Biến thiên | Ảnh dữ liệu cũ (Dùng để **Undo**). |
| **AfterLen** | Int32 | 4 Bytes | Độ dài của dữ liệu After-Image. |
| **AfterImage** | Blob | Biến thiên | Ảnh dữ liệu mới (Dùng để **Redo**). |
| **Committed** | Bool | 1 Byte | Cờ xác nhận giao dịch đã Commit. |


---

## 6. Minh họa Hex Dump: Một bản ghi nhật ký (LogRecord)

Dữ liệu thực tế được ghi vào tệp `.wal` khi một trang bị thay đổi được trích xuất từ engine (Ví dụ cho giao dịch thay đổi Page 1):

```text
Offset    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F    Annotation
----------------------------------------------------------------------------
00000000  F0 19 DE 23 55 B4 FA 4C 92 A8 E1 03 A1 D6 31 64    [ TransactionID ]
00000010  01 00 00 00 0A 00 00 00 00 11 22 33 44 55 66 77    [PgId][BLen][BImg]
00000020  08 99 0A 00 00 00 99 88 77 66 55 44 33 22 11 00    [ALen][AfterImage]
00000030  01                                                 [ Committed:True ]
```

- **TransactionID**: 16 bytes [GUID](../00-glossary/01-glossary.md#guid) định danh phiên làm việc.
- **PgId**: 4 bytes số nguyên (01 00 00 00 -> Page 1).
- **BLen/ALen**: 4 bytes độ dài của ảnh dữ liệu trước/sau (0A 00 00 00 -> 10 bytes).
- **Committed**: 1 byte boolean (01 -> True).

Để ngăn tệp `.wal` phình to vô hạn, định kỳ KBMS thực hiện quá trình [Checkpoint](../00-glossary/01-glossary.md#checkpoint):
1. **Flush Dirty Pages**: Ghi tất cả các trang bẩn từ Buffer Pool xuống tệp `.kdb`.
2. **Sync Log**: Đảm bảo mọi nhật ký liên quan đã được ghi xuống đĩa.
3. **Truncate**: Xóa bỏ hoặc đánh dấu rảnh các phần của tệp `.wal` đã được đồng bộ hóa an toàn, giúp giải phóng không gian lưu trữ và rút ngắn thời gian Recovery sau này.
