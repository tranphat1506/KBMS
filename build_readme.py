import os
import shutil
import re
import glob

def build_readme():
    print("Building README.md from docs...")
    
    # 1. Copy latex_report/assets to root assets
    src_assets = "latex_report/assets"
    dst_assets = "assets"
    
    if os.path.exists(dst_assets):
        shutil.rmtree(dst_assets)
    shutil.copytree(src_assets, dst_assets)
    print(f"Copied {src_assets} to {dst_assets}")

    # 2. Gather all markdown files in docs
    doc_folders = sorted([f for f in os.listdir("docs") if os.path.isdir(os.path.join("docs", f)) and f != "assets"])
    
    readme_content = "# HỆ QUẢN TRỊ CƠ SỞ TRI THỨC DẠNG COKB\n\n"
    
    for folder in doc_folders:
        folder_path = os.path.join("docs", folder)
        md_files = sorted(glob.glob(os.path.join(folder_path, "*.md")))
        
        for md_file in md_files:
            with open(md_file, "r", encoding="utf-8") as f:
                content = f.read()
                
                # Replace image links: ![alt text](../assets/diagrams/image.png) -> ![alt text](./assets/image.png)
                # We want to catch the filename and redirect it to ./assets/filename
                # Regex matches: ![...](.../filename.ext)
                content = re.sub(r'!\[([^\]]*)\]\([^)]*/([^/]+\.(?:png|jpg|jpeg|pdf|svg))\)', r'![\1](./assets/\2)', content)
                
                # Clean up the custom LaTeX attributes like | width=1.1
                content = re.sub(r'!\[([^\]]*?)\s*\|\s*width=[^\]]+\]', r'![\1]', content)
                
                readme_content += content + "\n\n"
                
    # 3. Write to README.md
    with open("README.md", "w", encoding="utf-8") as f:
        f.write(readme_content)
        
    print("Successfully generated README.md")

if __name__ == "__main__":
    build_readme()
