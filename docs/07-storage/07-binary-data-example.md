# 07.7. Thực tế hóa: Từ Mô hình đối tượng sang Chuỗi nhi phân

Để kết nối lý thuyết mô hình (Chương 5) và hạ tầng lưu trữ (Chương 7), chúng ta sẽ theo dõi lộ trình của một đối tượng cụ thể đi qua các tầng xử lý của KBMS.

---

## 1. Đối tượng đầu vào (C# Layer)

Giả sử người dùng nhập vào một thực thể của khái niệm `TamGiac` (đã định nghĩa ở mục 5.7):

```csharp
var t1 = new ObjectInstance {
    Id = new Guid("550e8400-e29b-41d4-a716-446655440000"),
    ConceptName = "TamGiac",
    Values = new Dictionary<string, object> {
        { "a", 3 }, { "b", 4 }, { "c", 5 }, { "p", 6 }, { "S", 6 }
    }
};
```

---

## 2. Tuần tự hóa thành Tuple (Serialization)

Khi thực hiện lệnh `INSERT`, `V3DataRouter` sẽ chuyển đổi đối tượng trên thành khối nhị phân `Tuple` với cấu trúc sau:

| Field | Nội dung | Kiểu | Kích thước |
| :--- | :--- | :--- | :--- |
| **0** | `550e8400-e29b-41d4-a716-446655440000` | GUID | 16 Bytes |
| **1** | `a|b|c|p|S` | UTF-8 String | 9 Bytes |
| **2** | `3` | UTF-8 String | 1 Byte |
| **3** | `4` | UTF-8 String | 1 Byte |
| **4** | `5` | UTF-8 String | 1 Byte |
| **5** | `6` | UTF-8 String | 1 Byte |
| **6** | `6` | UTF-8 String | 1 Byte |

**Tính toán kích thước Tuple:**
-   **Header:** 2B (FieldCount) + $7 \times 2B$ (Offsets) = **16 Bytes**.
-   **Payload:** $16 + 9 + 1 + 1 + 1 + 1 + 1$ = **30 Bytes**.
-   **Tổng cộng:** $16 + 30 = \mathbf{46}$ **Bytes**.

---

## 3. Bản đồ vị trí trong Trang (Slotted Page Mapping)

Giả sử `Tuple` này được chèn vào một trang trống (`PageId=101`).

1.  **Header (24B):** Cập nhật `TupleCount = 1`, `FreeSpacePointer = 16338` ($16384 - 46$).
2.  **Slot Array:** Slot đầu tiên (Index 0) chứa giá trị `[Offset: 16338, Length: 46]`.
3.  **Data Area:** 46 bytes dữ liệu nằm ở cuối trang (từ vị trí 16338 đến 16383).

![Trạng thái trang sau khi chèn](../assets/diagrams/kbms_core_tupl.png)
*Hình 7.6: Minh họa vị trí thực tế của đối tượng TamGiac trong bộ nhớ nhị phân.*

---

## 4. Biểu diễn dưới dạng mã Hex (Hex Dump Trace)

Dưới đây là mô phỏng 64 bytes đầu tiên của trang dữ liệu trên đĩa (đã giải mã AES):

```text
Offset    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F    Decoded
-------------------------------------------------------------------------
00000000  65 00 00 00 01 00 00 00 FF FF FF FF FF FF FF FF    e...............
          [ PageId: 101 ] [ LSN: 1  ] [ PrevPageId: -1  ]
00000010  FF FF FF FF D2 3F 00 00 01 00 00 00 D2 3F 00 00    .....?.......?..
          [ NextPage: -1] [ FSP:16338] [ Count: 1 ] [Slot0: Off=16338, Len=46]
```

> [!NOTE]
> Kết quả này cho thấy sự chính xác tuyệt đối trong việc ánh xạ dữ liệu: từ một đối tượng trừu tượng trong lập trình C#, KBMS đã "vắt" nó thành các con số nhị phân chính xác đếm từng byte để lưu trữ bền vững.
