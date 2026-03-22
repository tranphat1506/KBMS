const fs = require('fs');
let s = fs.readFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/CliServerIntegrationTests.cs', 'utf8');
s = s.replace(/LOGIN ([^ ]+) ([^" ]+);/g, "LOGIN $1 $2");
fs.writeFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/CliServerIntegrationTests.cs', s, 'utf8');

let p5 = fs.readFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase5IntegrationTests.cs', 'utf8');
p5 = p5.replace(/"success":true/g, '"Success":true');
fs.writeFileSync('/Users/lechautranphat/Desktop/KBMS/KBMS.Tests/Phase5IntegrationTests.cs', p5, 'utf8');
