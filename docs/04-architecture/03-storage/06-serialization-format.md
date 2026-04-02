# 4.3.6 Quy trình Tuần tự hóa Nhị phân (Binary Serialization)

Quy trình tuần tự hóa dữ liệu (Serialization) trong hệ quản trị KBMS thực hiện chức năng chuyển đổi các thực thể tri thức bậc cao từ ngôn ngữ lập trình sang định dạng nhị phân có độ dài biến thiên để lưu trữ trong cấu trúc Slotted Page.

## 4.3.6.1 Cơ chế Mã hóa Thực thể Tri thức

Hệ thống triển khai phân hệ `ModelBinaryUtility` để điều phối hoạt động mã hóa dữ liệu. Mỗi thực thể tri thức (Concept, Fact, hoặc Rule) được biểu diễn thành một chuỗi byte có cấu trúc theo các quy tắc kĩ thuật sau:

1.  **Định danh Loại Thực thể (Entity Type Encoding)**: Header của mỗi bản ghi nhị phân chứa mã định danh loại thực thể, cho phép hệ thống nhận diện và giải mã chính xác các đối tượng dữ liệu trong quá trình truy xuất.
2.  **Mã hóa Độ dài Biến thiên (Variable-length Encoding)**: Đối với các trường dữ liệu chuỗi hoặc danh sách thuộc tính, hệ thống sử dụng tham số độ dài (`Length Prefix`) đi kèm với giá trị dữ liệu. Phương pháp này tối ưu hóa dung lượng lưu trữ so với phương pháp sử dụng đệm (Padding) cố định.
3.  **Tuần tự hóa Kiểu dữ liệu nguyên thủy**: Các giá trị thuộc tính tri thức được chuyển đổi trực tiếp sang các kiểu dữ liệu nguyên thủy (Primitive Types) như `Int32`, `Double`, hoặc `Boolean` để đảm bảo độ chính xác trong các thao tác tính toán logic.

## 4.3.6.2 Quy trình Mã hóa và Giải mã (Encoding & Decoding Paths)

Quá trình chuyển hóa dữ liệu được thực thi thông qua hai quy trình:

-   **Quy trình Mã hóa (Encoding Path)**:
    1.  Khởi tạo bộ nhớ đệm tạm thời (Memory Stream).
    2.  Duyệt qua danh sách các thuộc tính của thực thể và thực hiện ghi dữ liệu nhị phân tương ứng.
    3.  Thực hiện mã hóa AES-256-CBC tại cấp độ trang nếu trang được lệnh đồng bộ hóa từ bộ nhớ đệm xuống thiết bị lưu trữ.
-   **Quy trình Giải mã (Decoding Path)**:
    1.  Sử dụng định danh `SlotId` để xác định vị trí offset của bản ghi trong Slotted Page.
    2.  Đọc định danh loại thực thể để thực thi quy trình khởi tạo đối tượng tương ứng (Factory Pattern).
    3.  Chuyển đổi dữ liệu nhị phân về các kiểu dữ liệu logic để phục vụ quy trình suy diễn tri thức.

## 4.3.6.3 Đặc tả Lưu trữ Thực thể Concept

Thực thể [Concept](../../00-glossary/01-glossary.md#concept) là thành phần cấu trúc phức tạp nhất, chứa các định nghĩa về biến (`Variables`) và các ràng buộc (`Constraints`). Đặc tả nhị phân của Concept được mô tả như sau:

*Bảng 4.4: Đặc tả nhị phân của thực thể Concept*
| Thành phần | Định dạng | Mô tả kĩ thuật |
| :--- | :--- | :--- |
| **Concept ID** | Guid (16B) | Mã định danh duy nhất của khái niệm. |
| **Name Length** | Int32 (4B) | Độ dài (n) của tên khái niệm. |
| **Name Bytes** | UTF8 (n-Bytes) | Giá trị dữ liệu tên khái niệm dưới dạng nhị phân. |
| **Var Count** | Int32 (4B) | Số lượng biến có trong định nghĩa của khái niệm. |
| **Var Definitions** | Binary | Các khối dữ liệu nhị phân mô tả tên và kiểu dữ liệu của biến. |

Việc chuẩn hóa định dạng tuần tự hóa nhị phân là yếu tố kĩ thuật cần thiết để duy trì hiệu năng thao tác I/O và đảm bảo sự nhất quán của dữ liệu tri thức qua các chu kỳ vận hành của hệ thống.
