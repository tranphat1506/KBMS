import os
import re

# --- LaTeX Configuration ---
DOCS_DIR = 'docs'
LATEX_DIR = 'latex_report'
CHAPTERS_DIR = os.path.join(LATEX_DIR, 'chapters')
ASSETS_DIR = 'docs/assets/diagrams'

def escape_latex(text):
    if not isinstance(text, str): return str(text)
    # Define mapping for special LaTeX characters
    mapping = {
        '&': r'\&',
        '%': r'\%',
        '$': r'\$',
        '#': r'\#',
        '_': r'\_',
        '{': r'\{',
        '}': r'\}',
        '~': r'\textasciitilde{}',
        '^': r'\textasciicircum{}',
        '\\': r'\textbackslash{}',
    }
    # Pattern to match those characters
    regex = re.compile('|'.join(re.escape(k) for k in mapping.keys()))
    return regex.sub(lambda mo: mapping[mo.group()], text)

def md_to_latex(md_content, chapter_idx, file_idx):
    # 1. Extract Math ($$) blocks immediately to keep them safe
    math_blocks = []
    def math_extractor(match):
        math_blocks.append(match.group(1))
        return f"___MATH{len(math_blocks)-1}___"
    md_content = re.sub(r'\$\$(.*?)\$\$', math_extractor, md_content, flags=re.DOTALL)

    # 2. Extract Code Blocks (Fenced) to keep them safe
    code_blocks = []
    def code_extractor(match):
        code_blocks.append(match.group(1))
        return f"___CODE{len(code_blocks)-1}___"
    md_content = re.sub(r'```.*?\n(.*?)\n```', code_extractor, md_content, flags=re.DOTALL)

    # 3. Headings
    md_content = re.sub(r'^#\s+[\d\.]*\s*(.*)', r'\\section{\1}', md_content, flags=re.MULTILINE)
    md_content = re.sub(r'^##\s+[\d\.]*\s*(.*)', r'\\subsection{\1}', md_content, flags=re.MULTILINE)
    md_content = re.sub(r'^###\s+[\d\.]*\s*(.*)', r'\\subsubsection{\1}', md_content, flags=re.MULTILINE)

    # 4. Images & Figures
    def fig_replacer(match):
        src, fid, cap = match.groups()
        fname = src.split('/')[-1] if src.startswith('../') else src
        fpath = os.path.join(ASSETS_DIR, fname)
        if os.path.exists(fpath):
            return f'\\begin{{figure}}[ht!]\n\\centering\n\\includegraphics[width=0.8\\textwidth]{{assets/{fname}}}\n\\caption{{{escape_latex(cap)}}}\n\\label{{fig:{fid.replace(".","_")}}}\n\\end{{figure}}'
        return f'\\textbf{{[Missing Image: {fname}]}}\\\\ \\textit{{Hình {fid}: {escape_latex(cap)}}}'
    md_content = re.sub(r'!\[.*?\]\((.*?)\)\s*\*Hình\s+([\d\.]+):\s*(.*?)\*', fig_replacer, md_content)

    # 5. Tables
    def tbl_replacer(match):
        lines = [l.strip() for l in match.group(0).strip().split('\n')]
        if len(lines) < 3: return match.group(0)
        hdr = [h.strip() for h in lines[0].strip('|').split('|')]
        cols = len(hdr)
        tbl = f'\\begin{{table}}[ht!]\n\\centering\n\\begin{{tabular}}{{|{"l|"*cols}}}\n\\hline\n'
        tbl += ' & '.join([escape_latex(h) for h in hdr]) + ' \\\\ \\hline\n'
        for l in lines[2:]:
            cls = [c.strip() for c in l.strip('|').split('|')]
            if len(cls) < cols: cls += [''] * (cols - len(cls))
            tbl += ' & '.join([escape_latex(c) for c in cls[:cols]]) + ' \\\\ \\hline\n'
        tbl += '\\end{tabular}\n\\end{table}'
        return tbl
    md_content = re.sub(r'(\|.*?\|.*?\n\|[\s\-\|]*\|.*?\n(?:\|.*?\|.*?\n)*)', tbl_replacer, md_content)

    # 6. Formatting & Escaping (non-command text)
    parts = re.split(r'(\\[a-z]+(?:\{.*?\})?|___[A-Z0-9]+___)', md_content, flags=re.IGNORECASE)
    final_parts = []
    for p in parts:
        if not p: continue
        if p.startswith('\\') or p.startswith('___'):
            final_parts.append(p)
        else:
            # Escape & _ % # in body text
            p = p.replace('&', r'\&').replace('_', r'\_').replace('%', r'\%').replace('#', r'\#')
            p = re.sub(r'\*\*(.*?)\*\*', r'\\textbf{\1}', p)
            p = re.sub(r'\*(.*?)\*', r'\\textit{\1}', p)
            final_parts.append(p)
    md_content = "".join(final_parts)

    # 7. Restore Code & Math
    for i, c in enumerate(code_blocks):
        md_content = md_content.replace(f"___CODE{i}___", f"\\begin{{verbatim}}\n{c}\n\\end{{verbatim}}")
    for i, m in enumerate(math_blocks):
        md_content = md_content.replace(f"___MATH{i}___", f"\\begin{{equation}}\n{m}\n\\end{{equation}}")
    
    return md_content

