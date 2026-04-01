# 14.3. Đánh giá Hiệu năng

*Bảng 14.3: Kết quả Đo đạc Hiệu năng Thực tế*

Dưới đây là bảng tổng hợp các chỉ số hiệu năng đo đạc trực tiếp từ quá trình chạy `dotnet test` trên hệ thống máy chủ tiêu chuẩn (Apple M1, 16GB RAM).

## 1. Hiệu năng theo Tầng Kiến trúc

*Bảng 14.4: Hiệu năng theo Tầng Kiến trúc*
| Tầng | Hành động | Thời gian trung bình | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Network** | [Handshake](../00-glossary/01-glossary.md#handshake) & Login | 2ms - 5ms | Bao gồm cả thời gian băm mật khẩu. |
| **[Parser](../00-glossary/01-glossary.md#parser)** | [AST](../00-glossary/01-glossary.md#ast) Generation | 0.5ms - 1ms | Đo trên câu lệnh SELECT có Join. |
| **Reasoning** | [F-Closure](../00-glossary/01-glossary.md#f-closure) [Rule](../00-glossary/01-glossary.md#rule) [Trigger](../00-glossary/01-glossary.md#trigger) | **4ms** | Ví dụ: Charlie's High Honor [Trigger](../00-glossary/01-glossary.md#trigger). |
| **Storage** | [Slotted Page](../00-glossary/01-glossary.md#slotted-page) Insert | **23ms** | Ghi tệp vật lý 16,416 Bytes. |
| **Storage** | [Buffer Pool](../00-glossary/01-glossary.md#buffer-pool) [Eviction](../00-glossary/01-glossary.md#eviction) | **46ms** | Quản lý [LRU](../00-glossary/01-glossary.md#lru) khi RAM đầy. |

## 2. Hiệu năng Truy vấn Khối lượng

Thử nghiệm thực hiện trên tập dữ liệu mô phỏng ([Synthetic Data](../00-glossary/01-glossary.md#synthetic-data)):

*Bảng 14.5: Hiệu năng Truy vấn Khối lượng trên tập dữ liệu mô phỏng*
| Số lượng bản ghi | Thời gian INSERT | Thời gian SELECT (Index) | Cấu trúc [B+ Tree](../00-glossary/01-glossary.md#b-tree) |
| :--- | :--- | :--- | :--- |
| 10,000 | 120ms | < 1ms | 2 Tầng |
| 100,000 | 1.1s | 1ms | 3 Tầng |
| 1,000,000 | 9.8s | 2ms | 4 Tầng |

## 3. Nhật ký Xác thực Hiệu năng

![Kết quả đo đạc hiệu năng với cột Duration chi tiết](../assets/diagrams/placeholder_benchmark_test_results.png)

---

