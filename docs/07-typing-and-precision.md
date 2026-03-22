# Hệ thống Kiểu dữ liệu & Độ chính xác (True Typing)

KBMS v1.1 giới thiệu hệ thống **True Typing**, giúp đảm bảo dữ liệu tri thức được lưu trữ, tính toán và hiển thị với độ chính xác tuyệt đối, đặc biệt là các kiểu dữ liệu số.

## 1. Các kiểu dữ liệu số cốt lõi
Hệ thống phân tách rõ ràng giữa ba nhóm số:

| Kiểu dữ liệu | Đặc điểm | Phù hợp cho |
| :--- | :--- | :--- |
| **INT / INTEGER** | Số nguyên 64-bit (long) | ID, số lượng, đếm |
| **DECIMAL(L, S)** | Số thực cố định (Precision L, Scale S) | Tài chính, tiền tệ, khoa học chính xác |
| **DOUBLE / FLOAT** | Số thực dấu phẩy động | Tọa độ, đồ họa, tính toán nhanh |

## 2. Bảo toàn độ chính xác (Precision Preservation)
Trong các hệ thống cũ, mọi con số thường bị ép về `double`, dẫn đến sai số dấu phẩy động (vd: `0.1 + 0.2 = 0.30000000000000004`). 

Với True Typing trong KBMS:
- **Phép toán DECIMAL**: Mọi phép tính (+, -, *, /) trên kiểu `DECIMAL` đều được thực hiện thông qua kiểu `decimal` của .NET, đảm bảo `100.00 * (1 + 0.0825) = 108.25` chính xác.
- **Tự động Làm tròn (Auto-rounding)**: Khi `INSERT` một giá trị vào biến `DECIMAL(10, 2)`, hệ thống sẽ tự động làm tròn giá trị đó về đúng 2 chữ số thập phân (vd: `19.995` -> `20.00`).

## 3. Chiến lược Thăng cấp số (Numeric Promotion)
Khi thực hiện biểu thức hỗn hợp, KBMS áp dụng quy tắc thăng cấp để không làm mất dữ liệu:
1. `INT` + `INT` = `INT` (64-bit).
2. `INT` + `DECIMAL` = `DECIMAL`.
3. `DECIMAL` + `DOUBLE` = `DOUBLE` (Chuyển sang dấu phẩy động nếu có bất kỳ toán hạng nào là floating point).

## 4. Hiển thị Nguyên bản (Raw Display)
CLI của KBMS được tinh chỉnh để hiển thị đúng độ chính xác đã lưu:
- Nếu biến là `DECIMAL(10, 2)`, giá trị `42` sẽ hiển thị là `42.00`.
- Điều này giúp người dùng kiểm soát được chất lượng dữ liệu ngay từ giao diện dòng lệnh.
