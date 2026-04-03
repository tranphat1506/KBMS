# -*- coding: utf-8 -*-
import os
import re
import shutil

# --- Configuration ---
DOCS_DIR = 'docs'
LATEX_DIR = 'latex_report'
ASSETS_DEST = os.path.join(LATEX_DIR, 'assets')
OUTPUT_TEX = os.path.join(LATEX_DIR, 'report_final.tex')

def clean_title(s):
    """Aggressive 3-pass cleaning to remove (4.16), xx:, Figure:, etc."""
    if not s: return ""
    # Pass 1: Remove leading numbers/xx and brackets
    s = re.sub(r'^\s*[\(\[x\.]*[\d\.]+[x\.\d]*[\)\]\:]*\s*', '', s)
    # Pass 2: Remove Keywords like Hình, Bảng, Figure, xx
    s = re.sub(r'^\s*(xx|Bảng|Hình|Figure|Table|Hinh|Bang)[:\s\.]*', '', s, flags=re.IGNORECASE)
    # Pass 3: Catch combined or residual patterns like "Hình 4.19:" or "xx:"
    s = re.sub(r'^\s*[\(\[x\.]*[\d\.]+[x\.\d]*[\)\]\:]*\s*', '', s)
    s = re.sub(r'^\s*xx[:\s\.]*', '', s, flags=re.IGNORECASE)
    return s.strip().lstrip(': ').strip(' *_')

# --- Chapter Vietnamese Mapping ---
CHAPTER_NAMES = {
    '01-introduction': 'GIỚI THIỆU ĐỀ TÀI', 
    '02-theory': 'CƠ SỞ LÝ THUYẾT VÀ MÔ HÌNH TRI THỨC DẠNG COKB', 
    '03-analysis-and-design': 'PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG', 
    '04-architecture': 'KIẾN TRÚC HỆ THỐNG',
    '05-implementation': 'CÀI ĐẶT VÀ TRIỂN KHAI HỆ THỐNG',
    '06-evaluation': 'THỰC NGHIỆM VÀ ĐÁNH GIÁ HIỆU NĂNG',
    '07-conclusion': 'KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN',
}

MAP_ACADEMIC = {
    'system-overview': 'Tổng quan Kiến trúc Hệ thống',
    'models': 'Mô hình Hệ quản trị Cơ sở Tri thức',
    'storage': 'Cơ chế Lưu trữ và Quản lý Bộ nhớ',
    'kbql': 'Ngôn ngữ Truy vấn Tri thức (KBQL)',
    'reasoning-layer': 'Kiến trúc Tầng Suy luận',
    'application': 'Tầng Giao diện và Ứng dụng',
    'cli': 'Giao diện Dòng lệnh (CLI)',
    'studio': 'Giao diện Web Quản lý (KBMS STUDIO)',
    'overview': 'Tổng quan và Mục tiêu Hệ thống',
    'models': 'Mô hình Hệ quản trị Cơ sở Tri thức',
    'network-layer': 'Giao thức kết nối và kiến trúc Tầng Mạng',
    'server-layer': 'Kiến trúc Tầng xử lý Server',
    'parser': 'Bộ phân tích Cú pháp (Parser)',
    'system-core': 'Nhân quản trị Hệ thống (Core)',
    'query-engine': 'Bộ máy Truy vấn và Tối ưu hóa truy vấn (Optimizer)',
    'storage-architecture': 'Kiến trúc Lưu trữ Vật lý',
    'system-overview': 'Tổng quan Kiến trúc Hệ thống',
}

def slugify(s):
    s = s.lower().strip()
    s = re.sub(r'[^\w\s-]', '', s)
    s = re.sub(r'[-\s]+', '-', s)
    return s

# --- Citations Configuration ---

CITATIONS_TOTAL = 8 # Default

CITATION_MAP = {
    '[1]': 'DoVanNhon1',
    '[2]': 'HIUJS_Article',
    '[3]': 'Dvn_Somet',
    '[4]': 'Dvn_Ijcsi',
    '[5]': 'DatabaseSystems',
    '[6]': 'RussellNorvig',
    '[7]': 'Musen_Protege',
    '[8]': 'PrologBook',
    '[9]': 'ReteAlgorithm',
    '[10]': 'ComerBTree'
}

def escape_latex(text):
    if not isinstance(text, str): return str(text)
    text = re.sub(r'[^\x00-\x7F\u00C0-\u1EF9\u2022\u2018\u2019\u201C\u201D]+', '', text) 
    mapping = {
        '&': r'\&', '%': r'\%', '$': r'\$', '#': r'\#', '_': r'\_',
        '{': r'\{', '}': r'\}', '~': r'\textasciitilde{}', '^': r'\textasciicircum{}', '\\': r'\textbackslash{}',
    }
    regex = re.compile('|'.join(re.escape(k) for k in mapping.keys()))
    return regex.sub(lambda mo: mapping[mo.group()], text)

