const fs = require('fs');
let s = fs.readFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/CliServerIntegrationTests.cs', 'utf8');

// 1. Replace CREATE CONCEPT X VARIABLES (v1 T1, v2 T2) with CREATE CONCEPT X ( VARIABLES (v1: T1, v2: T2) )
// This regex specifically handles the test cases in CliServerIntegrationTests
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

// CREATE CONCEPT X VARIABLES a INT, b STRING -> CREATE CONCEPT X ( VARIABLES (a: INT, b: STRING) )
s = s.replace(/CREATE CONCEPT ([A-Za-z0-9_]+) VARIABLES ([^;]+);/g, (match, concept, varsStr) => {
    // If it already has ( VARIABLES it shouldn't match because [^;]+ would grab it all, but let's be careful.
    if(match.includes('( VARIABLES')) return match; 
    
    let newVars = varsStr.split(',').map(v => {
        let parts = v.trim().split(' ');
        if (parts.length === 2 && !parts[0].includes(':')) {
            return `${parts[0]}: ${parts[1]}`;
        }
        return v.trim();
    }).join(', ');
    return `CREATE CONCEPT ${concept} ( VARIABLES (${newVars}) );`;
});

// 2. Replace INSERT INTO X VALUES (...) to INSERT INTO X ATTRIBUTE (...)
s = s.replace(/INSERT INTO ([A-Za-z0-9_]+) VALUES \(/g, "INSERT INTO $1 ATTRIBUTE (");

fs.writeFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/CliServerIntegrationTests.cs', s, 'utf8');
