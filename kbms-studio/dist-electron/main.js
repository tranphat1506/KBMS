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
	MessageType[MessageType["STATS"] = 10] = "STATS";
	MessageType[MessageType["LOGS_STREAM"] = 11] = "LOGS_STREAM";
	MessageType[MessageType["SESSIONS"] = 12] = "SESSIONS";
	MessageType[MessageType["MANAGEMENT_CMD"] = 13] = "MANAGEMENT_CMD";
	return MessageType;
}({});
var Protocol = class {
	static pack(message) {
		const contentBuffer = Buffer$1.from(message.content, "utf8");
		const sessionIdBuffer = message.sessionId ? Buffer$1.from(message.sessionId, "utf8") : Buffer$1.alloc(0);
		const requestIdBuffer = message.requestId ? Buffer$1.from(message.requestId, "utf8") : Buffer$1.alloc(0);
		const sessionIdLength = sessionIdBuffer.length;
		const requestIdLength = requestIdBuffer.length;
		const totalLength = contentBuffer.length + 2 + sessionIdLength + 2 + requestIdLength;
		const lengthBuffer = Buffer$1.alloc(4);
		lengthBuffer.writeInt32BE(totalLength, 0);
		const typeBuffer = Buffer$1.from([message.type]);
		const sessionIdLenBuffer = Buffer$1.alloc(2);
		sessionIdLenBuffer.writeUInt16BE(sessionIdLength, 0);
		const requestIdLenBuffer = Buffer$1.alloc(2);
		requestIdLenBuffer.writeUInt16BE(requestIdLength, 0);
		return Buffer$1.concat([
			lengthBuffer,
			typeBuffer,
			sessionIdLenBuffer,
			sessionIdBuffer,
			requestIdLenBuffer,
			requestIdBuffer,
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
			const requestIdOffset = offset + 7 + sessionIdLength;
			const requestIdLength = buffer.readUInt16BE(requestIdOffset);
			let requestId = void 0;
			if (requestIdLength > 0) requestId = buffer.toString("utf8", requestIdOffset + 2, requestIdOffset + 2 + requestIdLength);
			const payloadLength = length - 2 - sessionIdLength - 2 - requestIdLength;
			const contentStart = requestIdOffset + 2 + requestIdLength;
			const content = buffer.toString("utf8", contentStart, contentStart + payloadLength);
			messages.push({
				type,
				content,
				sessionId,
				requestId
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
		this.isReconnecting = false;
		this.activeRequest = null;
		this.pendingRequestsMetadata = /* @__PURE__ */ new Map();
		this.heartbeatTimer = null;
	}
	setWindow(win) {
		this.win = win;
	}
	sendToUI(channel, payload) {
		if (this.win && !this.win.isDestroyed() && this.win.webContents && !this.win.webContents.isDestroyed()) try {
			this.win.webContents.send(channel, payload);
		} catch (e) {
			console.error("(KBMS Client) Failed to send message to UI:", e);
		}
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
				this.sendToUI("kbms-status", "connected");
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
				this.sendToUI("kbms-status", "disconnected");
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
				this.sendToUI("kbms-status", "error");
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
				const requestId = "hb_" + Date.now();
				console.log("(KBMS Client) Sending Heartbeat...");
				this.pendingRequestsMetadata.set(requestId, { isBackground: true });
				this.socket.write(Protocol.pack({
					type: MessageType.QUERY,
					content: "",
					requestId,
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
	execute(query, isBackground = false, requestId, isManagement = false) {
		return new Promise((resolve, reject) => {
			if (!requestId) requestId = `${isManagement ? "mgmt" : "web"}_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
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
	getStats(requestId) {
		return new Promise((resolve, reject) => {
			if (!requestId) requestId = "stats_" + Date.now();
			this.pendingRequestsMetadata.set(requestId, { isBackground: true });
			this.requestQueue.push({
				type: MessageType.STATS,
				query: "",
				isBackground: true,
				requestId,
				resolve,
				reject
			});
			this.processQueue();
		});
	}
	getSessions(requestId) {
		return new Promise((resolve, reject) => {
			if (!requestId) requestId = "sessions_" + Date.now();
			this.pendingRequestsMetadata.set(requestId, { isBackground: true });
			this.requestQueue.push({
				type: MessageType.SESSIONS,
				query: "",
				isBackground: true,
				requestId,
				resolve,
				reject
			});
			this.processQueue();
		});
	}
	sendManagementAction(action, data = {}, requestId) {
		return new Promise((resolve, reject) => {
			if (!requestId) requestId = `mgmt_${action.toLowerCase()}_${Date.now()}`;
			this.pendingRequestsMetadata.set(requestId, { isBackground: true });
			this.requestQueue.push({
				type: MessageType.MANAGEMENT_CMD,
				query: JSON.stringify({
					action,
					...data
				}),
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
			content: "",
			sessionId: this.sessionId
		}));
	}
	async processQueue() {
		if (this.isProcessingQueue || this.requestQueue.length === 0) return;
		this.isProcessingQueue = true;
		this.activeRequest = this.requestQueue[0];
		if (!this.socket || this.socket.destroyed) {
			this.activeRequest.reject(/* @__PURE__ */ new Error("Not connected to server"));
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
	handleServerMessage(msg) {
		console.log(`(KBMS Client) Incoming message [${msg.type}]: ${msg.content.substring(0, 100)}`);
		const meta = msg.requestId ? this.pendingRequestsMetadata.get(msg.requestId) : null;
		const isBackground = meta ? meta.isBackground : this.activeRequest ? !!this.activeRequest.isBackground : false;
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
			const { text, markers } = this.extractErrorInfo(msg.content);
			if (markers.length > 0) {
				if (!this.queryResultData.editorMarkers) this.queryResultData.editorMarkers = [];
				this.queryResultData.editorMarkers.push(...markers);
			}
			this.queryResultData.messages.push({
				type: "error",
				text
			});
			this.sendToUI("kbms-stream", {
				type: "error",
				text,
				markers,
				requestId: msg.requestId,
				isBackground
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
			} else {
				this.queryResultData = {
					ConceptName: metadata.ConceptName || metadata.conceptName || "",
					headers: metadata.Columns || [],
					rows: [],
					messages: []
				};
				this.sendToUI("kbms-stream", {
					type: "metadata",
					metadata,
					isBackground,
					requestId: msg.requestId
				});
			}
		} catch (e) {
			console.error("(KBMS Client) Failed to parse metadata", e);
		}
		else if (msg.type === MessageType.ROW) try {
			const rowData = JSON.parse(msg.content);
			if (!Array.isArray(rowData) && rowData.error) {
				if (!this.queryResultData) this.queryResultData = {
					headers: ["Error"],
					rows: [],
					messages: []
				};
				this.queryResultData.messages.push({
					type: "error",
					text: rowData.error
				});
			} else {
				if (this.queryResultData) if (Array.isArray(rowData)) this.queryResultData.rows.push(...rowData);
				else this.queryResultData.rows.push(rowData);
				this.sendToUI("kbms-stream", {
					type: "row",
					row: rowData,
					isBackground,
					requestId: msg.requestId
				});
			}
		} catch (e) {
			console.error("(KBMS Client) Failed to parse row", e);
		}
		else if (msg.type === MessageType.RESULT) {
			const isMgmt = this.activeRequest?.type === MessageType.MANAGEMENT_CMD;
			if (isMgmt && msg.content.trim().startsWith("{") || isMgmt && msg.content.trim().startsWith("[")) try {
				this.queryResultData = JSON.parse(msg.content);
			} catch {
				this.queryResultData = {
					headers: ["Result"],
					rows: [],
					messages: [{
						type: "info",
						text: msg.content
					}]
				};
			}
			else {
				if (!this.queryResultData) this.queryResultData = {
					headers: ["Result"],
					rows: [],
					messages: []
				};
				const { text, markers, isError } = this.extractErrorInfo(msg.content);
				if (markers.length > 0) {
					if (!this.queryResultData.editorMarkers) this.queryResultData.editorMarkers = [];
					this.queryResultData.editorMarkers.push(...markers);
				}
				this.queryResultData.messages.push({
					type: isError ? "error" : "info",
					text
				});
				this.sendToUI("kbms-stream", {
					type: "result",
					text,
					markers,
					isBackground,
					requestId: msg.requestId
				});
			}
		} else if (msg.type === MessageType.LOGS_STREAM) try {
			const logData = JSON.parse(msg.content);
			this.sendToUI("kbms-stream", {
				type: "log-signal",
				data: logData
			});
		} catch (e) {
			console.error("(KBMS Client) Failed to parse log signal", e);
		}
		else if (msg.type === MessageType.FETCH_DONE) {
			try {
				const summary = JSON.parse(msg.content);
				if (!this.queryResultData) this.queryResultData = {
					headers: [],
					rows: [],
					messages: []
				};
				const text = `Done: ${summary.statementsExecuted || 0} stmts in ${summary.executionTime}s`;
				this.queryResultData.messages.push({
					type: "info",
					text
				});
				this.sendToUI("kbms-stream", {
					type: "result",
					text,
					isBackground,
					requestId: msg.requestId
				});
			} catch {}
			const resolver = this.currentQueryResolver;
			const result = this.queryResultData;
			this.currentQueryResolver = null;
			this.currentQueryRejecter = null;
			this.queryResultData = null;
			if (resolver) resolver(result);
			this.requestQueue.shift();
			if (msg.requestId) this.pendingRequestsMetadata.delete(msg.requestId);
			this.isProcessingQueue = false;
			this.processQueue();
		}
	}
	extractErrorInfo(content) {
		let text = content;
		let markers = [];
		let isError = false;
		try {
			const json = JSON.parse(content);
			const msgField = json.Message || json.message || json.Error || json.error;
			const typeField = json.Type || json.type || "";
			const line = json.Line ?? json.line;
			const col = json.Column ?? json.column;
			isError = typeField.toLowerCase().includes("error") || !!(json.Error || json.error);
			if (msgField) {
				const location = line != null && col != null && line > 0 ? ` at line ${line}, col ${col}` : "";
				text = typeField ? `[${typeField}] ${msgField}${location}` : `${msgField}${location}`;
				if (line != null && col != null && line > 0) markers.push({
					startLineNumber: line,
					startColumn: col,
					endLineNumber: line,
					endColumn: col + 1,
					message: msgField,
					severity: 8
				});
			}
		} catch {
			isError = content.toLowerCase().includes("error");
		}
		return {
			text,
			markers,
			isError
		};
	}
	disconnect() {
		this.stopHeartbeat();
		if (this.socket) {
			this.socket.destroy();
			this.socket = null;
			this.sessionId = "";
			this.sendToUI("kbms-status", "disconnected");
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
var splash;
app.name = "KBMS Studio";
if (process.platform === "darwin") app.setName("KBMS Studio");
function createSplashScreen() {
	splash = new BrowserWindow({
		width: 500,
		height: 400,
		transparent: true,
		frame: false,
		alwaysOnTop: true,
		center: true,
		resizable: false,
		show: false,
		backgroundColor: "#ffffff",
		icon: path.join(process.env.VITE_PUBLIC, "icon.png"),
		webPreferences: {
			nodeIntegration: false,
			contextIsolation: true
		}
	});
	splash.loadFile(path.join(process.env.VITE_PUBLIC, "splash.html"));
	splash.once("ready-to-show", () => {
		splash?.show();
	});
	splash.on("closed", () => splash = null);
}
function createWindow() {
	win = new BrowserWindow({
		width: 1280,
		height: 800,
		show: false,
		titleBarStyle: "hiddenInset",
		backgroundColor: "#ffffff",
		title: "KBMS Studio",
		icon: path.join(process.env.VITE_PUBLIC, "icon.png"),
		webPreferences: { preload: path.join(__dirname, "preload.mjs") }
	});
	win.once("ready-to-show", () => {
		if (splash) setTimeout(() => {
			splash?.close();
			win?.show();
			win?.focus();
		}, 500);
		else win?.show();
	});
	kbmsClient.setWindow(win);
	ipcMain.handle("kbms:execute", async (_, query, options = {}) => {
		try {
			const isBackground = !!options.isBackground;
			const requestId = options.requestId;
			const isManagement = !!options.isManagement;
			console.log("Execute called from UI:", query, isBackground ? "(Background)" : "", requestId ? `[Req: ${requestId}]` : "", isManagement ? "(Management)" : "");
			return await kbmsClient.execute(query, isBackground, requestId, isManagement);
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
	ipcMain.handle("kbms:get-stats", async (_, requestId) => {
		return kbmsClient.getStats(requestId);
	});
	ipcMain.handle("kbms:get-sessions", async (_, requestId) => {
		return kbmsClient.getSessions(requestId);
	});
	ipcMain.handle("kbms:mgmt-action", async (_, action, data = {}, requestId) => {
		try {
			return await kbmsClient.sendManagementAction(action, data, requestId);
		} catch (err) {
			return {
				success: false,
				error: err.message
			};
		}
	});
	ipcMain.on("kbms:subscribe-logs", () => {
		kbmsClient.subscribeLogs();
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
app.whenReady().then(() => {
	if (process.platform === "darwin" && process.env.VITE_PUBLIC && app.dock) {
		const iconPath = path.join(process.env.VITE_PUBLIC, "icon.png");
		if (fs.existsSync(iconPath)) app.dock.setIcon(iconPath);
	}
	createSplashScreen();
	createWindow();
});
//#endregion
