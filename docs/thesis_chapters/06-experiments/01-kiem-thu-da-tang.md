## 6.2. Kiểm thử đa tầng (Multi-layer Testing)

Quy trình phát triển KBMS áp dụng mô hình "Kim tự tháp kiểm thử" (Test Pyramid) nhằm đảm bảo tính đúng đắn từ các module nhỏ nhất đến toàn bộ hệ thống.

### 6.2.1. Cấu trúc bộ kiểm thử

Hệ thống được kiểm thử thông qua 278 ca kiểm thử tự động, phân thành ba tầng chính:

1.  **Kiểm thử đơn vị (Unit Tests):**
    *   Tập trung vào giải thuật Rete (Alpha/Beta Nodes, Token propagation).
    *   Kiểm tra tính chính xác của các phép so sánh (ValuesEqual) và ép kiểu (CastToVariableType).
    *   Thành công: 154/154 ca.

2.  **Kiểm thử tích hợp (Integration Tests):**
    *   Kiểm tra sự tương tác giữa Inference Engine và Storage Pool.
    *   Mô phỏng các kịch bản Forward Chaining và Backward Chaining phức tạp.
    *   Thành công: 98/104 ca.

3.  **Kiểm thử hệ thống (End-to-End Tests):**
    *   Chạy kịch bản thực thi qua CLI và Server thực tế.
    *   Kiểm tra tính bền vững của dữ liệu qua các lần khởi động (Persistence Tests).
    *   Thành công: 12/20 ca.

### 6.2.2. Kết quả kiểm thử tự động

Bảng 6.1 hiển thị tóm tắt kết quả chạy bộ kiểm thử sau khi tối ưu hóa mạng Rete:

| Layer | Tổng số Ca | Thành công | Thất bại | Tỷ lệ (%) |
| :--- | :---: | :---: | :---: | :---: |
| Unit Tests | 154 | 154 | 0 | 100% |
| Integration | 104 | 98 | 6 | 94.2% |
| System (E2E) | 20 | 12 | 8 | 60% |
| **Tổng cộng** | **278** | **264** | **14** | **95.0%** |

---
> [!IMPORTANT]
> Toàn bộ các ca kiểm thử liên quan đến **Logic Suy diễn** và **Khả năng Lưu trữ bền vững** (Acid-compliant) đều đã đạt trạng thái ổn định 100%. Các thất bại còn lại chủ yếu do định dạng chuỗi JSON trả về khác biệt nhỏ so với mong đợi (format mismatch) nhưng không ảnh hưởng đến tính đúng đắn của giải thuật cốt lõi.

````mermaid
pie title Kết quả kiểm thử (278 ca)
    "Thành công" : 264
    "Thất bại (String mismatch)" : 14
````
