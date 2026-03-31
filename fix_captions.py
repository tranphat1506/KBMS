#!/usr/bin/env python3
"""
Automated caption fixer for KBMS thesis markdown files.
1. Adds *Bảng X.Y: [title]* captions to tables that lack them.
2. Replaces raw filename alt-text in images with descriptive Vietnamese text.
"""
import os, re

DOCS_DIR = 'docs'

# --- Mapping: chapter folder prefix -> chapter number ---
CHAPTER_MAP = {
    '03': 3, '04': 4, '05': 5, '06': 6, '07': 7, '08': 8,
    '09': 9, '10': 10, '11': 11, '12': 12, '13': 13, '14': 14
}

# --- Descriptive image names (filename -> Vietnamese alt-text) ---
IMAGE_NAMES = {
    # Chapter 6
    'placeholder_parser_test_log.png': 'Kết quả chạy bộ kiểm thử ParserTests',
    'placeholder_lexer_token_stream.png': 'Nhật ký Token Stream của câu lệnh SELECT phức tạp',
    # Chapter 8
    'placeholder_solver_iterations_log.png': 'Nhật ký log của Engine khi tìm nghiệm phương trình bậc hai',
    # Chapter 9
    'query_lifecycle_v3.png': 'Vòng đời xử lý truy vấn xuyên suốt 4 tầng kiến trúc',
    'packet_processing_v3.png': 'Quy trình đóng gói và phân rã gói tin nhị phân',
    'code_test_binary_protocol.png': 'Kịch bản kiểm thử đóng gói và phân rã gói tin nhị phân',
    'terminal_test_protocol_hex.png': 'Nhật ký gói tin nhị phân (Little-endian) truyền trên Socket',
    'terminal_test_concurrent_clients.png': 'Bằng chứng Server xử lý đồng thời nhiều Client mà không nghẽn',
    # Chapter 10
    'async_threading_model.png': 'Mô hình xử lý bất đồng bộ sử dụng ThreadPool của .NET',
    'knowledge_manager_v3.png': 'Sơ đồ tương tác giữa Knowledge Manager và các phân hệ cấp thấp',
    'server_boot_flow.png': 'Luồng khởi động Server và đăng ký các Manager',
    'terminal_test_server_bootlogs.png': 'Nhật ký khởi động máy chủ thành công với đầy đủ các Manager',
    'code_test_server_integration.png': 'Kịch bản kiểm thử tích hợp End-to-End thông qua TCP Sockets',
    'result_test_cli.png': 'Kết quả thực thi 111 kịch bản kiểm thử tích hợp toàn diện',
    'code_test_security.png': 'Minh chứng mã nguồn kiểm thử bảo mật RBAC',
    'result_test_security.png': 'Kết quả từ chối truy cập đối với người dùng không có quyền',
    # Chapter 11
    'lexer_token_flow_v2.png': 'Luồng Token được phân tách bởi Lexer trước khi đưa vào Parser',
    'ast_tree_layout_v2.png': 'Cấu trúc cây phân tích cú pháp (AST) của KBQL',
    'ast_overview_v2.png': 'Tổng quan kiến trúc nút AST của ngôn ngữ KBQL',
    'ast_kdl_detail.png': 'Sơ đồ chi tiết nút AST cho ngôn ngữ KDL',
    'ast_kql_kml_detail.png': 'Sơ đồ chi tiết nút AST cho ngôn ngữ KQL và KML',
    'ast_kcl_tcl_detail.png': 'Sơ đồ chi tiết nút AST cho ngôn ngữ KCL và TCL',
    'code_test_parser.png': 'Minh chứng mã nguồn kiểm thử độ bao phủ của bộ phân tích',
    'result_test_parser.png': 'Kết quả vượt qua toàn bộ 21000+ kịch bản kiểm thử cú pháp',
    'terminal_test_parser_nested.png': 'Chứng minh Parser xử lý thành công các biểu thức lồng nhau phức tạp',
    # Chapter 12
    'cli_input_flow.png': 'Sơ đồ luồng xử lý đầu vào trong giao diện CLI',
    'uc_cli_batch_source.png': 'Kịch bản thực thi hàng loạt qua lệnh SOURCE trong CLI',
    'uc_cli_vertical_mode.png': 'Chế độ hiển thị dọc (Vertical Mode) trong CLI',
    'code_test_cli.png': 'Minh chứng mã nguồn kiểm thử giao diện dòng lệnh CLI',
    'terminal_test_cli_query.png': 'Giao diện tương tác CLI thực thi câu lệnh truy vấn tri thức',
    'placeholder_cli_describe_output.png': 'Kết quả lệnh DESCRIBE hiển thị bảng biến và kiểu dữ liệu trong CLI',
    # Chapter 13
    '4_tier_studio_flow.png': 'Luồng xử lý một yêu cầu tri thức xuyên suốt 4 tầng từ Studio UI',
    '4_tier_notification_flow.png': 'Cơ chế Server Push cho các thông báo hệ thống thời gian thực',
    'code_test_dashboard.png': 'Minh chứng mã nguồn API giám sát Dashboard',
    'studio_concept_editor.png': 'Giao diện soạn thảo tri thức trực quan trong KBMS Studio',
    'terminal_test_studio_electron.png': 'Chứng minh luồng dữ liệu Studio và Electron Main truyền nhận gói tin nhị phân',
    # Chapter 14
    'placeholder_windows_install_success.png': 'Giao diện cài đặt thành công KBMS trên Windows',
    'placeholder_macos_install_success.png': 'Giao diện cài đặt thành công KBMS trên macOS',
    'placeholder_linux_install_success.png': 'Giao diện cài đặt thành công KBMS trên Linux',
    'terminal_test_summary_stats.png': 'Tổng kết kết quả 111 kịch bản kiểm thử (100% Passed)',
    'placeholder_benchmark_test_results.png': 'Kết quả đo đạc hiệu năng với cột Duration chi tiết',
    'terminal_test_reasoning_trace.png': 'Vết suy diễn của thuật toán F-Closure trên dữ liệu kiểm thử',
    # Chapter 7
    'code_test_io_buffer.png': 'Kịch bản kiểm thử luồng I/O và Buffer Pool',
    'terminal_test_raw_page.png': 'Kết quả đọc trang nhị phân trực tiếp từ đĩa cứng',
    'code_test_btree_index.png': 'Kịch bản kiểm thử hiệu năng chỉ mục B+ Tree',
    'terminal_test_btree_search.png': 'Kết quả tìm kiếm và duyệt cây B+ Tree',
    'code_test_wal_recovery.png': 'Kịch bản kiểm thử khả năng phục hồi dữ liệu từ file WAL',
    'terminal_test_wal_recovery.png': 'Bằng chứng phục hồi trạng thái Concept thành công sau giả lập mất điện',
    # Chapter 8 Reasoning
    'code_test_f_closure.png': 'Kịch bản kiểm thử thuật toán F-Closure nâng cao',
    'result_test_f_closure.png': 'Kết quả tìm tập đóng và vết suy diễn tương ứng',
}

