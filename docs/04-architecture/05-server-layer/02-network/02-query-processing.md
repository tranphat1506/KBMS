# 4.5.2.2. Vòng đời Xử lý Truy vấn (Query Lifecycle)

Trong kiến trúc phân tầng của KBMS, sau khi gói tin nhị phân được giải mã tại Tầng Mạng (Phân hệ 4.5.2.1), Server khởi động một quy trình xử lý đa tầng để phiên dịch ý định người dùng (User Intent) thành các phép toán và suy diễn tri thức cụ thể. Quy trình này đảm bảo tính nhất quán từ lúc tiếp nhận luồng byte cho đến khi trả về tập kết quả cuối cùng.

## 1. Vòng đời của một Truy vấn (Request Flow)

Mọi yêu cầu được chuẩn hóa theo một đường ống xử lý (Pipeline) nghiêm ngặt để tối ưu hóa tài nguyên máy chủ:

![Vòng đời xử lý truy vấn xuyên suốt 4 tầng kiến trúc](../../../assets/diagrams/query_lifecycle_v3.png)
*Hình 4.xx: Sơ đồ trình tự vòng đời của một truy vấn (Query Lifecycle).*

---

## 2. Luồng xử lý Packet nhị phân

Hệ thống thực hiện bóc tách các byte dữ liệu thô từ luồng (Stream) và chuyển đổi chúng thành các cấu trúc dữ liệu logic:

1.  **Phân rã Header**: Xác định loại thông điệp và kích thước Payload.
2.  **Định danh Phiên**: Gắn kết gói tin với một `Session` cụ thể trong `ConnectionManager.cs`.
3.  **Trích xuất Payload**: Payload (thường là mã nguồn KBQL) được giải mã UTF-8 và chuyển giao cho bộ phận phân tích ngôn ngữ.

---

## 3. Phân rã và Giải mã Cú pháp (Parsing & AST)

Đây là giai đoạn then chốt trong việc chuyển đổi ý định người dùng sang thực thể lập trình được:
*   **Phân tích Từ vựng (Lexing)**: Quét chuỗi ký tự thô để nhận diện các Tokens (Từ khóa, Định danh, Toán tử).
*   **Xây dựng Cây AST**: Sử dụng kỹ thuật **Recursive Descent** để kiểm tra tính hợp lệ của ngữ pháp và xây dựng cây cú pháp trừu tượng (Abstract Syntax Tree). 

---

## 4. Điều phối và Thực thi (Dispatching)

Sau khi có cấu trúc AST hoàn chỉnh, `KnowledgeManager.cs` thực hiện vai trò điều phối:
1.  **Thẩm định Quyền (RBAC)**: Kiểm tra quyền thực thi (`SELECT`, `INSERT`, `SOLVE`) dựa trên thông tin người dùng trong Session.
2.  **Định tuyến Logic**: Chuyển giao AST tới `StorageEngine` (để truy xuất/ghi dữ liệu vật lý) hoặc `ReasoningEngine` (để lan truyền suy diễn trên mạng Rete).
3.  **Hồi đáp (Response Coding)**: Kết quả từ các tầng dưới được Module I đóng gói ngược lại vào các khung nhị phân (`METADATA`, `ROW`, `FETCH_DONE`) để gửi về cho khách hàng.

## 5. Ví dụ thực tế: Luồng xử lý lệnh SELECT

Khi người dùng thực thi: `SELECT name FROM Student WHERE age > 18;`

1.  **Lớp Network**: Nhận gói tin `TYPE:QUERY`, bóc tách payload chuỗi.
2.  **Lớp Parser**: Tạo đối tượng `SelectAstNode` chứa thông tin về bảng `Student` và điều kiện lọc.
3.  **Lớp Core**: Xác minh người dùng có quyền `READ` trên bảng `Student`.
4.  **Lớp Engine**: Thực hiện tìm kiếm trên chỉ mục B+ Tree của `Student`, lọc dữ liệu và trả về các dòng thỏa mãn.

Giai đoạn giải mã mạng và điều phối này chỉ là bước khởi đầu. Để thực sự hiểu ý nghĩa sâu xa của câu lệnh người dùng, hệ thống cần đi sâu vào Phân hệ II — nơi các chuỗi văn bản vô hồn được bẻ gãy thành các nguyên tử ngôn ngữ (Tokens) và cấu trúc cây logic.