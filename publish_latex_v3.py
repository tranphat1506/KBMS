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
    '01-introduction': 'Giới thiệu và Đặt vấn đề', 
    '02-theory': 'Cơ sở Lý thuyết COKB', 
    '03-analysis-and-design': 'Phân tích và Thiết kế Hệ thống', 
    '04-architecture': 'Kiến trúc Hệ thống và các Tầng xử lý',
    '05-models': 'Định nghĩa Mô hình và Khái niệm', 
    '06-kbql-reference': 'Ngôn ngữ Truy vấn Tri thức KBQL',
    '07-storage': 'Cơ chế Lưu trữ và Quản lý Dữ liệu nhị phân', 
    '08-reasoning': 'Bộ máy Suy diễn và Thuật toán Bao đóng',
    '09-network': 'Giao thức Mạng và Truyền tải Dữ liệu', 
    '10-server': 'Hệ thống Máy chủ và Quản trị Tài nguyên',
    '11-parser': 'Bộ Phân tích Cú pháp và Trình biên dịch', 
    '12-cli': 'Giao diện Dòng lệnh (CLI)',
    '13-kbms-studio': 'Môi trường Phát triển KBMS Studio', 
    '14-installation-and-testing': 'Cài đặt, Kiểm thử và Đánh giá Hiệu năng',
    '15-references': 'Tài liệu Tham khảo'
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
    text = re.sub(r'\*\*(.*?)\*\*', r'\\textbf{\1}', text)
    text = re.sub(r'\*(.*?)\*', r'\\textit{\1}', text)
    text = re.sub(r'\[(.*?)\]\((.*?)\)', r'\\href{\2}{\1}', text)
    text = text.replace('‘', "'").replace('’', "'").replace('“', "``").replace('”', "''")
    return text