def process_inline_formatting(text):
    # Bold: **text**
    text = re.sub(r'\*\*(.*?)\*\*', r'\\textbf{\1}', text)
    text = re.sub(r'\*(.*?)\*', r'\\textit{\1}', text)
    
    def link_repl(m):
        label, url = m.groups()
        if url.startswith('http'):
            return f'\\href{{{url}}}{{\\url{{{url}}}}}'
        return f'\\href{{{url}}}{{{label}}}'
    
    text = re.sub(r'\[([^\]]+)\]\(([^\)]+)\)', link_repl, text)
    
    # Citations: [1], [2], ... [8] -> \cite{key}
    for marker, key in CITATION_MAP.items():
        text = text.replace(marker, f'\\cite{{{key}}}')

    # Also handle straight quotes
    text = text.replace('"', "''")
    text = text.replace('‘', "'").replace('’', "'").replace('“', "``").replace('”', "''")
    return text

def md_block_to_latex(block, af_id, at_id, prev_block='', next_block='', level=0):
    """Processes a Markdown block into LaTeX. Returns (tex, af, at, consumed)"""
    block = block.strip()
    consumed_next = False
    if not block: return "", af_id, at_id, False

    # 1. Split mid-block headers (v3.95: Flexible detection)
    # Detect if any line starts with '#' that isn't the first line
    if '\n#' in block:
        # Look for the FIRST occurrence of a header at the start of any line
        h_search = re.search(r'(?m)^#+', block)
        if h_search and h_search.start() > 0:
            head = block[:h_search.start()].strip()
            tail = block[h_search.start():].strip()
            h_tex, af1, at1, c1 = md_block_to_latex(head, af_id, at_id, level=level)
            t_tex, af2, at2, c2 = md_block_to_latex(tail, af1, at1, level=level)
            return h_tex + t_tex, af2, at2, c1 or c2

    # No glossary mode in v4.1 (Removed)

    # 1. Skip non-content blocks
    if block == '![' or block == '---': return "", af_id, at_id, False

    # 2. Figures (Extract ALL within block with strict regex)
    fig_pattern = r'!\[([^\]]*)\]\(([^\)]*)\)'
    figs = list(re.finditer(fig_pattern, block))
    if figs:
        processed_tex = ""
        current_pos = 0
        for match in figs:
            pre_text = block[current_pos : match.start()].strip()
            if pre_text:
                pre_tex, af_id, at_id, _ = md_block_to_latex(pre_text, af_id, at_id, level=level)
                processed_tex += pre_tex
            
            alt, src = match.groups()
            width = "0.7" 
            if '|' in alt:
                w_match = re.search(r'width=([\d\.]+)', alt)
                if w_match: width = w_match.group(1).strip()
                alt = alt.split('|')[0].strip()
            
            # Flexible Legend (v3.84)
            legend_match = re.search(r'(?m)^[\*_]*(?:Hình|Figure|Hinh|Pict)[:\s\d\.\-\[\]\(\)]*\s*(.*?)(?:[\*_]*\n|$|\*)', block[match.end():], re.IGNORECASE)
            in_same_block = False
            if legend_match:
                raw_caption = legend_match.group(1).strip()
                in_same_block = True
            elif next_block:
                legend_match = re.search(r'(?m)^[\*_]*(?:Hình|Figure|Hinh|Pict)[:\s\d\.\-\[\]\(\)]*\s*(.*?)(?:[\*_]*\s*|\n|$)', next_block, re.IGNORECASE)
                if legend_match:
                    raw_caption = legend_match.group(1).strip()
                    consumed_next = True
                else:
                    stem = src.split('/')[-1].split('.')[0].replace('-', ' ').replace('_', ' ')
                    raw_caption = alt.strip() or f"Hình {af_id}"
            else:
                raw_caption = alt.strip() or f"Hình {af_id}"
            
            fname = src.split('/')[-1]
            # Verify file exists after flattening
            if os.path.exists(os.path.join(ASSETS_DEST, fname)):
                img_cmd = r'\includegraphics[width=%s\textwidth]{assets/%s}' % (width if width else "0.7", fname)
            else:
                img_cmd = r'\fbox{Thiếu ảnh: %s}' % escape_latex(fname)
            
            clean_cap = clean_title(raw_caption)
            
            caption = process_inline_formatting(escape_latex(clean_cap))
            fid = slugify(clean_cap)[:50] if clean_cap else f"auto_{af_id}"
            
            processed_tex += '\\begin{figure}[H]\n  \\centering\n  %s\n  \\caption{%s}\\label{fig:%s}\n\\end{figure}\n\n' % (img_cmd, caption, fid)

            if in_same_block:
                current_pos = match.end() + legend_match.end()
            else:
                current_pos = match.end()
            af_id += 1

        post_text = block[current_pos:].strip()
        if post_text:
            post_tex, af_id, at_id, _ = md_block_to_latex(post_text, af_id, at_id, level=level)
            processed_tex += post_tex
        return processed_tex, af_id, at_id, consumed_next

    # 3. Tables (Strict capture)
    if '|' in block and '-' in block and '\n' in block:
        # Improved Caption Logic (v4.05): Prioritize Bảng/Table lines and avoid paragraphs
        cap_match = re.search(r'(?m)^[\*_]*(?:Bảng|Table|Bang|Table|Danh mục)[:\s\.\d\-\[\]\(\)]*\s*(.*?)(?:[\*_]*\n|$|\*)', block, re.IGNORECASE)
        if not cap_match and prev_block:
            # Only match if it looks like a formal caption line (short, Bảng/Table prefix)
            cap_match = re.search(r'(?m)^[\*_]*(?:Bảng|Table|Bang|Table|Danh mục)[:\s\.\d\-\[\]\(\)]*\s*(.*?)(?:[\*_]*\n|$|\*)', prev_block, re.IGNORECASE)
        
        if not cap_match and next_block:
            cap_match = re.search(r'(?m)^[\*_]*(?:Bảng|Table|Bang|Table|Danh mục)[:\s\.\d\-\[\]\(\)]*\s*(.*?)(?:[\*_]*\s*|\n|$)', next_block, re.IGNORECASE)
            if cap_match: consumed_next = True

        raw_caption = ""
        if cap_match:
            raw_caption = cap_match.group(1).strip()
        elif prev_block and len(prev_block.split('\n')) == 1 and (":" in prev_block or "Bảng" in prev_block) and len(prev_block) < 100 and not prev_block.strip().endswith(':'):
            # Fallback only if the preceding block is a single line, looks like a title, 
            # is not too long, and DOES NOT end with a colon (to avoid capturing descriptive paragraphs)
            raw_caption = re.sub(r'^#+\s*[\d\.]*\s*', '', prev_block).strip()
        
        raw_caption = clean_title(raw_caption)
        
        if not raw_caption: 
            # Use a more generic but professional fallback if no title found
            raw_caption = f"Phân tích dữ liệu thực nghiệm {at_id}"
        
        caption = process_inline_formatting(escape_latex(raw_caption))
        tid = slugify(raw_caption)[:50] if raw_caption else f"gtbl_{at_id}"
        
        lines = [l.strip() for l in block.split('\n') if '|' in l]
        if len(lines) >= 3:
            hdr_line = [h.strip() for h in lines[0].strip('|').split('|')]
            cols = len(hdr_line)
            is_glossary = "Tham chiếu" in hdr_line or "Thuật ngữ" in hdr_line
            
            if False: # Removed glossary-specific table rendering in v4.1
                pass
            else:
                # Dynamic column mapping (v3.90)
                if cols <= 3:
                    col_spec = '|l|' + 'X|' * (cols - 1)
                    t_font = "\\small"
                elif cols == 4:
                    col_spec = '|l|' + 'L|' * (cols - 1)
                    t_font = "\\small"
                else:
                    # For Many columns (5+), use p-columns to force narrowing
                    # Rough estimate: 2cm for 1st col, then distribute remaining space
                    col_spec = '|l|' + 'L|' * (cols - 1)
                    t_font = "\\footnotesize"
                
                tbl = '\\begin{table}[H]\n  \\centering\\captionsetup{position=below}%s\\selectfont\n' % t_font
                tbl += '  \\begin{tabularx}{\\textwidth}{%s}\n  \\hline\n' % col_spec
                tbl += ' & '.join([process_inline_formatting(escape_latex(h)) for h in hdr_line]) + ' \\\\ \\hline\n'

            for l in lines[2:]:
                if re.search(r'[\*_]*Bảng\s*[\d\.]*', l, re.IGNORECASE): continue
                cells = [c.strip() for c in l.strip('|').split('|')]
                if len(cells) < cols: cells += [''] * (cols - len(cells))
                
                processed_cells = []
                for i, c in enumerate(cells[:cols]):
                    esc_c = escape_latex(c.strip())
                    processed_cells.append(process_inline_formatting(esc_c))

                tbl += ' & '.join(processed_cells) + ' \\\\ \\hline\n'
            
            if False: # xltabular removed
                pass
            else:
                tbl += '  \\end{tabularx}\n'
                tbl += '  \\caption{%s}\\label{tbl:%s}\n' % (caption, tid)
                tbl += '\\end{table}\n\n'
            return tbl, af_id, at_id + 1, consumed_next

    # 4. Headings (v3.94: Flexible regex)
    h_match = re.match(r'^(#+)\s*(.*)', block)
    if h_match:
        hashes, h_text_raw = h_match.groups()
        h_type = len(hashes)
        if h_type > 0:
            lines = block.split('\n')
            # v3.98: Restore level-based offset for folder/file hierarchy (e.g., 4.5 -> 4.5.1)
            new_level = h_type + level
            h_cmd = 'section' if new_level == 1 else 'subsection' if new_level == 2 else 'subsubsection' if new_level == 3 else 'paragraph'
            
            # Clean text from leading numbers or dots
            h_text = re.sub(r'^[\d\s\.]+', '', h_text_raw.strip())
            
            if new_level > 4:
                res = '\\noindent\\textbf{\\textit{%s}}\n\n' % process_inline_formatting(escape_latex(h_text))
            else:
                res = '\\%s{%s}\n\n' % (h_cmd, process_inline_formatting(escape_latex(h_text)))
            
            if len(lines) > 1:
                rem_tex, af_id, at_id, _ = md_block_to_latex("\n".join(lines[1:]), af_id, at_id, level=level)
                res += rem_tex
            return res, af_id, at_id, consumed_next

    # 5. Lists
    BULLET_RE = r'^\s*(?:\*(?!\*)|\d+\.|\-|•)\s+'
    if re.match(BULLET_RE, block):
        lines = block.split('\n')
        res, stack = "", []
        for line in lines:
            if not line.strip(): continue
            indent = len(line) - len(line.lstrip())
            m = re.match(r'^\s*(?:\*(?!\*)|\d+\.|\-|•)\s+(.*)', line)
            if m:
                env = 'enumerate' if re.match(r'^\s*\d+\.', line) else 'itemize'
                if not stack or indent > stack[-1][0]:
                    if env == 'enumerate' and any(s[1] == 'enumerate' and s[0] == indent for s in stack[:-1]):
                        res += '\\begin{enumerate}[resume]\n'
                    else:
                        res += '\\begin{%s}\n' % env
                    stack.append((indent, env))
                elif indent < stack[-1][0]:
                    while stack and indent < stack[-1][0]:
                        res += '\\end{%s}\n' % stack[-1][1]
                        stack.pop()
                    if stack and stack[-1][0] == indent and stack[-1][1] != env:
                        res += '\\end{%s}\n' % stack[-1][1]
                        if env == 'enumerate':
                            res += '\\begin{enumerate}[resume]\n'
                        else:
                            res += '\\begin{itemize}\n'
                        stack[-1] = (indent, env)
                elif stack[-1][1] != env:
                    res += '\\end{%s}\n' % stack[-1][1]
                    if env == 'enumerate':
                        res += '\\begin{enumerate}[resume]\n'
                    else:
                        res += '\\begin{itemize}\n'
                    stack[-1] = (indent, env)
                res += '  \\item %s\n' % process_inline_formatting(escape_latex(m.group(1).strip()))
            else:
                res += '    %s\n' % process_inline_formatting(escape_latex(line.strip()))
        while stack:
            res += '\\end{%s}\n' % stack[-1][1]
            stack.pop()
        return res + "\n\n", af_id, at_id, consumed_next

    # 5b. Blockquotes
    if block.startswith('>'):
        lines = [l.lstrip('> ').strip() for l in block.split('\n')]
        content = "\\par\n".join([process_inline_formatting(escape_latex(l)) for l in lines if l])
        res = "\\begin{quote}\\itshape\\small\n%s\n\\end{quote}\n\n" % content
        return res, af_id, at_id, consumed_next

    # 6. Default Paragraph
    lines = block.split('\n')
    processed_lines = [process_inline_formatting(escape_latex(l.strip())) for l in lines]
    return "\\par\n".join(processed_lines) + "\n\n", af_id, at_id, consumed_next