def main():
    print("Regenerating LaTeX Project (Fixing Newlines/Escaping)...")
    folders = sorted([f for f in os.listdir(DOCS_DIR) if re.match(r'^\d{2}-', f)])
    files_list = []
    for folder in folders:
        c_idx = int(folder.split('-')[0])
        f_path = os.path.join(DOCS_DIR, folder)
        m_files = sorted([f for f in os.listdir(f_path) if f.endswith('.md')])
        title = folder.split("-")[-1].replace("_", " ").title()
        content = f'\\chapter{{{title}}}\n'
        for f in m_files:
            with open(os.path.join(f_path, f), 'r', encoding='utf-8') as f_in:
                content += md_to_latex(f_in.read(), c_idx, 0) + '\n'
        fname = f'chapter_{c_idx:02d}.tex'
        with open(os.path.join(CHAPTERS_DIR, fname), 'w', encoding='utf-8') as f_out:
            f_out.write(content)
        files_list.append(fname)

    # Final main.tex logic
    main_head = r'''\documentclass[13pt,a4paper,oneside]{report}
\usepackage{fontspec}
\usepackage[vietnamese]{babel}
\usepackage{graphicx}
\usepackage{amsmath}
\usepackage{hyperref}
\usepackage{geometry}
\usepackage{longtable}
\usepackage{titlesec}
\usepackage{cite}
\setmainfont{Times New Roman}
\geometry{left=1.2in, right=0.8in, top=0.8in, bottom=0.8in}
\titleformat{\chapter}[display]{\normalfont\huge\bfseries}{}{0pt}{\huge}
\begin{document}
\begin{titlepage}
    \centering
    \textbf{TRƯỜNG ĐẠI HỌC QUỐC TẾ HỒNG BÀNG}\\
    \textbf{BỘ MÔN CÔNG NGHỆ THÔNG TIN}\\
    \vspace{4cm}
    {\Huge \textbf{ĐỒ ÁN TỐT NGHIỆP}\par}
    \vspace{2cm}
    {\huge \textbf{ĐỀ TÀI: “THIẾT KẾ HỆ HỖ TRỢ QUẢN TRỊ \\ CƠ SỞ TRI THỨC DẠNG COKB”}\par}
    \vspace{4cm}
    \begin{flushleft}
    \textbf{GVHD: Gs. ĐỖ VĂN NHƠN \& Ths. MAI TRUNG THÀNH}\\
    \textbf{SVTH: LÊ CHÂU TRẦN PHÁT - 2211110068}\\
    \textbf{Lớp: TH22DH-CN2}
    \end{flushleft}
    \vfill
    {\large TP. Hồ Chí Minh, 2026\par}
\end{titlepage}
\tableofcontents
\listoffigures
\listoftables
'''
    main_body = "".join([f'\\include{{chapters/{f.replace(".tex","")}}}\n' for f in files_list])
    main_foot = "\n\\bibliographystyle{IEEEtran}\n\\bibliography{references}\n\\end{document}\n"
    
    with open(os.path.join(LATEX_DIR, 'main.tex'), 'w', encoding='utf-8') as f:
        f.write(main_head + main_body + main_foot)

    with open(os.path.join(LATEX_DIR, 'references.bib'), 'w', encoding='utf-8') as f:
        f.write(r'''
@thesis{MainThesis,
  author = {Mai Trung Thành},
  title = {Nghiên cứu mô hình cơ sở tri thức đối tượng tính toán (COKB) và ứng dụng},
  school = {Trường Đại học Quốc tế Hồng Bàng},
  year = {2024},
  type = {Luận văn Thạc sĩ}
}
@book{DoVanNhon,
  author = {Đỗ Văn Nhơn},
  title = {Các mô hình biểu diễn tri thức và các hệ chuyên gia},
  publisher = {Học viện Công nghệ Bưu chính Viễn thông},
  year = {2010}
}
@book{PrologBook,
  author = {W. F. Clocksin and C. S. Mellish},
  title = {Programming in Prolog},
  publisher = {Springer Science \& Business Media},
  year = {2012}
}
''')
    print("Success! LaTeX Project Rebuilt correctly.")

if __name__ == '__main__':
    main()
