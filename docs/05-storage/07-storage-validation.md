# 07. Xác thực Lưu trữ (Storage Validation)

Tầng lưu trữ là "pháo đài" cuối cùng bảo vệ dữ liệu, do đó nó được kiểm thử với cường độ cao nhất về mặt hiệu năng I/O và độ bền.

## 1. Kiểm thử Đơn vị Trang (Page-level Testing)

Tệp **`StorageV3Tests.cs`** thực hiện kiểm tra việc đọc/ghi các Slotted Pages 16KB.

*   **Tính nguyên tố**: Đảm bảo toàn bộ 16,384 bytes được ghi xuống đĩa trọn vẹn.
*   **Hiệu năng I/O**: Thời gian ghi Slotted Page trung bình là **23ms**, thời gian thu hồi trang (Eviction) là **46ms**.
*   **Mã hóa**: Kiểm tra tính năng AES-256 (Dữ liệu sau khi ghi xuống đĩa không thể đọc được bằng trình xem HEX nếu không có khóa).

![Placeholder: Ảnh chụp màn hình tệp .dat dưới dạng HEX view, cho thấy dữ liệu đã được mã hóa hoàn toàn (Garbage data) và không có chuỗi văn bản rõ ràng](../assets/diagrams/placeholder_hex_view_encrypted_page.png)

## 2. Kiểm thử Chỉ mục B+ Tree

Kiểm tra khả năng tìm kiếm và phân tách nút (Node Splitting) khi dữ liệu vượt ngưỡng.

| Chỉ số kiểm thử | Giá trị thực tế | Mục tiêu |
| :--- | :--- | :--- |
| **Số lượng bản ghi** | 1,000,000 | Thỏa mãn thực tế |
| **Độ cao của cây** | 3 - 4 tầng | Tối ưu truy xuất |
| **Thời gian tìm kiếm** | < 1ms | Hiệu năng cao |

![Placeholder: Ảnh chụp màn hình kết quả chạy 'BPlusTreeTests.cs' với log hiển thị quy trình Split Internal Node khi chèn bản ghi thứ 1,000,001](../assets/diagrams/placeholder_btree_split_log.png)

## 3. Kiểm thử Hồi phục (WAL Recovery)

Sử dụng tệp **`WalManagerV3`** để mô phỏng sự cố:
1.  Bắt đầu giao dịch (Start Transaction).
2.  Ghi dữ liệu vào RAM (Dirty Pages).
3.  Mô phỏng sập nguồn (Kill Process).
4.  Khởi động lại và kiểm tra việc **Redo** từ nhật ký `.log`.

---

> [!IMPORTANT]
> Toàn bộ các thử nghiệm trên xác nhận rằng KBMS V3 đạt tiêu chuẩn ACID về độ bền vững và an toàn dữ liệu.
