# 13.4. Xác thực Studio

Studio [IDE](../00-glossary/01-glossary.md#ide) là trung tâm điều khiển tri thức trực quan. Hiệu năng hiển thị và tính đúng đắn của Dashboard được kiểm soát qua bộ lọc dữ liệu thực tế.

## 1. Kiểm thử IntelliSense & Đồ họa

Xác thực khả năng hỗ trợ lập trình tri thức ([Monaco](../00-glossary/01-glossary.md#monaco) Engine) và đồ thị tri thức (Graph View).

*   **[IntelliSense](../00-glossary/01-glossary.md#intellisense)**: Tự động gợi ý từ khóa `CREATE`, `SELECT` và tên các [Concept](../00-glossary/01-glossary.md#concept) có sẵn.
*   **Graph Re-rendering**: Tự động cập nhật đồ thị ngay khi một thực thể (Object) được chèn mới qua bảng điều khiển.

### Minh chứng Mã nguồn (Dashboard API):
![Minh chứng mã nguồn API giám sát Dashboard](../assets/diagrams/code_test_dashboard.png)
*Hình 13.1: Minh chứng mã nguồn [API](../00-glossary/01-glossary.md#api) giám sát (Dashboard API).*

### Minh chứng Giao diện (Studio IDE):
![Giao diện soạn thảo tri thức trực quan trong KBMS Studio](../assets/diagrams/studio_concept_editor.png)
*Hình 13.2: Giao diện soạn thảo tri thức trực quan trong [KBMS](../00-glossary/01-glossary.md#kbms) Studio.*

## 2. Kiểm thử Giám sát & Quản lý

 Dashboard `System Snapshot` cho phép quản trị viên xem trạng thái thời gian thực của Server.

### Minh chứng Kết quả (Live Logs):
![Chứng minh luồng dữ liệu Studio và Electron Main truyền nhận gói tin nhị phân](../assets/diagrams/terminal_test_studio_electron.png)
*Hình 13.3: Chứng minh luồng dữ liệu Studio và [Electron](../00-glossary/01-glossary.md#electron) Main truyền nhận gói tin nhị phân.*

---

