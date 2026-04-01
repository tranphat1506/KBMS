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

def link_terms(content):
    placeholders = []
    
    def mask(m):
        placeholders.append(m.group(0))
        return f"PH__{len(placeholders)-1}__PH"

    # Mask everything that should NOT be modified
    # First: mask <!-- no-glossary --> ... <!-- /no-glossary --> blocks entirely
    content = re.sub(r'<!--\s*no-glossary\s*-->.*?<!--\s*/no-glossary\s*-->', mask, content, flags=re.DOTALL)
    # Mask Markdown headers (lines starting with #) so they don't appear linked in TOC
    content = re.sub(r'^#+.*$', mask, content, flags=re.MULTILINE)
    content = re.sub(r'```.*?```', mask, content, flags=re.DOTALL)
    content = re.sub(r'`.*?`', mask, content)
    # Mask math blocks
    content = re.sub(r'\$\$.*?\$\$', mask, content, flags=re.DOTALL)
    content = re.sub(r'\$.*?\$', mask, content)
    # Mask existing links [text](url)
    content = re.sub(r'\[[^\]]+\]\([^\)]+\)', mask, content)
    content = re.sub(r'!\[[^\]]+\]\([^\)]+\)', mask, content)
    content = re.sub(r'<[^>]+>', mask, content)

    # Combine terms into one big regex for single-pass replacement
    pattern = r'\b(' + '|'.join(re.escape(t) for t in sorted_terms) + r')\b'
    
    def repl(m):
        raw_text = m.group(1)
        key = raw_text.lower()
        for t in sorted_terms:
            if t.lower() == key:
                slug = term_data[t]
                return f"[{raw_text}](../00-glossary/01-glossary.md#{slug})"
        return raw_text

    content = re.sub(pattern, repl, content, flags=re.IGNORECASE)

    # Restore placeholders in reverse order
    for i in range(len(placeholders) - 1, -1, -1):
        content = content.replace(f"PH__{i}__PH", placeholders[i])
        
    return content

# 2. Walk through all md files
for root, dirs, files in os.walk(DOCS_DIR):
    for file in files:
        if file.endswith('.md') and file != '01-glossary.md' and 'reference' not in file.lower():
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # More aggressive link stripping to clean up broken past attempts
            # Matches variations of glossary links (handles dashes, extra spaces, etc.)
            content = re.sub(r'\[([^\]]+)\]\(\.\./00[-−\s]*glossary/01[-−\s]*glossary\.md[^)]*\)', r'\1', content)
            
            new_content = link_terms(content)
            
            if new_content != content:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Updated {path}")
