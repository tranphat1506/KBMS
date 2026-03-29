# 07.7. Xác thực Lưu trữ (Storage Validation)

Tầng lưu trữ là "pháo đài" cuối cùng bảo vệ dữ liệu, do đó nó được kiểm thử với cường độ cao nhất về mặt hiệu năng I/O và độ bền.

## 1. Kiểm thử Đơn vị Trang (Page-level Testing)

Tệp **`StorageV3Tests.cs`** thực hiện kiểm tra việc đọc/ghi các Slotted Pages 16KB.

*   **Tính nguyên tố**: Đảm bảo toàn bộ 16,384 bytes được ghi xuống đĩa trọn vẹn.
*   **Hiệu năng I/O**: Thời gian ghi Slotted Page trung bình là **23ms**, thời gian thu hồi trang (Eviction) là **46ms**.
*   **Mã hóa**: Kiểm tra tính năng AES-256 (Dữ liệu sau khi ghi xuống đĩa không thể đọc được bằng trình xem HEX nếu không có khóa).

### Minh chứng Mã nguồn (Storage I/O):
![code_test_storage_io.png](../assets/diagrams/code_test_storage_io.png)
*Hình 7.6: Kịch bản kiểm thử luồng I/O và Buffer Pool.*

### Minh chứng Lưu trữ (HEX View):
![terminal_test_storage_hex.png](../assets/diagrams/terminal_test_storage_hex.png)
*Hình 7.7: Kết quả đọc trang nhị phân trực tiếp từ đĩa cứng.*

## 2. Kiểm thử Chỉ mục B+ Tree

Kiểm tra khả năng tìm kiếm và phân tách nút (Node Splitting) khi dữ liệu vượt ngưỡng.

| Chỉ số kiểm thử | Giá trị thực tế | Mục tiêu |
| :--- | :--- | :--- |
| **Số lượng bản ghi** | 1,000,000 | Thỏa mãn thực tế |
| **Độ cao của cây** | 3 - 4 tầng | Tối ưu truy xuất |
| **Thời gian tìm kiếm** | < 1ms | Hiệu năng cao |

### Minh chứng Mã nguồn (B+ Tree):
![code_test_index.png](../assets/diagrams/code_test_index.png)
*Hình 7.8: Kịch bản kiểm thử hiệu năng chỉ mục B+ Tree.*

### Minh chứng Log (Split Node):
![result_test_index.png](../assets/diagrams/result_test_index.png)
*Hình 7.9: Kết quả tìm kiếm và duyệt cây B+ Tree.*

## 3. Kiểm thử Hồi phục (WAL Recovery)

Sử dụng tệp **`WalManagerV3`** để mô phỏng sự cố:
1.  Bắt đầu giao dịch (Start Transaction).
2.  Ghi dữ liệu vào RAM (Dirty Pages).
3.  Mô phỏng sập nguồn (Kill Process).
4.  Khởi động lại và kiểm tra việc **Redo** từ nhật ký `.log`.

### Minh chứng Mã nguồn (WAL Recovery):
![code_test_recovery.png](../assets/diagrams/code_test_recovery.png)
*Hình 7.10: Kịch bản kiểm thử khả năng phục hồi dữ liệu từ file WAL.*

### Minh chứng Kết quả (Redo Log):
![result_test_recovery.png](../assets/diagrams/result_test_recovery.png)
*Hình 7.11: Bằng chứng phục hồi trạng thái Concept thành công sau khi giả lập mất điện.*

---

> [!IMPORTANT]
> Toàn bộ các thử nghiệm trên xác nhận rằng KBMS V3 đạt tiêu chuẩn ACID về độ bền vững và an toàn dữ liệu.
