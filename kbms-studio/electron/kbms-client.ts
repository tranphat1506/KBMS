import net from 'node:net';
import { Protocol, MessageType, KbmsMessage } from './kbms-protocol';
import { BrowserWindow } from 'electron';

export class KbmsTCPClient {
   private socket: net.Socket | null = null;
   private buffer: Buffer = Buffer.alloc(0);
   private win: BrowserWindow | null = null;
   public sessionId: string = '';

   private connectResolver: ((val: boolean) => void) | null = null;
   private currentQueryResolver: ((res: any) => void) | null = null;
   private currentQueryRejecter: ((err: any) => void) | null = null;
   private queryResultData: any = null;

   private requestQueue: Array<{ query: string; resolve: (res: any) => void; reject: (err: any) => void }> = [];
   private isProcessingQueue = false;
   private heartbeatTimer: NodeJS.Timeout | null = null;

   constructor() { }

   setWindow(win: BrowserWindow) {
      this.win = win;
   }

   connect(host: string = '127.0.0.1', port: number = 3307, user = 'admin', pass = 'admin'): Promise<boolean> {
      return new Promise((resolve) => {
         this.disconnect(); // Ensure clean state

         this.socket = new net.Socket();
         this.socket.setKeepAlive(true, 30000);
         this.socket.setTimeout(600000); // 10 minutes timeout
         this.connectResolver = resolve;

         this.socket.connect(port, host, () => {
            console.log(`(KBMS Client) Socket connected to ${host}:${port}`);
            this.win?.webContents.send('kbms-status', 'connected');

            // Initial Login
            const loginPayload = `LOGIN ${user} ${pass}`;
            this.socket?.write(Protocol.pack({
               type: MessageType.LOGIN,
               content: loginPayload
            }));

            this.startHeartbeat();
         });

         this.socket.on('data', (data) => {
            try {
               this.buffer = Buffer.concat([this.buffer, data]);
               const { messages, remaining } = Protocol.unpack(this.buffer);
               this.buffer = remaining;

               for (const msg of messages) {
                  try {
                     this.handleServerMessage(msg);
                  } catch (err) {
                     console.error('(KBMS Client) Error handling message:', err);
                  }
               }
            } catch (err) {
               console.error('(KBMS Client) Data processing error:', err);
            }
         });

         this.socket.on('close', () => {
            console.log('(KBMS Client) Connection closed');
            this.cleanup('Connection closed');
            this.win?.webContents.send('kbms-status', 'disconnected');
            this.socket = null;
         });

         this.socket.on('timeout', () => {
            console.warn('(KBMS Client) Socket Timeout');
            this.cleanup('Socket Timeout');
            if (this.socket) this.socket.destroy();
         });

         this.socket.on('error', (err) => {
            console.error('(KBMS Client) Socket error:', err.message);
            this.cleanup(err.message);
            this.win?.webContents.send('kbms-status', 'error');
            if (this.connectResolver) {
               this.connectResolver(false);
               this.connectResolver = null;
            }
         });
      });
   }

   getStatus() {
      return {
         status: this.socket && !this.socket.destroyed ? 'connected' : 'disconnected',
         sessionId: this.sessionId,
         host: this.socket?.remoteAddress,
         port: this.socket?.remotePort
      };
   }

   private cleanup(message: string) {
      if (this.currentQueryRejecter) {
         this.currentQueryRejecter(new Error(message));
      }
      this.currentQueryResolver = null;
      this.currentQueryRejecter = null;
      this.queryResultData = null;
      this.sessionId = '';

      while (this.requestQueue.length > 0) {
         const req = this.requestQueue.shift();
         req?.reject(new Error(message));
      }
      this.isProcessingQueue = false;
      this.stopHeartbeat();
   }

   private startHeartbeat() {
      this.stopHeartbeat();
      this.heartbeatTimer = setInterval(() => {
         if (this.socket && !this.socket.destroyed && this.sessionId) {
            console.log("(KBMS Client) Sending Heartbeat...");
            this.socket.write(Protocol.pack({
               type: MessageType.QUERY,
               content: '',
               sessionId: this.sessionId
            }));
         }
      }, 45000); // 45 seconds
   }

   private stopHeartbeat() {
      if (this.heartbeatTimer) {
         clearInterval(this.heartbeatTimer);
         this.heartbeatTimer = null;
      }
   }

   execute(query: string): Promise<any> {
      return new Promise((resolve, reject) => {
         this.requestQueue.push({ query, resolve, reject });
         this.processQueue();
      });
   }

   private async processQueue() {
      if (this.isProcessingQueue || this.requestQueue.length === 0) return;

      this.isProcessingQueue = true;
      const request = this.requestQueue[0];

      if (!this.socket || this.socket.destroyed) {
         request.reject(new Error("Not connected to server"));
         this.requestQueue.shift();
         this.isProcessingQueue = false;
         this.processQueue();
         return;
      }

      this.currentQueryResolver = request.resolve;
      this.currentQueryRejecter = request.reject;
      this.queryResultData = null;

      const payload = Protocol.pack({
         type: MessageType.QUERY,
         content: request.query,
         sessionId: this.sessionId
      });

      this.socket.write(payload);
   }

