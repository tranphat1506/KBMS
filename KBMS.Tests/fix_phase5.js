const fs = require('fs');

let p5 = fs.readFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase5IntegrationTests.cs', 'utf8');
// Only line 86, 94 for export/import 
// line 108 for trigger
p5 = p5.replace(/Assert\.Contains\("\\"Success\\":true", expStr\);/g, 'Assert.Contains("\\"success\\":true", expStr);');
p5 = p5.replace(/Assert\.Contains\("\\"Success\\":true", impStr\);/g, 'Assert.Contains("\\"success\\":true", impStr);');
p5 = p5.replace(/Assert\.Contains\("\\"Success\\":true", s\);/g, 'Assert.Contains("\\"success\\":true", s);');

fs.writeFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase5IntegrationTests.cs', p5, 'utf8');

// Also, the Describe tests DO return PascalCase "Success":true because they use QueryResultSet
// But the replace up there would revert describe tests if they use `s` as string variable name!
// Let me just ensure the describe tests use `"Success"`
