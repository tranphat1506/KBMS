# 6.1. Kịch bản Kiểm thử

Hệ thống KBMS V3 được xác thực thông qua 7 nhóm kiểm thử (Test Groups) được tự động hóa trong script `run_all_and_report.sh`. Mỗi nhóm tập trung vào một khía cạnh cụ thể của hệ thống.

## 1. Nhóm 1: Performance Benchmarks (Stress Test 1M)

**Test Class:** `PerformanceBenchmarkV3`

Mục tiêu: Đánh giá hiệu năng tối đa của Storage Engine V3 trên tập dữ liệu lớn (1 triệu bản ghi).

**Các chỉ số đo đạc:**
- Thông lượng ghi tối đa (ops/sec)
- Độ trễ truy vấn trung bình
- Hiệu quả Buffer Pool Manager
- Memory vs I/O Tradeoff

## 2. Nhóm 2: Storage Architecture (Slotted Page & Persistence)

**Test Classes:**
- `StorageV3Tests` - Kiểm tra Slotted Page, B+ Tree Index
- `SystemV3Tests` - Kiểm tra System Catalog
- `ModelBinaryUtilityTests` - Kiểm tra serialization nhị phân

Mục tiêu: Xác minh kiến trúc lưu trữ vật lý và khả năng persistence.

## 3. Nhóm 3: Transactions & WAL (Atomicity & Recovery)

**Test Class:** `TransactionV3Tests`

Mục tiêu: Đảm bảo tính ACID của giao dịch và khả năng phục hồi sau sự cố.

## 4. Nhóm 4: Query Engine (KQL CRUD & Execution)

**Test Classes:**
- `DataOperationsV3Tests` - CRUD operations
- `ExecutionV3Tests` - Query execution plans
- `FullIntegrationV3Tests` - Integration tests toàn diện

Mục tiêu: Xác thực Query Engine xử lý đúng các câu lệnh KBQL.

## 5. Nhóm 5: Schema Evolution (ALTER CONCEPT & Migration)

**Test Classes:**
- `SchemaV3Tests` - Kiểm tra ALTER CONCEPT
- `ExhaustiveAlterIntegrationTests` - Integration tests đầy đủ

Mục tiêu: Đánh giá khả năng tiến hóa schema mà không làm mất dữ liệu.

## 6. Nhóm 6: Reasoning (Forward/Backward Chaining)

**Test Classes:**
- `BackwardChainingTests` - Kiểm tra Backward Chaining
- `Phase5ForwardChainingTests` - Kiểm tra Forward Chaining
- `ReteCoordinationTests` - Kiểm tra mạng Rete

Mục tiêu: Xác thực Inference Engine thực hiện suy diễn đúng.

## 7. Nhóm 7: Language Design (Lexer & Parser)

**Test Classes:**
- `LexerTests` - Kiểm tra tokenization
- `ParserTests` - Kiểm tra AST generation

Mục tiêu: Xác thực Lexer và Parser phân tích đúng cú pháp KBQL.
