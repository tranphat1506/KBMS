# 07.4. Ánh xạ Mô hình sang Dữ liệu nhị phân (Model Serialization)

Cỗ máy lưu trữ đóng vai trò là xương sống bền vững cho [KBMS](../00-glossary/01-glossary.md#kbms), thực hiện việc chuyển đổi hai chiều giữa các thực thể lập trình (C# Objects) và các khối nhị phân lưu trữ trên đĩa (Binary Pages).

## 1. Chiến lược Tuần tự hóa Nhị phân Thuần túy

Hệ thống áp dụng chiến lược **nhị phân thống nhất (Unified [Tuple](../00-glossary/01-glossary.md#tuple)-based [Serialization](../00-glossary/01-glossary.md#serialization))** cho toàn bộ các lớp dữ liệu. Mọi thực thể (Dữ liệu đối tượng) và siêu dữ liệu (Lược đồ) đều được đóng gói vào cấu trúc `Tuple` nhị phân có bảng chỉ dẫn Offset để tối ưu hóa hiệu năng truy xuất ngẫu nhiên.

*Bảng 7.3: Chiến lược lưu trữ nhị phân thống nhất trong [KBMS](../00-glossary/01-glossary.md#kbms).*

| Nhóm dữ liệu | Cấu trúc bộ nhớ | Cơ chế xử lý |
| :--- | :--- | :--- |
| **Thực thể (Object)** | [Tuple](../00-glossary/01-glossary.md#tuple) (Offset-based) | Định dạng chuỗi UTF-8 tương thích schema |
| **Siêu dữ liệu (Meta)** | [Tuple](../00-glossary/01-glossary.md#tuple) (Offset-based) | Đóng gói trực tiếp qua ModelBinaryUtility |


---

## 2. Đặc tả Cấu trúc Tuple cho các lớp dữ liệu

Tất cả các bản ghi trong [KBMS](../00-glossary/01-glossary.md#kbms) (từ đối tượng người dùng đến định nghĩa tri thức) đều được lưu trữ trong một đơn vị `Tuple`. Cấu trúc này gồm 3 phần: **Field Count** (2B), **Offsets Array** ($N \times 2B$), và **Data Payload**.

Dưới đây là ánh xạ chi tiết các trường dữ liệu cho từng loại mô hình:

### A. Layout cho ObjectInstance
*Bảng 7.4: Layout nhị phân của [ObjectInstance](../00-glossary/01-glossary.md#objectinstance).*

| F.Idx | Thuộc tính | Kiểu | Mô tả |
| :--- | :--- | :--- | :--- |
| 0 | Id | [GUID](../00-glossary/01-glossary.md#guid) | Định danh duy nhất của đối tượng. |
| 1 | Schema | LPS | Danh sách biến (vd: "a\|b\|c"). |
| 2..N | Values | LPS | Giá trị các biến tương ứng dưới dạng chuỗi UTF-8. |


### B. Layout cho KnowledgeBase
*Bảng 7.5: Layout nhị phân của KnowledgeBase (KB).*

| F.Idx | Thuộc tính | Kiểu | Mô tả tóm tắt |
| :--- | :--- | :--- | :--- |
| 0 | Id | [GUID](../00-glossary/01-glossary.md#guid) | Định danh duy nhất. |
| 1 | OwnerId | [GUID](../00-glossary/01-glossary.md#guid) | ID người sở hữu. |
| 2 | Name | LPS | Tên KB (UTF-8). |
| 3 | CreatedAt | Long | Ticks thời gian. |
| 4 | Description| LPS | Mô tả nội dung. |
| 5 | ObjectCount| Int32 | Tổng số đối tượng. |
| 6 | RuleCount  | Int32 | Tổng số luật. |
| 7 | Rules       | Blob | List\<Rule\>. |
| 8 | Relations   | Blob | List\<Relation\>. |
| 9 | Operators   | Blob | List\<Operator\>. |
| 10| Functions   | Blob | List\<Function\>. |
| 11| [Hierarchies](../00-glossary/01-glossary.md#hierarchies) | Blob | List\<Hierarchy\>. |


### C. Layout cho [Concept]
*Bảng 7.9: Layout Toán tử (Operator) & Hàm (Function).*

| F.Idx | Thuộc tính | Kiểu | Mô tả nội dung |
| :--- | :--- | :--- | :--- |
| 0 | Id | [GUID](../00-glossary/01-glossary.md#guid) | Định danh [Concept](../00-glossary/01-glossary.md#concept). |
| 1 | KbId | [GUID](../00-glossary/01-glossary.md#guid) | ID KB chứa. |
| 2 | Name | LPS | Tên khái niệm. |
| 3 | Variables | Blob | Danh sách biến số. |
| 4 | Constraints | Blob | Các ràng buộc. |
| 5 | CompRels | Blob | Quan hệ tính toán. |
| 6-12 | Others | Blob| Aliases, Properties, Equations, Rules... |


---

## 4. Cấu trúc Sub-blob đệ quy

Đối với các cấu trúc phức tạp như mô hình phân triển hoặc cây biểu thức, [KBMS](../00-glossary/01-glossary.md#kbms) không sử dụng văn bản mà mã hóa đệ quy trực tiếp:

1.  **Node Entry**: Ghi loại nút (vd: Operator, Variable, Constant) và giá trị của nút đó.
2.  **Child Count**: Ghi số lượng nhánh con phát sinh từ nút hiện tại.
3.  **Recursion**: Tiếp tục ghi các nút con theo thứ tự **[Pre-order](../00-glossary/01-glossary.md#pre-order) traversal**.

Cơ chế này giúp bộ máy suy diễn ([Inference Engine](../00-glossary/01-glossary.md#inference-engine)) tái cấu trúc lại cây biểu thức trong bộ nhớ RAM mà không cần thực hiện bước phân tích cú pháp (Parsing), giúp tăng tốc độ suy diễn lên gấp 5-10 lần so với phương pháp truyền thống.

---

## 4. Cơ chế Phục hồi Kiểu dữ liệu (Hydration & Casting)

Vì tầng lưu trữ nhị phân xử lý dữ liệu thô (Raw bytes/Strings) để đảm bảo tốc độ, việc khôi phục các kiểu dữ liệu logic (Strong Typing) được thực hiện ở tầng `V3DataRouter`:

1.  **Giải nén:** Đọc `Tuple` từ trang `SlottedPage`, tách các trường dựa trên bảng Offset.
2.  **Khớp dữ liệu:** Lấy danh sách tên biến từ Trường 1 (Schema).
3.  **Ép kiểu ([Late-Binding](../00-glossary/01-glossary.md#late-binding) [Casting](../00-glossary/01-glossary.md#casting)):** Đối chiếu với định nghĩa của `Concept` hiện tại để chuyển đổi chuỗi sang kiểu dữ liệu đích:
- `INT`/`LONG` $\rightarrow$ `long.Parse()`
- `FLOAT`/`DOUBLE` $\rightarrow$ `double.Parse()`
- `BOOLEAN` $\rightarrow$ `bool.Parse()`
- `DECIMAL` $\rightarrow$ `decimal.Parse()`
4.  **Xử lý tiến hóa:** Nếu [Concept](../00-glossary/01-glossary.md#concept) có thêm biến mới chưa có trong dữ liệu cũ, giá trị `null` sẽ được gán tự động để bảo toàn logic suy diễn.

![Quy trình Serialization](../assets/diagrams/io_strategy.png)
*Hình 7.3: Luồng dữ liệu qua các tầng xử lý từ Model đến Disk.*
