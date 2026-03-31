# 07.0. Tham chiếu Thuật ngữ Kỹ thuật (Technical Glossary)

Để đảm bảo tính nhất quán và giúp người đọc dễ dàng tiếp cận các nội dung kỹ thuật chuyên sâu trong Chương 7, phần này định nghĩa các thuật ngữ nền tảng về hệ quản trị cơ sở dữ liệu (DBMS) được áp dụng trong KBMS V3.

---

## 1. Đơn vị lưu trữ và Cấu trúc dữ liệu

*   **Page (Trang):** Đơn vị lưu trữ cơ bản và nhỏ nhất mà Hệ quản trị bộ nhớ (Storage Engine) có thể đọc hoặc ghi. Trong KBMS, mỗi trang có kích thước cố định là 16KB.
*   **Tuple (Bản ghi):** Một tập hợp dữ liệu nhị phân đại diện cho một thực thể (ví dụ: một đối tượng tam giác). Một trang có thể chứa nhiều Tuples.
*   **Slotted Page:** Kỹ thuật tổ chức trang bằng cách sử dụng một mảng các khe cắm (Slots) ở đầu trang để trỏ đến dữ liệu nằm ở cuối trang, giúp quản lý các bản ghi có độ dài thay đổi hiệu quả.
*   **B+ Tree:** Một cấu trúc cây tự cân bằng cho phép tìm kiếm, truy cập tuần tự, chèn và xóa dữ liệu trong thời gian logarit. Đây là cấu trúc chính để lưu trữ chỉ mục trong KBMS.

---

## 2. Quản lý Bộ nhớ và I/O

*   **I/O (Input/Output):** Thao tác đọc/ghi dữ liệu giữa bộ nhớ RAM và thiết bị lưu trữ vật lý (SSD/HDD). I/O là thao tác tốn kém nhất về hiệu năng.
*   **Buffer Pool (Bộ đệm):** Một vùng nhớ RAM trung gian dùng để lưu trữ tạm thời các trang dữ liệu từ đĩa. Giúp giảm thiểu số lần phải đọc/ghi trực tiếp xuống đĩa.
*   **LRU (Least Recently Used):** Thuật toán thay thế trang trong bộ đệm, ưu tiên giữ lại các trang được truy cập thường xuyên nhất và loại bỏ các trang ít được dùng nhất khi bộ đệm đầy.
*   **Flush:** Hành động ghi một trang dữ liệu từ RAM xuống đĩa cứng để đảm bảo tính vĩnh cửu.

---

## 3. Độ bền và An toàn dữ liệu

*   **WAL (Write-Ahead Logging):** Nguyên tắc "Ghi nhật ký trước khi ghi dữ liệu". Mọi thay đổi phải được ghi vào file log trước khi cập nhật vào file dữ liệu chính để phòng ngừa mất dữ liệu khi sập nguồn.
*   **LSN (Log Sequence Number):** Số thứ tự duy nhất được gán cho mỗi bản ghi nhật ký (Log Record) để theo dõi trình tự thời gian của các giao dịch.
*   **ACID:** Các tính chất đảm bảo độ tin cậy của giao dịch: Nguyên tử (Atomicity), Nhất quán (Consistency), Cô lập (Isolation), và Bền vững (Durability).
*   **AES-256:** Tiêu chuẩn mã hóa khóa đối xứng với độ dài khóa 256-bit, được KBMS dùng để bảo vệ dữ liệu tĩnh trên đĩa.

---

## 4. Cơ chế chuyển đổi dữ liệu

*   **Serialization (Tuần tự hóa):** Quá trình chuyển đổi cấu trúc dữ liệu hoặc trạng thái đối tượng (trong C#) thành một định dạng (chuỗi byte) có thể lưu trữ hoặc truyền đi.
*   **Deserialization (Giải tuần tự hóa):** Quá trình ngược lại, tái tạo đối tượng từ chuỗi byte nhị phân.
