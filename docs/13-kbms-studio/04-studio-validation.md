# 13.4. Xác thực Studio (Studio Validation)

Studio IDE là trung tâm điều khiển tri thức trực quan. Hiệu năng hiển thị và tính đúng đắn của Dashboard được kiểm soát qua bộ lọc dữ liệu thực tế.

## 1. Kiểm thử IntelliSense & Đồ họa

Xác thực khả năng hỗ trợ lập trình tri thức (Monaco Engine) và đồ thị tri thức (Graph View).

*   **IntelliSense**: Tự động gợi ý từ khóa `CREATE`, `SELECT` và tên các Concept có sẵn.
*   **Graph Re-rendering**: Tự động cập nhật đồ thị ngay khi một thực thể (Object) được chèn mới qua bảng điều khiển.

### Minh chứng Mã nguồn (Dashboard API):
![Minh chứng mã nguồn API giám sát Dashboard](../assets/diagrams/code_test_dashboard.png)
*Hình 13.1: Minh chứng mã nguồn API giám sát (Dashboard API).*

### Minh chứng Giao diện (Studio IDE):
![Giao diện soạn thảo tri thức trực quan trong KBMS Studio](../assets/diagrams/studio_concept_editor.png)
*Hình 13.2: Giao diện soạn thảo tri thức trực quan trong KBMS Studio.*

## 2. Kiểm thử Giám sát & Quản lý (Management Proof)

 Dashboard `System Snapshot` cho phép quản trị viên xem trạng thái thời gian thực của Server.

### Minh chứng Kết quả (Live Logs):
![Chứng minh luồng dữ liệu Studio và Electron Main truyền nhận gói tin nhị phân](../assets/diagrams/terminal_test_studio_electron.png)
*Hình 13.3: Chứng minh luồng dữ liệu Studio và Electron Main truyền nhận gói tin nhị phân.*

---

> [!IMPORTANT]
> KBMS Studio không chỉ là giao diện đồ họa mà là một công cụ chẩn đoán (Diagnostic Tool) mạnh mẽ giúp chuyên gia tri thức kiểm soát mọi khía cạnh của hệ thống.
