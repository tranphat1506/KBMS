const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const crypto = require('crypto');

const docsDir = path.join(__dirname, 'docs');
const outputDir = path.join(docsDir, 'assets', 'diagrams');

if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir, { recursive: true });
}

function getFiles(dir, fileList = []) {
    const files = fs.readdirSync(dir);
    files.forEach(file => {
        const filePath = path.join(dir, file);
        if (fs.statSync(filePath).isDirectory()) {
            if (file !== 'assets' && file !== 'images') {
                getFiles(filePath, fileList);
            }
        } else if (file.endsWith('.md')) {
            fileList.push(filePath);
        }
    });
    return fileList;
}

const mdFiles = getFiles(docsDir);

mdFiles.forEach(file => {
    let content = fs.readFileSync(file, 'utf8');
    const mermaidRegex = /```mermaid\r?\n([\s\S]*?)```/g;
    
    // We use replace with a callback to avoid lastIndex issues
    const newContent = content.replace(mermaidRegex, (match, mermaidCode, offset) => {
        // Try to find the nearest heading above the block
        const textBefore = content.substring(0, offset);
        const headingMatch = [...textBefore.matchAll(/^#+\s+(.*)$/gm)].pop();
        const caption = headingMatch ? headingMatch[1].trim() : 'Sơ đồ hệ thống';

        const hash = crypto.createHash('md5').update(mermaidCode).digest('hex').substring(0, 8);
        const fileName = `diagram_${hash}.png`;
        const filePath = path.join(outputDir, fileName);

        // Save mermaid code to temp file
        const tempMmd = path.join(__dirname, `temp_${hash}.mmd`);
        fs.writeFileSync(tempMmd, mermaidCode);

        console.log(`Rendering "${caption}" (${fileName}) from ${path.basename(file)}...`);
        try {
            // Run mmdc via npx
            // Using -b transparent for better blending with dark/light modes
            execSync(`npx -y @mermaid-js/mermaid-cli@latest -i "${tempMmd}" -o "${filePath}" -b transparent -w 1200`, { stdio: 'inherit' });
            
            // Calculate relative path for the markdown file
            const relativePath = path.relative(path.dirname(file), filePath);
            
            // Return the replacement string
            return `![${caption}](${relativePath})\n*Hình: ${caption}*`;
        } catch (err) {
            console.error(`Failed to render mermaid for ${file}:`, err.message);
            return match; // Keep the original if failed
        } finally {
            if (fs.existsSync(tempMmd)) fs.unlinkSync(tempMmd);
        }
    });

    if (newContent !== content) {
        fs.writeFileSync(file, newContent);
        console.log(`Updated ${file}`);
    }
});