def process_directory(dir_path, level, af_id, at_id):
    """Recursively processes hierarchy: Folder (XX-name) -> level(section/subsection)."""
    content = []
    items = sorted(os.listdir(dir_path))
    
    # Deduplication: check if any .md file has same name as the folder
    folder_basename = os.path.basename(dir_path).split("-", 1)[-1].lower() if "-" in os.path.basename(dir_path) else os.path.basename(dir_path).lower()

    for item in items:
        full_path = os.path.join(dir_path, item)
        if os.path.isdir(full_path):
            if re.match(r'^\d{2}-', item):
                slug = item.split("-", 1)[-1].lower()
                title = MAP_ACADEMIC.get(slug, slug.replace("_", " ").replace("-", " ").title())
                
                # level 0 (Chapter folders) -> section (v3.71)
                cmd = "section" if level == 0 else "subsection" if level == 1 else "subsubsection"
                content.append('\\%s{%s}\n\n' % (cmd, title))
                sub_content, af_id, at_id = process_directory(full_path, level + 1, af_id, at_id)
                content.extend(sub_content)
        elif item.endswith('.md'):
            # Hierarchical Control (v4.0): Specify which folders should nest their files (e.g., 4.5 -> 4.5.1)
            # Folders not in this list will have files as siblings to the folder title (e.g., 4.2 folder -> 4.3 file H1)
            HIERARCHICAL_WHITELIST = ['02-models', '04-storage', '05-kbql', '06-network-layer', '07-server-layer', '08-reasoning-layer', '09-application', '05-implementation', '06-evaluation', '01-overview', '03-parser', '04-system-core', '05-query-engine']
            folder_slug = os.path.basename(dir_path)
            is_hierarchical = any(slug in folder_slug for slug in HIERARCHICAL_WHITELIST)
            level_offset = level if is_hierarchical else (level - 1 if level > 0 else 0)

            file_slug = item.split("-", 1)[-1].replace(".md", "").lower() if "-" in item else item.replace(".md", "").lower()
            skip_header = (file_slug == folder_basename)

            with open(full_path, 'r', encoding='utf-8') as f_in:
                md_text = f_in.read()
                math_blocks, code_blocks = [], []
                def math_ext(m): math_blocks.append(m.group(0)); return "PHMATHX%dX" % (len(math_blocks)-1)
                md_text = re.sub(r'\$\$.*?\$\$', math_ext, md_text, flags=re.DOTALL)
                md_text = re.sub(r'\$.*?\$', math_ext, md_text)
                def code_ext(m):
                    lang = m.group(1).lower() if m.group(1) else ""
                    if lang == "mermaid": return ""
                    code_blocks.append(m.group(2))
                    return "PHCODEX%dX" % (len(code_blocks)-1)
                md_text = re.sub(r'(?m)^[ \t]*```(\w+)?\n(.*?)\n[ \t]*```', code_ext, md_text, flags=re.DOTALL)
                md_text = re.sub(r'</?details>|<summary>.*?</summary>', '', md_text)

                # Smart Block Splitting (v3.71): Handle headers mid-block
                blocks = re.split(r'\n\s*\n|(?m)^(?=#+ )', md_text)
                prev_b = ''
                skip_next = False
                for i, b in enumerate(blocks):
                    if skip_next:
                        skip_next = False
                        continue
                    b = b.strip()
                    if not b: continue
                    if skip_header and b.startswith(('# ', '## ')): 
                        prev_b = b
                        continue

                    nx_b = blocks[i+1].strip() if i+1 < len(blocks) else ""
                    tex_b, af_id, at_id, consumed_nx = md_block_to_latex(b, af_id, at_id, prev_block=prev_b, next_block=nx_b, level=level_offset)
                    if consumed_nx: skip_next = True
                    for j, m in enumerate(math_blocks):
                        ph = "PHMATHX%dX" % j
                        if m.startswith('$$'): tex_b = tex_b.replace(ph, '\\begin{equation}\n%s\n\\end{equation}' % m.strip('$').strip())
                        else: tex_b = tex_b.replace(ph, m)
                    for j, c in enumerate(code_blocks):
                        code_tex = "\\begin{academicbox}\n\\begin{Verbatim}[commandchars=\\\\\\{\\},breaklines=true,breakanywhere=true,fontfamily=tt,fontsize=\\small]\n"
                        code_tex += c.strip()
                        code_tex += "\n\\end{Verbatim}\n\\end{academicbox}\n\n"
                        tex_b = tex_b.replace("PHCODEX%dX" % j, code_tex)
                    content.append(tex_b)
                    prev_b = b
    return content, af_id, at_id

