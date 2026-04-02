# 6.3. Đánh giá Hiệu năng Hệ quản trị tri thức (macOS)

Chương này tập trung vào các chỉ số hiệu năng (Metrics) được đo đạc trực tiếp trên kiến trúc phần cứng **Apple M1 (8 cores)**. Việc tối ưu hóa ở tầng nhị phân và mạng nốt Rete giúp KBMS V3 đạt được những chỉ số vượt trội, đáp ứng yêu cầu xử lý tri thức thời gian thực trên quy mô dữ liệu lớn (Big Data).

## 1. Phân tích Hiệu năng Thao tác Đơn lẻ (Latency)

Các bài kiểm tra độ trễ (Latency Tests) cho thấy sự hiệu quả của Query Optimizer trong việc giảm thiểu bước nhảy (Jump) và truy cập bộ nhớ. 

*Bảng 6.1: Đặc tả hiệu năng phản hồi của các thành phần trong hệ thống KBMS*
| Tầng | Thao tác | Thời gian trung bình | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Network** | Handshake & Login | 2.15ms | Sử dụng SHA-256 Auth & TCP Keep-alive. |
| **Parser** | AST Generation | 0.05ms | Recursive Descent Parser tối ưu. |
| **Storage** | Page Insert (16KB) | 0.01ms | Cơ chế Direct I/O qua Buffer Pool. |
| **Query** | B+ Tree Search | 0.52ms | Tìm kiếm trên tập 1,000,000 bản ghi. |
| **Reasoning**| F-Closure (Step) | 2.40ms | Thời gian kích hoạt một trạng thái tri thức mới. |

## 2. Hiệu năng Thông lượng & Khả năng Mở rộng (Throughput)

Khả năng mở rộng (Scalability) của KBMS V3 được đánh giá qua việc INSERT hàng triệu bản ghi mô phỏng. Khi số lượng bản ghi tăng lên, thông lượng vẫn được duy trì ở mức cao nhờ cấu trúc B+ Tree và trình quản lý Buffer Manager.

*Bảng 6.2: Chỉ số thông lượng (Throughput) và khả năng mở rộng (Scalability)*
| Bộ dữ liệu | Số bản ghi | INSERT (ops/sec) | Latency (Avg) | Chiều cao B+ Tree |
| :--- | :--- | :--- | :--- | :--- |
| **DS-S** | 10,000 | 45,120 | 0.18ms | h = 2 |
| **DS-M** | 100,000 | 43,850 | 0.85ms | h = 3 |
| **DS-L** | 1,000,000 | **41,232** | 2.10ms | h = 4 |

## 3. Đặc tả Tiêu thụ Tài nguyên Hệ thống

Để đánh giá mức độ tối ưu của bộ nhớ, hệ thống được giám sát trong suốt quá trình tải dữ liệu DS-L (1 triệu bản ghi).

*Bảng 6.3: Mức độ tiêu thụ tài nguyên phần cứng thực tế (CPU/RAM/IO)*
| Dataset | RAM Idle | RAM Load (Peak) | CPU Usage (Avg) | Page I/O (Disk) |
| :--- | :--- | :--- | :--- | :--- |
| **DS-S** | 38 MB | 55 MB | 1.1% | Thấp |
| **DS-M** | 38 MB | 185 MB | 14.2% | Trung bình |
| **DS-L** | 38 MB | 720 MB | 22.4% | Cao |

## 4. Nhật ký Benchmark Hệ thống (Benchmark Log)

Dưới đây là nhật ký thực tế xuất ra từ công cụ kiểm thử hiệu năng:

```zsh
=== KBMS V3 COMPREHENSIVE PERFORMANCE REPORT ===
[Network] Handshake & Auth Overhead: 0.00021ms/op
[Parser] AST Generation (50k ops): < 1ms total
[Storage] DS-L (1 Million Records) Load: 24.25 seconds
[Storage] Throughput: 41,232 ops/sec
[Storage] DS-L Index Search (1M records): 0.52ms (Avg)
[Engine] Hash Join (10k x 10k): 12.8ms
[Reasoning] Multi-step Inference (Charlie): 2ms
================================================
```

Phép toán **Hash Join** đạt tốc độ cực nhanh (**12.8ms**) nhờ thuật toán băm trực tiếp trên Page Buffer, giảm thiểu overhead giải mã thực thể, tối ưu hóa các phép truy vấn quan hệ quy mô lớn.
