import os
import re
import shutil

# --- Configuration ---
DOCS_DIR = 'docs'
LATEX_DIR = 'latex_report'
ASSETS_DEST = os.path.join(LATEX_DIR, 'assets')
OUTPUT_TEX = os.path.join(LATEX_DIR, 'report_final.tex')

# --- Chapter Vietnamese Mapping ---
CHAPTER_NAMES = {
    '01-introduction': 'GIỚI THIỆU VÀ ĐẶT VẤN ĐỀ', 
    '02-theory': 'CƠ SỞ LÝ THUYẾT COKB', 
    '03-analysis-and-design': 'PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG', 
    '04-architecture': 'KIẾN TRÚC HỆ THỐNG VÀ CÁC TẦNG XỬ LÝ',
    '05-models': 'ĐỊNH NGHĨA MÔ HÌNH VÀ KHÁI NIỆM', 
    '06-kbql-reference': 'NGÔN NGỮ TRUY VẤN TRI THỨC KBQL',
    '07-storage': 'CƠ CHẾ LƯU TRỮ VÀ QUẢN LÝ DỮ LIỆU NHỊ PHÂN', 
    '08-reasoning': 'BỘ MÁY SUY DIỄN VÀ THUẬT TOÁN BAO ĐÓNG',
    '09-network': 'GIAO THỨC MẠNG VÀ TRUYỀN TẢI DỮ LIỆU', 
    '10-server': 'HỆ THỐNG MÁY CHỦ VÀ QUẢN TRỊ TÀI NGUYÊN',
    '11-parser': 'BỘ PHÂN TÍCH CÚ PHÁP VÀ TRÌNH BIÊN DỊCH', 
    '12-cli': 'GIAO DIỆN DÒNG LỆNH (CLI)',
    '13-kbms-studio': 'MÔI TRƯỜNG PHÁT TRIỂN KBMS STUDIO', 
    '14-installation-and-testing': 'CÀI ĐẶT, KIỂM THỬ VÀ ĐÁNH GIÁ HIỆU NĂNG',
    '15-references': 'TÀI LIỆU THAM KHẢO'
}

GLOSSARY_MAP = {} # slug -> ID

def slugify(s):
    s = s.lower().strip()
    s = re.sub(r'[^\w\s-]', '', s)
    s = re.sub(r'[-\s]+', '-', s)
    return s

# Pre-parse glossary to build the map
glossary_path = os.path.join(DOCS_DIR, '00-glossary', '01-glossary.md')
if os.path.exists(glossary_path):
    with open(glossary_path, 'r', encoding='utf-8') as f:
        for line in f:
            if '|' in line and '[' in line and ']' in line:
                parts = [p.strip() for p in line.split('|')]
                if len(parts) >= 3:
                    gid = parts[1].strip(' *[]')
                    term = parts[2].strip(' *[]')
                    if gid and term:
                        GLOSSARY_MAP[slugify(term)] = gid

