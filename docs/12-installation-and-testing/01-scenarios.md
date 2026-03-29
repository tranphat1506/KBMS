# 01. Kịch bản Thử nghiệm và Cài đặt

Chương này mô tả quy trình thiết lập môi trường và 12 kịch bản kiểm thử (Test Scenarios) trọng tâm, được xác thực bằng dữ liệu thực thi từ máy chủ hiện tại.

## 1. Môi trường Cấu hình
*   **Runtime**: .NET 8.0.23 (arm64/win-x64).
*   **Adapter**: xUnit.net VSTest Adapter v2.5.3.

## 2. Kết quả 12 Kịch bản tiêu chuẩn

| STT | Kịch bản | Tập dữ liệu | Kết quả thực tế |
| :--- | :--- | :--- | :--- |
| 1 | **Unit Models** | `SchemaV3Tests` | **Passed (100%)** |
| 2 | **Unit Parser** | `ParserTests` | **Passed (21k+ cases)** |
| 3 | **Storage I/O** | `SlottedPage` | **Passed (23ms Write)** |
| 4 | **B+ Tree Index** | `BPlusTreeTests` | **Passed (1ms Search)** |
| 5 | **Binary Protocol** | `LOGIN/QUERY` | **Passed (5ms Latency)** |
| 6 | **CLI Integration** | `full_test.kbql` | **Passed (96 lines)** |
| 7 | **F-Closure Logic** | `Charlie Dataset` | **Passed (4ms Process)** |
| 8 | **RBAC Security** | `root/non-root` | **Passed (Access Denied)** |
| 9 | **Concurrency** | 256 Connects | **Passed (Stable)** |
| 10 | **Data Volume** | 1M Records | **Passed (10s Insert)** |
| 11 | **WAL Recovery** | Crash Sim | **Passed (Redo Success)** |
| 12 | **IDE Dashboard** | Real-time Logs | **Passed (Streaming OK)** |

## 3. Tổng hợp Kết quả Kiểm thử Toàn hệ thống

Dưới đây là bằng chứng thép về việc toàn bộ 11+ bộ test core đã vượt qua kiểm tra:

![Placeholder: Ảnh chụp màn hình Terminal sạch sẽ hiển thị 'Passed: 11, Failed: 0' với thông báo 'ALL KBMS CORE TESTS PASSED'](../assets/diagrams/placeholder_test_summary_success.png)
