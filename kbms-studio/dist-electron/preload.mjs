let electron = require("electron");
//#region electron/preload.ts
electron.contextBridge.exposeInMainWorld("kbmsApi", {
	execute: (query, options = {}) => {
		return electron.ipcRenderer.invoke("kbms:execute", query, options);
	},
	connect: (host, port, user, pass) => electron.ipcRenderer.invoke("kbms:connect", host, port, user, pass),
	disconnect: () => electron.ipcRenderer.invoke("kbms:disconnect"),
	getStatus: () => electron.ipcRenderer.invoke("kbms:get-status"),
	getStats: (requestId) => electron.ipcRenderer.invoke("kbms:get-stats", requestId),
	getSessions: (requestId) => electron.ipcRenderer.invoke("kbms:get-sessions", requestId),
	subscribeLogs: () => electron.ipcRenderer.send("kbms:subscribe-logs"),
	saveFile: (content, currentPath, isNewFile = true) => electron.ipcRenderer.invoke("kbms:save-file", content, currentPath, isNewFile),
	openFile: () => electron.ipcRenderer.invoke("kbms:open-file"),
	onStatusChange: (callback) => {
		const listener = (_event, status) => callback(status);
		electron.ipcRenderer.on("kbms-status", listener);
		return () => electron.ipcRenderer.removeListener("kbms-status", listener);
	},
	onDataStream: (callback) => {
		const listener = (_event, data) => callback(data);
		electron.ipcRenderer.on("kbms-stream", listener);
		return () => electron.ipcRenderer.removeListener("kbms-stream", listener);
	},
	setUnsavedStatus: (status) => electron.ipcRenderer.send("kbms:set-unsaved-status", status),
	onAppCloseRequested: (callback) => {
		electron.ipcRenderer.on("kbms:app-close-request", () => callback());
	},
	forceQuit: () => electron.ipcRenderer.send("kbms:force-quit")
});
//#endregion
