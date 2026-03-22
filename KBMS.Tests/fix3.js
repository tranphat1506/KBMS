const fs = require('fs');

function fixParserFile(filePath) {
    let content = fs.readFileSync(filePath, 'utf8');
    
    // We want to replace string literals passed to ParseStatement that don't end with semicolon
    // Example: ParseStatement("CREATE CONCEPT Person VARIABLES (name STRING)") -> ParseStatement("CREATE CONCEPT Person VARIABLES (name STRING);")
    
    // Regex logic: find occurrences of `"some SQL string"` and append `;` before the closing quote
    // But how to know it's a query string?
    // In ParserTests.cs, the strings are passed to ParseStatement("...") or var parser = new Parser.Parser("...")
    // Let's use string replace for specific cases or just manually fix.
    
    let count = 0;
    const lines = content.split('\n');
    for (let i = 0; i < lines.length; i++) {
        // A simple heuristic: if a line contains a string that looks like KBQL, we replace the closing quote with ;"
        // But only if it doesn't already have ;"
        if (lines[i].includes('ParseStatement') || lines[i].includes('new Parser.Parser')) {
           // Skip if it contains ParseStatement("") or already ends with ;"
           if (!lines[i].includes('ParseStatement("")') && !lines[i].match(/;"\)/)) {
               lines[i] = lines[i].replace(/([^;])("\s*\))/, (m, p1, p2) => {
                   count++;
                   return p1 + ';' + p2;
               });
           }
        }
    }
    
    fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
    console.log(`Updated ${count} lines in ${filePath}`);
}

fixParserFile('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/ParserTests.cs');
fixParserFile('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase19Tests.cs');
