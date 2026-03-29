Mỗi khi người dùng gửi một câu lệnh KBQL từ Client, Server sẽ thực hiện một quy trình xử lý đa tầng để hiểu ý định (Intent) và thực thi đúng các phép toán/suy diễn.

## 1. Vòng đời của một Truy vấn (Query Lifecycle)

Dưới đây là sơ đồ tuần tự mô tả các bước từ khi Client gửi câu lệnh đến khi nhận được kết quả cuối cùng:

![diagram_88d6101b.png](../assets/diagrams/diagram_88d6101b.png)
*Hình: diagram_88d6101b.png*

---

## 2. Luồng xử lý Packet (Packet Processing Flow)

Hệ thống xử lý các byte dữ liệu thô từ Stream và chuyển đổi chúng thành các cấu trúc thông điệp có ý nghĩa:

![2. Luồng xử lý Packet (Packet Processing Flow)](../assets/diagrams/diagram_95017569.png)
*Hình: 2. Luồng xử lý Packet (Packet Processing Flow)*

---

## 1. Phân tích Từ vựng (Lexical Analysis)

**Ý tưởng:** Biến chuỗi văn bản (String) thô thành một danh sách các tín hiệu có nghĩa (Tokens).
*   **Thuật toán Tokenizer:** Quét chuỗi ký tự, nhận diện các từ khóa (`SELECT`, `CREATE`), định danh (tên Concept), toán tử và giá trị số/chuỗi.
*   **Báo lỗi:** Nếu gặp ký tự lạ, Tokenizer ngay lập tức trả về lỗi kèm vị trí chính xác (Dòng, Cột).

---

## 2. Phân tích Cú pháp (Parsing & AST)

**Ý tưởng:** Xây dựng cây cú pháp trừu tượng (Abstract Syntax Tree - AST) từ danh sách Tokens.
*   **Kỹ thuật:** Sử dụng trình phân tích cú pháp **Recursive Descent** (Phân tích đi xuống đệ quy) để khớp các Tokens với bộ ngữ pháp của KBQL.
*   **Cấu trúc AST:** Mỗi câu lệnh được biểu diễn thành một cây các đối tượng. Ví dụ, lệnh `SELECT` sẽ có các nhánh con là `Fields`, `Source`, `WhereCondition`, và `Limit`.

---

## 3. Liên kết và Kiểm tra (Binding & Semantic Analysis)

Sau khi có AST, hệ thống tiến hành kiểm tra ý nghĩa của câu lệnh:
1.  **Catalog Check:** Concept được truy vấn có tồn tại không?
2.  **Schema Check:** Các biến (Variables) trong phần `SELECT` có thuộc về Concept đó hay không?
3.  **Type Check:** Các phép toán trong `CALC()` có hợp lệ về kiểu dữ liệu hay không?

---

## 4. Lập kế hoạch Thực thi (Query Execution Plan)

Hệ thống quyết định cách thức thực hiện truy vấn tối ưu nhất:
*   **Access Pattern:** Sử dụng Index (B+ Tree) hay quét tuần tự (Full Scan)?
*   **Join Strategy:** Áp dụng phương pháp JOIN nào để kết hợp các Khái niệm?
*   **Reasoning Injection:** Nếu truy vấn yêu cầu suy diễn (`INFER` hoặc truy cập các biến suy diễn), hệ thống sẽ tiêm (Inject) thêm các bước gọi vào `ReasoningEngine` trước khi trả kết quả cuối cùng.

---

## 5. Ví dụ luồng xử lý thực tế

Câu lệnh: `SELECT name FROM Student WHERE age > 18;`

1.  **Lexer:** `[SELECT, name, FROM, Student, WHERE, age, >, 18, ;]`
2.  **Parser:** Tạo đối tượng `SelectStatement` với `Table=Student`, `Filter=(age > 18)`.
3.  **Binder:** Xác nhận `Student` có trong KB, `name` và `age` là hợp lệ.
4.  **Executor:**
    *   Gọi `StorageEngine` để lấy dữ liệu từ B+ Tree của `Student`.
    *   Sử dụng `ReasoningEngine` để lọc các bản ghi có `age > 18`.
    *   Chỉ trả về trường `name` cho phía Network Layer.
