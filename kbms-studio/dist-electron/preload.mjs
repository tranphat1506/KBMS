let electron = require("electron");
//#region electron/preload.ts
electron.contextBridge.exposeInMainWorld("kbmsApi", {
	execute: (query, isBackground = false, requestId) => {
		console.log(`[Preload] Execute called with isBackground=${isBackground}, requestId=${requestId}`);
		return electron.ipcRenderer.invoke("kbms:execute", query, isBackground, requestId);
	},
	connect: (host, port, user, pass) => electron.ipcRenderer.invoke("kbms:connect", host, port, user, pass),
	disconnect: () => electron.ipcRenderer.invoke("kbms:disconnect"),
	getStatus: () => electron.ipcRenderer.invoke("kbms:get-status"),
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
