const fs = require('fs');

function fixFile(filePath, method) {
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Regex to match method("QUERY") or method($"QUERY")
    // It captures the start, the query string, and the end.
    // Example: ExecuteCommandAsync("LOGIN root root") -> ExecuteCommandAsync("LOGIN root root;")
    let modified = content;
    
    // Find all occurrences of ExecuteCommandAsync("...")
    // This is a bit tricky, let's just do a simpler replacement
    let count = 0;
    const lines = modified.split('\n');
    for (let i = 0; i < lines.length; i++) {
        if (lines[i].includes(method)) {
            // Find the last quote before the closing parenthesis exactly
            // ExecuteCommandAsync("LOGIN root root")
            let line = lines[i];
            
            // Regex to find: (method\(\$?"[^"]+)("\s*\))
            let regex = new RegExp(`(${method}\\(\\$?"[^"]+[^;])("\\s*\\))`, 'g');
            lines[i] = line.replace(regex, (match, p1, p2) => {
                count++;
                return p1 + ';' + p2;
            });
        }
    }
    
    fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
    console.log(`Updated ${count} queries in ${filePath}`);
}

fixFile('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/CliServerIntegrationTests.cs', 'ExecuteCommandAsync');
fixFile('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase5IntegrationTests.cs', 'Exec');
