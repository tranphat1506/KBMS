# DIAGRAMS QUAN TRỌNG CHO BÀI THUYẾT TRÌNH

Vị trí: `/docs/assets/diagrams/`

---

## SLIDES NÊN DÙNG DIAGRAM NÀO

### 1. Slide Kiến trúc Tổng quan
```
kbms_4_tier_architecture.png    ← CHÍNH - 4 tầng kiến trúc
kbms_architecture_overview.png  ← Phụ - overview tổng quát
```

### 2. Slide Luồng Xử lý
```
kbms_request_flow_v3.png        ← CHÍNH - Request từ client đến storage
data_flow.png                   ← Phụ - Data flow tổng quát
```

### 3. Slide Parser/Lexer
```
lexer_token_flow_v2.png         ← Lexical analysis
ast_overview_v2.png             ← Abstract Syntax Tree
parser_pipeline.png (nếu có)
```

### 4. Slide Tầng Suy diễn
```
reasoning_architecture.png      ← CHÍNH - Inference Engine
rete_network_topology.png       ← Rete network
compilation_logic_flow.png      ← Compilation flow
```

### 5. Slide Tầng Lưu trữ
```
storage_architecture_v3.png     ← CHÍNH
btree_graph.png                 ← B+ Tree structure
buffer_pool_flow.png            ← Buffer Pool
binary_page_layout.png          ← Slotted Page
```

### 6. Slide Network
```
network_architecture_v3.png     ← Network layer
binary_protocol_exchange.png    ← Protocol
```

### 7. Slide Security
```
kbms_security_diagnostics_flow.png
audit_management_v3.png
```

---

## IN SLIDES

Để in diagrams chất lượng cao:
```bash
# Diagrams đã là .png, có thể chèn trực tiếp vào PowerPoint/Google Slides
# Kích thước chuẩn: 1920x1080 (HD)

# Hoặc convert sang PDF nếu cần
cd /Users/lechautranphat/Desktop/KBMS/docs/assets/diagrams/
```

---

## DANH SÁCH TẤT CẢ DIAGRAMS (163 files)

### Architecture (5 files)
- kbms_4_tier_architecture.png
- kbms_architecture_overview.png
- kbms_4layer_architecture.png
- network_architecture_v3.png
- storage_architecture_v3.png

### AST/Parser (10 files)
- ast_overview_v2.png
- ast_hierarchy_v3.png
- ast_tree_layout_v2.png
- ast_kdl_detail.png
- ast_kql_kml_detail.png
- ast_kcl_tcl_detail.png
- lexer_token_flow_v2.png
- lexer_tokenization_flow.png

### Reasoning (8 files)
- reasoning_architecture.png
- rete_network_topology.png
- compilation_logic_flow.png
- math_solving_flow.png
- forward_chaining_flow.png (nếu có)

### Storage (15 files)
- btree_graph.png
- btree_split_flow.png
- buffer_pool_flow.png
- binary_page_layout.png
- slotted_page_structure.png (nếu có)
- wal_flow.png (nếu có)

### Flow/Sequence (20 files)
- kbms_request_flow_v3.png
- data_flow.png
- cli_processing_flow.png
- 4_tier_studio_flow.png

### Security (5 files)
- kbms_security_diagnostics_flow.png
- audit_management_v3.png

---

## TIPS SỬ DỤNG DIAGRAM

1. **Không đưa quá nhiều text** - Diagram tự nói lên điều gì
2. **Zoom vào phần quan trọng** - Khi demo, có thể zoom vùng cần nói
3. **Số thứ tự** - Thêm số 1, 2, 3... để hướng dẫn mắt người xem
4. **Màu sắc** - Giải thích ý nghĩa màu (xanh = input, đỏ = output, vàng = processing)

