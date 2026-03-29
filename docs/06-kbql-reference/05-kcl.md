# 06.5. Kiểm soát truy cập (KCL)

KCL là tập hợp các lệnh dùng để quản lý hệ thống bảo mật, tài khoản và quyền hạn truy cập của người dùng trong KBMS.

---

## 1. Quản lý Người dùng (USER Management)

KBMS cho phép định cấu hình người dùng và vai trò để bảo vệ tri thức khỏi các truy cập không mong muốn.

### Tạo Người dùng mới (CREATE USER)
```kbql
CREATE USER <username> 
PASSWORD '<password>' 
[ROLE {ADMIN|SERVICE|USER}];
```

### Chỉnh sửa Người dùng (ALTER USER)
Sử dụng để đổi mật khẩu hoặc trạng thái quản trị.
```kbql
ALTER USER <username> (
    SET (PASSWORD: '<new_pass>', ADMIN: true)
);
```

### Xóa Người dùng (DROP USER)
```kbql
DROP USER <username>;
```

---

## 2. Quản trị Quyền hạn (GRANT / REVOKE)

Sử dụng để cấp hoặc thu hồi quyền thao tác trên các Concept và Tri thức cụ thể.

### Cấp quyền (GRANT)
```kbql
GRANT {SELECT, INSERT, UPDATE, DELETE, ...} 
ON CONCEPT <name> 
TO <username>;
```

### Thu hồi quyền (REVOKE)
```kbql
REVOKE {SELECT, INSERT, UPDATE, DELETE, ...} 
ON CONCEPT <name> 
FROM <username>;
```

---

## 3. Các thực thể Quản trị đặc biệt (System Admin)

*   **ADMIN**: Có toàn quyền trên tất cả các KB, Concept và người dùng khác.
*   **SERVICE**: Có quyền truy cập vào các API hệ thống nhưng không thể thay đổi cấu trúc Metadata.
*   **USER**: Quyền hạn mặc định, cần được cấp phép cụ thể cho từng đối tượng.
