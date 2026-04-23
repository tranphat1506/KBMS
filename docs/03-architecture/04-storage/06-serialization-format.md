# Tuần tự hóa Siêu dữ liệu và Tri thức

Cơ sở tri thức (Knowledge Base) không chỉ chứa dữ liệu thực thể mà còn bao gồm các định nghĩa trừu tượng như Khái niệm (Concepts) và Luật dẫn (Rules). Hệ thống KBMS chuyển đổi các đối tượng này thành chuỗi nhị phân thu nhỏ (Tuples) để lưu trữ hiệu quả.

## 4.4.11. Bố cục Nhị phân của Thực thể (Tuple Layout)

Khi một thực thể tri thức được chèn vào hệ thống, nó được phân tách thành các trường dữ liệu cố định và biến thiên:

*Bảng 4.2: Đặc tả bố cục nhị phân của một thực thể tri thức (Tuple)*
| Trường (Field) | Loại dữ liệu | Kích thước | Mô tả |
| :--- | :--- | :--- | :--- |
| **Header** | `Int32` | 4 - 8B | Số lượng trường và các con trỏ offset. |
| **Fixed Data** | `GUID / Int` | 16B - 4B | Các định danh hoặc giá trị số cố định. |
| **Variable Data** | `LPS String` | Biến thiên | Các chuỗi ký tự hoặc dữ liệu nhị phân (Blobs). |

## 4.4.12. Ví dụ Phân rã Hex: Siêu dữ liệu Concept

Siêu dữ liệu cho một Khái niệm (Concept) bao gồm danh sách các biến và ràng buộc. Dưới đây là mô phỏng 48 byte đầu tiên của một Tuple Concept:

```text
Offset    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F    Giải thích
----------------------------------------------------------------------------
00000000  0D 00 ...  (FieldCount = 13)
          [... 26 byte mảng Offset (13 trường × 2 byte) ...]

; Trường 0 — Concept ID (GUID, 16 byte)
00000026  AA AA AA AA 00 00 00 00 00 00 00 00 00 00 00 01

; Trường 2 — Tên "TamGiac" (LPS: 1 byte độ dài + 7 byte UTF-8)
00000046  07 54 61 6D 47 69 61 63    → (len=7) "TamGiac"

; Trường 3 — Danh sách biến (Variables)
0000004E  03 00 00 00               → Số lượng (Count) = 3
00000052  01 61 03 69 6E 74 00 00   → "a", "int", HasLen=false
```

### Phân tích định dạng Nhị phân (Serialization Logic)

Ví dụ trên cho thấy cách `ModelBinaryUtility` nén các đối tượng tri thức phức tạp thành chuỗi các Byte liên tục:

1.  **FieldCount (Byte 0-1)**: Giá trị `0D 00` báo hiệu đối tượng này có 13 trường dữ liệu. Điều này cho phép `Deserializer` biết trước cần phải đọc bao nhiêu Offset trong mảng con trỏ ngay sau Header.
2.  **LPS (Length-Prefixed String)**: Tại Offset 46, byte đầu tiên `07` xác định độ dài của chuỗi ký tự theo sau là 7. Sau đó là 7 byte mã UTF-8 `54 61 6D 47 69 61 63` (`TamGiac`). Cách làm này giúp đọc chuỗi cực nhanh mà không cần quét tìm ký tự kết thúc `\0`.
3.  **Hệ thống Phụ lục (Sub-blobs - Byte 52 trở đi)**: Các danh sách lồng nhau (như danh sách biến `a, b, c`) được tuần tự hóa đệ quy. Bốn byte `03 00 00 00` xác định có 3 biến. Mỗi biến lại có cấu trúc LPS riêng cho Tên và Kiểu dữ liệu.

Cấu trúc này tối ưu hóa việc lưu trữ vì nó loại bỏ hoàn toàn các chuỗi ký tự mô tả thuộc tính (như tên cột trong SQL), giúp giảm kích thước file `.kdb` xuống chỉ còn khoảng 20% so với định dạng JSON.

### Ưu điểm của Định dạng Nhị phân (Binary Utility)
-   **Kích thước tối thiểu**: Loại bỏ các thuộc tính dư thừa của JSON/XML.
-   **Tốc độ bóc tách**: `ModelBinaryUtility` có thể giải mã trực tiếp từ con trỏ bộ nhớ (Memory Pointer), giảm thiểu chi phí khởi tạo đối tượng (Object Allocation).
-   **Tính tương thích**: Đảm bảo cấu trúc dữ liệu không thay đổi khi di chuyển giữa các phân hệ Server và Storage.