def md_block_to_latex(block, af_id, at_id):
    block = block.strip()
    if not block: return "", af_id, at_id

    # 1. Skip non-content blocks
    if block.startswith('> [!') or block.startswith('[!') or (block.startswith('> ') and '[!' in block): return "", af_id, at_id
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

        raw_caption = cap_match.group(2).strip() if cap_match else f"Bảng {at_id}"
        caption = process_inline_formatting(escape_latex(raw_caption))
        tid = cap_match.group(1).replace(".", "_") if cap_match and cap_match.group(1) else f"auto_tbl_{at_id}"
        
        lines = [l.strip() for l in block.split('\n') if '|' in l]
        if len(lines) >= 3:
            hdr_line = [h.strip() for h in lines[0].strip('|').split('|')]
            cols = len(hdr_line)
            # Use tabularx for auto-width: first col is l, rest are X
            if cols <= 2:
                col_spec = '|l|X|'
            else:
                col_spec = '|l|' + 'X|' * (cols - 1)
            tbl = '\\begin{table}[H]\n  \\centering\n  \\begin{tabularx}{\\textwidth}{%s}\n  \\hline\n' % col_spec
            tbl += ' & '.join([process_inline_formatting(escape_latex(h)) for h in hdr_line]) + ' \\\\ \\hline\n'
            for l in lines[2:]:
                if re.search(r'[\*_]*Bảng\s*[\d\.]*', l, re.IGNORECASE): continue
                cells = [c.strip() for c in l.strip('|').split('|')]
                if len(cells) < cols: cells += [''] * (cols - len(cells))
                tbl += ' & '.join([process_inline_formatting(escape_latex(c[:200])) for c in cells[:cols]]) + ' \\\\ \\hline\n'
            tbl += '  \\end{tabularx}\n  \\caption{%s}\\label{tbl:%s}\n\\end{table}\n\n' % (caption, tid)
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
                if not stack or indent > stack[-1][0] or env != stack[-1][1]:
                    res += '\\begin{%s}\n' % env
                    stack.append((indent, env))
                elif indent < stack[-1][0]:
                    while stack and indent < stack[-1][0]:
                        res += '\\end{%s}\n' % stack[-1][1]
                        stack.pop()
                res += '  \\item %s\n' % process_inline_formatting(escape_latex(m.group(1).strip()))
            else:
                res += '    %s\n' % process_inline_formatting(escape_latex(line.strip()))
        while stack:
            res += '\\end{%s}\n' % stack[-1][1]
            stack.pop()
        return res + "\n\n", af_id, at_id

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
    folders = sorted([f for f in os.listdir(DOCS_DIR) if re.match(r'^\d{2}-', f)])
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
        chapter_title = CHAPTER_NAMES.get(folder, folder.split("-")[-1].replace("_", " ").title())
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

                for b in blocks:
                    tex_b, af_id, at_id = md_block_to_latex(b, af_id, at_id)
                    # Restoration
                    for i, m in enumerate(math_blocks):
                        ph = "PHMATHX%dX" % i
                        if m.startswith('$$'): tex_b = tex_b.replace(ph, '\\begin{equation}\n%s\n\\end{equation}' % m.strip('$'))
                        else: tex_b = tex_b.replace(ph, m)
                    for i, c in enumerate(code_blocks):
                        tex_b = tex_b.replace("PHCODEX%dX" % i, '\\begin{verbatim}\n%s\n\\end{verbatim}' % c)
                    report_content.append(tex_b)
        report_content.append('\\clearpage\n')

    main_tex = r'''\documentclass[13pt,a4paper,oneside]{extreport}
\usepackage{fontspec}
\usepackage[vietnamese]{babel}
\usepackage{graphicx}
\usepackage{amsmath}
\usepackage{hyperref}
\usepackage{geometry}
\usepackage{titlesec}
\usepackage{cite}
\usepackage[most]{tcolorbox}
\usepackage{booktabs}
\usepackage{tabularx}
\usepackage{xcolor}
\usepackage{indentfirst}
\usepackage{float}

\setmainfont{Times New Roman}
\geometry{left=1.2in, right=0.8in, top=0.8in, bottom=0.8in}
\setlength{\parindent}{2.5em}

\definecolor{mydarkblue}{rgb}{0,0,0.5}

% Center-align and rename non-numbered sections (TOC, LOF, LOT, Bib)
\AtBeginDocument{
  \renewcommand{\contentsname}{MỤC LỤC}
  \renewcommand{\listfigurename}{DANH MỤC HÌNH ẢNH}
  \renewcommand{\listtablename}{DANH MỤC BẢNG}
  \renewcommand{\bibname}{TÀI LIỆU THAM KHẢO}
}

\titleformat{\section}
  {\normalfont\fontsize{14pt}{17pt}\selectfont\bfseries\color{mydarkblue}}{\thesection}{1em}{}
\titleformat{\subsection}
  {\normalfont\fontsize{13pt}{16pt}\selectfont\bfseries\color{mydarkblue}}{\thesubsection}{1em}{}
\titleformat{\subsubsection}
  {\normalfont\fontsize{13pt}{16pt}\selectfont\bfseries\color{black}}{\hspace{2em}\thesubsubsection}{1em}{}

% Numbered Chapters: "CHƯƠNG X: TITLE" (Same line, Left-aligned, Red, UPPERCASE)
\titleformat{\chapter}[block]
  {\normalfont\huge\bfseries\raggedright\color{red}\MakeUppercase}
  {CHƯƠNG \thechapter: }
  {0.5em}
  {}

% Unnumbered Chapters (TOC, LOF, LOT, Bib): "TITLE" (Centered, Red, UPPERCASE)
\titleformat{name=\chapter,numberless}[block]
  {\normalfont\huge\bfseries\filcenter\color{red}\MakeUppercase}
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
\tableofcontents
\listoffigures
\listoftables
'''
    main_tex += "\n".join(report_content)
    main_tex += r'''
\bibliographystyle{IEEEtran}
\bibliography{references}
\end{document}
'''
    with open(OUTPUT_TEX, 'w', encoding='utf-8') as f: f.write(main_tex)
    print(f"Success! Final Polished Report generated: {OUTPUT_TEX}")

if __name__ == '__main__': main()
