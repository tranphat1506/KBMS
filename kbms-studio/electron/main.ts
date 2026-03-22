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

  ipcMain.handle('kbms:save-file', async (_e, content: string, currentPath?: string, isNewFile: boolean = true) => {
     if (!win) return { success: false };
     
     let targetPath = currentPath;
     
     if (isNewFile || !targetPath) {
         const { canceled, filePath } = await dialog.showSaveDialog(win, {
            title: 'Save KBQL Query',
            defaultPath: targetPath || 'Query.kbql',
            filters: [{ name: 'KBMS Query', extensions: ['kbql', 'sql', 'txt'] }]
         });
         
         if (canceled || !filePath) return { success: false, canceled: true };
         targetPath = filePath;
     }
     
     try {
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
}

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.whenReady().then(createWindow);
