const fs = require('fs');

function fixFile(filePath) {
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Replace ADD HIERARCHY with CREATE HIERARCHY
    content = content.replace(/ADD HIERARCHY/g, 'CREATE HIERARCHY');
    
    // Replace IS_A with ISA
    content = content.replace(/IS_A/g, 'ISA');
    
    fs.writeFileSync(filePath, content, 'utf8');
    console.log(`Syntax fixes applied to ${filePath}`);
}

fixFile('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/CliServerIntegrationTests.cs');
fixFile('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase5IntegrationTests.cs');
