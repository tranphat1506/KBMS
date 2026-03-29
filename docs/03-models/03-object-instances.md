# 03. Quản lý Thực thể và Sự thật (Object & Facts)

Thực thể (**Object**) là các thể hiện cụ thể của một Concept. 

### Khởi tạo đối tượng từ bộ kiểm thử:
```sql
INSERT INTO Product ATTRIBUTE (501, 'Laptop', 1200.0, 10, 'Electronics');
```

### Đồ thị Tri thức (Knowledge Graph):

![Placeholder: Ảnh chụp màn hình đồ thị Knowledge Graph trong Studio, hiển thị thực thể Alice (Emp) kết nối với Engineering (Dept)](../assets/diagrams/placeholder_knowledge_graph_view.png)

## 2. Kiểm thử Dữ liệu (True Typing)

Cơ chế `True Typing` đảm bảo dữ liệu khi nạp vào phải khớp hoàn toàn với định nghĩa Concept. 

| Tệp Kiểm thử | Mô tả Case | Kết quả |
| :--- | :--- | :--- |
| `TrueTypingTests.cs` | Chèn sai kiểu dữ liệu (Double vào INT). | **Error Caught** |
| `DataOperationsV3Tests.cs` | Chèn 10,000 thực thể liên tục. | **Success** |

![Placeholder: Ảnh chụp nhật ký log (Debug log) của Server khi thực hiện chèn dữ liệu khối lượng lớn (Bulk Insert)](../assets/diagrams/placeholder_server_bulk_insert_log.png)
