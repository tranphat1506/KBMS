import { BrowserWindow as e, app as t, dialog as n, ipcMain as r } from "electron";
import i from "node:fs";
import a from "node:path";
import { fileURLToPath as o } from "node:url";
import s from "node:net";
import { Buffer as c } from "node:buffer";
//#region electron/kbms-protocol.ts
var l = /* @__PURE__ */ function(e) {
	return e[e.LOGIN = 1] = "LOGIN", e[e.QUERY = 2] = "QUERY", e[e.RESULT = 3] = "RESULT", e[e.ERROR = 4] = "ERROR", e[e.LOGOUT = 5] = "LOGOUT", e[e.METADATA = 6] = "METADATA", e[e.ROW = 7] = "ROW", e[e.FETCH_DONE = 8] = "FETCH_DONE", e;
}({}), u = class {
	static pack(e) {
		let t = c.from(e.content, "utf8"), n = e.sessionId ? c.from(e.sessionId, "utf8") : c.alloc(0), r = e.requestId ? c.from(e.requestId, "utf8") : c.alloc(0), i = n.length, a = r.length, o = t.length + 2 + i + 2 + a, s = c.alloc(4);
		s.writeInt32BE(o, 0);
		let l = c.from([e.type]), u = c.alloc(2);
		u.writeUInt16BE(i, 0);
		let d = c.alloc(2);
		return d.writeUInt16BE(a, 0), c.concat([
			s,
			l,
			u,
			n,
			d,
			r,
			t
		]);
	}
	static unpack(e) {
		let t = [], n = 0;
		for (; n + 4 <= e.length;) {
			let r = e.readInt32BE(n), i = 5 + r;
			if (n + i > e.length) break;
			let a = e.readUInt8(n + 4), o = e.readUInt16BE(n + 5), s;
			o > 0 && (s = e.toString("utf8", n + 7, n + 7 + o));
			let c = n + 7 + o, l = e.readUInt16BE(c), u;
			l > 0 && (u = e.toString("utf8", c + 2, c + 2 + l));
			let d = r - 2 - o - 2 - l, f = c + 2 + l, p = e.toString("utf8", f, f + d);
			t.push({
				type: a,
				content: p,
				sessionId: s,
				requestId: u
			}), n += i;
		}
		return {
			messages: t,
			remaining: e.subarray(n)
		};
	}
}, d = new class {
	constructor() {
		this.socket = null, this.buffer = Buffer.alloc(0), this.win = null, this.sessionId = "", this.connectResolver = null, this.currentQueryResolver = null, this.currentQueryRejecter = null, this.queryResultData = null, this.requestQueue = [], this.isProcessingQueue = !1, this.isReconnecting = !1, this.activeRequest = null, this.pendingRequestsMetadata = /* @__PURE__ */ new Map(), this.heartbeatTimer = null;
	}
	setWindow(e) {
		this.win = e;
	}
	sendToUI(e, t) {
		if (this.win && !this.win.isDestroyed() && this.win.webContents && !this.win.webContents.isDestroyed()) try {
			this.win.webContents.send(e, t);
		} catch (e) {
			console.error("(KBMS Client) Failed to send message to UI:", e);
		}
	}
	connect(e = "127.0.0.1", t = 3307, n = "admin", r = "admin") {
		return new Promise((i) => {
			this.disconnect(), this.socket = new s.Socket(), this.socket.setKeepAlive(!0, 3e4), this.socket.setTimeout(6e5), this.connectResolver = i, this.socket.connect(t, e, () => {
				console.log(`(KBMS Client) Socket connected to ${e}:${t}`), this.sendToUI("kbms-status", "connected");
				let i = `LOGIN ${n} ${r}`;
				this.socket?.write(u.pack({
					type: l.LOGIN,
					content: i
				})), this.startHeartbeat();
			}), this.socket.on("data", (e) => {
				try {
					this.buffer = Buffer.concat([this.buffer, e]);
					let { messages: t, remaining: n } = u.unpack(this.buffer);
					this.buffer = n;
					for (let e of t) try {
						this.handleServerMessage(e);
					} catch (e) {
						console.error("(KBMS Client) Error handling message:", e);
					}
				} catch (e) {
					console.error("(KBMS Client) Data processing error:", e);
				}
			}), this.socket.on("close", () => {
				console.log("(KBMS Client) Connection closed"), this.cleanup("Connection closed"), this.sendToUI("kbms-status", "disconnected"), this.socket = null;
			}), this.socket.on("timeout", () => {
				console.warn("(KBMS Client) Socket Timeout"), this.cleanup("Socket Timeout"), this.socket && this.socket.destroy();
			}), this.socket.on("error", (e) => {
				console.error("(KBMS Client) Socket error:", e.message), this.cleanup(e.message), this.sendToUI("kbms-status", "error"), this.connectResolver &&= (this.connectResolver(!1), null);
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
	cleanup(e) {
		for (this.currentQueryRejecter && this.currentQueryRejecter(Error(e)), this.currentQueryResolver = null, this.currentQueryRejecter = null, this.queryResultData = null, this.sessionId = ""; this.requestQueue.length > 0;) this.requestQueue.shift()?.reject(Error(e));
		this.isProcessingQueue = !1, this.stopHeartbeat();
	}
	startHeartbeat() {
		this.stopHeartbeat(), this.heartbeatTimer = setInterval(() => {
			if (this.socket && !this.socket.destroyed && this.sessionId) {
				let e = "hb_" + Date.now();
				console.log("(KBMS Client) Sending Heartbeat..."), this.pendingRequestsMetadata.set(e, { isBackground: !0 }), this.socket.write(u.pack({
					type: l.QUERY,
					content: "",
					requestId: e,
					sessionId: this.sessionId
				}));
			}
		}, 45e3);
	}
	stopHeartbeat() {
		this.heartbeatTimer &&= (clearInterval(this.heartbeatTimer), null);
	}
	execute(e, t = !1, n) {
		return new Promise((r, i) => {
			n ||= `web_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`, this.pendingRequestsMetadata.set(n, { isBackground: t }), this.requestQueue.push({
				query: e,
				isBackground: t,
				requestId: n,
				resolve: r,
				reject: i
			}), this.processQueue();
		});
	}
	async processQueue() {
		if (this.isProcessingQueue || this.requestQueue.length === 0) return;
		if (this.isProcessingQueue = !0, this.activeRequest = this.requestQueue[0], !this.socket || this.socket.destroyed) {
			this.activeRequest.reject(/* @__PURE__ */ Error("Not connected to server")), this.requestQueue.shift(), this.isProcessingQueue = !1, this.activeRequest = null, this.processQueue();
			return;
		}
		this.currentQueryResolver = this.activeRequest.resolve, this.currentQueryRejecter = this.activeRequest.reject, this.queryResultData = null;
		let e = u.pack({
			type: l.QUERY,
			content: this.activeRequest.query,
			sessionId: this.sessionId,
			requestId: this.activeRequest.requestId
		});
		this.socket.write(e);
	}
	handleServerMessage(e) {
		console.log(`(KBMS Client) Incoming message [${e.type}]: ${e.content.substring(0, 100)}`);
		let t = e.requestId ? this.pendingRequestsMetadata.get(e.requestId) : null, n = t ? t.isBackground : this.activeRequest ? !!this.activeRequest.isBackground : !1;
		if (e.type === l.RESULT && e.content.startsWith("LOGIN_SUCCESS")) {
			console.log("(KBMS Client) Login SUCCESS");
			let t = e.content.split(":");
			t.length > 3 && (this.sessionId = t[3]), this.connectResolver &&= (this.connectResolver(!0), null);
			return;
		}
		if (e.type === l.ERROR) {
			if (console.error(`(KBMS Client) Server Error: ${e.content}`), this.connectResolver) {
				this.connectResolver(!1), this.connectResolver = null;
				return;
			}
			this.queryResultData ||= {
				headers: ["Error"],
				rows: [],
				messages: []
			};
			let { text: t, markers: r } = this.extractErrorInfo(e.content);
			r.length > 0 && (this.queryResultData.editorMarkers || (this.queryResultData.editorMarkers = []), this.queryResultData.editorMarkers.push(...r)), this.queryResultData.messages.push({
				type: "error",
				text: t
			}), this.sendToUI("kbms-stream", {
				type: "error",
				text: t,
				markers: r,
				requestId: e.requestId,
				isBackground: n
			});
		} else if (e.type === l.METADATA) try {
			let t = JSON.parse(e.content);
			t.error ? (this.queryResultData ||= {
				headers: ["Error"],
				rows: [],
				messages: []
			}, this.queryResultData.messages.push({
				type: "error",
				text: t.error
			})) : (this.queryResultData = {
				ConceptName: t.ConceptName || t.conceptName || "",
				headers: t.Columns || [],
				rows: [],
				messages: []
			}, this.sendToUI("kbms-stream", {
				type: "metadata",
				metadata: t,
				isBackground: n,
				requestId: e.requestId
			}));
		} catch (e) {
			console.error("(KBMS Client) Failed to parse metadata", e);
		}
		else if (e.type === l.ROW) try {
			let t = JSON.parse(e.content);
			!Array.isArray(t) && t.error ? (this.queryResultData ||= {
				headers: ["Error"],
				rows: [],
				messages: []
			}, this.queryResultData.messages.push({
				type: "error",
				text: t.error
			})) : (this.queryResultData && (Array.isArray(t) ? this.queryResultData.rows.push(...t) : this.queryResultData.rows.push(t)), this.sendToUI("kbms-stream", {
				type: "row",
				row: t,
				isBackground: n,
				requestId: e.requestId
			}));
		} catch (e) {
			console.error("(KBMS Client) Failed to parse row", e);
		}
		else if (e.type === l.RESULT) {
			this.queryResultData ||= {
				headers: ["Result"],
				rows: [],
				messages: []
			};
			let { text: t, markers: r, isError: i } = this.extractErrorInfo(e.content);
			r.length > 0 && (this.queryResultData.editorMarkers || (this.queryResultData.editorMarkers = []), this.queryResultData.editorMarkers.push(...r)), this.queryResultData.messages.push({
				type: i ? "error" : "info",
				text: t
			}), this.sendToUI("kbms-stream", {
				type: "result",
				text: t,
				markers: r,
				isBackground: n,
				requestId: e.requestId
			});
		} else if (e.type === l.FETCH_DONE) {
			try {
				let t = JSON.parse(e.content);
				this.queryResultData ||= {
					headers: [],
					rows: [],
					messages: []
				};
				let r = `Done: ${t.statementsExecuted || 0} stmts in ${t.executionTime}s`;
				this.queryResultData.messages.push({
					type: "info",
					text: r
				}), this.sendToUI("kbms-stream", {
					type: "result",
					text: r,
					isBackground: n,
					requestId: e.requestId
				});
			} catch {}
			let t = this.currentQueryResolver, r = this.queryResultData;
			this.currentQueryResolver = null, this.currentQueryRejecter = null, this.queryResultData = null, t && t(r), this.requestQueue.shift(), e.requestId && this.pendingRequestsMetadata.delete(e.requestId), this.isProcessingQueue = !1, this.processQueue();
		}
	}
	extractErrorInfo(e) {
		let t = e, n = [], r = !1;
		try {
			let i = JSON.parse(e), a = i.Message || i.message || i.Error || i.error, o = i.Type || i.type || "", s = i.Line ?? i.line, c = i.Column ?? i.column;
			if (r = o.toLowerCase().includes("error") || !!(i.Error || i.error), a) {
				let e = s != null && c != null && s > 0 ? ` at line ${s}, col ${c}` : "";
				t = o ? `[${o}] ${a}${e}` : `${a}${e}`, s != null && c != null && s > 0 && n.push({
					startLineNumber: s,
					startColumn: c,
					endLineNumber: s,
					endColumn: c + 1,
					message: a,
					severity: 8
				});
			}
		} catch {
			r = e.toLowerCase().includes("error");
		}
		return {
			text: t,
			markers: n,
			isError: r
		};
	}
	disconnect() {
		this.stopHeartbeat(), this.socket && (this.socket.destroy(), this.socket = null, this.sessionId = "", this.win?.webContents.send("kbms-status", "disconnected"));
	}
}(), f = a.dirname(o(import.meta.url));
process.env.DIST = a.join(f, "../dist"), process.env.VITE_PUBLIC = t.isPackaged ? process.env.DIST : a.join(process.env.DIST, "../public");
var p;
function m() {
	p = new e({
		icon: a.join(process.env.VITE_PUBLIC, "electron-vite.svg"),
		webPreferences: { preload: a.join(f, "preload.mjs") },
		width: 1280,
		height: 800,
		titleBarStyle: "hiddenInset",
		backgroundColor: "#ffffff"
	}), d.setWindow(p), r.handle("kbms:execute", async (e, t, n = !1, r) => {
		try {
			console.log("Execute called from UI:", t, n ? "(Background)" : "", r ? `[Req: ${r}]` : "");
			let e = await d.execute(t, n, r);
			return console.log("[DEBUG] Execute result returned to UI:", JSON.stringify(e, null, 2)), e;
		} catch (e) {
			return {
				success: !1,
				messages: [e.message],
				rows: [],
				headers: []
			};
		}
	}), r.handle("kbms:connect", async (e, t, n, r, i) => {
		try {
			return { success: await d.connect(t, n, r, i) };
		} catch (e) {
			return {
				success: !1,
				error: e.message
			};
		}
	}), r.handle("kbms:get-status", async () => d.getStatus()), r.handle("kbms:disconnect", async () => (d.disconnect(), { success: !0 })), r.handle("kbms:save-file", async (e, t, r, o = !1) => {
		if (!p) return { success: !1 };
		let s = r, c = s && a.isAbsolute(s);
		if (o || !c) {
			let { canceled: e, filePath: t } = await n.showSaveDialog(p, {
				title: o ? "Save As" : "Save KBQL Query",
				defaultPath: s || "Query.kbql",
				filters: [{
					name: "KBMS Query",
					extensions: [
						"kbql",
						"sql",
						"txt"
					]
				}]
			});
			if (e || !t) return {
				success: !1,
				canceled: !0
			};
			s = t;
		}
		try {
			return s ? (i.writeFileSync(s, t, "utf8"), {
				success: !0,
				filePath: s
			}) : {
				success: !1,
				error: "No target path"
			};
		} catch (e) {
			return {
				success: !1,
				error: e.message
			};
		}
	}), r.handle("kbms:open-file", async () => {
		if (!p) return { success: !1 };
		let { canceled: e, filePaths: t } = await n.showOpenDialog(p, {
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
		if (e || t.length === 0) return {
			success: !1,
			canceled: !0
		};
		try {
			let e = i.readFileSync(t[0], "utf8");
			return {
				success: !0,
				filePath: t[0],
				content: e
			};
		} catch (e) {
			return {
				success: !1,
				error: e.message
			};
		}
	}), process.env.VITE_DEV_SERVER_URL ? p.loadURL(process.env.VITE_DEV_SERVER_URL) : p.loadFile(a.join(process.env.DIST, "index.html"));
	let t = !1;
	r.on("kbms:set-unsaved-status", (e, n) => {
		t = n;
	}), r.on("kbms:force-quit", () => {
		t = !1, p && p.close();
	}), p.on("close", (e) => {
		t && p && (e.preventDefault(), p.webContents.send("kbms:app-close-request"));
	});
}
t.on("window-all-closed", () => {
	process.platform !== "darwin" && t.quit();
}), t.whenReady().then(m);
//#endregion