def main():
    print("Executing Thesis-Standard Multi-Level Hierarchy (v3.98) - Flattening Assets...")
    if not os.path.exists(LATEX_DIR): os.makedirs(LATEX_DIR)
    
    # --- STEP 1: Aggressive Asset Sync & Flattening ---
    if os.path.exists(os.path.join(DOCS_DIR, 'assets')):
        if os.path.exists(ASSETS_DEST): shutil.rmtree(ASSETS_DEST)
        os.makedirs(ASSETS_DEST)
        for root, dirs, files in os.walk(os.path.join(DOCS_DIR, 'assets')):
            for file in files:
                if file.endswith('.png') or file.endswith('.jpg') or file.endswith('.pdf'):
                    src_path = os.path.join(root, file)
                    shutil.copy2(src_path, os.path.join(ASSETS_DEST, file))
        print(f"Success! Flattened docs/assets to {ASSETS_DEST}")
    
    # Discovery
    all_refs = []
    # Skip glossary and references folder (handled via BibTeX)
    folders = sorted([f for f in os.listdir(DOCS_DIR) if re.match(r'^\d{2}-', f) and f not in ['00-glossary', '06-references', '15-references']])
    for folder in folders:
        fp = os.path.join(DOCS_DIR, folder)
        for f in os.listdir(fp):
            if f.endswith('.md'):
                with open(os.path.join(fp, f), 'r', encoding='utf-8') as fin:
                    all_refs.extend(re.findall(r'!\[.*?\]\((.*?)\)', fin.read()))
    for ref in set(all_refs):
        fname = ref.split('/')[-1]
        for root, dirs, files in os.walk('.'):
            if '.git' in root or '.venv' in root or LATEX_DIR in root: continue
            if fname in files:
                shutil.copy(os.path.join(root, fname), os.path.join(ASSETS_DEST, fname))
                break

    report_content = []
    af_id, at_id = 1, 1
    for folder in folders:
        chapter_title = CHAPTER_NAMES.get(folder, folder.split("-")[-1].replace("_", " ").upper())
        report_content.append('\\chapter{%s}\n' % chapter_title)
        
        folder_path = os.path.join(DOCS_DIR, folder)
        # Use level=0 for Chapter folders (v3.71)
        chap_content, af_id, at_id = process_directory(folder_path, 0, af_id, at_id)
        report_content.extend(chap_content)
        report_content.append('\\clearpage\n')

    # Glossary synthesis removed in v4.1 (Cleaned from front-matter)

    main_tex = r'''\documentclass[13pt,a4paper,oneside]{extreport}
\usepackage{fontspec}
\usepackage[vietnamese]{babel}
\usepackage{amsmath}
\usepackage{amssymb}
\usepackage{graphicx}
\usepackage{enumitem}
\usepackage{tabularx}
\usepackage{geometry}
\usepackage{longtable}
\usepackage{booktabs}
\usepackage{xltabular}
\usepackage{caption}
\usepackage{setspace}
\usepackage{float}
\usepackage{titlesec}
\usepackage[table]{xcolor}
\usepackage{listings}
\usepackage[most]{tcolorbox}
\tcbuselibrary{breakable}

\definecolor{mydarkblue}{RGB}{0, 51, 102}
\definecolor{myred}{RGB}{204, 0, 0}

\usepackage[colorlinks=true, linkcolor=black, citecolor=black, urlcolor=mydarkblue]{hyperref}
\usepackage{url}
\usepackage[titles]{tocloft}

% Compact TOC / LOF / LOT: remove extra vertical space between entries
\setlength{\cftbeforechapskip}{0pt}
\setlength{\cftbeforesecskip}{0pt}
\setlength{\cftbeforesubsecskip}{0pt}
\setlength{\cftbeforesubsubsecskip}{0pt}
\setlength{\cftbeforefigskip}{0pt}
\setlength{\cftbeforetabskip}{0pt}
\renewcommand{\cftchapleader}{\cftdotfill{\cftdotsep}}
% Show "CHƯƠNG X" prefix in TOC chapter entries
\renewcommand{\cftchappresnum}{CHƯƠNG }
\renewcommand{\cftchapaftersnum}{: }
\setlength{\cftchapnumwidth}{6.5em}
\setlength{\cftsecindent}{1.5em}
\setlength{\cftsecnumwidth}{2.3em}
\setlength{\cftsubsecindent}{3.8em}
\setlength{\cftsubsecnumwidth}{3.2em}
\setlength{\cftbeforetoctitleskip}{0pt}
\setlength{\cftaftertoctitleskip}{1em}
\setlength{\cftbeforeloftitleskip}{0pt}
\setlength{\cftafterloftitleskip}{1em}
\setlength{\cftbeforelottitleskip}{0pt}
\setlength{\cftafterlottitleskip}{1em} 

% Listings configuration for code blocks
% Professional Code Box (v3.88) - No listings dependency
\usepackage{tcolorbox}
\usepackage{fvextra}
\tcbuselibrary{skins,breakable}

\newtcolorbox{academicbox}{
  colback=white,
  colframe=gray!40,
  sharp corners,
  boxrule=0.5pt,
  left=5pt,
  right=5pt,
  top=0pt,
  bottom=0pt,
  breakable,
  fontupper=\small,
}

% Table configuration (v3.90) - Fix overlap for multi-column tables
\usepackage{array}
\newcolumntype{L}{>{\raggedright\arraybackslash}X}
\setlength{\tabcolsep}{4pt} % Tighter columns for more content space

\captionsetup[table]{position=below, skip=10pt, justification=centering, font=normalfont}
\captionsetup[figure]{position=below, skip=10pt, justification=centering, font=normalfont}
\setstretch{1.2} % Better line spacing for entire document

% Font configuration
\setmainfont{Times New Roman}
\setmonofont{Courier New}



\geometry{left=1.2in, right=0.8in, top=0.8in, bottom=0.8in}
\setlength{\parindent}{2em}

% Center-align and rename non-numbered sections (TOC, LOF, LOT, Bib)
\AtBeginDocument{
  \renewcommand{\contentsname}{MỤC LỤC}
  \renewcommand{\listfigurename}{DANH MỤC HÌNH ẢNH}
  \renewcommand{\listfigurename}{DANH MỤC HÌNH ẢNH}
  \renewcommand{\listtablename}{DANH MỤC BIỂU ĐỒ}
  \renewcommand{\bibname}{TÀI LIỆU THAM KHẢO}
  \setlength{\headheight}{15pt}
}

\titleformat{\section}
  {\normalfont\fontsize{14pt}{17pt}\selectfont\bfseries\color{mydarkblue}}{\thesection}{1em}{}
\titleformat{\subsection}
  {\normalfont\fontsize{14pt}{17pt}\selectfont\bfseries\color{mydarkblue}}{\thesubsection}{1em}{}
\titleformat{\subsubsection}
  {\normalfont\fontsize{13pt}{16pt}\selectfont\bfseries\color{black}}{\thesubsubsection}{1em}{}
\titleformat{\paragraph}
  {\normalfont\fontsize{13pt}{16pt}\selectfont\bfseries\color{black}}{\theparagraph}{1em}{}

\setcounter{secnumdepth}{4}
\setcounter{tocdepth}{2}

% Numbered Chapters: "CHƯƠNG X: TITLE" (Same line, Centered, Red)
\titleformat{\chapter}[block]
  {\normalfont\huge\bfseries\filcenter\color{red}}
  {CHƯƠNG \thechapter: }
  {0.5em}
  {}

% Unnumbered Chapters (TOC, LOF, LOT, Bib): "TITLE" (Centered, Red, UPPERCASE style)
\titleformat{name=\chapter,numberless}[block]
  {\normalfont\huge\bfseries\filcenter\color{red}}
  {}
  {0pt}
  {}







\usepackage{fancyhdr}
\pagestyle{fancy}
\fancyhf{}
\fancyhead[R]{\textit{Hệ hỗ trợ quản trị tri thức COKB}}
\fancyfoot[C]{\thepage}

\begin{document}
\newgeometry{margin=0.8in}
\begin{titlepage}
    \begin{tcolorbox}[
        enhanced,
        sharp corners,
        boxrule=1.5pt,
        colback=white,
        colframe=black,
        width=\textwidth,
        height=\textheight,
        left=2cm,
        right=2cm,
        top=1.5cm,
        bottom=1cm,
        borderline={0.5pt}{4pt}{black} % Hiệu ứng viền đôi chuyên nghiệp
    ]
    \centering
    {\fontsize{14pt}{18pt}\selectfont \textbf{TRƯỜNG ĐẠI HỌC QUỐC TẾ HỒNG BÀNG}\par}
    {\fontsize{14pt}{18pt}\selectfont \textbf{BỘ MÔN CÔNG NGHỆ THÔNG TIN}\par}
    \vspace{0.2cm}
    {\fontsize{18pt}{18pt}\selectfont \fontspec{Wingdings} \char"F096\char"F026\char"F097 \par}
    
    \vspace{1.5cm}
    \includegraphics[width=10cm]{assets/logo_hiu.jpg}\par
    \vspace{1.5cm}
    
    {\fontsize{22pt}{28pt}\selectfont \textbf{ĐỒ ÁN TỐT NGHIỆP}\par}
    \vspace{1.5cm}
    
    {\fontsize{17pt}{24pt}\selectfont \textbf{\underline{ĐỀ TÀI}: “THIẾT KẾ HỆ HỖ TRỢ QUẢN TRỊ} \\ 
    \textbf{CƠ SỞ TRI THỨC DẠNG COKB”}\par}
    
    \vspace{2.5cm}
    
    \begin{flushleft}
        \hspace{4.5cm}
        \fontsize{14pt}{24pt}\selectfont
        \begin{tabular}{ll}
            \textbf{GVHD:} & \textbf{Gs. ĐỖ VĂN NHƠN \& Ths. MAI TRUNG THÀNH} \\
            \textbf{SVTH:} & \textbf{LÊ CHÂU TRẦN PHÁT} \\
            \textbf{MSSV:} & \textbf{2211110068} \\
            \textbf{LỚP:}  & \textbf{TH22DH-CN2}
        \end{tabular}
    \end{flushleft}
    
    \vfill
    {\fontsize{14pt}{17pt}\selectfont \textbf{TP. Hồ Chí Minh, 2026}\par}
    \end{tcolorbox}
\end{titlepage}
\restoregeometry

% --- LỜI CẢM ƠN ---
\chapter*{LỜI CẢM ƠN}
\addcontentsline{toc}{chapter}{LỜI CẢM ƠN}
\thispagestyle{plain}

Trước tiên, với lòng biết ơn sâu sắc và chân thành nhất, em xin gửi lời cảm ơn đến quý Thầy Cô và Nhà trường Đại học Quốc tế Hồng Bàng đã tạo điều kiện thuận lợi, hỗ trợ em trong suốt quá trình học tập và nghiên cứu đề tài này.

\noindent Trong khoảng thời gian học tập tại trường, em đã nhận được sự quan tâm, chỉ dạy tận tình từ quý Thầy Cô cùng sự động viên và giúp đỡ nhiệt thành của bạn bè. Nhờ đó, em có thêm kiến thức và động lực để hoàn thành đề tài một cách tốt nhất.

\noindent Hơn hết, em xin bày tỏ lòng biết ơn chân thành đến \textbf{Gs. Đỗ Văn Nhơn} và \textbf{Ths. Mai Trung Thành} -- những người đã trực tiếp hướng dẫn, định hướng và hỗ trợ em trong suốt quá trình thực hiện đề tài. Những lời chỉ dạy và kiến thức quý báu của Thầy đã giúp em không ngừng hoàn thiện bản thân và nâng cao chất lượng nghiên cứu.

\noindent Tuy vậy, đề tài được thực hiện trong khoảng thời gian có giới hạn. Không tránh khỏi những thiếu sót, em rất mong nhận được những ý kiến đóng góp quý báu từ quý Thầy Cô để nghiên cứu được hoàn thiện hơn trong tương lai.

\noindent Em xin chân thành cảm ơn!

\vspace{2cm}
\begin{flushright}
    \textit{TP. Hồ Chí Minh, ngày 1 tháng 4 năm 2026}\\[0.5cm]
    \textit{Người thực hiện}\\[1.5cm]
    \textbf{LÊ CHÂU TRẦN PHÁT}
\end{flushright}
\clearpage

% --- TRANG CAM KẾT ---
\chapter*{TRANG CAM KẾT}
\addcontentsline{toc}{chapter}{TRANG CAM KẾT}
\thispagestyle{plain}

Em xin cam kết rằng báo cáo khóa luận tốt nghiệp này được hoàn thành dựa trên kết quả nghiên cứu của bản thân em, dưới sự hướng dẫn của quý Thầy Cô.

\noindent Các kết quả trình bày trong báo cáo là trung thực và chưa được công bố trong bất kỳ công trình nào khác ở cùng cấp.

\noindent Các tài liệu tham khảo, trích dẫn và số liệu sử dụng trong báo cáo đều được ghi chú rõ ràng, đầy đủ và trung thực theo đúng quy định.

\vspace{3cm}
\begin{flushright}
    \textit{TP. Hồ Chí Minh, ngày 1 tháng 4 năm 2026}\\[0.5cm]
    \textit{Sinh viên thực hiện}\\[1.5cm]
    \textbf{LÊ CHÂU TRẦN PHÁT}
\end{flushright}
\clearpage

\cleardoublepage
\phantomsection
\addcontentsline{toc}{chapter}{DANH MỤC HÌNH ẢNH}
\listoffigures
\clearpage

\cleardoublepage
\phantomsection
\addcontentsline{toc}{chapter}{DANH MỤC BIỂU ĐỒ}
\listoftables
\clearpage

\cleardoublepage
\phantomsection
\addcontentsline{toc}{chapter}{MỤC LỤC}
\tableofcontents
\clearpage
'''
    # main_tex += "".join(glossary_content) # Removed
    main_tex += "\n".join(report_content)
    main_tex += r'''
\clearpage
\addcontentsline{toc}{chapter}{TÀI LIỆU THAM KHẢO}
\bibliographystyle{IEEEtran}
\bibliography{references}
\end{document}
'''
    with open(OUTPUT_TEX, 'w', encoding='utf-8') as f: f.write(main_tex)
    
    print(f"Success! Final Polished Report generated: {OUTPUT_TEX}")

if __name__ == '__main__': main()
