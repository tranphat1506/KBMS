# 04.1. Tổng quan Kiến trúc

Hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) (phiên bản V3) được thiết kế dựa trên mô hình kiến trúc 4 tầng (4-Tier Architecture) hiện đại [1]. Cấu trúc này giúp tách biệt rõ ràng trách nhiệm giữa việc tương tác người dùng, truyền dẫn mạng, xử lý logic và lưu trữ vật lý [3].

## 1. Mô hình Kiến trúc 4 Tầng

Cấu trúc phân tầng của [KBMS](../00-glossary/01-glossary.md#kbms) bao gồm:

![kbms_4_tier_architecture.png | width=0.2](../assets/diagrams/kbms_4_tier_architecture.png)
*Hình 4.1: Kiến trúc 4 tầng chuẩn hóa của hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) V3.*

1.  **Application Layer**: Giao diện người dùng (Studio/[CLI](../00-glossary/01-glossary.md#cli)), chịu trách nhiệm thu thập yêu cầu và hiển thị kết quả.
2.  **Network Layer**: Giao thức truyền dẫn nhị phân, đảm bảo kết nối ổn định và hiệu năng cao.
3.  **Server Layer**: "Bộ não" của hệ thống, thực hiện biên dịch ngôn ngữ [KBQL](../00-glossary/01-glossary.md#kbql) và suy diễn tri thức.
4.  **Storage Layer**: Quản lý dữ liệu bền vững trên đĩa cứng thông qua các thuật toán [B+ Tree](../00-glossary/01-glossary.md#b-tree) và WAL.

## 2. Luồng Yêu cầu Tổng quát

Mọi yêu cầu từ người dùng đều đi xuyên suốt qua 4 tầng này để đạt được kết quả cuối cùng:

![kbms_request_flow_v3.png | width=0.9](../assets/diagrams/kbms_request_flow_v3.png)
*Hình 4.2: Luồng xử lý yêu cầu đi xuyên suốt 4 tầng kiến trúc.*

## 3. Bảo mật & Chẩn đoán Hệ thống

Bên cạnh luồng dữ liệu chính, [KBMS](../00-glossary/01-glossary.md#kbms) duy trì một "mạch quản trị" song song để đảm bảo tính an toàn và minh bạch:

![kbms_security_diagnostics_flow.png | width=0.9](../assets/diagrams/kbms_security_diagnostics_flow.png)
*Hình 4.3: Sơ đồ luồng bảo mật và chẩn đoán hệ thống song song.*

## 4. Công nghệ Sử dụng

*Bảng 4.1: Mô tả chức năng 4 tầng kiến trúc [KBMS](../00-glossary/01-glossary.md#kbms)*
| Tầng | Thành phần chính | Công nghệ & Thư viện |
| :--- | :--- | :--- |
| **Application** | Studio [IDE](../00-glossary/01-glossary.md#ide) | [React](../00-glossary/01-glossary.md#react), [Electron](../00-glossary/01-glossary.md#electron), [Monaco](../00-glossary/01-glossary.md#monaco) Editor, Tailwind CSS |
| **Network** | [Binary Protocol](../00-glossary/01-glossary.md#binary-protocol) | TCP Sockets, UTF-8 Encoding, [Little-Endian](../00-glossary/01-glossary.md#little-endian) |
| **Server** | Core Engine | .NET Core 8, C#, [F-Closure](../00-glossary/01-glossary.md#f-closure) Algorithm |
| **Storage** | Physical Layer | [B+ Tree](../00-glossary/01-glossary.md#b-tree), WAL, [Slotted Page](../00-glossary/01-glossary.md#slotted-page), [AES-256](../00-glossary/01-glossary.md#aes-256) Encryption |

**Khả năng mở rộng**: Với kiến trúc 4 tầng chuẩn hóa, [KBMS](../00-glossary/01-glossary.md#kbms) dễ dàng hỗ trợ các ứng dụng bên thứ ba tích hợp thông qua Giao thức Mạng mà không cần can thiệp vào mã nguồn Server.
