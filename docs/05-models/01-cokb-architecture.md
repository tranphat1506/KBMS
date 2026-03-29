# 05.1. Kiến trúc Mô hình COKB

Mô hình **COKB (Computational Objects Knowledge Base)** là nền tảng lý thuyết cốt lõi của KBMS [1], cho phép biểu diễn tri thức dưới dạng các đối tượng có khả năng tính toán và suy diễn [4].

## 1. Thành phần của mô hình COKB

Theo định nghĩa toán học và cấu trúc mã nguồn trong `KBMS.Models`, một hệ tri thức COKB được xác định bởi bộ ba:

$$COKB = (C, O, R)$$

Trong đó:
*   **C (Concepts)**: Tập hợp các khái niệm (lớp đối tượng).
*   **O (Objects)**: Tập hợp các thực thể (instances).
*   **R (Rules)**: Tập hợp các luật và công thức toán học.

## 2. Kiểm thử Mô hình (Model Verification)

Hệ thống cung cấp bộ kịch bản kiểm thử đơn vị cưỡng bức cho tầng Models nhằm đảm bảo tính toàn vẹn của dữ liệu:

| Tệp Kiểm thử | Chức năng kiểm tra | Tình trạng |
| :--- | :--- | :--- |
| `SchemaV3Tests.cs` | Kiểm thử định nghĩa Concept và Metadata. | **OK** |
| `TrueTypingTests.cs` | Kiểm thử kiểu dữ liệu tĩnh và động (Static/Dynamic Types). | **OK** |
| `KnowledgeUpdateTests.cs` | Kiểm thử cập nhật thuộc tính đối tượng. | **OK** |

![Placeholder: Ảnh chụp màn hình CLI chạy unit test 'dotnet test KBMS.Tests --filter Category=Models' cho thấy 100% test case thành công](../assets/diagrams/placeholder_model_unit_test_log.png)
*Hình 5.1: Kết quả kiểm thử đơn vị cho các thành phần của tầng Models.*