# --- Table title templates per chapter ---
TABLE_TITLES = {
    '04-architecture/01-system-overview.md': ['Mô tả chức năng 4 tầng kiến trúc KBMS', 'Đặc tả trách nhiệm từng tầng xử lý'],
    '04-architecture/06-requirements-spec.md': ['Đặc tả yêu cầu phi chức năng hệ thống KBMS', 'Ma trận công nghệ và thành phần triển khai'],
    '06-kbql-reference/01-introduction.md': ['Phân loại Từ khóa dành riêng (Reserved Keywords)', 'Bảng toán tử và ký tự đặc biệt trong KBQL', 'Phân loại Kiểu dữ liệu nguyên thuỷ trong KBQL', 'Danh mục câu lệnh nhóm KDL', 'Danh mục câu lệnh nhóm KQL và KML'],
    '06-kbql-reference/08-expressions.md': ['Bảng thứ tự ưu tiên toán tử (Operator Precedence)'],
    '06-kbql-reference/10-language-validation.md': ['Phân bổ kịch bản kiểm thử theo nhóm ngôn ngữ'],
    '07-storage/01-overview.md': ['Cấu trúc Header của trang dữ liệu', 'Chiến lược quản lý vùng đệm (Buffer Pool)', 'Cấu trúc bản ghi WAL'],
    '07-storage/03-page-layout.md': ['Cấu trúc chi tiết Slotted-Page Header', 'Cấu trúc Slot Entry'],
    '07-storage/05-disk-management.md': ['Cấu trúc tệp dữ liệu vật lý'],
    '07-storage/06-file-formats.md': ['Đặc tả định dạng tệp nhị phân KBMS'],
    '07-storage/07-storage-validation.md': ['Tổng hợp kết quả kiểm thử tầng lưu trữ'],
    '08-reasoning/02-algorithms.md': ['Bảng ký hiệu thuật toán F-Closure', 'Phân tích độ phức tạp thuật toán suy diễn'],
    '08-reasoning/03-reasoning-validation.md': ['Tổng hợp kết quả kiểm thử bộ máy suy diễn'],
    '09-network/01-protocol.md': ['Đặc tả cấu trúc gói tin nhị phân KBMS'],
    '10-server/05-server-validation.md': ['Tổng hợp kết quả kiểm thử hệ thống máy chủ'],
    '11-parser/01-lexer-deep-dive.md': ['Phân loại các nhóm Token trong Lexer'],
    '12-cli/01-overview.md': ['Danh mục lệnh điều khiển CLI'],
    '12-cli/04-cli-validation.md': ['Kiểm thử định dạng kết quả truy vấn trong CLI'],
    '14-installation-and-testing/00-installation-guide.md': ['Yêu cầu cấu hình hệ thống tối thiểu'],
    '14-installation-and-testing/01-scenarios.md': ['Danh sách kịch bản kiểm thử tích hợp toàn diện', 'Kết quả kiểm thử tổng hợp'],
}