CITATION_MAP = {
    '[1]': 'DoVanNhon1',
    '[2]': 'HIUJS_Article',
    '[3]': 'Dvn_Somet',
    '[4]': 'Dvn_Ijcsi',
    '[5]': 'DatabaseSystems',
    '[6]': 'RussellNorvig',
    '[7]': 'Musen_Protege',
    '[8]': 'PrologBook'
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
        if '01-glossary.md' in url:
            anchor = url.split('#')[-1] if '#' in url else slugify(label)
            gid = GLOSSARY_MAP.get(anchor, '')
            if gid:
                # Use glos: prefix to match the hypertarget in the glossary table
                return f'\\texorpdfstring{{{label}~[\\protect\\hyperlink{{glos:{anchor}}}{{{gid}}}]}}{{{label} [{gid}]}}'
            return f'\\protect\\hyperlink{{glos:{anchor}}}{{{label}}}'
        return f'\\href{{{url}}}{{{label}}}'
    
    text = re.sub(r'\[([^\]]+)\]\(([^\)]+)\)', link_repl, text)
    
    # Citations: [1], [2], ... [8] -> \cite{key}
    for marker, key in CITATION_MAP.items():
        text = text.replace(marker, f'\\cite{{{key}}}')

    # Also handle straight quotes
    text = text.replace('"', "''")
    text = text.replace('‘', "'").replace('’', "'").replace('“', "``").replace('”', "''")
    return text

def md_block_to_latex(block, af_id, at_id, prev_block=''):
    block = block.strip()
    if not block: return "", af_id, at_id

    # 1. Skip non-content blocks
    if block == '![' or block == '---': return "", af_id, at_id

    # 1b. Split mixed blocks (text followed by bullet list)
    BULLET_RE = r'^\s*(?:\*(?!\*)|\d+\.|\-|•)\s+'
    if '\n' in block and not re.match(BULLET_RE, block):
        lines = block.split('\n')
        for i in range(1, len(lines)):
            if re.match(BULLET_RE, lines[i]):
                head = "\n".join(lines[:i])
                tail = "\n".join(lines[i:])
                h_tex, af1, at1 = md_block_to_latex(head, af_id, at_id)
                t_tex, af2, at2 = md_block_to_latex(tail, af1, at1)
                return h_tex + t_tex, af2, at2


    # 2. Figures (Extract ALL within block with strict regex)
    fig_pattern = r'!\[([^\]]*)\]\(([^\)]*)\)'
    figs = list(re.finditer(fig_pattern, block))
    if figs:
        processed_tex = ""
        current_pos = 0
        for match in figs:
            pre_text = block[current_pos : match.start()].strip()
            if pre_text:
                pre_tex, af_id, at_id = md_block_to_latex(pre_text, af_id, at_id)
                processed_tex += pre_tex
            
            alt, src = match.groups()
            width = "0.7" 
            if '|' in alt:
                w_match = re.search(r'width=([\d\.]+)', alt)
                if w_match: width = w_match.group(1).strip()
                alt = alt.split('|')[0].strip()
            
            following_text = block[match.end():].split('\n\n')[0]
            legend_match = re.search(r'[\*_]*Hình\s+([\d\.]+):?\s*(.*?)(?:[\*_]*\n|$|\*)', following_text, re.IGNORECASE)
            
            raw_caption = legend_match.group(2).strip() if legend_match else alt.strip() or f"Hình {af_id}"
            caption = process_inline_formatting(escape_latex(raw_caption))
            caption = re.sub(r'\]\s*\(.*?\)', '', caption)
            
            fid = legend_match.group(1).replace(".", "_") if legend_match and legend_match.group(1) else f"auto_{af_id}"
            fname = src.split('/')[-1]
            img_cmd = r'\includegraphics[width=%s\textwidth]{assets/%s}' % (width, fname) if os.path.exists(os.path.join(ASSETS_DEST, fname)) else r'\fbox{Thiếu ảnh: %s}' % escape_latex(fname)
            
            processed_tex += '\\begin{figure}[H]\n  \\centering\n  %s\n  \\caption{%s}\\label{fig:%s}\n\\end{figure}\n\n' % (img_cmd, caption, fid)

            current_pos = match.end() + (legend_match.end() if legend_match else 0)
            af_id += 1

        post_text = block[current_pos:].strip()
        if post_text:
            post_tex, af_id, at_id = md_block_to_latex(post_text, af_id, at_id)
            processed_tex += post_tex
        return processed_tex, af_id, at_id

    # 3. Tables (Strict capture)
    if '|' in block and '-' in block and '\n' in block:
        cap_match = re.search(r'[\*_]*Bảng\s+([\d\.]+):?\s*(.*?)(?:[\*_]*\n|$|\*)', block, re.IGNORECASE)
        if not cap_match and prev_block:
            # Fall back: caption may be in the immediately preceding block (separated by blank line)
            cap_match = re.search(r'[\*_]*Bảng\s+([\d\.]+):?\s*(.*?)(?:[\*_]*\n|$|\*)', prev_block, re.IGNORECASE)

        raw_caption = cap_match.group(2).strip() if cap_match else f"Bảng {at_id}"
        caption = process_inline_formatting(escape_latex(raw_caption))
        tid = cap_match.group(1).replace(".", "_") if cap_match and cap_match.group(1) else f"auto_tbl_{at_id}"
        
        lines = [l.strip() for l in block.split('\n') if '|' in l]
        if len(lines) >= 3:
            hdr_line = [h.strip() for h in lines[0].strip('|').split('|')]
            cols = len(hdr_line)
            # Use longtable for tables that might span multiple pages (like the glossary)
            is_glossary = "Tham chiếu" in hdr_line or "Thuật ngữ" in hdr_line
            
            if is_glossary:
                col_spec = 'p{2cm} p{3.5cm} X'
                tbl = '\\begin{small}\n\\begin{xltabular}{\\textwidth}{|%s|}\n  \\hline\n' % col_spec.replace(' ', '|')
                tbl += ' \\textbf{' + '} & \\textbf{'.join([process_inline_formatting(escape_latex(h)) for h in hdr_line]) + '} \\\\ \\hline\n'
                tbl += ' \\endfirsthead\n'
                tbl += ' \\hline \\multicolumn{%d}{|c|}{\\textit{Tiếp theo trang trước}} \\\\ \\hline\n' % cols
                tbl += ' \\textbf{' + '} & \\textbf{'.join([process_inline_formatting(escape_latex(h)) for h in hdr_line]) + '} \\\\ \\hline\n'
                tbl += ' \\endhead\n'
                tbl += ' \\hline \\multicolumn{%d}{|r|}{\\textit{Xem tiếp trang sau}} \\\\ \\hline\n' % cols
                tbl += ' \\endfoot\n'
                tbl += ' \\hline\n'
                tbl += ' \\endlastfoot\n'
            else:
                if cols <= 3:
                    col_spec = '|l|' + 'X|' * (cols - 1)
                else:
                    col_spec = '|c|' + 'X|' * (cols - 1)
                tbl = '\\begin{table}[H]\n  \\centering\\fontsize{11pt}{13pt}\\selectfont\n  \\begin{tabularx}{\\textwidth}{%s}\n  \\hline\n' % col_spec
                tbl += ' & '.join([process_inline_formatting(escape_latex(h)) for h in hdr_line]) + ' \\\\ \\hline\n'

            for l in lines[2:]:
                if re.search(r'[\*_]*Bảng\s*[\d\.]*', l, re.IGNORECASE): continue
                cells = [c.strip() for c in l.strip('|').split('|')]
                if len(cells) < cols: cells += [''] * (cols - len(cells))
                
                processed_cells = []
                for i, c in enumerate(cells[:cols]):
                    esc_c = escape_latex(c.strip())
                    if i == 1 and is_glossary: # Term column
                        slug = slugify(c.strip(' *[]'))
                        # Add a prefix to glossary targets to avoid collision with section labels
                        processed_cells.append('\\hypertarget{glos:%s}{%s}' % (slug, process_inline_formatting(esc_c)))
                    else:
                        processed_cells.append(process_inline_formatting(esc_c))

                tbl += ' & '.join(processed_cells) + ' \\\\ \\hline\n'
            
            if is_glossary:
                tbl += '  \\end{xltabular}\n\\end{small}\n\n'
            else:
                caption_str = '\\caption{' + caption + '}\\label{tbl:' + tid + '}'
                tbl = tbl.replace('\\begin{table}[H]\n  \\centering\\fontsize{11pt}{13pt}\\selectfont', 
                                  '\\begin{table}[H]\n  \\centering\\fontsize{11pt}{13pt}\\selectfont\n  ' + caption_str)
                tbl += '  \\end{tabularx}\n\\end{table}\n\n'
            return tbl, af_id, at_id + 1


    # 4. Headings
    if block.startswith('#'):
        lines = block.split('\n')
        h_line = lines[0]
        h_type = 4 if h_line.startswith('#### ') else 3 if h_line.startswith('### ') else 2 if h_line.startswith('## ') else 1 if h_line.startswith('# ') else 0

        if h_type > 0 and h_type <= 3:
            h_cmd = ['','section','subsection','subsubsection'][h_type]
            h_text = re.sub(r'^[\d\s\.]+', '', h_line[h_type+1:].strip())
            res = '\\%s{%s}\n\n' % (h_cmd, process_inline_formatting(escape_latex(h_text)))
            if len(lines) > 1:
                inner, af_n, at_n = md_block_to_latex("\n".join(lines[1:]), af_id, at_id)
                return res + inner, af_n, at_n
            return res, af_id, at_id
        elif h_type == 4:
            h_text = re.sub(r'^[\d\s\.]+', '', h_line[5:].strip())
            res = '\\noindent\\textit{%s}\n\n' % process_inline_formatting(escape_latex(h_text))
            if len(lines) > 1:
                inner, af_n, at_n = md_block_to_latex("\n".join(lines[1:]), af_id, at_id)
                return res + inner, af_n, at_n
            return res, af_id, at_id


    # 5. Nested Lists
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
        return res + "\n\n", af_id, at_id

    # 5b. Blockquotes
    if block.startswith('>'):
        lines = [l.lstrip('> ').strip() for l in block.split('\n')]
        content = "\\par\n".join([process_inline_formatting(escape_latex(l)) for l in lines if l])
        res = "\\begin{quote}\\itshape\\small\n%s\n\\end{quote}\n\n" % content
        return res, af_id, at_id

    # 6. Default Paragraph
    lines = block.split('\n')
    processed_lines = [process_inline_formatting(escape_latex(l.strip())) for l in lines]
    return "\\par\n".join(processed_lines) + "\n\n", af_id, at_id

def main():
    print("Executing Thesis-Standard LaTeX Synthesis (v3.32) - Fixing Position & Sizes...")
    if not os.path.exists(LATEX_DIR): os.makedirs(LATEX_DIR)
    if not os.path.exists(ASSETS_DEST): os.makedirs(ASSETS_DEST)
    
    # Discovery
    all_refs = []
    # Skip glossary and and manual references folder (now handled via BibTeX)
    folders = sorted([f for f in os.listdir(DOCS_DIR) if re.match(r'^\d{2}-', f) and f not in ['00-glossary', '15-references']])
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
        files = sorted([f for f in os.listdir(folder_path) if f.endswith('.md')])
        for f_name in files:
            with open(os.path.join(folder_path, f_name), 'r', encoding='utf-8') as f_in:
                md_text = f_in.read()
                # Pre-processing (Math/Code Extraction)
                math_blocks, code_blocks = [], []
                def math_ext(m): math_blocks.append(m.group(0)); return "PHMATHX%dX" % (len(math_blocks)-1)
                md_text = re.sub(r'\$\$.*?\$\$', math_ext, md_text, flags=re.DOTALL)
                md_text = re.sub(r'\$.*?\$', math_ext, md_text)
                
                # Filter out Mermaid blocks
                def code_ext(m):
                    lang = m.group(1).lower() if m.group(1) else ""
                    if lang == "mermaid": return ""
                    code_blocks.append(m.group(2))
                    return "PHCODEX%dX" % (len(code_blocks)-1)
                md_text = re.sub(r'```(\w+)?\n(.*?)\n```', code_ext, md_text, flags=re.DOTALL)

                # Remove details tags
                md_text = re.sub(r'</?details>', '', md_text)
                md_text = re.sub(r'<summary>.*?</summary>', '', md_text)

                blocks = md_text.split('\n\n')

                prev_b = ''
                for b in blocks:
                    tex_b, af_id, at_id = md_block_to_latex(b, af_id, at_id, prev_block=prev_b)
                    # Restoration
                    for i, m in enumerate(math_blocks):
                        ph = "PHMATHX%dX" % i
                        if m.startswith('$$'): 
                            content = m.strip('$').strip()
                            tex_b = tex_b.replace(ph, '\\begin{equation}\n%s\n\\end{equation}' % content)
                        else: 
                            tex_b = tex_b.replace(ph, m)
                    for i, c in enumerate(code_blocks):
                        tex_b = tex_b.replace("PHCODEX%dX" % i, '\\begin{lstlisting}\n%s\n\\end{lstlisting}' % c.strip())
                    report_content.append(tex_b)
                    prev_b = b  # track previous block for caption fallback
        report_content.append('\\clearpage\n')

    # --- Special: Glossary Synthesis (Unnumbered, in Front Matter) ---
    glossary_content = []
    glossary_path = os.path.join(DOCS_DIR, '00-glossary', '01-glossary.md')
    if os.path.exists(glossary_path):
        glossary_content.append('\\chapter*{THUẬT NGỮ VÀ TỪ VIẾT TẮT}\n')
        glossary_content.append('\\addcontentsline{toc}{chapter}{THUẬT NGỮ VÀ TỪ VIẾT TẮT}\n')
        with open(glossary_path, 'r', encoding='utf-8') as f_in:
            md_text = f_in.read()
            blocks = md_text.split('\n\n')
            for b in blocks:
                if b.startswith('# '): continue
                tex_b, af_id, at_id = md_block_to_latex(b, af_id, at_id)
                glossary_content.append(tex_b)
        glossary_content.append('\\clearpage\n')

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
\lstset{
  basicstyle=\ttfamily\small,
  breaklines=true,
  frame=single,
  backgroundcolor=\color{gray!5},
  showstringspaces=false,
  commentstyle=\color{green!40!black},
  keywordstyle=\color{blue},
  stringstyle=\color{red!60!black},
  columns=fullflexible,
  keepspaces=true,
  extendedchars=true,
  literate={á}{á}1 {à}{à}1 {ả}{ả}1 {ã}{ã}1 {ạ}{ạ}1 {ă}{ă}1 {ắ}{ắ}1 {ằ}{ằ}1 {ẳ}{ẳ}1 {ẵ}{ẵ}1 {ặ}{ặ}1 {â}{â}1 {ấ}{ấ}1 {ầ}{ầ}1 {ẩ}{ẩ}1 {ẫ}{ẫ}1 {ậ}{ậ}1 {đ}{đ}1 {é}{é}1 {è}{è}1 {ẻ}{ẻ}1 {ẽ}{ẽ}1 {ẹ}{ẹ}1 {ê}{ê}1 {ế}{ế}1 {ề}{ề}1 {ể}{ể}1 {ễ}{ễ}1 {ệ}{ệ}1 {í}{í}1 {ì}{ì}1 {ỉ}{ỉ}1 {ĩ}{ĩ}1 {ị}{ị}1 {ó}{ó}1 {ò}{ò}1 {ỏ}{ỏ}1 {õ}{õ}1 {ọ}{ọ}1 {ô}{ô}1 {ố}{ố}1 {ồ}{ồ}1 {ổ}{ổ}1 {ỗ}{ỗ}1 {ộ}{ộ}1 {ơ}{ơ}1 {ớ}{ớ}1 {ờ}{ờ}1 {ở}{ở}1 {ỡ}{ỡ}1 {ợ}{ợ}1 {ú}{ú}1 {ù}{ù}1 {ủ}{ủ}1 {ũ}{ũ}1 {ụ}{ụ}1 {ư}{ư}1 {ứ}{ứ}1 {ừ}{ừ}1 {ử}{ử}1 {ữ}{ữ}1 {ự}{ự}1 {ý}{ý}1 {ỳ}{ỳ}1 {ỷ}{ỷ}1 {ỹ}{ỹ}1 {ỵ}{ỵ}1 {Á}{Á}1 {À}{À}1 {Ả}{Ả}1 {Ã}{Ã}1 {Ạ}{Ạ}1 {Ă}{Ă}1 {Ắ}{Ắ}1 {Ằ}{Ằ}1 {Ẳ}{Ẳ}1 {Ẵ}{Ẵ}1 {Ặ}{Ặ}1 {Â}{Â}1 {Ấ}{Ấ}1 {Ầ}{Ầ}1 {Ẩ}{Ẩ}1 {Ẫ}{Ẫ}1 {Ậ}{Ậ}1 {Đ}{Đ}1 {É}{É}1 {È}{È}1 {Ẻ}{Ẻ}1 {Ẽ}{Ẽ}1 {Ẹ}{Ẹ}1 {Ê}{Ê}1 {Ế}{Ế}1 {Ề}{Ề}1 {ể}{ể}1 {ễ}{ễ}1 {ệ}{ệ}1 {Í}{Í}1 {Ì}{Ì}1 {Ỉ}{Ỉ}1 {Ĩ}{Ĩ}1 {Ị}{Ị}1 {Ó}{Ó}1 {Ò}{Ò}1 {Ỏ}{Ỏ}1 {Õ}{Õ}1 {Ọ}{Ọ}1 {Ô}{Ô}1 {Ố}{Ố}1 {Ồ}{Ồ}1 {Ổ}{Ổ}1 {Ỗ}{Ỗ}1 {Ộ}{Ộ}1 {Ơ}{Ơ}1 {Ớ}{Ớ}1 {Ờ}{Ờ}1 {Ở}{Ở}1 {Ỡ}{Ỡ}1 {Ợ}{Ợ}1 {Ú}{Ú}1 {Ù}{Ù}1 {Ủ}{Ủ}1 {Ũ}{Ũ}1 {Ụ}{Ụ}1 {Ư}{Ư}1 {Ứ}{Ứ}1 {Ừ}{Ừ}1 {Ử}{Ử}1 {Ữ}{Ữ}1 {Ự}{Ự}1 {Ý}{Ý}1 {Ỳ}{Ỳ}1 {Ỷ}{Ỷ}1 {Ỹ}{Ỹ}1 {Ỵ}{Ỵ}1
}

\captionsetup[table]{position=above, skip=10pt, justification=centering, font=bf}
\captionsetup[figure]{position=below, skip=10pt, justification=centering, font=bf}
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
  \renewcommand{\listtablename}{DANH MỤC BẢNG}
  \renewcommand{\bibname}{TÀI LIỆU THAM KHẢO}
  \setlength{\headheight}{15pt}
}

