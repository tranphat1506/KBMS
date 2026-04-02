# Đặc tả Quy trình Tuần tự hóa Nhị phân

Quy trình tuần tự hóa dữ liệu (**Serialization**) trong hệ quản trị **[KBMS](../../../00-glossary/01-glossary.md#kbms)** thực hiện chức năng chuyển đổi các thực thể tri thức bậc cao từ ngôn ngữ lập trình sang định dạng nhị phân có độ dài biến thiên để lưu trữ hiệu quả trong cấu trúc trang phân khe.

## 1. Cơ chế Mã hóa Thực thể Tri thức

Hệ thống triển khai phân hệ `ModelBinaryUtility` để điều phối hoạt động mã hóa dữ liệu. Mỗi thực thể tri thức (**Concept**, **Fact**, hoặc **Rule**) được biểu diễn thành một chuỗi byte có cấu trúc theo các quy tắc kỹ thuật sau:

1.  **Mã hóa Loại Thực thể (Entity Type Encoding)**: Phần đầu của mỗi bản ghi nhị phân chứa mã hiệu định danh loại thực thể, cho phép hệ thống nhận diện và giải mã chính xác các đối tượng dữ liệu trong quá trình truy xuất.
2.  **Mã hóa Độ dài Biến thiên (Variable-length Encoding)**: Đối với các trường dữ liệu chuỗi hoặc danh sách thuộc tính, hệ thống sử dụng tiền tố độ dài (**Length Prefix**) đi kèm với giá trị dữ liệu. Phương pháp này tối ưu hóa dung lượng lưu trữ so với phương thức sử dụng đệm (Padding) cố định.
3.  **Tuần tự hóa Kiểu dữ liệu Nguyên thủy**: Các giá trị thuộc tính tri thức được chuyển đổi trực tiếp sang các kiểu dữ liệu cơ sở như `Int32`, `Double`, hoặc `Boolean` nhằm đảm bảo độ chính xác tuyệt đối trong các thao tác tính toán logic.

## 2. Quy trình Mã hóa và Giải mã (Encoding/Decoding Path)

Quá trình chuyển hóa dữ liệu giữa các trạng thái logic và vật lý được thực thi thông qua hai luồng xử lý chính:

-   **Quy trình Mã hóa (Encoding Path)**:
    1.  Khởi tạo luồng ghi dữ liệu tạm thời (**Memory Stream**).
    2.  Duyệt qua danh sách các thuộc tính của thực thể và thực hiện ghi dữ liệu nhị phân tương ứng theo thứ tự định nghĩa hình thức.
    3.  Thực hiện mã hóa **AES-256-CBC** tại cấp độ trang khi dữ liệu được lệnh đồng bộ hóa xuống thiết bị lưu trữ.
-   **Quy trình Giải mã (Decoding Path)**:
    1.  Sử dụng định danh `SlotId` để xác định vị trí thực tế của bản ghi trong trang phân khe.
    2.  Đọc mã loại thực thể để thực thi quy trình khởi tạo đối tượng tương ứng thông qua mô hình nhà máy (**Factory Pattern**).
    3.  Chuyển đổi dữ liệu nhị phân về các kiểu dữ liệu logic để phục vụ luồng thực thi và suy diễn tri thức.

## 3. Đặc tả Lưu trữ Thực thể Khái niệm (Concept)

Thực thể **[Concept](../../../00-glossary/01-glossary.md#concept)** là thành phần cấu trúc phức tạp nhất, chứa các định nghĩa về biến số và các ràng buộc logic. Đặc tả nhị phân của thực thể Concept được mô tả như sau:

*Bảng 4.4: Đặc tả nhị phân của thực thể Concept*
| Thành phần | Định dạng | Mô tả Chức năng Kỹ thuật |
| :--- | :--- | :--- |
| **Concept ID** | Guid (16B) | Mã định danh duy nhất của khái niệm trong hệ thống. |
| **Name Length** | Int32 (4B) | Độ dài (n bytes) của tên khái niệm. |
| **Name Bytes** | UTF8 (n-Bytes) | Giá trị dữ liệu tên khái niệm dưới dạng nhị phân. |
| **Var Count** | Int32 (4B) | Số lượng biến có trong định nghĩa của khái niệm. |
| **Var Definitions** | Binary | Các khối dữ liệu nhị phân mô tả định danh và kiểu dữ liệu của biến. |

Việc chuẩn hóa định dạng tuần tự hóa nhị phân là yếu tố kỹ thuật then chốt để duy trì hiệu năng thao tác I/O và đảm bảo tính nhất quán dữ liệu bền vững qua các chu kỳ vận hành của hệ thống.
