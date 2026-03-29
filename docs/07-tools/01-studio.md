# Công cụ Tương tác (Client Tools)

KBMS cung cấp hai phương thức chính để người dùng tương tác với Cơ sở Tri thức: CLI (Giao diện dòng lệnh) và Studio (Giao diện đồ họa hiện đại).

## 1. KBMS CLI (Command Line Interface)

CLI là công cụ nhẹ nhàng, dùng để kiểm tra nhanh các câu lệnh KBQL hoặc chạy các script tự động.

### Tính năng & Ý tưởng
*   **REPL (Read-Eval-Print Loop):** Người dùng nhập câu lệnh, CLI gửi tới Server, nhận kết quả và in ra ngay lập tức.
*   **Command History:** Lưu lại các câu lệnh đã thực hiện để gọi lại bằng phím mũi tên.
*   **Multi-line Input:** Hỗ trợ nhập các câu lệnh dài (như `CREATE RULE`) bằng cách kết thúc lệnh bằng dấu chấm phẩy `;`.

### Cách chạy
```bash
cd KBMS.CLI
dotnet run
```

---

## 2. KBMS Studio (IDE cho Tri thức)

KBMS Studio là một ứng dụng Electron hoàn chỉnh, được thiết kế để trở thành một IDE chuyên nghiệp cho việc soạn thảo bài toán và quản trị hệ thống.

### a. Trình soạn thảo Monaco (Editor)
*   **Ý tưởng:** Sử dụng Monaco Editor (lõi của VS Code) để cung cấp trải nghiệm lập trình tốt nhất.
*   **Tính năng:**
    *   **Syntax Highlighting:** Tự động tô màu các từ khóa KBQL.
    *   **Error Markers:** Khi Server trả về lỗi kèm vị trí (`Line`, `Column`), Studio sẽ hiển thị các vệt đỏ (Squiggles) chính xác tại lỗi đó.
    *   **Auto-completion:** Gợi ý tên Concept và tên Biến khi người dùng gõ phím.

### b. Chế độ Xem Kết quả (Result View)
*   **Data Grid:** Hiển thị Facts dưới dạng bảng có thể lọc và sắp xếp.
*   **Reasoning Steps:** Một tab đặc biệt để người dùng xem lại quá trình suy diễn (Derivation Trace).

### c. Giám sát Hệ thống (System Dashboard)
*   **Ý tưởng:** Theo dõi sức khỏe của KBMS Server theo thời gian thực.
*   **Các chỉ số:**
    *   **Heap Usage:** Bộ nhớ RAM đang sử dụng.
    *   **Buffer Pool Hit Rate:** Tỷ lệ tìm thấy trang trong RAM (giúp đánh giá hiệu năng Storage).
    *   **Activity Logs:** Nhật ký các truy vấn và hành động của Client.

### d. Quản lý Trạng thái (State Management)
*   **Kiến trúc:** Sử dụng **Redux/Zustand** để giữ cho giao diện luôn đồng bộ với dữ liệu từ Server.
*   **Cơ chế Stream:** Studio nhận các chunk dữ liệu từ Server thông qua `Network Protocol` và cập nhật từng phần vào giao diện (Streaming UI), giúp xử lý các truy vấn lớn mà không làm treo ứng dụng.