def get_chapter_num(filepath):
    """Extract chapter number from filepath like docs/06-kbql-reference/..."""
    parts = filepath.split('/')
    for p in parts:
        m = re.match(r'^(\d{2})-', p)
        if m:
            return CHAPTER_MAP.get(m.group(1), int(m.group(1)))
    return 0

def fix_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    chapter = get_chapter_num(filepath)
    rel_path = filepath.replace('docs/', '')
    
    # --- Fix image alt-text ---
    def fix_image(match):
        alt = match.group(1)
        path = match.group(2)
        fname = path.split('/')[-1]
        
        # If alt is just a filename (raw), replace with descriptive text
        if fname.replace('.png','').replace('.mmd','') == alt.replace('.png','').replace('.mmd','') or alt.startswith('Placeholder:'):
            new_alt = IMAGE_NAMES.get(fname, alt)
            if new_alt != alt:
                return f'![{new_alt}]({path})'
        return match.group(0)
    
    content = re.sub(r'!\[([^\]]*)\]\(([^\)]*)\)', fix_image, content)
    
    # --- Fix table captions ---
    table_titles = TABLE_TITLES.get(rel_path, [])
    table_idx = 0
    tbl_counter = [1]  # mutable counter
    
    def add_table_caption(match):
        nonlocal table_idx
        full_match = match.group(0)
        # Check if there's already a *Bảng caption above
        before = content[:match.start()]
        lines_before = before.split('\n')
        # Look at the last few non-empty lines before the table
        for i in range(min(5, len(lines_before)), 0, -1):
            line = lines_before[-i].strip()
            if re.search(r'Bảng\s+\d+\.\d+', line):
                return full_match  # Already has caption
        
        title = table_titles[table_idx] if table_idx < len(table_titles) else f"Đặc tả dữ liệu"
        caption = f"\n*Bảng {chapter}.{tbl_counter[0]}: {title}*\n"
        table_idx += 1
        tbl_counter[0] += 1
        return caption + full_match
    
    # Only add captions to tables that don't have them
    # Find table blocks: lines starting with | followed by separator |---|
    if table_titles:  # Only process if we have titles for this file
        lines = content.split('\n')
        new_lines = []
        i = 0
        while i < len(lines):
            line = lines[i]
            # Detect start of a table (header line with |)
            if '|' in line and i + 1 < len(lines) and re.match(r'\s*\|[\s\-:]+\|', lines[i+1]):
                # Check if previous non-empty line is already a caption
                has_caption = False
                for j in range(i-1, max(i-4, -1), -1):
                    prev = lines[j].strip()
                    if prev and re.search(r'Bảng\s+\d+', prev):
                        has_caption = True
                        break
                    if prev and not prev.startswith('#'):
                        break
                
                if not has_caption and table_idx < len(table_titles):
                    title = table_titles[table_idx]
                    new_lines.append(f"*Bảng {chapter}.{tbl_counter[0]}: {title}*")
                    table_idx += 1
                    tbl_counter[0] += 1
            
            new_lines.append(line)
            i += 1
        content = '\n'.join(new_lines)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  FIXED: {filepath}")
    else:
        print(f"  OK:    {filepath}")

def main():
    print("=== KBMS Caption Fixer ===")
    for root, dirs, files in sorted(os.walk(DOCS_DIR)):
        dirs.sort()
        for fname in sorted(files):
            if fname.endswith('.md'):
                fix_file(os.path.join(root, fname))
    print("\nDone! Run publish_latex_v3.py to regenerate the PDF.")

if __name__ == '__main__':
    main()
