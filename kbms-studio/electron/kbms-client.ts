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

   private requestQueue: Array<{ 
      type: MessageType; 
      query: string; 
      isBackground?: boolean; 
      requestId?: string; 
      resolve: (res: any) => void; 
      reject: (err: any) => void; 
   }> = [];
   private isProcessingQueue = false;
   private isReconnecting = false;
   private activeRequest: { 
      type: MessageType; 
      query: string; 
      isBackground?: boolean; 
      requestId?: string; 
      resolve: (res: any) => void; 
      reject: (err: any) => void; 
   } | null = null;
   private pendingRequestsMetadata = new Map<string, { isBackground: boolean }>();
   private heartbeatTimer: NodeJS.Timeout | null = null;

   constructor() { }

   setWindow(win: BrowserWindow) {
      this.win = win;
   }

   private sendToUI(channel: string, payload: any) {
      if (this.win && !this.win.isDestroyed() && this.win.webContents && !this.win.webContents.isDestroyed()) {
         try {
            this.win.webContents.send(channel, payload);
         } catch (e) {
            console.error("(KBMS Client) Failed to send message to UI:", e);
         }
      }
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
            this.sendToUI('kbms-status', 'connected');

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
            this.sendToUI('kbms-status', 'disconnected');
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
            this.sendToUI('kbms-status', 'error');
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
            const requestId = 'hb_' + Date.now();
            console.log("(KBMS Client) Sending Heartbeat...");
            this.pendingRequestsMetadata.set(requestId, { isBackground: true });
            this.socket.write(Protocol.pack({
               type: MessageType.QUERY,
               content: '',
               requestId: requestId,
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

   execute(query: string, isBackground: boolean = false, requestId?: string, isManagement: boolean = false): Promise<any> {
      return new Promise((resolve, reject) => {
         if (!requestId) {
            requestId = `${isManagement ? 'mgmt' : 'web'}_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
         }
         this.pendingRequestsMetadata.set(requestId, { isBackground });
         this.requestQueue.push({ 
            type: isManagement ? MessageType.MANAGEMENT_CMD : MessageType.QUERY,
            query, 
            isBackground, 
            requestId, 
            resolve, 
            reject 
         });
         this.processQueue();
      });
   }

   getStats(requestId?: string): Promise<any> {
      return new Promise((resolve, reject) => {
         if (!requestId) requestId = 'stats_' + Date.now();
         this.pendingRequestsMetadata.set(requestId, { isBackground: true });
         this.requestQueue.push({ 
            type: MessageType.STATS,
            query: '', 
            isBackground: true, 
            requestId, 
            resolve, 
            reject 
         });
         this.processQueue();
      });
   }

   getSessions(requestId?: string): Promise<any> {
      return new Promise((resolve, reject) => {
         if (!requestId) requestId = 'sessions_' + Date.now();
         this.pendingRequestsMetadata.set(requestId, { isBackground: true });
         this.requestQueue.push({ 
            type: MessageType.SESSIONS,
            query: '', 
            isBackground: true, 
            requestId, 
            resolve, 
            reject 
         });
         this.processQueue();
      });
   }

   subscribeLogs() {
      if (!this.socket || this.socket.destroyed) return;
      this.socket.write(Protocol.pack({
         type: MessageType.LOGS_STREAM,
         content: '',
         sessionId: this.sessionId
      }));
   }

   private async processQueue() {
      if (this.isProcessingQueue || this.requestQueue.length === 0) return;

      this.isProcessingQueue = true;
      this.activeRequest = this.requestQueue[0];

      if (!this.socket || this.socket.destroyed) {
         this.activeRequest.reject(new Error("Not connected to server"));
         this.requestQueue.shift();
         this.isProcessingQueue = false;
         this.activeRequest = null;
         this.processQueue();
         return;
      }

      this.currentQueryResolver = this.activeRequest.resolve;
      this.currentQueryRejecter = this.activeRequest.reject;
      this.queryResultData = null;

      const payload = Protocol.pack({
         type: this.activeRequest.type,
         content: this.activeRequest.query,
         sessionId: this.sessionId,
         requestId: this.activeRequest.requestId
      });

      this.socket.write(payload);
   }

   private handleServerMessage(msg: KbmsMessage) {
      console.log(`(KBMS Client) Incoming message [${msg.type}]: ${msg.content.substring(0, 100)}`);

      const meta = msg.requestId ? this.pendingRequestsMetadata.get(msg.requestId) : null;
      const isBackground = meta ? meta.isBackground : (this.activeRequest ? !!this.activeRequest.isBackground : false);

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

         // Use helper for structured error extraction
         const { text, markers } = this.extractErrorInfo(msg.content);
         if (markers.length > 0) {
            if (!this.queryResultData.editorMarkers) this.queryResultData.editorMarkers = [];
            this.queryResultData.editorMarkers.push(...markers);
         }
         this.queryResultData.messages.push({ type: 'error', text: text });
         this.sendToUI('kbms-stream', { type: 'error', text, markers, requestId: msg.requestId, isBackground });
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
               this.sendToUI('kbms-stream', { type: 'metadata', metadata, isBackground, requestId: msg.requestId });
            }
         } catch (e) {
            console.error("(KBMS Client) Failed to parse metadata", e);
         }
      }
      else if (msg.type === MessageType.ROW) {
         try {
            const rowData = JSON.parse(msg.content);
            if (!Array.isArray(rowData) && rowData.error) {
               if (!this.queryResultData) this.queryResultData = { headers: ["Error"], rows: [], messages: [] };
               this.queryResultData.messages.push({ type: 'error', text: rowData.error });
            } else {
               if (this.queryResultData) {
                  if (Array.isArray(rowData)) {
                     this.queryResultData.rows.push(...rowData);
                  } else {
                     this.queryResultData.rows.push(rowData);
                  }
               }
               this.sendToUI('kbms-stream', { type: 'row', row: rowData, isBackground, requestId: msg.requestId });
            }
         } catch (e) {
            console.error("(KBMS Client) Failed to parse row", e);
         }
      }
      else if (msg.type === MessageType.RESULT) {
         if (!this.queryResultData) {
            this.queryResultData = { headers: ["Result"], rows: [], messages: [] };
         }

         const { text, markers, isError } = this.extractErrorInfo(msg.content);
         if (markers.length > 0) {
            if (!this.queryResultData.editorMarkers) this.queryResultData.editorMarkers = [];
            this.queryResultData.editorMarkers.push(...markers);
         }
         this.queryResultData.messages.push({ type: isError ? 'error' : 'info', text: text });
         this.sendToUI('kbms-stream', { type: 'result', text, markers, isBackground, requestId: msg.requestId });
      }
      else if (msg.type === MessageType.LOGS_STREAM) {
         try {
            const logData = JSON.parse(msg.content);
            this.sendToUI('kbms-stream', { type: 'log-signal', data: logData });
         } catch (e) {
            console.error("(KBMS Client) Failed to parse log signal", e);
         }
      }
      else if (msg.type === MessageType.FETCH_DONE) {
         try {
            const summary = JSON.parse(msg.content);
            if (!this.queryResultData) this.queryResultData = { headers: [], rows: [], messages: [] };
            const text = `Done: ${summary.statementsExecuted || 0} stmts in ${summary.executionTime}s`;
            this.queryResultData.messages.push({
               type: 'info',
               text: text
            });
            this.sendToUI('kbms-stream', { type: 'result', text, isBackground, requestId: msg.requestId });
         } catch { }

         const resolver = this.currentQueryResolver;
         const result = this.queryResultData;

         this.currentQueryResolver = null;
         this.currentQueryRejecter = null;
         this.queryResultData = null;

         if (resolver) resolver(result);

         this.requestQueue.shift();
         if (msg.requestId) {
            this.pendingRequestsMetadata.delete(msg.requestId);
         }
         this.isProcessingQueue = false;
         this.processQueue();
      }
   }

   private extractErrorInfo(content: string) {
      let text = content;
      let markers: any[] = [];
      let isError = false;

      try {
         const json = JSON.parse(content);
         const msgField = json.Message || json.message || json.Error || json.error;
         const typeField: string = json.Type || json.type || '';
         const line = json.Line ?? json.line;
         const col = json.Column ?? json.column;
         isError = typeField.toLowerCase().includes('error') || !!(json.Error || json.error);

         if (msgField) {
            const location = (line != null && col != null && line > 0) ? ` at line ${line}, col ${col}` : '';
            text = typeField ? `[${typeField}] ${msgField}${location}` : `${msgField}${location}`;

            if (line != null && col != null && line > 0) {
               markers.push({
                  startLineNumber: line,
                  startColumn: col,
                  endLineNumber: line,
                  endColumn: col + 1, // Focus on the start of the error
                  message: msgField,
                  severity: 8 // monaco.MarkerSeverity.Error
               });
            }
         }
      } catch {
         isError = content.toLowerCase().includes('error');
      }

      return { text, markers, isError };
   }

   disconnect() {
      this.stopHeartbeat();
      if (this.socket) {
         this.socket.destroy();
         this.socket = null;
         this.sessionId = '';
         this.sendToUI('kbms-status', 'disconnected');
      }
   }
}

export const kbmsClient = new KbmsTCPClient();
