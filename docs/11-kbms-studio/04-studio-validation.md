# 04. Xác thực Giao diện (Studio Validation)

Studio IDE là trung tâm điều khiển tri thức trực quan. Hiệu năng hiển thị và tính đúng đắn của Dashboard được kiểm soát qua bộ lọc dữ liệu thực tế.

## 1. Kiểm thử IntelliSense & Đồ họa

Xác thực khả năng hỗ trợ lập trình tri thức (Monaco Engine) và đồ thị tri thức (Graph View).

*   **IntelliSense**: Tự động gợi ý từ khóa `CREATE`, `SELECT` và tên các Concept có sẵn.
*   **Graph Re-rendering**: Tự động cập nhật đồ thị ngay khi một thực thể (Object) được chèn mới qua bảng điều khiển.

![Placeholder: Ảnh chụp màn hình Studio IDE hiển thị danh sách gợi ý (Autocomplete) các tên Concept khi người dùng gõ phím 'S'](../assets/diagrams/placeholder_studio_intellisense_proof.png)

## 2. Kiểm thử Giám sát & Quản lý (Management Proof)

 Dashboard `System Snapshot` cho phép quản trị viên xem trạng thái thời gian thực của Server.

![Placeholder: Ảnh chụp màn hình Dashboard của Studio hiển thị biểu đồ CPU, RAM và danh sách 12 kịch bản test tải (Load Test) đang thực thi](../assets/diagrams/placeholder_studio_performance_dashboard.png)

## 3. Nhật ký Log thời gian thực (Live Stream)

Xác thực tính năng nhận luồng log nhị phân (`LOGS_STREAM`) từ Server.

![Placeholder: Ảnh chụp màn hình khung Log chuyên biệt trong Studio, hiển thị các dòng tin nhắn nhị phân đã được giải mã sang văn bản Audit Log](../assets/diagrams/placeholder_studio_live_logs.png)

---

> [!IMPORTANT]
> KBMS Studio không chỉ là giao diện đồ họa mà là một công cụ chẩn đoán (Diagnostic Tool) mạnh mẽ giúp chuyên gia tri thức kiểm soát mọi khía cạnh của hệ thống.
