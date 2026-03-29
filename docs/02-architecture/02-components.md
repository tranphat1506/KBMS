# Chi tiết các Thành phần Hệ thống

Hệ thống KBMS được cấu thành từ các project C# riêng biệt, mỗi project đảm nhận một vai trò cụ thể trong chu trình sống của tri thức.

## 1. KBMS.Server (Core Orchestratrator)

Đóng vai trò là trung tâm điều phối của toàn bộ hệ thống.
*   **Chức năng:** Quản lý vòng đời của các phiên làm việc (Sessions), tiếp nhận yêu cầu mạng từ `KBMS.Network` và gọi `KBMS.Parser` để phân tích câu lệnh.
*   **Trách nhiệm:** Tổng hợp kết quả từ cỗ máy suy diễn và cỗ máy lưu trữ để trả về cho Client.

---

## 2. KBMS.Parser (Lexer & Parser)

Bộ phận dịch thuật ngôn ngữ KBQL sang cấu trúc máy hiểu được.
*   **Chức năng:** Phân tích từ vựng (Tokenization) và phân tích cú pháp (Parsing) dựa trên mô hình Recursive Descent.
*   **Đầu ra:** Abstract Syntax Tree (AST), ví dụ như `SelectStatement`, `CreateRuleStatement`.

---

## 3. KBMS.Reasoning & KBMS.Knowledge

"Bộ não" thực hiện các phép logic và toán học.
*   **Inference Engine:** Triển khai thuật toán Forward Chaining để suy diễn tri thức từ tập quy tắc (Rules).
*   **Knowledge Model:** Định nghĩa các thực thể cao cấp như `Concept`, `Rule`, `Equation`, `Constraint`.
*   **Math Solver:** Tích hợp bộ giải số học (Newton-Raphson, Brent) để giải các bài toán phương trình.

---

## 4. KBMS.Storage (Data Persistence)

Đảm bảo an toàn cho tri thức được lưu trữ dưới đĩa cứng.
*   **V3 Engine:** Triển khai cây B+ Tree phân trang hỗ trợ index và tìm kiếm hiệu năng cao.
*   **Buffer Pool:** Quản lý bộ đệm thông qua chính sách LRU để giảm thiểu I/O.
*   **Slotted Page:** Cách thức tổ chức dữ liệu linh hoạt cho các Fact có độ dài khác nhau.

---

## 5. KBMS.Network (TCP Protocol)

Lớp vỏ bọc giao tiếp giữa Client và Server.
*   **Chức năng:** Định nghĩa gói tin Binary và các loại thông điệp (`MessageType`).
*   **Mục tiêu:** Truyền tải dữ liệu không suy hao và hỗ trợ phản hồi lỗi chính xác.

---

## 6. KBMS.Models & KBMS.CLI

*   **KBMS.Models:** Chứa các định nghĩa chung về kiểu dữ liệu (Data types), Biến (Variables) và các lớp tiện ích được dùng bởi toàn bộ hệ thống.
*   **KBMS.CLI:** Giao diện dòng lệnh (REPL) đơn giản cho phép tương tác trực tiếp với Server thông qua console.

---

## 7. KBMS Studio (Web/Desktop Client)

Giao diện đồ họa người dùng (UI) mạnh mẽ nhất.
*   **Cấu trúc:** Được xây dựng bằng **Electron + React + Monaco Editor**.
*   **Tính năng:** Soạn thảo mã bài toán với gợi ý cú pháp, xem kết quả suy diễn dưới dạng bảng và theo dõi biểu đồ hiệu năng hệ thống.
