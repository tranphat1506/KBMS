# 03.3. Sơ đồ Hoạt động Tổng quát (Visual Overview)

Tài liệu này trình bày cái nhìn tổng quan về cách các thành phần tương tác và luồng dữ liệu xuyên suốt 4 tầng kiến trúc của KBMS.

## 1. Sơ đồ Use Case Tổng quát

Mô tả sự tương tác giữa các tác nhân bên ngoài và các dịch vụ lõi của hệ thống:

![kbms_general_use_case.png](../assets/diagrams/kbms_general_use_case.png)
*Hình 3.1: Sơ đồ Use Case tổng quát sự tương tác giữa người dùng và các dịch vụ lõi.*

*   **Interaction**: Người dùng tương tác thông qua hai môi trường (CLI/Studio).
*   **Service Core**: Các yêu cầu được gửi tới Server để kích hoạt bộ suy diễn (RE), quản lý dữ liệu (KDL) hoặc bảo trì (KML).

## 2. Sơ đồ Sequence Hệ thống

Mô tả luồng "sinh mệnh" của một yêu cầu từ khi xuất phát tại App Layer cho tới khi được lưu trữ bền vững tại Storage Layer:

![kbms_general_system_sequence.png](../assets/diagrams/kbms_general_system_sequence.png)
*Hình 3.2: Sơ đồ Sequence mô tả luồng sinh mệnh của một yêu cầu trong hệ thống.*

### Các giai đoạn chính:
1.  **Request Stage**: App đóng gói yêu cầu nhị phân và gửi qua Network.
2.  **Computing Stage**: Server Manager điều phối Parser để biên dịch câu lệnh và gửi tới Engine để tính toán/suy diễn.
3.  **I/O Stage**: Engine tương tác với Storage Layer thông qua hệ thống Paging để đọc/ghi dữ liệu nhị phân.
4.  **Response Stage**: Kết quả được "Streaming" ngược lại cho khách hàng theo từng Row dữ liệu để tối ưu hóa bộ nhớ.

---

> [!NOTE]
> Sự phân tách rõ rệt này giúp hệ thống đạt được hiệu năng cao và dễ dàng bảo trì hoặc mở rộng từng phần độc lập.