\titleformat{\section}
  {\normalfont\fontsize{14pt}{17pt}\selectfont\bfseries\color{mydarkblue}}{\thesection}{1em}{}
\titleformat{\subsection}
  {\normalfont\fontsize{14pt}{17pt}\selectfont\bfseries\color{mydarkblue}}{\thesubsection}{1em}{}
\titleformat{\subsubsection}
  {\normalfont\fontsize{13pt}{16pt}\selectfont\bfseries\color{black}}{\thesubsubsection}{1em}{}

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
\begin{titlepage}
    \centering
    {\large \textbf{TRƯỜNG ĐẠI HỌC QUỐC TẾ HỒNG BÀNG}\par}
    {\large \textbf{BỘ MÔN CÔNG NGHỆ THÔNG TIN}\par}
    \vspace{4cm}
    {\Huge \textbf{ĐỒ ÁN TỐT NGHIỆP}\par}
    \vspace{2cm}
    {\huge \textbf{ĐỀ TÀI: “THIẾT KẾ HỆ HỖ TRỢ QUẢN TRỊ \\ CƠ SỞ TRI THỨC DẠNG COKB”}\par}
    \vspace{4cm}
    \begin{flushleft}
        \textbf{GVHD: Gs. Đỗ Văn Nhơn \& Ths. Mai Trung Thành}\\
        \textbf{SVTH: Lê Châu Trần Phát - 2211110068}\\
        \textbf{Lớp: TH22DH-CN2}
    \end{flushleft}
    \vfill
    {\large TP. Hồ Chí Minh, 2026\par}
\end{titlepage}

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

\tableofcontents
\addcontentsline{toc}{chapter}{MỤC LỤC}
\clearpage
\listoffigures
\addcontentsline{toc}{chapter}{DANH MỤC HÌNH ẢNH}
\clearpage
\listoftables
\addcontentsline{toc}{chapter}{DANH MỤC BẢNG}
\clearpage
'''
    main_tex += "".join(glossary_content)
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
