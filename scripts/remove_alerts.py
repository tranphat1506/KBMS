import os
import re

def remove_alerts_from_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Regex for matching GitHub alert blocks:
    # Starts with > [!..., and captures subsequent lines that also start with >
    # Using re.MULTILINE to allow ^ to match start of lines
    pattern = re.compile(r'^> \[!.*(?:\n>.*)*', re.MULTILINE)
    
    new_content = pattern.sub('', content)
    
    # Clean up redundant empty lines (max 2 consecutive newlines)
    # This replaces 3 or more newlines with 2 newlines (one empty line between blocks)
    new_content = re.sub(r'\n{3,}', '\n\n', new_content)
    
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        return True
    return False

def process_directory(directory):
    modified_files = []
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith('.md'):
                file_path = os.path.join(root, file)
                if remove_alerts_from_file(file_path):
                    modified_files.append(file_path)
    return modified_files

if __name__ == "__main__":
    docs_dir = "/Users/lechautranphat/Desktop/KBMS/docs"
    modified = process_directory(docs_dir)
    print(f"Successfully processed docs directory.")
    if modified:
        print(f"Modified {len(modified)} files:")
        for f in modified:
            print(f" - {f}")
    else:
        print("No files were modified.")
