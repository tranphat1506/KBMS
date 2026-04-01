import os
import re

DOCS_DIR = 'docs'

def clean_title(line):
    # Match H1-H2
    match = re.match(r'^(#|##)\s+(.*)', line)
    if not match:
        return line
    
    hashes = match.group(1)
    content = match.group(2).strip()
    
    # 1. First, handle special case: title containing a linked term in parens ([Term](url))
    # Regex: find a trailing " ([...](...))"
    new_content = re.sub(r'\s*\(\[[^\]]+\]\([^)]+\)\)\s*$', '', content)
    
    # 2. Then, handle plain trailing parens "(ENGLISH)"
    new_content = re.sub(r'\s*\([^)]+\)\s*$', '', new_content)
    
    # 3. Handle double-bracketed headers like "[[COKB](...)]"
    new_content = re.sub(r'\s*\[\[[^\]]+\]\([^)]+\)\]\s*$', '', new_content)
    
    # 4. Clean up any trailing IDs like [C03] if they are preceded by English parens, 
    # but the user said "mấy cái title h1 h2 có từ tiếng anh dạng (ENGLISH) thì bỏ hết đi"
    # So we only target the parens.
    
    return f"{hashes} {new_content.strip()}\n"

for root, dirs, files in os.walk(DOCS_DIR):
    for file in files:
        if file.endswith('.md'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            new_lines = []
            changed = False
            for line in lines:
                if line.startswith('#') or line.startswith('##'):
                    cleaned = clean_title(line)
                    if cleaned != line:
                        new_lines.append(cleaned)
                        changed = True
                        continue
                new_lines.append(line)
            
            if changed:
                with open(path, 'w', encoding='utf-8') as f:
                    f.writelines(new_lines)
                print(f"Cleaned H1/H2 titles in {path}")
