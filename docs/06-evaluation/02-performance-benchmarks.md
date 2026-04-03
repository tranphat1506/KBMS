# 6.2. Đánh giá Hiệu năng

Chương này tập trung vào các chỉ số hiệu năng (Metrics) được đo đạc trực tiếp từ `PerformanceBenchmarkV3`.

## 1. Performance Benchmark V3

**Test Class:** `KBMS.Tests.PerformanceBenchmarkV3`

Các bài kiểm tra hiệu năng đo lường khả năng xử lý của Storage Engine V3:

### 1.1. Stress Test 1 Triệu Bản ghi

Hệ thống được test với 3 kích thước dữ liệu:

*Bảng 6.1: Kết quả stress test theo kích thước dữ liệu*

| Dataset | Số bản ghi | Thao tác | Kết quả |
|---------|------------|----------|---------|
| DS-S | 10,000 | INSERT | Baseline |
| DS-M | 100,000 | INSERT | Medium load |
| DS-L | 1,000,000 | INSERT | Stress test |

### 1.2. Kết quả Benchmark

Từ `storage_v3_results.txt`:

*Bảng 6.2: Nhật ký benchmark hệ thống KBMS V3*

```
=== KBMS V3 COMPREHENSIVE PERFORMANCE REPORT ===
[Storage] DS-L (1 Million Records) Load: ~5 seconds
[Storage] Throughput Peak: 200,000+ ops/sec
[Storage] DS-L Index Search (1M records): ~1.00ms (Avg)
[Engine] Hash Join (10k x 10k): ~7.0ms
================================================
```

## 2. Buffer Pool Comparison

**Nguồn:** `buffer_pool_comparison.txt`

### 2.1. Memory vs I/O Tradeoff

*Bảng 6.3: So sánh hiệu năng theo cấu hình Buffer Pool*

| Cấu hình Buffer Pool | RAM Usage | Disk I/O | Thông lượng |
|---------------------|-----------|----------|-------------|
| No Buffer | Minimal | High | Low |
| 64MB Buffer | 64MB | Medium | Medium |
| 256MB Buffer | 256MB | Minimal | High |

### 2.2. LRU Cache Effectiveness

Buffer Pool Manager sử dụng thuật toán LRU (Least Recently Used) để quản lý page cache:
- Hit ratio tăng khi buffer size tăng
- Disk I/O giảm đáng kể với buffer đủ lớn
- Zero disk writes khi buffer pool đủ chứa toàn bộ working set

## 3. Kết luận Hiệu năng

*Bảng 6.4: Tổng kết chỉ số hiệu năng KBMS V3*

| Chỉ số | Giá trị |
|--------|---------|
| Thông lượng tối đa | 200,000+ ops/sec |
| Độ trễ truy vấn | < 10ms |
| Khả năng mở rộng | Linear scaling |
