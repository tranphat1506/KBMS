# 4.3.4 Phân hệ Chỉ mục B+ Tree (Indexing & Retrieval)

Hệ thống KBMS quản lý việc lưu trữ và truy xuất các thực thể tri thức dựa trên cấu trúc dữ liệu **B+ Tree**. Đây là một cấu trúc cây cân bằng đa cấp được tối ưu hóa cho các hệ thống lưu trữ phân quản theo khối, giúp duy trì hiệu năng cao cho các thao tác truy vấn tri thức quy mô lớn.

## 4.3.4.1 Cấu trúc Nốt (Node Structure) và Ràng buộc Cấu trúc (Invariants)

Cấu trúc B+ Tree bao gồm các nốt trung gian (Internal Nodes) và các nốt lá (Leaf Nodes). Mỗi nốt được triển khai dưới dạng một Slotted Page độc lập. Sự phân cấp này đảm bảo hiệu quả truy xuất và khả năng mở rộng của hệ thống chỉ mục:

-   **Internal Nodes**: Lưu trữ các cặp dữ liệu `[Khóa (Key) | ID Trang con (Child PageId)]`. Chức năng chính là điều phối quy trình điều hướng trong các thao tác tìm kiếm khóa.
-   **Leaf Nodes**: Lưu trữ các cặp dữ liệu `[Khóa (Key) | Định danh bản ghi (RID)]`. Các nốt lá được liên kết thông qua cấu trúc danh sách liên kết kép (`PrevPageId` và `NextPageId`), hỗ trợ các truy vấn quét phạm vi (Range Scans).

Các ràng buộc cấu trúc cốt lõi (Invariants):
1.  **Tính Cân bằng (Balanced Property)**: Mọi nốt lá trong cây luôn nằm ở cùng một độ sâu (Depth), đảm bảo tính nhất quán của độ phức tạp truy xuất dữ liệu.
2.  **Độ Phức tạp Thời gian**: Độ phức tạp của các thao tác tìm kiếm, chèn và xóa dữ liệu được ổn định ở mức **$O(\log_b n)$**, với $b$ là bậc (Fan-out) của nốt. Với kích thước trang 16KB, hệ thống đạt được $b \approx 500$, giúp duy trì độ sâu cây ở mức thấp ngay cả với tập dữ liệu hàng triệu thực thể.

## 4.3.4.2 Quy trình Sửa đổi Cấu trúc (Insertion & Splitting)

Khi một nốt đạt giới hạn dung lượng khả dụng trong quá trình chèn dữ liệu mới, hệ thống thực thi quy trình **Page Splitting** để tái lập tính cân bằng:

1.  **Khởi tạo Trang mới**: Một trang Slotted Page mới được cấp phát thông qua phân hệ quản lý bộ nhớ đệm.
2.  **Phân bố Dữ liệu**: Một nửa số lượng bản ghi của nốt hiện tại được chuyển sang trang mới để tạo không gian nhớ khả dụng.
3.  **Cập nhật Tầng trên**: Khóa phân tách (Separator Key) và định danh của trang mới sẽ được chèn vào nốt cha tương ứng. Quy trình này có thể thực hiện đệ quy ngược lên cho đến cấp nốt gốc (Root).

![btree_v3.png | width=1.0](../../assets/diagrams/btree_v3.png)
*Hình 4.14: Sơ đồ phân cấp cấu trúc và cơ chế liên kết nốt lá trong hệ thống chỉ mục B+ Tree.*

## 4.3.4.3 Cơ chế Truy xuất Thực thể Tri thức

Cấu trúc B+ Tree đóng vai trò là phân hệ trung gian điều phối giữa yêu cầu truy vấn logic và truy cập vật lý:

-   **Khóa Tri thức (Knowledge Keys)**: Hệ thống sử dụng định danh của [Concept](../../00-glossary/01-glossary.md#concept) và [Fact](../../00-glossary/01-glossary.md#fact) làm khóa chính, cho phép định vị dữ liệu thực thể thông qua một quy trình duyệt cây duy nhất.
-   **Truy vấn Phạm vi (Range Scans)**: Nhờ sự liên kết trực tiếp giữa các nốt lá, các thao tác truy vấn liên quan đến quan hệ tri thức kế cận hoặc liệt kê thực thể được thực thi mà không cần tái duyệt cấu trúc cây từ nốt gốc.
