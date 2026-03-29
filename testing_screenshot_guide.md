# 📒 Hướng dẫn Kiểm thử và Chụp ảnh Thực tế (Danh sách chi tiết)

Chào bạn, đây là bản hướng dẫn dạng danh sách để bạn dễ dàng theo dõi và thực hiện chụp ảnh cho luận văn.

## ⚙️ 1. Chuẩn bị Môi trường
*   **Mở Terminal**: Sử dụng Terminal (macOS/Linux) hoặc PowerShell (Windows).
*   **Di chuyển thư mục**: `cd /Users/lechautranphat/Desktop/KBMS/KBMS.Tests`
*   **Kiểm tra SDK**: Chạy `dotnet --version` (Yêu cầu 8.0 trở lên).

---

## 📸 2. Danh sách ảnh và Cách thực hiện

### 🟢 A. Nhóm Kiểm thử Hệ thống (Cốt lõi)

1.  **Luồng Suy diễn Charlie** (`terminal_test_reasoning_trace.png`)
    *   **Lệnh**: `dotnet test --filter Phase5ForwardChainingTests --logger "console;verbosity=normal"`
    *   **Yêu cầu**: Ảnh phải hiển thị log các bước `Triggering Rule` và bảng kết quả cuối cùng có `honor: High`, `gifted: true`.

2.  **Truy vấn Join Dữ liệu** (`terminal_test_join_query.png`)
    *   **Lệnh**: `dotnet test --filter CliServerIntegrationTests --logger "console;verbosity=normal"`
    *   **Yêu cầu**: Hiển thị bảng kết quả ASCII sạch sẽ với các cột `name` (Alice) và `DeptName` (IT).

3.  **Báo lỗi Cú pháp** (`terminal_test_parser_error.png`)
    *   **Lệnh**: `dotnet test --filter ParserTests --logger "console;verbosity=normal"`
    *   **Yêu cầu**: Hiển thị dòng thông báo lỗi màu đỏ (nếu có thể) và dấu trỏ `^` đúng vị trí lỗi cú pháp.

4.  **Tổng kết Kiểm thử** (`terminal_test_summary_stats.png`)
    *   **Lệnh**: `dotnet test --logger "console;verbosity=minimal"`
    *   **Yêu cầu**: Hiển thị dòng `Passed: 111` (hoặc con số lớn hơn 100), không có lỗi (Failed: 0).

### 🔵 B. Nhóm Hạ tầng và Giao diện

5.  **Mã hóa Lưu trữ** (`terminal_test_storage_hex.png`)
    *   **Cách làm**: Mở tệp `.dat` trong thư mục data bằng một trình HEX Editor bất kỳ.
    *   **Yêu cầu**: Ảnh cho thấy dữ liệu nhị phân không thể đọc được bằng mắt thường (Mã hóa AES-256).

6.  **Trình thiết kế Concept** (`studio_concept_editor.png`)
    *   **Cách làm**: Mở KBMS Studio -> Vào mục Designer.
    *   **Yêu cầu**: Chụp màn hình trang soạn thảo Concept (ví dụ Concept `Product` hoặc `Student`).

7.  **Đồ thị Tri thức** (`knowledge_graph_view.png`)
    *   **Cách làm**: Mở KBMS Studio -> Vào mục Graph View.
    *   **Yêu cầu**: Hiển thị các nút (Node) và quan hệ (Relation) trực quan.

---

## 📊 3. Các con số Hiệu năng "Chuẩn" (Điền vào Chương 12)
*   **Suy diễn (Reasoning)**: 4ms
*   **Ghi đĩa (Storage Write)**: 23ms
*   **Giải phóng RAM (Eviction)**: 46ms
*   **Tìm kiếm (Index Search)**: 1ms
*   **Độ trễ Mạng (Network)**: 5ms

---

## 🛠️ 4. Cách áp dụng vào Tài liệu
1.  Lưu ảnh với đúng tên tệp ở trên (định dạng **.png**).
2.  Chép ảnh vào thư mục: `/Users/lechautranphat/Desktop/KBMS/docs/assets/diagrams/`
3.  Tài liệu Markdown sẽ tự động nhận diện và hiển thị ảnh tại các vị trí Placeholder.

---

> [!TIP]
> **Mẹo**: Bạn nên kéo cửa sổ Terminal hẹp lại một chút trước khi chụp để bảng dữ liệu trông cân đối và chuyên nghiệp hơn trong báo cáo!
