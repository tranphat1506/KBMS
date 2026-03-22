import { BrowserWindow, app, dialog, ipcMain } from "electron";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import net from "node:net";
import { Buffer as Buffer$1 } from "node:buffer";
//#region electron/kbms-protocol.ts
var MessageType = /* @__PURE__ */ function(MessageType) {
	MessageType[MessageType["LOGIN"] = 1] = "LOGIN";
	MessageType[MessageType["QUERY"] = 2] = "QUERY";
	MessageType[MessageType["RESULT"] = 3] = "RESULT";
	MessageType[MessageType["ERROR"] = 4] = "ERROR";
	MessageType[MessageType["LOGOUT"] = 5] = "LOGOUT";
	MessageType[MessageType["METADATA"] = 6] = "METADATA";
	MessageType[MessageType["ROW"] = 7] = "ROW";
	MessageType[MessageType["FETCH_DONE"] = 8] = "FETCH_DONE";
	return MessageType;
}({});
var Protocol = class {
	static pack(message) {
		const contentBuffer = Buffer$1.from(message.content, "utf8");
		const sessionIdBuffer = message.sessionId ? Buffer$1.from(message.sessionId, "utf8") : Buffer$1.alloc(0);
		const sessionIdLength = sessionIdBuffer.length;
		const totalLength = contentBuffer.length + 2 + sessionIdLength;
		const lengthBuffer = Buffer$1.alloc(4);
		lengthBuffer.writeInt32BE(totalLength, 0);
		const typeBuffer = Buffer$1.from([message.type]);
		const sessionIdLenBuffer = Buffer$1.alloc(2);
		sessionIdLenBuffer.writeUInt16BE(sessionIdLength, 0);
		return Buffer$1.concat([
			lengthBuffer,
			typeBuffer,
			sessionIdLenBuffer,
			sessionIdBuffer,
			contentBuffer
		]);
	}
	/**
	* Parses a buffer stream and returns messages.
	* Returns the parsed messages and the remaining buffer.
	*/
	static unpack(buffer) {
		const messages = [];
		let offset = 0;
		while (offset + 4 <= buffer.length) {
			const length = buffer.readInt32BE(offset);
			const packetTotalBytes = 5 + length;
			if (offset + packetTotalBytes > buffer.length) break;
			const type = buffer.readUInt8(offset + 4);
			const sessionIdLength = buffer.readUInt16BE(offset + 5);
			let sessionId = void 0;
			if (sessionIdLength > 0) sessionId = buffer.toString("utf8", offset + 7, offset + 7 + sessionIdLength);
			const payloadLength = length - 2 - sessionIdLength;
			const contentStart = offset + 7 + sessionIdLength;
			const content = buffer.toString("utf8", contentStart, contentStart + payloadLength);
			messages.push({
				type,
				content,
				sessionId
			});
			offset += packetTotalBytes;
		}
		return {
			messages,
			remaining: buffer.subarray(offset)
		};
	}
};
//#endregion
//#region electron/kbms-client.ts
var KbmsTCPClient = class {
	constructor() {
		this.socket = null;
		this.buffer = Buffer.alloc(0);
		this.win = null;
		this.sessionId = "";
		this.connectResolver = null;
		this.currentQueryResolver = null;
		this.currentQueryRejecter = null;
		this.queryResultData = null;
		this.requestQueue = [];
		this.isProcessingQueue = false;
		this.heartbeatTimer = null;
	}
	setWindow(win) {
		this.win = win;
	}
	connect(host = "127.0.0.1", port = 3307, user = "admin", pass = "admin") {
		return new Promise((resolve) => {
			this.disconnect();
			this.socket = new net.Socket();
			this.socket.setKeepAlive(true, 3e4);
			this.socket.setTimeout(6e5);
			this.connectResolver = resolve;
			this.socket.connect(port, host, () => {
				console.log(`(KBMS Client) Socket connected to ${host}:${port}`);
				this.win?.webContents.send("kbms-status", "connected");
				const loginPayload = `LOGIN ${user} ${pass}`;
				this.socket?.write(Protocol.pack({
					type: MessageType.LOGIN,
					content: loginPayload
				}));
				this.startHeartbeat();
			});
			this.socket.on("data", (data) => {
				try {
					this.buffer = Buffer.concat([this.buffer, data]);
					const { messages, remaining } = Protocol.unpack(this.buffer);
					this.buffer = remaining;
					for (const msg of messages) try {
						this.handleServerMessage(msg);
					} catch (err) {
						console.error("(KBMS Client) Error handling message:", err);
					}
				} catch (err) {
					console.error("(KBMS Client) Data processing error:", err);
				}
			});
			this.socket.on("close", () => {
				console.log("(KBMS Client) Connection closed");
				this.cleanup("Connection closed");
				this.win?.webContents.send("kbms-status", "disconnected");
				this.socket = null;
			});
			this.socket.on("timeout", () => {
				console.warn("(KBMS Client) Socket Timeout");
				this.cleanup("Socket Timeout");
				if (this.socket) this.socket.destroy();
			});
			this.socket.on("error", (err) => {
				console.error("(KBMS Client) Socket error:", err.message);
				this.cleanup(err.message);
				this.win?.webContents.send("kbms-status", "error");
				if (this.connectResolver) {
					this.connectResolver(false);
					this.connectResolver = null;
				}
			});
		});
	}
	getStatus() {
		return {
			status: this.socket && !this.socket.destroyed ? "connected" : "disconnected",
			sessionId: this.sessionId,
			host: this.socket?.remoteAddress,
			port: this.socket?.remotePort
		};
	}
	cleanup(message) {
		if (this.currentQueryRejecter) this.currentQueryRejecter(new Error(message));
		this.currentQueryResolver = null;
		this.currentQueryRejecter = null;
		this.queryResultData = null;
		this.sessionId = "";
		while (this.requestQueue.length > 0) this.requestQueue.shift()?.reject(new Error(message));
		this.isProcessingQueue = false;
		this.stopHeartbeat();
	}
	startHeartbeat() {
		this.stopHeartbeat();
		this.heartbeatTimer = setInterval(() => {
			if (this.socket && !this.socket.destroyed && this.sessionId) {
				console.log("(KBMS Client) Sending Heartbeat...");
				this.socket.write(Protocol.pack({
					type: MessageType.QUERY,
					content: "",
					sessionId: this.sessionId
				}));
			}
		}, 45e3);
	}
	stopHeartbeat() {
		if (this.heartbeatTimer) {
			clearInterval(this.heartbeatTimer);
			this.heartbeatTimer = null;
		}
	}
	execute(query) {
		return new Promise((resolve, reject) => {
			this.requestQueue.push({
				query,
				resolve,
				reject
			});
			this.processQueue();
		});
	}
	async processQueue() {
		if (this.isProcessingQueue || this.requestQueue.length === 0) return;
		this.isProcessingQueue = true;
		const request = this.requestQueue[0];
		if (!this.socket || this.socket.destroyed) {
			request.reject(/* @__PURE__ */ new Error("Not connected to server"));
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
	handleServerMessage(msg) {
		console.log(`(KBMS Client) Incoming message [${msg.type}]: ${msg.content.substring(0, 100)}`);
		if (msg.type === MessageType.RESULT && msg.content.startsWith("LOGIN_SUCCESS")) {
			console.log(`(KBMS Client) Login SUCCESS`);
			const parts = msg.content.split(":");
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
			if (!this.queryResultData) this.queryResultData = {
				headers: ["Error"],
				rows: [],
				messages: []
			};
			let errorText = msg.content;
			try {
				const parsed = JSON.parse(msg.content);
				const msgField = parsed.Message || parsed.message;
				const typeField = parsed.Type || parsed.type || "";
				const line = parsed.Line ?? parsed.line;
				const col = parsed.Column ?? parsed.column;
				if (msgField) {
					const location = line != null && col != null ? ` at line ${line}, col ${col}` : "";
					errorText = typeField ? `[${typeField}] ${msgField}${location}` : `${msgField}${location}`;
					if (line != null && col != null) {
						if (!this.queryResultData.editorMarkers) this.queryResultData.editorMarkers = [];
						this.queryResultData.editorMarkers.push({
							startLineNumber: line,
							startColumn: col,
							endLineNumber: line,
							endColumn: col + 10,
							message: msgField,
							severity: 8
						});
					}
				}
			} catch {}
			this.queryResultData.messages.push({
				type: "error",
				text: errorText
			});
		} else if (msg.type === MessageType.METADATA) try {
			const metadata = JSON.parse(msg.content);
			if (metadata.error) {
				if (!this.queryResultData) this.queryResultData = {
					headers: ["Error"],
					rows: [],
					messages: []
				};
				this.queryResultData.messages.push({
					type: "error",
					text: metadata.error
				});
			} else this.queryResultData = {
				ConceptName: metadata.ConceptName || metadata.conceptName || "",
				headers: metadata.Columns || [],
				rows: [],
				messages: []
			};
		} catch (e) {
			console.error("(KBMS Client) Failed to parse metadata", e);
		}
		else if (msg.type === MessageType.ROW) try {
			const row = JSON.parse(msg.content);
			if (row.error) {
				if (!this.queryResultData) this.queryResultData = {
					headers: ["Error"],
					rows: [],
					messages: []
				};
				this.queryResultData.messages.push({
					type: "error",
					text: row.error
				});
			} else if (this.queryResultData) this.queryResultData.rows.push(row);
		} catch (e) {
			console.error("(KBMS Client) Failed to parse row", e);
		}
		else if (msg.type === MessageType.RESULT) {
			if (!this.queryResultData) this.queryResultData = {
				headers: ["Result"],
				rows: [],
				messages: []
			};
			try {
				const json = JSON.parse(msg.content);
				const errorText = json.error || json.Error;
				const messageText = json.message || json.Message;
				const isErrorType = (json.Type || json.type || "").toLowerCase().includes("error");
				if (errorText) this.queryResultData.messages.push({
					type: "error",
					text: errorText
				});
				else if (messageText) {
					const msgType = isErrorType ? "error" : "info";
					this.queryResultData.messages.push({
						type: msgType,
						text: messageText
					});
				} else this.queryResultData.messages.push({
					type: "info",
					text: msg.content
				});
			} catch {
				this.queryResultData.messages.push({
					type: "info",
					text: msg.content
				});
			}
		} else if (msg.type === MessageType.FETCH_DONE) {
			try {
				const summary = JSON.parse(msg.content);
				if (!this.queryResultData) this.queryResultData = {
					headers: [],
					rows: [],
					messages: []
				};
				this.queryResultData.messages.push({
					type: "info",
					text: `Done: ${summary.statementsExecuted || 0} stmts in ${summary.executionTime}s`
				});
			} catch {}
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
			this.sessionId = "";
			this.win?.webContents.send("kbms-status", "disconnected");
		}
	}
};
var kbmsClient = new KbmsTCPClient();
//#endregion
//#region electron/main.ts
var __dirname = path.dirname(fileURLToPath(import.meta.url));
process.env.DIST = path.join(__dirname, "../dist");
process.env.VITE_PUBLIC = app.isPackaged ? process.env.DIST : path.join(process.env.DIST, "../public");
var win;
function createWindow() {
	win = new BrowserWindow({
		icon: path.join(process.env.VITE_PUBLIC, "electron-vite.svg"),
		webPreferences: { preload: path.join(__dirname, "preload.mjs") },
		width: 1280,
		height: 800,
		titleBarStyle: "hiddenInset",
		backgroundColor: "#ffffff"
	});
	kbmsClient.setWindow(win);
	ipcMain.handle("kbms:execute", async (_, query) => {
		try {
			console.log("Execute called from UI:", query);
			const result = await kbmsClient.execute(query);
			console.log("[DEBUG] Execute result returned to UI:", JSON.stringify(result, null, 2));
			return result;
		} catch (err) {
			return {
				success: false,
				messages: [err.message],
				rows: [],
				headers: []
			};
		}
	});
	ipcMain.handle("kbms:connect", async (_, host, port, user, pass) => {
		try {
			return { success: await kbmsClient.connect(host, port, user, pass) };
		} catch (err) {
			return {
				success: false,
				error: err.message
			};
		}
	});
	ipcMain.handle("kbms:get-status", async () => {
		return kbmsClient.getStatus();
	});
	ipcMain.handle("kbms:disconnect", async () => {
		kbmsClient.disconnect();
		return { success: true };
	});
	ipcMain.handle("kbms:save-file", async (_e, content, currentPath, isNewFile = false) => {
		if (!win) return { success: false };
		let targetPath = currentPath;
		const isAbsolutePath = targetPath && path.isAbsolute(targetPath);
		if (isNewFile || !isAbsolutePath) {
			const { canceled, filePath } = await dialog.showSaveDialog(win, {
				title: isNewFile ? "Save As" : "Save KBQL Query",
				defaultPath: targetPath || "Query.kbql",
				filters: [{
					name: "KBMS Query",
					extensions: [
						"kbql",
						"sql",
						"txt"
					]
				}]
			});
			if (canceled || !filePath) return {
				success: false,
				canceled: true
			};
			targetPath = filePath;
		}
		try {
			if (!targetPath) return {
				success: false,
				error: "No target path"
			};
			fs.writeFileSync(targetPath, content, "utf8");
			return {
				success: true,
				filePath: targetPath
			};
		} catch (err) {
			return {
				success: false,
				error: err.message
			};
		}
	});
	ipcMain.handle("kbms:open-file", async () => {
		if (!win) return { success: false };
		const { canceled, filePaths } = await dialog.showOpenDialog(win, {
			title: "Open KBQL Query",
			filters: [{
				name: "KBMS Query",
				extensions: [
					"kbql",
					"sql",
					"txt"
				]
			}],
			properties: ["openFile"]
		});
		if (canceled || filePaths.length === 0) return {
			success: false,
			canceled: true
		};
		try {
			const content = fs.readFileSync(filePaths[0], "utf8");
			return {
				success: true,
				filePath: filePaths[0],
				content
			};
		} catch (err) {
			return {
				success: false,
				error: err.message
			};
		}
	});
	if (process.env.VITE_DEV_SERVER_URL) win.loadURL(process.env.VITE_DEV_SERVER_URL);
	else win.loadFile(path.join(process.env.DIST, "index.html"));
	let hasUnsavedChanges = false;
	ipcMain.on("kbms:set-unsaved-status", (_, status) => {
		hasUnsavedChanges = status;
	});
	ipcMain.on("kbms:force-quit", () => {
		hasUnsavedChanges = false;
		if (win) win.close();
	});
	win.on("close", (e) => {
		if (hasUnsavedChanges && win) {
			e.preventDefault();
			win.webContents.send("kbms:app-close-request");
		}
	});
}
app.on("window-all-closed", () => {
	if (process.platform !== "darwin") app.quit();
});
app.whenReady().then(createWindow);
//#endregion
