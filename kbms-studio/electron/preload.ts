import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('kbmsApi', {
  execute: (query: string, options: any = {}) => {
    // Keep backward compatibility for old calls if possible, but store.ts is updated
    return ipcRenderer.invoke('kbms:execute', query, options);
  },
  connect: (host: string, port: number, user: string, pass: string) => ipcRenderer.invoke('kbms:connect', host, port, user, pass),
  disconnect: () => ipcRenderer.invoke('kbms:disconnect'),
  getStatus: () => ipcRenderer.invoke('kbms:get-status'),
  getStats: (requestId?: string) => ipcRenderer.invoke('kbms:get-stats', requestId),
  getSessions: (requestId?: string) => ipcRenderer.invoke('kbms:get-sessions', requestId),
  subscribeLogs: () => ipcRenderer.send('kbms:subscribe-logs'),
  saveFile: (content: string, currentPath?: string, isNewFile: boolean = true) => ipcRenderer.invoke('kbms:save-file', content, currentPath, isNewFile),
  openFile: () => ipcRenderer.invoke('kbms:open-file'),
  onStatusChange: (callback: (status: string) => void) => {
    const listener = (_event: any, status: string) => callback(status);
    ipcRenderer.on('kbms-status', listener);
    return () => ipcRenderer.removeListener('kbms-status', listener);
  },
  onDataStream: (callback: (data: any) => void) => {
    const listener = (_event: any, data: any) => callback(data);
    ipcRenderer.on('kbms-stream', listener);
    return () => ipcRenderer.removeListener('kbms-stream', listener);
  },
  setUnsavedStatus: (status: boolean) => ipcRenderer.send('kbms:set-unsaved-status', status),
  onAppCloseRequested: (callback: () => void) => {
    ipcRenderer.on('kbms:app-close-request', () => callback());
  },
  forceQuit: () => ipcRenderer.send('kbms:force-quit')
});
