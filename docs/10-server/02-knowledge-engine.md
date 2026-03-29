# Kiến trúc Bộ não Điều phối (Knowledge Engine)

`KnowledgeManager.cs` là thành phần trung tâm, đóng vai trò "Bộ não" của toàn bộ hệ thống KBMS. Nó kết nối giữa Tầng Ngôn ngữ (Parser/AST) và Tầng Lưu trữ/Suy diễn (Storage/Reasoning).

## 1. Cơ chế Bộ hạ lệnh (The Dispatcher)

Sau khi `KBMS.Parser` bẻ gãy câu lệnh thành một cây cú pháp (AST), `KnowledgeManager` sẽ thực hiện đệ quy qua danh sách các nút này và gọi hàm `Execute(ast, user, currentKb)`.

```csharp
// Luồng thực thi lõi trong KnowledgeManager.cs
public object Execute(AstNode ast, User user, string? currentKb) {
    var kbName = DetermineKbName(ast) ?? currentKb;
    var action = DetermineAction(ast);
    
    if (!CheckPrivilege(user, action, kbName)) 
        return ErrorResponse.PermissionErrorResponse(action, kbName);
        
    return ExecuteQuery(ast, kbName);
}
```

### Chức năng chính:
*   **Routing (Định tuyến)**: Xác định lệnh này thuộc về Knowledge Base (KB) nào dựa trên từ khóa `USE` hoặc tham số câu lệnh.
*   **Security Guard (Bảo vệ)**: Sử dụng `CheckPrivilege` để lọc quyền của `User` hiện tại (ROOT, ADMIN, WRITE, SELECT). Mọi hành động xâm phạm đều bị chặn ngay tại đây trước khi chạm vào dữ liệu.
*   **Execution Pipeline**: Hàm `ExecuteQuery` chứa một bộ chuyển mạch (Switch-case) khổng lồ điều hướng hơn **50 loại nút AST** khác nhau (từ `CREATE_CONCEPT` đến `SOLVE`).

---

## 2. Các Công nghệ Liên kết Tri thức (Knowledge Binding)

Hệ thống điều phối của KBMS V3 mang tính đột phá nhờ các cơ chế sau:

### 2.1. Auto-expand Variables (Tự nở rộng biến)
Khi bạn khai báo một Concept chứa thuộc tính là một Concept khác (Ví dụ: `p: Point`), `KnowledgeManager` sẽ tự động "nở" biến này thành các thuộc tính con (`p.x`, `p.y`). 
*   **Lợi ích**: Giúp bộ máy suy diễn (`ReasoningEngine`) có thể truy cập trực tiếp vào các biến cơ sở mà không cần phải parse lại cấu trúc object phức tạp (Flattening process).

### 2.2. Triggers Registry
`KnowledgeManager` duy trì một danh sách các **Trigger** đang hoạt động. Ngay sau khi một lệnh `INSERT`, `UPDATE` hoặc `DELETE` thành công, bộ điều phối sẽ tự động gọi hàm `FireTriggers` để kích hoạt các phản ứng dây chuyền (Chain reactions) trong tri thức.

### 2.3. V3 Data Router
Bộ điều phối không còn truy cập file trực tiếp. Nó giao tiếp thông qua `V3DataRouter`. Router này chịu trách nhiệm xác định vị trí vật lý của dữ liệu trên hàng chục tệp tin `.kbf`, `.dat` khác nhau, giúp KBMS mở rộng quy mô dữ liệu theo kiến trúc Multi-Database.

---

## 3. Quản lý Giao dịch (Transaction Control)

Khác với các hệ thống đơn giản, bộ điều phối của bạn hỗ trợ **Transaction Control Language (TCL)**:
*   **BEGIN TRANSACTION**: Bật cờ `inTransaction` và khởi tạo bộ đệm (`txBuffer`).
*   **COMMIT**: Phóng toàn bộ dữ liệu từ Buffer Pool xuống đĩa qua WAL. 
*   **ROLLBACK**: Xóa sạch bộ đệm, khôi phục trạng thái tri thức về điểm an toàn trước đó.

> [!IMPORTANT]
> **Sự kết hợp hoàn hảo**
> Toàn bộ logic tri thức được đóng gói trong `KnowledgeManager.cs`. Đây là nơi điều phối việc tạo cơ sở tri thức, nạp/xả Concept vào Buffer Pool và kích hoạt Reasoning Engine.

![knowledge_manager_v3.png](../assets/diagrams/knowledge_manager_v3.png)
*Hình 10.2: Sơ đồ tương tác giữa Knowledge Manager và các phân hệ cấp thấp.*
