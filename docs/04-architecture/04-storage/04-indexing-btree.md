# Chỉ mục Cây B+ (B+ Tree) [5], [10]

Hệ thống KBMS sử dụng cấu trúc chỉ mục Cây B+ để tăng tốc độ truy xuất các thực thể tri thức. Chỉ mục này giúp ánh xạ các khóa tìm kiếm (như `ObjectId` hoặc tên thuộc tính) về đúng mã trang (`PageId`) chứa dữ liệu.

## 4.4.7. Cấu trúc Hình học của Cây B+

Cấu trúc Cây B+ trong KBMS bao gồm hai loại trang:

1.  **Trang trong (Internal Page)**: Chứa các khóa dẫn hướng và con trỏ trỏ tới các trang con ở tầng dưới. Các trang này không chứa dữ liệu thực tế.
2.  **Trang lá (Leaf Page)**: Chứa các cặp `[Key, Value]` thực tế, trong đó `Value` là vị trí của bản ghi tri thức. Các trang lá được liên kết với nhau theo cả hai chiều để hỗ trợ truy vấn theo khoảng (Range Query) hiệu quả.

![Cấu trúc Chỉ mục Cây B+](../../assets/diagrams/btree_structure.png)
*Hình 4.12: Sơ đồ phân tầng và liên kết giữa các nốt trong Cây B+.*

## 4.4.8. Giải thuật Tìm kiếm và Cân bằng

Các thao tác trên cây chỉ mục đảm bảo độ phức tạp thời gian luôn là $O(\log n)$:

-   **Tìm kiếm**: Bắt đầu từ trang gốc (Root), so sánh khóa để rẽ nhánh xuống các tầng thấp hơn cho đến khi chạm tới trang lá.
-   **Chèn và Tách**: Khi một trang lá bị đầy, hệ thống thực hiện tách trang và cập nhật khóa dẫn hướng lên trang cha.
-   **Xóa và Gộp**: Khi số lượng bản ghi trong một trang xuống dưới ngưỡng cho phép, hệ thống sẽ thực hiện gộp với trang lân cận để tối ưu hóa không gian.

Việc tích hợp Cây B+ trực tiếp vào tầng lưu trữ phân trang giúp KBMS có thể xử lý hàng triệu bản ghi tri thức mà không làm suy giảm hiệu năng truy xuất của máy chủ.
