# 08.3. Xác thực Suy diễn (Reasoning Validation)

Bộ máy suy diễn là trung tâm trí tuệ của KBMS, do đó tính chính xác tuyệt đối của thuật toán F-Closure là bắt buộc.

## 1. Kiểm thử Thuật toán F-Closure

Hệ thống sử dụng tệp **`Phase5ForwardChainingTests.cs`** để mô phỏng các kịch bản suy diễn phức tạp:

| Kịch bản | Dữ liệu đầu vào | Kết quả mong đợi |
| :--- | :--- | :--- |
| **Simple Rule** | `Emp.salary = 100` | `salary = 110` (Nếu bonus = 0.1) |
| **Multi-step** | `Update Rule R1` -> `Triggers R2` | Điểm đóng F-Closure cuối cùng. |
| **Recursive** | Luật đệ quy lồng nhau. | Dừng đúng lúc tại điểm đóng. |

### Minh chứng Mã nguồn (F-Closure):
![code_test_f_closure.png](../assets/diagrams/code_test_f_closure.png)
*Hình 8.3: Kịch bản kiểm thử thuật toán F-Closure nâng cao.*

### Minh chứng Kết quả (Log Trace):
![result_test_f_closure.png](../assets/diagrams/result_test_f_closure.png)
*Hình 8.4: Kết quả tìm tập đóng và vết suy diễn tương ứng.*

## 2. Kiểm thử Logic Toán học (Solver Testing)

Xác thực tính năng giải hệ phương trình thông qua `Newton-Raphson` và `Brent Solver`.

![Placeholder: Ảnh chụp nhật ký log của Engine khi thực hiện tìm nghiệm của một phương trình bậc hai, hiển thị số bước lặp (Iterations) và sai số (Error margin)](../assets/diagrams/placeholder_solver_iterations_log.png)

---

> [!TIP]
> **Chứng minh thực tế**: Khi chạy `full_test.kbql`, các luật tính toán lương và doanh thu được kích hoạt và kiểm toán 100% về độ chính xác số học.
