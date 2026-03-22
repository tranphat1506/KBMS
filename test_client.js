const net = require('net');
const client = new net.Socket();
client.connect(3307, '127.0.0.1', () => {
  client.write("{" + "\"type\":\"LOGIN\",\"content\":\"LOGIN root root\",\"sessionId\":\"\"}");
});
client.on('data', (d) => {
  console.log("DATA=>", d.toString());
  if(d.toString().includes("LOGIN_SUCCESS")) {
     client.write("{" + "\"type\":\"QUERY\",\"content\":\"CREATE HIERARCHY Person ISA Person2;\",\"sessionId\":\"\"}");
  }
});
