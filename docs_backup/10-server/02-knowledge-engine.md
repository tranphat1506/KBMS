# 10.2. Kiến trúc Điều phối Tri thức Trung tâm

`KnowledgeManager.cs` là thành phần hạt nhân, đóng vai trò điều phối toàn diện cho hệ thống [KBMS](../00-glossary/01-glossary.md#kbms). Thành phần này thiết lập sự liên kết giữa Tầng Phân tích Ngôn ngữ ([Parser](../00-glossary/01-glossary.md#parser)/[AST](../00-glossary/01-glossary.md#ast)) và Tầng Thực thi/Lưu trữ (Reasoning/Storage).

## 1. Cơ chế Điều phối Truy vấn (Query Dispatching)

Sau khi `KBMS.Parser` chuyển đổi câu lệnh truy vấn thành cấu trúc cây cú pháp trừu tượng (AST), `KnowledgeManager` thực hiện duyệt đệ quy các nút và kích hoạt phương thức `Execute(ast, user, currentKb)`.

```csharp
// Tiến trình thực thi lõi trong KnowledgeManager.cs
public object Execute(AstNode ast, User user, string? currentKb) {
    var kbName = DetermineKbName(ast) ?? currentKb;
    var action = DetermineAction(ast);
    
    if (!CheckPrivilege(user, action, kbName)) 
        return ErrorResponse.PermissionErrorResponse(action, kbName);
        
    return ExecuteQuery(ast, kbName);
}
```

### Chức năng chính:
*   **Định tuyến Tri thức (Knowledge Routing)**: Xác thực và điều hướng câu lệnh tới Cơ sở tri thức (KB) mục tiêu dựa trên ngữ cảnh phiên làm việc hoặc tham số định danh.
*   **Kiểm soát Quyền hạn (Access Control Logic)**: Sử dụng phương thức `CheckPrivilege` để thẩm định quyền của `User` (ROOT, ADMIN, WRITE, SELECT). Mọi yêu cầu không hợp lệ đều bị ngăn chặn trước khi tương tác với tầng dữ liệu.
*   **Đường ống Thực thi ([Execution Pipeline](../00-glossary/01-glossary.md#execution-pipeline))**: Phương thức `ExecuteQuery` đóng vai trò là bộ định tuyến logic cho hơn **50 loại nút AST**, bao gồm các cấu trúc định nghĩa (`CREATE_CONCEPT`) và các yêu cầu suy diễn (`SOLVE`).

---

## 2. Các Công nghệ Liên kết và Tối ưu hóa Tri thức

Hệ thống điều phối của KBMS V3 tích hợp các cơ chế tối ưu hóa hiện đại nhằm đảm bảo hiệu năng thực thi:

### 2.1. Tự động Phân rã Biến (Auto-expand Variables)
Đối với các [Concept](../00-glossary/01-glossary.md#concept) có cấu trúc lồng nhau (Nested Concepts), `KnowledgeManager` thực hiện quy trình phân rã tự động các thuộc tính phức tạp thành các biến nguyên thủy.
*   **Hiệu quả**: Cho phép mạng lưới Rete (`ReteNetwork`) thực hiện lan truyền dữ kiện trực tiếp trên các biến cơ sở mà không cần chi phí phân tích lại cấu trúc đối tượng tại thời điểm thực thi.

### 2.2. Đăng ký và Kích hoạt Trình kích hoạt (Triggers Registry)
`KnowledgeManager` quản lý một danh sách các **[Trigger](../00-glossary/01-glossary.md#trigger)** thuộc về mô hình tri thức. Ngay sau khi các thao tác đột biến dữ liệu (`INSERT`, `UPDATE`, `DELETE`) được xác nhận, hệ thống sẽ kích hoạt hàm `FireTriggers`, tạo ra các luồng lan truyền trạng thái gia tăng trong mạng lưới Rete.

### 2.3. Điều phối Dữ liệu V3 (V3 Data Router)
Quá trình tương tác vật lý được trừu tượng hóa qua thành phần `V3DataRouter`. Router này chịu trách nhiệm định vị và quản lý luồng dữ liệu nhị phân trên các tệp tin lưu trữ phân tán, hỗ trợ kiến trúc đa cơ sở tri thức (Multi-KB).

---

## 3. Kiểm soát Giao dịch và Toàn vẹn (Transaction Control)

Bộ điều phối hỗ trợ đầy đủ nhóm ngôn ngữ kiểm soát giao dịch ([TCL](../00-glossary/01-glossary.md#tcl)) nhằm bảo vệ tính toàn vẹn của tri thức:
*   **BEGIN TRANSACTION**: Khởi tạo ngữ cảnh giao dịch và bộ đệm thay đổi (txBuffer).
*   **COMMIT**: Thực thi quy trình đồng bộ dữ liệu bền vững qua cơ chế Ghi trước nhật ký (WAL). 
*   **ROLLBACK**: Hủy bỏ các thay đổi tạm thời, đưa trạng thái mạng lưới tri thức về điểm nhất quán gần nhất.

![Sơ đồ tương tác giữa Knowledge Manager và các phân hệ cấp thấp](../assets/diagrams/knowledge_manager_v3.png)
*Hình 10.2: Sơ đồ tương tác giữa [Knowledge Manager](../00-glossary/01-glossary.md#knowledge-manager) và các phân hệ thực thi cấp dưới.*
