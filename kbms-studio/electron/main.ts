import { app, BrowserWindow, ipcMain, dialog } from 'electron';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { thingentClient } from './thingent-client';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Cấu hình đường dẫn
process.env.DIST = path.join(__dirname, '../dist');
process.env.VITE_PUBLIC = app.isPackaged ? process.env.DIST : path.join(process.env.DIST, '../public');

let win: BrowserWindow | null;
let splash: BrowserWindow | null;

app.name = 'Thingent Studio';

// Ensure the app name is correct in development (macOS)
if (process.platform === 'darwin') {
  app.setName('Thingent Studio');
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
    icon: path.join(process.env.VITE_PUBLIC!, 'assets/Thingent-Icons.png'),
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
    title: 'Thingent Studio',
    icon: path.join(process.env.VITE_PUBLIC!, 'assets/Thingent-Icons.png'),
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
  thingentClient.setWindow(win);

  ipcMain.handle('thingent:execute', async (_, query, options: any = {}) => {
      try {
        const isBackground = !!options.isBackground;
        const requestId = options.requestId;
        const isManagement = !!options.isManagement;

        console.log('Execute called from UI:', query, isBackground ? '(Background)' : '', requestId ? `[Req: ${requestId}]` : '', isManagement ? '(Management)' : '');
        const result = await thingentClient.execute(query, isBackground, requestId, isManagement);
        return result;
      } catch (err: any) {
        return { success: false, messages: [err.message], rows: [], headers: [] };
      }
   });

  ipcMain.handle('thingent:connect', async (_, host, port, user, pass) => {
     try {
       const success = await thingentClient.connect(host, port, user, pass);
       return { success };
     } catch (err: any) {
       return { success: false, error: err.message };
     }
  });

   ipcMain.handle('thingent:get-status', async () => {
      return thingentClient.getStatus();
   });

  ipcMain.handle('thingent:disconnect', async () => {
     thingentClient.disconnect();
     return { success: true };
  });
  
  ipcMain.handle('thingent:get-stats', async (_, requestId?: string) => {
     return thingentClient.getStats(requestId);
  });

  ipcMain.handle('thingent:get-sessions', async (_, requestId?: string) => {
     return thingentClient.getSessions(requestId);
  });

  ipcMain.handle('thingent:get-lsp-completions', async (_, code: string, line: number, column: number, kbName?: string) => {
     try {
       return await thingentClient.getLspCompletions(code, line, column, kbName);
     } catch (err: any) {
       return { completions: [] };
     }
  });

  ipcMain.handle('thingent:get-lsp-diagnostics', async (_, code: string) => {
     try {
       return await thingentClient.getLspDiagnostics(code);
     } catch (err: any) {
       return { valid: true, errors: [] };
     }
  });

  ipcMain.handle('thingent:mgmt-action', async (_, action: string, data: any = {}, requestId?: string) => {
     try {
       return await thingentClient.sendManagementAction(action, data, requestId);
     } catch (err: any) {
       return { success: false, error: err.message };
     }
  });

  ipcMain.on('thingent:subscribe-logs', () => {
     thingentClient.subscribeLogs();
  });

  ipcMain.handle('thingent:save-file', async (_e, content: string, currentPath?: string, isNewFile: boolean = false) => {
     if (!win) return { success: false };
     
     let targetPath = currentPath;
     const isAbsolutePath = targetPath && path.isAbsolute(targetPath);
     
     // Only show dialog if it's explicitly a new file, or we don't have an absolute path yet
     if (isNewFile || !isAbsolutePath) {
         const { canceled, filePath } = await dialog.showSaveDialog(win, {
            title: isNewFile ? 'Save As' : 'Save KBQL Query',
            defaultPath: targetPath || 'Query.kbql',
            filters: [{ name: 'Query', extensions: ['kbql', 'sql', 'txt'] }]
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

  ipcMain.handle('thingent:open-file', async () => {
     if (!win) return { success: false };
     const { canceled, filePaths } = await dialog.showOpenDialog(win, {
        title: 'Open KBQL Query',
        filters: [{ name: 'Query', extensions: ['kbql', 'sql', 'txt'] }],
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
  ipcMain.on('thingent:set-unsaved-status', (_, status: boolean) => {
    hasUnsavedChanges = status;
  });

  ipcMain.on('thingent:force-quit', () => {
    hasUnsavedChanges = false; // Bypass the check
    if (win) win.close();
  });

  win.on('close', (e) => {
    if (hasUnsavedChanges && win) {
      e.preventDefault();
      // Notify renderer to show custom confirmation dialog
      win.webContents.send('thingent:app-close-request');
    }
  });

  ipcMain.on('thingent:set-theme', (_, theme: 'light' | 'dark') => {
    if (process.platform === 'darwin' && app.dock) {
      const iconName = theme === 'dark' ? 'Thingent-Icons-Night.png' : 'Thingent-Icons.png';
      const iconPath = path.join(process.env.VITE_PUBLIC!, 'assets', iconName);
      if (fs.existsSync(iconPath)) {
        app.dock.setIcon(iconPath);
      }
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
     const iconPath = path.join(process.env.VITE_PUBLIC, 'assets/Thingent-Icons.png');
     if (fs.existsSync(iconPath)) {
        app.dock.setIcon(iconPath);
     }
  }
  
  createSplashScreen();
  createWindow();
});
