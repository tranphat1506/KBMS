# 📒 Hướng dẫn Chụp ảnh Minh chứng Kiểm thử (Mã nguồn & Kết quả)

Chào bạn, đây là bảng tra cứu đầy đủ nhất để bạn chụp ảnh cho toàn bộ 12 kịch bản kiểm thử trong luận văn. Mỗi kịch bản sẽ cần 2 ảnh: **Ảnh Mã nguồn (IDE)** và **Ảnh Kết quả (Terminal)**.

---

## ⚙️ Quy trình chung
1.  **Mở IDE (VS Code)**: Mở tệp `.cs` tương ứng, bôi đậm hoặc trỏ chuột vào tên hàm Test (`[Fact]`). Chụp màn hình (Lưu tên `code_test_...`).
2.  **Mở Terminal**: Chạy lệnh test tương ứng. Chụp màn hình kết quả xanh (Lưu tên `result_test_...`).
3.  **Lưu ảnh**: Copy toàn bộ vào `/docs/assets/diagrams/`.

---

## 📸 Danh sách 12 Kịch bản Kiểm thử

| STT | Kịch bản | Tệp Test nguồn (`KBMS.Tests/`) | Tên ảnh Mã nguồn (IDE) | Tên ảnh Kết quả (Terminal) |
| :--- | :--- | :--- | :--- | :--- |
| **1** | **Unit Models** | `SchemaV3Tests.cs` | `code_test_models.png` | `result_test_models.png` |
| **2** | **Unit Parser** | `ParserTests.cs` | `code_test_parser.png` | `result_test_parser.png` |
| **3** | **Storage I/O** | `StorageV3Tests.cs` | `code_test_storage_io.png` | `result_test_storage_io.png` |
| **4** | **B+ Tree Index** | `StorageV3Tests.cs` | `code_test_index.png` | `result_test_index.png` |
| **5** | **Binary Protocol** | `CliServerIntegrationTests.cs` | `code_test_network.png` | `result_test_network.png` |
| **6** | **CLI Integration** | `FullIntegrationV3Tests.cs` | `code_test_cli.png` | `result_test_cli.png` |
| **7** | **Reasoning Logic** | `Phase5ForwardChainingTests.cs` | `code_test_reasoning.png` | `result_test_reasoning.png` |
| **8** | **RBAC Security** | `AuthV3Tests.cs` | `code_test_security.png` | `result_test_security.png` |
| **9** | **Concurrency** | `LoadAndStressTests.cs` | `code_test_concurrency.png` | `result_test_concurrency.png` |
| **10** | **Data Volume** | `LoadAndStressTests.cs` | `code_test_volume.png` | `result_test_volume.png` |
| **11** | **WAL Recovery** | `TransactionV3Tests.cs` | `code_test_recovery.png` | `result_test_recovery.png` |
| **12** | **Dashboard API** | `DashboardApiTests.cs` | `code_test_dashboard.png` | `result_test_dashboard.png` |

---

## 🚀 Các lệnh chạy tương ứng (Terminal)

| STT | Lệnh thực thi | Mục tiêu cần thấy trong ảnh |
| :--- | :--- | :--- |
| **1-4** | `dotnet test --filter KBMS.Tests.StorageV3Tests` | Toàn bộ Page/Slot/LRU Success. |
| **2** | `dotnet test --filter KBMS.Tests.ParserTests` | 21,000+ tests passed. |
| **7** | `dotnet test --filter Phase5ForwardChainingTests --logger "console;verbosity=normal"` | Log suy diễn Charlie (4ms). |
| **9-10**| `dotnet test --filter LoadAndStressTests` | 256 Connects / 1000+ Inserts. |
| **ALL** | `dotnet test --logger "console;verbosity=minimal"` | Summary: `Passed: 111`, `Failed: 0`. |

---

## 📊 Hằng số Hiệu năng (Sử dụng ghi chú trong báo cáo)
*   **Reasoning**: 4ms
*   **Storage Write**: 23ms
*   **B+ Tree**: 1ms
*   **Buffer Eviction**: 46ms
*   **Login Latency**: 5ms