   private handleServerMessage(msg: KbmsMessage) {
      console.log(`(KBMS Client) Incoming message [${msg.type}]: ${msg.content.substring(0, 100)}`);

      if (msg.type === MessageType.RESULT && msg.content.startsWith('LOGIN_SUCCESS')) {
         console.log(`(KBMS Client) Login SUCCESS`);
         const parts = msg.content.split(':');
         if (parts.length > 3) this.sessionId = parts[3];

         if (this.connectResolver) {
            this.connectResolver(true);
            this.connectResolver = null;
         }
         return;
      }

      if (msg.type === MessageType.ERROR) {
         console.error(`(KBMS Client) Server Error: ${msg.content}`);
         if (this.connectResolver) {
            this.connectResolver(false);
            this.connectResolver = null;
            return;
         }

         if (!this.queryResultData) {
            this.queryResultData = { headers: ["Error"], rows: [], messages: [] };
         }

         // Server may send error as a JSON object: {"Type":"ParserError","Message":"...","Line":1,"Column":8}
         let errorText = msg.content;
         try {
            const parsed = JSON.parse(msg.content);
            const msgField = parsed.Message || parsed.message;
            const typeField: string = parsed.Type || parsed.type || '';
            const line = parsed.Line ?? parsed.line;
            const col = parsed.Column ?? parsed.column;
            if (msgField) {
               const location = (line != null && col != null) ? ` at line ${line}, col ${col}` : '';
               errorText = typeField ? `[${typeField}] ${msgField}${location}` : `${msgField}${location}`;

               // Store as structured marker for Monaco editor highlighting
               if (line != null && col != null) {
                  if (!this.queryResultData.editorMarkers) this.queryResultData.editorMarkers = [];
                  this.queryResultData.editorMarkers.push({
                     startLineNumber: line,
                     startColumn: col,
                     endLineNumber: line,
                     endColumn: col + 10,
                     message: msgField,
                     severity: 8 // monaco.MarkerSeverity.Error
                  });
               }
            }
         } catch { /* not JSON, use raw content */ }

         this.queryResultData.messages.push({ type: 'error', text: errorText });
      }
      else if (msg.type === MessageType.METADATA) {
         try {
            const metadata = JSON.parse(msg.content);
            if (metadata.error) {
               if (!this.queryResultData) this.queryResultData = { headers: ["Error"], rows: [], messages: [] };
               this.queryResultData.messages.push({ type: 'error', text: metadata.error });
            } else {
               this.queryResultData = {
                  ConceptName: metadata.ConceptName || metadata.conceptName || '',
                  headers: metadata.Columns || [],
                  rows: [],
                  messages: []
               };
            }
         } catch (e) {
            console.error("(KBMS Client) Failed to parse metadata", e);
         }
      }
      else if (msg.type === MessageType.ROW) {
         try {
            const row = JSON.parse(msg.content);
            if (row.error) {
               if (!this.queryResultData) this.queryResultData = { headers: ["Error"], rows: [], messages: [] };
               this.queryResultData.messages.push({ type: 'error', text: row.error });
            } else {
               if (this.queryResultData) this.queryResultData.rows.push(row);
            }
         } catch (e) {
            console.error("(KBMS Client) Failed to parse row", e);
         }
      }
      else if (msg.type === MessageType.RESULT) {
         if (!this.queryResultData) {
            this.queryResultData = { headers: ["Result"], rows: [], messages: [] };
         }
         try {
            const json = JSON.parse(msg.content);
            // Server sends PascalCase (e.g. json.Message, json.Type) or camelCase
            const errorText = json.error || json.Error;
            const messageText = json.message || json.Message;
            const typeText: string = json.Type || json.type || '';
            const isErrorType = typeText.toLowerCase().includes('error');

            if (errorText) {
               this.queryResultData.messages.push({ type: 'error', text: errorText });
            } else if (messageText) {
               // If Type contains "Error", treat it as an error
               const msgType = isErrorType ? 'error' : 'info';
               this.queryResultData.messages.push({ type: msgType, text: messageText });
            } else {
               this.queryResultData.messages.push({ type: 'info', text: msg.content });
            }
         } catch {
            this.queryResultData.messages.push({ type: 'info', text: msg.content });
         }
      }
      else if (msg.type === MessageType.FETCH_DONE) {
         try {
            const summary = JSON.parse(msg.content);
            if (!this.queryResultData) this.queryResultData = { headers: [], rows: [], messages: [] };
            this.queryResultData.messages.push({
               type: 'info',
               text: `Done: ${summary.statementsExecuted || 0} stmts in ${summary.executionTime}s`
            });
         } catch { }

         const resolver = this.currentQueryResolver;
         const result = this.queryResultData;

         this.currentQueryResolver = null;
         this.currentQueryRejecter = null;
         this.queryResultData = null;

         if (resolver) resolver(result);

         this.requestQueue.shift();
         this.isProcessingQueue = false;
         this.processQueue();
      }
   }

   disconnect() {
      this.stopHeartbeat();
      if (this.socket) {
         this.socket.destroy();
         this.socket = null;
         this.sessionId = '';
         this.win?.webContents.send('kbms-status', 'disconnected');
      }
   }
}

export const kbmsClient = new KbmsTCPClient();
