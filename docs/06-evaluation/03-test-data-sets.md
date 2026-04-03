# 6.3. Chi tiết Các Nhóm Kiểm thử

## 1. GROUP 1: Performance Benchmarks

**Filter:** `FullyQualifiedName=KBMS.Tests.PerformanceBenchmarkV3`

*Bảng 6.5: Các bài kiểm tra hiệu năng*

| Test Case | Mô tả |
|-----------|-------|
| Stress Test 1M | Insert 1 triệu bản ghi |
| Throughput Test | Đo thông lượng ghi/đọc |
| Buffer Pool Test | So sánh các cấu hình cache |

**Nguồn dữ liệu kết quả:**
- `storage_v3_results.txt`
- `buffer_pool_comparison.txt`

---

## 2. GROUP 2: Storage Architecture

**Filter:** `FullyQualifiedName~StorageV3Tests|FullyQualifiedName~SystemV3Tests|FullyQualifiedName~ModelBinaryUtility`

*Bảng 6.6: Các bài kiểm tra kiến trúc lưu trữ*

| Test Class | Mô tả |
|------------|-------|
| StorageV3Tests | Slotted Page, B+ Tree operations |
| SystemV3Tests | System Catalog, Metadata |
| ModelBinaryUtilityTests | Serialization, Binary encoding |

---

## 3. GROUP 3: Transactions & WAL

**Filter:** `FullyQualifiedName~TransactionV3Tests`

*Bảng 6.7: Các bài kiểm tra giao dịch và WAL*

| Test Case | Mô tả |
|-----------|-------|
| Transaction Commit | Kiểm tra commit thành công |
| Transaction Rollback | Kiểm tra rollback |
| WAL Recovery | Phục hồi sau crash |
| ACID Verification | Đảm bảo tính ACID |

---

## 4. GROUP 4: Query Engine

**Filter:** `FullyQualifiedName~DataOperationsV3Tests|FullyQualifiedName~ExecutionV3Tests|FullyQualifiedName~FullIntegrationV3Tests`

*Bảng 6.8: Các bài kiểm tra Query Engine*

| Test Class | Mô tả |
|------------|-------|
| DataOperationsV3Tests | INSERT, SELECT, UPDATE, DELETE |
| ExecutionV3Tests | Query plan, Optimization |
| FullIntegrationV3Tests | End-to-end scenarios |

---

## 5. GROUP 5: Schema Evolution

**Filter:** `FullyQualifiedName~SchemaV3Tests|FullyQualifiedName~ExhaustiveAlterIntegration`

*Bảng 6.9: Các bài kiểm tra tiến hóa schema*

| Test Class | Mô tả |
|------------|-------|
| SchemaV3Tests | ALTER CONCEPT operations |
| ExhaustiveAlterIntegrationTests | Data preservation, Migration |

---

## 6. GROUP 6: Reasoning

**Filter:** `FullyQualifiedName~BackwardChainingTests|FullyQualifiedName~Phase5ForwardChainingTests|FullyQualifiedName~ReteCoordination`

*Bảng 6.10: Các bài kiểm tra suy diễn*

| Test Class | Mô tả |
|------------|-------|
| BackwardChainingTests | Goal-directed reasoning |
| Phase5ForwardChainingTests | Data-driven reasoning |
| ReteCoordinationTests | Rete network, Pattern matching |

---

## 7. GROUP 7: Language Design

**Filter:** `FullyQualifiedName~LexerTests|FullyQualifiedName~ParserTests`

*Bảng 6.11: Các bài kiểm tra ngôn ngữ KBQL*

| Test Class | Mô tả |
|------------|-------|
| LexerTests | Tokenization |
| ParserTests | AST generation |
