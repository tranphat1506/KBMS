# 14.4. Các Bộ Dữ liệu Kiểm thử

Hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) sử dụng các bộ dữ liệu (Datasets) được định nghĩa chuẩn hóa trong thư mục `/datasets` để phục vụ cho các bài kiểm thử tự động và thủ công.

## 1. Danh sách Datasets Chuẩn

*Bảng 14.2: Danh mục các Bộ Dữ liệu Kiểm thử chuẩn*
| Tên Bộ dữ liệu | Tệp tin nguồn | Mô tả mục tiêu | Kỹ thuật kiểm thử |
| :--- | :--- | :--- | :--- |
| **Enterprise IT** | [enterprise_it.kbql](../../datasets/enterprise_it.kbql) | Mẫu dữ liệu nhân viên, phòng ban. | Join, Metadata, RBAC. |
| **Charlie Reasoning** | [charlie_reasoning.kbql](../../datasets/charlie_reasoning.kbql) | Mẫu dữ liệu sinh viên và điểm số. | F-Closure, Forward Chaining. |
| **Stress Volume** | [stress_volume.kbql](../../datasets/stress_volume.kbql) | Mẫu dữ liệu lớn (BigData). | Load Test, Volume, Indexing. |

## 2. Chi tiết Bộ dữ liệu trọng tâm: "Charlie"

Sử dụng trong `Phase5ForwardChainingTests.cs` để kiểm tra khả năng suy diễn đa tầng, dựa trên mô hình tri thức mẫu [2].

### Dữ liệu nguồn:
*Bảng 14.6: Dữ liệu nguồn bộ kiểm thử Charlie*
| name | grade | honor | gifted |
| :--- | :--- | :--- | :--- |
| Charlie | 95 | *Tự động (High)* | *Tự động (true)* |

### Logic Luật áp dụng:
1.  **[Rule](../00-glossary/01-glossary.md#rule) 1**: Nếu `grade >= 90` thì `honor = 'High'`.
2.  **Rule 2**: Nếu `honor = 'High'` thì `gifted = true`.

## 3. Nhật ký Logic suy diễn

Dưới đây là bằng chứng thực tế khi hệ thống xử lý bộ dữ liệu Charlie từ tệp `charlie_reasoning.kbql`:

![Vết suy diễn của thuật toán F-Closure trên dữ liệu kiểm thử](../assets/diagrams/terminal_test_reasoning_trace.png)

---

