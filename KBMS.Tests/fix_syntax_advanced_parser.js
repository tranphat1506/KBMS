const fs = require('fs');

function fixSyntax(filePath) {
    let s = fs.readFileSync(filePath, 'utf8');

    // 1. Replace CREATE CONCEPT X VARIABLES (v1 T1, v2 T2) with CREATE CONCEPT X ( VARIABLES (v1: T1, v2: T2) )
    s = s.replace(/CREATE CONCEPT ([A-Za-z0-9_]+) VARIABLES \(([^)]+)\)/g, (match, concept, vars) => {
        let newVars = vars.split(',').map(v => {
            let parts = v.trim().split(' ');
            if (parts.length === 2 && !parts[0].includes(':')) {
                return `${parts[0]}: ${parts[1]}`;
            }
            return v.trim();
        }).join(', ');
        return `CREATE CONCEPT ${concept} ( VARIABLES (${newVars}) )`;
    });

    // 2. CREATE CONCEPT X VARIABLES a INT, b STRING -> CREATE CONCEPT X ( VARIABLES (a: INT, b: STRING) )
    s = s.replace(/CREATE CONCEPT ([A-Za-z0-9_]+) VARIABLES ([^;]+);/g, (match, concept, varsStr) => {
        if(match.includes('( VARIABLES')) return match; 
        
        // Remove trailing )" if any
        let cleanVarsStr = varsStr;
        let suffix = ';';
        if (varsStr.endsWith(')"')) {
            cleanVarsStr = varsStr.substring(0, varsStr.length - 2);
            suffix = ');"';
        }
        
        let newVars = cleanVarsStr.split(',').map(v => {
            let parts = v.trim().split(' ');
            if (parts.length === 2 && !parts[0].includes(':')) {
                return `${parts[0]}: ${parts[1]}`;
            }
            return v.trim();
        }).join(', ');
        return `CREATE CONCEPT ${concept} ( VARIABLES (${newVars}) )${suffix}`;
    });

    // 3. Replace INSERT INTO X VALUES (...) to INSERT INTO X ATTRIBUTE (...)
    s = s.replace(/INSERT INTO ([A-Za-z0-9_]+) VALUES \(/g, "INSERT INTO $1 ATTRIBUTE (");

    fs.writeFileSync(filePath, s, 'utf8');
}

fixSyntax('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/ParserTests.cs');
fixSyntax('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase19Tests.cs');
