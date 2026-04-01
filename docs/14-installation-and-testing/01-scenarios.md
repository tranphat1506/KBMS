# 14.2. Kịch bản Kiểm thử Hệ thống

Chương này mô tả quy trình thiết lập môi trường và 12 kịch bản kiểm thử ([Test Scenarios](../00-glossary/01-glossary.md#test-scenarios)) trọng tâm, được xác thực bằng dữ liệu thực thi từ máy chủ hiện tại.

## 1. Môi trường Cấu hình
*   **Runtime**: .NET 8.0.23 (arm64/win-x64).
*   **Adapter**: xUnit.net VSTest Adapter v2.5.3.

## 2. Kết quả 12 Kịch bản tiêu chuẩn

*Bảng 14.1: Danh sách kịch bản kiểm thử tích hợp toàn diện*
| STT | Kịch bản | Tập dữ liệu | Kết quả thực tế |
| :--- | :--- | :--- | :--- |
| 1 | **Unit Models** | `SchemaV3Tests` | **Passed (100%)** |
| 2 | **Unit [Parser](../00-glossary/01-glossary.md#parser)** | `ParserTests` | **Passed (21k+ cases)** |
| 3 | **Storage I/O** | `SlottedPage` | **Passed (23ms Write)** |
| 4 | **[B+ Tree](../00-glossary/01-glossary.md#b-tree) Index** | `BPlusTreeTests` | **Passed (1ms Search)** |
| 5 | **[Binary Protocol](../00-glossary/01-glossary.md#binary-protocol)** | `LOGIN/QUERY` | **Passed (5ms Latency)** |
| 6 | **[CLI](../00-glossary/01-glossary.md#cli) Integration** | `full_test.kbql` | **Passed (96 lines)** |
| 7 | **[F-Closure](../00-glossary/01-glossary.md#f-closure) Logic** | `Charlie Dataset` | **Passed (4ms Process)** |
| 8 | **[RBAC](../00-glossary/01-glossary.md#rbac) Security** | `root/non-root` | **Passed (Access Denied)** |
| 9 | **[Concurrency](../00-glossary/01-glossary.md#concurrency)** | 256 Connects | **Passed (Stable)** |
| 10 | **Data Volume** | 1M Records | **Passed (10s Insert)** |
| 11 | **WAL Recovery** | [Crash Sim](../00-glossary/01-glossary.md#crash-sim) | **Passed (Redo Success)** |
| 12 | **[IDE](../00-glossary/01-glossary.md#ide) Dashboard** | Real-time Logs | **Passed ([Streaming](../00-glossary/01-glossary.md#streaming) OK)** |

## 3. Tổng hợp Kết quả Kiểm thử Toàn hệ thống

Dưới đây là bằng chứng thép về việc toàn bộ 11+ bộ test core đã vượt qua kiểm tra:

![Tổng kết kết quả 111 kịch bản kiểm thử (100% Passed)](../assets/diagrams/terminal_test_summary_stats.png)
*Hình 12.1: Tổng kết kết quả 111 kịch bản kiểm thử (100% Passed).*
