import os
import re
import glob

workspace = "/Users/geminicancode/Desktop/GITHUB_REPO/KBMS"

# 1. Rename directories
directories_to_rename = [
    ("KBMS.Knowledge/V3", "KBMS.Knowledge/Core"),
    ("KBMS.Models/V3", "KBMS.Models/Core"),
    ("KBMS.Server/V3", "KBMS.Server/Core"),
    ("KBMS.Storage/V3", "KBMS.Storage/Core")
]

for old_sub, new_sub in directories_to_rename:
    old_path = os.path.join(workspace, old_sub)
    new_path = os.path.join(workspace, new_sub)
    if os.path.exists(old_path):
        os.rename(old_path, new_path)
        print(f"Renamed directory {old_path} -> {new_path}")

# 2. Find and rename files containing V3
renamed_files_map = {
    "V3DataRouter.cs": "StorageRouter.cs",
    "WalManagerV3.cs": "WalManager.cs"
}

for root, dirs, files in os.walk(workspace):
    if "node_modules" in root or ".git" in root or "bin" in root or "obj" in root:
        continue
    for file in files:
        if "V3" in file:
            old_file_path = os.path.join(root, file)
            # Remove V3 from file name, or use map
            if file in renamed_files_map:
                new_file = renamed_files_map[file]
            else:
                new_file = file.replace("V3", "")
            
            new_file_path = os.path.join(root, new_file)
            os.rename(old_file_path, new_file_path)
            print(f"Renamed file {file} -> {new_file}")

# 3. Replace strings inside files
replacements = {
    r'\bV3DataRouter\b': 'StorageRouter',
    r'\bV3Catalog\b': 'SystemCatalog',
    r'\bKBMS\.Knowledge\.V3\b': 'KBMS.Knowledge.Core',
    r'\bKBMS\.Models\.V3\b': 'KBMS.Models.Core',
    r'\bKBMS\.Server\.V3\b': 'KBMS.Server.Core',
    r'\bKBMS\.Storage\.V3\b': 'KBMS.Storage.Core',
    r'\(V3 Catalog\)': '(System Catalog)',
    r'\bStorageV3\b': 'CoreStorage'
}

for root, dirs, files in os.walk(workspace):
    if "node_modules" in root or ".git" in root or "bin" in root or "obj" in root:
        continue
    for file in files:
        if not file.endswith('.cs') and not file.endswith('.md') and not file.endswith('.txt'):
            continue
            
        file_path = os.path.join(root, file)
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            new_content = content
            for old_pattern, new_repl in replacements.items():
                new_content = re.sub(old_pattern, new_repl, new_content)
                
            if new_content != content:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Updated content in {file_path}")
        except Exception as e:
            print(f"Failed to read/write {file_path}: {e}")

