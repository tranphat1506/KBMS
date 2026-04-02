# Ngôn ngữ Kiểm soát và Quản trị Tri thức (KCL)

**KCL** (Knowledge Control Language) tập hợp các lệnh để quản lý hệ thống bảo mật, tài khoản người dùng và phân quyền truy cập thông tin trong cơ sở tri thức.

## 1. Cơ chế Quản lý Tài khoản Người dùng

Hệ thống cho phép cấu hình định danh và vai trò để bảo vệ dữ liệu tri thức khỏi các truy cập không hợp lệ.

### 1.1. Khởi tạo Người dùng mới (CREATE USER)

```kbql
CREATE USER <username> 
PASSWORD '<password>' 
[ROLE {ADMIN | SERVICE | USER}];
```

### 1.2. Hiệu chỉnh Thông tin Tài khoản (ALTER USER)

Lệnh `ALTER USER` hỗ trợ thay đổi mật khẩu hoặc trạng thái quản trị của tài khoản:

```kbql
ALTER USER <username> (
    SET (PASSWORD: '<new_password>', ADMIN: true)
);
```

### 1.3. Loại bỏ Tài khoản (DROP USER)

```kbql
DROP USER <username>;
```

## 2. Quản trị Quyền hạn và Phân quyền

Cơ chế phân quyền cho phép giới hạn khả năng thao tác của người dùng trên các Khái niệm ([Concept](../../00-glossary/01-glossary.md#concept)) và thực thể tri thức cụ thể.

### 2.1. Cấp quyền (GRANT)

```kbql
GRANT {SELECT, INSERT, UPDATE, DELETE, ...} 
ON CONCEPT <concept_name> 
TO <username>;
```

### 2.2. Thu hồi quyền (REVOKE)

```kbql
REVOKE {SELECT, INSERT, UPDATE, DELETE, ...} 
ON CONCEPT <concept_name> 
FROM <username>;
```

## 3. Hệ thống Vai trò và Quyền hạn Đặc quyền

Hệ thống phân cấp quyền hạn dựa trên ba nhóm vai trò chính:

*   **ADMIN**: Nhóm quyền quản trị tối cao, có khả năng thao tác trên tất cả các cơ sở tri thức, khái niệm và tài khoản người dùng.
*   **USER**: Nhóm quyền mặc định của người dùng cuối, hành vi thao tác cần được cấp phép cụ thể cho từng thực thể.
