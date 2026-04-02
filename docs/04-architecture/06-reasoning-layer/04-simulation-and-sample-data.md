# 4.6.4. Mô phỏng Thực thi và Dữ liệu Mẫu

Để minh chứng cho độ chính xác của hệ thống suy luận, phần này trình bày một kịch bản thực tế dựa trên các khái niệm hình học, mô phỏng cách `InferenceEngine.cs` ([Inference Engine](../../../00-glossary/01-glossary.md#i10)) và `ReteNetwork.cs` ([Rete Network](../../../00-glossary/01-glossary.md#r15)) phối hợp xử lý.

## 4.6.4.1. Kịch bản Tri thức (Knowledge Scenario)

Giả sử ta có một Luật suy luận (Rule) đơn giản:
*"Nếu một tam giác (ABC) có cạnh kề $a$, cạnh kề $b$ và góc xen giữa $\gamma$, thì diện tích $S$ được tính bằng $S = 0.5 \times a \times b \times \sin(\gamma)$."*

Trong KBMS, tri thức này được biên dịch thành một chuỗi nốt Rete thông qua `ReteCompiler` ([Rete Compilation](../../../00-glossary/01-glossary.md#r16)):
- **Nốt Alpha**: Lọc dữ kiện cho biến `a`.
- **Nốt Alpha**: Lọc dữ kiện cho biến `b`.
- **Nốt Alpha**: Lọc dữ kiện cho biến `gamma`.
- **Nốt Beta (Join 1)**: Kết hợp `a` và `b`.
- **Nốt Beta (Join 2)**: Kết hợp kết quả từ Join 1 với `gamma`.
- **Nốt Terminal**: Thực thi công thức diện tích.

## 4.6.4.2. Nhật ký Lan truyền (Propagation Trace)

Khi người dùng thực hiện lệnh `INSERT` các dữ kiện vào hệ thống, quá trình lan truyền Token diễn ra như sau ([Traceability](../../../00-glossary/01-glossary.md#t08)):

*Bảng: Nhật ký lan truyền dữ kiện qua mạng Rete trong kịch bản hình học*
| Bước | Phân đoạn | Hành động Lõi | Kết quả |
| :--- | :--- | :--- | :--- |
| 1 | **Alpha Match** | Nốt Alpha(a=10) lọc và lưu vào AlphaMemory. | `Token(a)` |
| 2 | **Alpha Match** | Nốt Alpha(b=20) lọc và lưu vào AlphaMemory. | `Token(b)` |
| 3 | **Beta Join** | Kết hợp `a` và `b` tại JoinNode 1. | `Tuple(a,b)` |
| 4 | **Alpha Match** | Nốt Alpha(gamma=30) kích hoạt. | `Token(gamma)` |
| 5 | **Execution** | `FireNext()` trong Engine thực thi công thức. | **Result: S = 50.0** |

## 4.6.4.3. Đánh giá Độ chính xác (Accuracy Evaluation)

Kết quả suy luận được đối chiếu trực tiếp với các giá trị lý định. Do KBMS sử dụng kiểu dữ liệu `double` độ chính xác cao và cơ chế `CastToVariableType` trong `InferenceEngine.cs`, sai số trong các phép toán lượng giác luôn nằm trong giới hạn cho phép ($< 10^{-10}$).

Bằng việc sử dụng mạng Rete, thời gian so khớp điều kiện giảm xuống mức $O(1)$ đối với các dữ kiện mới, thay vì $O(N)$ như các phương pháp duyệt luật truyền thống, đảm bảo hiệu năng tối ưu ngay cả khi số lượng luật tăng lên hàng ngàn.
