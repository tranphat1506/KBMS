import { app, BrowserWindow, ipcMain, dialog } from 'electron';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { kbmsClient } from './kbms-client';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Cấu hình đường dẫn
process.env.DIST = path.join(__dirname, '../dist');
process.env.VITE_PUBLIC = app.isPackaged ? process.env.DIST : path.join(process.env.DIST, '../public');

let win: BrowserWindow | null;

function createWindow() {
  win = new BrowserWindow({
    icon: path.join((process.env.VITE_PUBLIC as string), 'electron-vite.svg'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.mjs'), // vite-plugin-electron builds mjs
    },
    width: 1280,
    height: 800,
    titleBarStyle: 'hiddenInset',
    backgroundColor: '#ffffff'
  });

  // Call API Backend Setup
  kbmsClient.setWindow(win);

  ipcMain.handle('kbms:execute', async (_, query) => {
     try {
       console.log('Execute called from UI:', query);
       const result = await kbmsClient.execute(query);
       console.log('[DEBUG] Execute result returned to UI:', JSON.stringify(result, null, 2));
       return result;
     } catch (err: any) {
       return { success: false, messages: [err.message], rows: [], headers: [] };
     }
  });

  ipcMain.handle('kbms:connect', async (_, host, port, user, pass) => {
     try {
       const success = await kbmsClient.connect(host, port, user, pass);
       return { success };
     } catch (err: any) {
       return { success: false, error: err.message };
     }
  });

   ipcMain.handle('kbms:get-status', async () => {
      return kbmsClient.getStatus();
   });

  ipcMain.handle('kbms:disconnect', async () => {
     kbmsClient.disconnect();
     return { success: true };
  });

  ipcMain.handle('kbms:save-file', async (_e, content: string, currentPath?: string, isNewFile: boolean = false) => {
     if (!win) return { success: false };
     
     let targetPath = currentPath;
     const isAbsolutePath = targetPath && path.isAbsolute(targetPath);
     
     // Only show dialog if it's explicitly a new file, or we don't have an absolute path yet
     if (isNewFile || !isAbsolutePath) {
         const { canceled, filePath } = await dialog.showSaveDialog(win, {
            title: isNewFile ? 'Save As' : 'Save KBQL Query',
            defaultPath: targetPath || 'Query.kbql',
            filters: [{ name: 'KBMS Query', extensions: ['kbql', 'sql', 'txt'] }]
         });
         
         if (canceled || !filePath) return { success: false, canceled: true };
         targetPath = filePath;
     }
     
     try {
        if (!targetPath) return { success: false, error: 'No target path' };
        fs.writeFileSync(targetPath, content, 'utf8');
        return { success: true, filePath: targetPath };
     } catch (err: any) {
        return { success: false, error: err.message };
     }
  });

  ipcMain.handle('kbms:open-file', async () => {
     if (!win) return { success: false };
     const { canceled, filePaths } = await dialog.showOpenDialog(win, {
        title: 'Open KBQL Query',
        filters: [{ name: 'KBMS Query', extensions: ['kbql', 'sql', 'txt'] }],
        properties: ['openFile']
     });
     
     if (canceled || filePaths.length === 0) return { success: false, canceled: true };
     
     try {
        const content = fs.readFileSync(filePaths[0], 'utf8');
        return { success: true, filePath: filePaths[0], content };
     } catch (err: any) {
        return { success: false, error: err.message };
     }
  });

  // Load UI
  if (process.env.VITE_DEV_SERVER_URL) {
    win.loadURL(process.env.VITE_DEV_SERVER_URL);
  } else {
    win.loadFile(path.join((process.env.DIST as string), 'index.html'));
  }

  // --- Unsaved Changes Protection ---
  let hasUnsavedChanges = false;
  ipcMain.on('kbms:set-unsaved-status', (_, status: boolean) => {
    hasUnsavedChanges = status;
  });

  ipcMain.on('kbms:force-quit', () => {
    hasUnsavedChanges = false; // Bypass the check
    if (win) win.close();
  });

  win.on('close', (e) => {
    if (hasUnsavedChanges && win) {
      e.preventDefault();
      // Notify renderer to show custom confirmation dialog
      win.webContents.send('kbms:app-close-request');
    }
  });
}

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.whenReady().then(createWindow);
