let electron = require("electron");
//#region electron/preload.ts
electron.contextBridge.exposeInMainWorld("kbmsApi", {
	execute: (query) => electron.ipcRenderer.invoke("kbms:execute", query),
	connect: (host, port, user, pass) => electron.ipcRenderer.invoke("kbms:connect", host, port, user, pass),
	disconnect: () => electron.ipcRenderer.invoke("kbms:disconnect"),
	getStatus: () => electron.ipcRenderer.invoke("kbms:get-status"),
	saveFile: (content, currentPath, isNewFile = true) => electron.ipcRenderer.invoke("kbms:save-file", content, currentPath, isNewFile),
	openFile: () => electron.ipcRenderer.invoke("kbms:open-file"),
	onStatusChange: (callback) => {
		electron.ipcRenderer.on("kbms-status", (_event, status) => callback(status));
	},
	onDataStream: (callback) => {
		electron.ipcRenderer.on("kbms-stream", (_event, data) => callback(data));
	},
	setUnsavedStatus: (status) => electron.ipcRenderer.send("kbms:set-unsaved-status", status),
	onAppCloseRequested: (callback) => {
		electron.ipcRenderer.on("kbms:app-close-request", () => callback());
	},
	forceQuit: () => electron.ipcRenderer.send("kbms:force-quit")
});
//#endregion
