import os
import re

DOCS_DIR = 'docs'
GLOSSARY_FILE = os.path.join(DOCS_DIR, '00-glossary', '01-glossary.md')

def slugify(s):
    s = s.lower().strip()
    s = re.sub(r'[^\w\s-]', '', s)
    s = re.sub(r'[-\s]+', '-', s)
    return s

# 1. Get all terms from glossary
term_data = {} # term -> slug
with open(GLOSSARY_FILE, 'r', encoding='utf-8') as f:
    for line in f:
        if '|' in line and '[' in line and ']' in line:
            parts = [p.strip() for p in line.split('|')]
            if len(parts) >= 3:
                term = parts[2].strip(' *[]')
                if term and len(term) > 2:
                    term_data[term] = slugify(term)

# Sort terms by length descending
sorted_terms = sorted(term_data.keys(), key=len, reverse=True)

def link_terms(content, rel_glossary_path):
    placeholders = []
    
    def mask(m):
        placeholders.append(m.group(0))
        return f"PH__{len(placeholders)-1}__PH"

    # Mask everything that should NOT be modified
    content = re.sub(r'<!--\s*no-glossary\s*-->.*?<!--\s*/no-glossary\s*-->', mask, content, flags=re.DOTALL)
    content = re.sub(r'^\s*#+.*$', mask, content, flags=re.MULTILINE)
    content = re.sub(r'^\s*\|.*\|\s*$', mask, content, flags=re.MULTILINE)
    content = re.sub(r'^\s*(Hình|Bảng|Sơ đồ|Đồ thị|Figure|Table)\s+\d+[:.].*$', mask, content, flags=re.IGNORECASE | re.MULTILINE)
    content = re.sub(r'```.*?```', mask, content, flags=re.DOTALL)
    content = re.sub(r'`.*?`', mask, content)
    content = re.sub(r'\$\$.*?\$\$', mask, content, flags=re.DOTALL)
    content = re.sub(r'\$.*?\$', mask, content)
    content = re.sub(r'\[[^\]]+\]\([^\)]+\)', mask, content)
    content = re.sub(r'!\[[^\]]+\]\([^\)]+\)', mask, content)
    content = re.sub(r'<[^>]+>', mask, content)

    # Combine terms into one big regex
    pattern = r'\b(' + '|'.join(re.escape(t) for t in sorted_terms) + r')\b'
    
    seen_terms_in_file = set()
    term_to_official = {t.lower(): t for t in sorted_terms}

    def repl(m):
        raw_text = m.group(1)
        term_key = raw_text.lower()
        if term_key in seen_terms_in_file: return raw_text
        official_term = term_to_official.get(term_key)
        if official_term:
            slug = term_data[official_term]
            seen_terms_in_file.add(term_key)
            return f"[{raw_text}]({rel_glossary_path}#{slug})"
        return raw_text

    content = re.sub(pattern, repl, content, flags=re.IGNORECASE)

    # Restore placeholders
    for i in range(len(placeholders) - 1, -1, -1):
        content = content.replace(f"PH__{i}__PH", placeholders[i])
    return content

# 2. Walk through all md files
for root, dirs, files in os.walk(DOCS_DIR):
    for file in files:
        # Skip the glossary itself and binary/temp folders
        if file.endswith('.md') and file != '01-glossary.md' and '00-glossary' not in root:
            # Also skip the actual bib chapter 06-references to keep it clean
            if '06-references' in root: continue
            
            path = os.path.join(root, file)
            # Calculate the relative path from this file's folder to the glossary file
            # e.g., from 'docs/04-architecture/02-models' to 'docs/00-glossary/01-glossary.md'
            rel_glossary = os.path.relpath(GLOSSARY_FILE, start=root)
            
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Deep Link Stripping: remove all glossary links (handles any number of ../)
            content = re.sub(r'\[([^\]]+)\]\((?:\.\./)*00-glossary/01-glossary\.md[^)]*\)', r'\1', content)
            
            new_content = link_terms(content, rel_glossary)
            
            if new_content != content:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Updated {path} -> {rel_glossary}")
