import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('thingentApi', {
  execute: (query: string, options: any = {}) => {
    // Keep backward compatibility for old calls if possible, but store.ts is updated
    return ipcRenderer.invoke('thingent:execute', query, options);
  },
  connect: (host: string, port: number, user: string, pass: string) => ipcRenderer.invoke('thingent:connect', host, port, user, pass),
  disconnect: () => ipcRenderer.invoke('thingent:disconnect'),
  getStatus: () => ipcRenderer.invoke('thingent:get-status'),
  getStats: (requestId?: string) => ipcRenderer.invoke('thingent:get-stats', requestId),
  getSessions: (requestId?: string) => ipcRenderer.invoke('thingent:get-sessions', requestId),
  getLspCompletions: (code: string, line: number, column: number, kbName?: string) => ipcRenderer.invoke('thingent:get-lsp-completions', code, line, column, kbName),
  getLspDiagnostics: (code: string) => ipcRenderer.invoke('thingent:get-lsp-diagnostics', code),
  mgmtAction: (action: string, data: any = {}, requestId?: string) => ipcRenderer.invoke('thingent:mgmt-action', action, data, requestId),
  subscribeLogs: () => ipcRenderer.send('thingent:subscribe-logs'),
  saveFile: (content: string, currentPath?: string, isNewFile: boolean = true) => ipcRenderer.invoke('thingent:save-file', content, currentPath, isNewFile),
  openFile: () => ipcRenderer.invoke('thingent:open-file'),
  onStatusChange: (callback: (status: string) => void) => {
    const listener = (_event: any, status: string) => callback(status);
    ipcRenderer.on('thingent-status', listener);
    return () => ipcRenderer.removeListener('thingent-status', listener);
  },
  onDataStream: (callback: (data: any) => void) => {
    const listener = (_event: any, data: any) => callback(data);
    ipcRenderer.on('thingent-stream', listener);
    return () => ipcRenderer.removeListener('thingent-stream', listener);
  },
  setUnsavedStatus: (status: boolean) => ipcRenderer.send('thingent:set-unsaved-status', status),
  onAppCloseRequested: (callback: () => void) => {
    ipcRenderer.on('thingent:app-close-request', () => callback());
  },
  setTheme: (theme: 'light' | 'dark') => ipcRenderer.send('thingent:set-theme', theme),
  forceQuit: () => ipcRenderer.send('thingent:force-quit')
});
