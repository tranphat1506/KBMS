import { KbmsTCPClient } from './kbms-studio/electron/kbms-client';

async function test() {
   const client = new KbmsTCPClient();
   const connected = await client.connect('127.0.0.1', 3307, 'root', 'root');
   console.log("Connected:", connected);
   if (connected) {
       const res = await client.execute("CREATE HIERARCHY Person ISA Person2;");
       console.log("RESULT===>\n", JSON.stringify(res, null, 2));
       client.disconnect();
   } else {
       console.log("Failed to connect");
       process.exit(1);
   }
}

test().catch(console.error);
