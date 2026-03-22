import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('kbmsApi', {
  execute: (query: string) => ipcRenderer.invoke('kbms:execute', query),
  connect: (host: string, port: number, user: string, pass: string) => ipcRenderer.invoke('kbms:connect', host, port, user, pass),
  disconnect: () => ipcRenderer.invoke('kbms:disconnect'),
  getStatus: () => ipcRenderer.invoke('kbms:get-status'),
  saveFile: (content: string, currentPath?: string, isNewFile: boolean = true) => ipcRenderer.invoke('kbms:save-file', content, currentPath, isNewFile),
  openFile: () => ipcRenderer.invoke('kbms:open-file'),
  onStatusChange: (callback: (status: string) => void) => {
    ipcRenderer.on('kbms-status', (_event, status) => callback(status));
  },
  onDataStream: (callback: (data: any) => void) => {
    ipcRenderer.on('kbms-stream', (_event, data) => callback(data));
  },
  setUnsavedStatus: (status: boolean) => ipcRenderer.send('kbms:set-unsaved-status', status),
  onAppCloseRequested: (callback: () => void) => {
    ipcRenderer.on('kbms:app-close-request', () => callback());
  },
  forceQuit: () => ipcRenderer.send('kbms:force-quit')
});
