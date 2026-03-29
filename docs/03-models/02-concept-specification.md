# 02. Đặc tả Khái niệm (Concept Specification)

Khái niệm (**Concept**) là thành phần định nghĩa cấu trúc của tri thức. Trong KBMS, việc định nghĩa Concept được thực hiện qua ngôn ngữ KDL.

### Ví dụ Thực tế (KDL):
```sql
CREATE CONCEPT Product (
    VARIABLES (
        id: INT,
        name: STRING,
        price: DECIMAL,
        stock: INT
    )
);
```

### Giao diện Thiết kế Concept (Studio):

![Placeholder: Ảnh chụp màn hình KBMS Studio đang định nghĩa Concept Product với Monaco Editor và cây thư mục tri thức bên trái](../assets/diagrams/placeholder_studio_concept_editor.png)

## 2. Xác thực Lược đồ (Schema Validation)

Mọi Concept khi được tạo đều qua bộ kiểm tra `SchemaV3Tests` để đảm bảo không có sự trùng lặp tên biến và kiểu dữ liệu hợp lệ.

---

> [!TIP]
> **Kiểm thử Module thực tế**: Khi chạy `dotnet test`, tệp `SchemaV3Tests` sẽ thực thi việc tạo ảo hàng trăm khái niệm để kiểm tra giới hạn của hệ thống.
