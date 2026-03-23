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
let splash: BrowserWindow | null;

app.name = 'KBMS Studio';

// Ensure the app name is correct in development (macOS)
if (process.platform === 'darwin') {
  app.setName('KBMS Studio');
}

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
    backgroundColor: '#ffffff',
    icon: path.join(process.env.VITE_PUBLIC!, 'icon.png'),
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true
    }
  });

  splash.loadFile(path.join(process.env.VITE_PUBLIC!, 'splash.html'));
  splash.once('ready-to-show', () => {
    splash?.show();
  });
  splash.on('closed', () => (splash = null));
}

function createWindow() {
  win = new BrowserWindow({
    width: 1280,
    height: 800,
    show: false, // Don't show immediately
    titleBarStyle: 'hiddenInset',
    backgroundColor: '#ffffff',
    title: 'KBMS Studio',
    icon: path.join(process.env.VITE_PUBLIC!, 'icon.png'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.mjs'), // vite-plugin-electron builds mjs
    },
  });

  // Transit from splash to main
  win.once('ready-to-show', () => {
    if (splash) {
      setTimeout(() => {
        splash?.close();
        win?.show();
        win?.focus();
      }, 500); // Small buffer for React to mount
    } else {
      win?.show();
    }
  });

  // Call API Backend Setup
  kbmsClient.setWindow(win);

  ipcMain.handle('kbms:execute', async (_, query, options: any = {}) => {
      try {
        const isBackground = !!options.isBackground;
        const requestId = options.requestId;
        const isManagement = !!options.isManagement;

        console.log('Execute called from UI:', query, isBackground ? '(Background)' : '', requestId ? `[Req: ${requestId}]` : '', isManagement ? '(Management)' : '');
        const result = await kbmsClient.execute(query, isBackground, requestId, isManagement);
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
  
  ipcMain.handle('kbms:get-stats', async (_, requestId?: string) => {
     return kbmsClient.getStats(requestId);
  });

  ipcMain.handle('kbms:get-sessions', async (_, requestId?: string) => {
     return kbmsClient.getSessions(requestId);
  });

  ipcMain.handle('kbms:mgmt-action', async (_, action: string, data: any = {}, requestId?: string) => {
     try {
       return await kbmsClient.sendManagementAction(action, data, requestId);
     } catch (err: any) {
       return { success: false, error: err.message };
     }
  });

  ipcMain.on('kbms:subscribe-logs', () => {
     kbmsClient.subscribeLogs();
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

app.whenReady().then(() => {
  // Set Dock icon for macOS in dev
  if (process.platform === 'darwin' && process.env.VITE_PUBLIC && app.dock) {
     const iconPath = path.join(process.env.VITE_PUBLIC, 'icon.png');
     if (fs.existsSync(iconPath)) {
        app.dock.setIcon(iconPath);
     }
  }
  
  createSplashScreen();
  createWindow();
});
