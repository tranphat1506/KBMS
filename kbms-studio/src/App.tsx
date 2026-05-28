import Layout from './components/Layout';
import ConnectModal from './components/ConnectModal';
import { useThingentStore } from './store/thingentStore';
import StudioSettings from './components/management/StudioSettings';
import NotificationDetailModal from './components/NotificationDetailModal';

import { useEffect, useState } from 'react';
import { ErrorBoundary } from './components/ErrorBoundary';
import ConfirmDialog from './components/ConfirmDialog';

function App() {
  const isConnectModalOpen = useThingentStore(state => state.isConnectModalOpen);
  const isStudioSettingsOpen = useThingentStore(state => state.isStudioSettingsOpen);
  const setStatus = useThingentStore(state => state.setStatus);
  const tabs = useThingentStore(state => state.tabs);
  const showConfirm = useThingentStore(state => state.showConfirm);

  useEffect(() => {
    // @ts-ignore
    if (window.thingentApi?.onAppCloseRequested) {
      // @ts-ignore
      window.thingentApi.onAppCloseRequested(() => {
        showConfirm(
          'Confirm Exit',
          'You have unsaved changes. Are you sure you want to quit? Any unsaved work will be lost.',
          () => {
            // @ts-ignore
            window.thingentApi.forceQuit();
          }
        );
      });
    }
  }, [showConfirm]);

  useEffect(() => {
    const hasUnsaved = tabs.some(t => !t.isSaved);
    // @ts-ignore
    if (window.thingentApi?.setUnsavedStatus) {
      // @ts-ignore
      window.thingentApi.setUnsavedStatus(hasUnsaved);
    }
  }, [tabs]);

  useEffect(() => {
    // Remove splash screen once App component is mounted
    const splash = document.getElementById('splash-wrapper');
    if (splash) {
      splash.style.opacity = '0';
      setTimeout(() => splash.remove(), 500);
    }
  }, []);

  useEffect(() => {
    // @ts-ignore
    const unsubscribeStatus = window.thingentApi.onStatusChange((status: any) => {
      console.log("(App) Connection status changed:", status);
      setStatus(status);
    });

    // @ts-ignore
    const unsubscribeStream = window.thingentApi.onDataStream((data: any) => {
      console.log("(App) Incoming data stream fragment:", data);
      useThingentStore.getState().handleResultFragment(data);
    });

    // Recover status on startup
    // @ts-ignore
    window.thingentApi.getStatus().then((res: any) => {
      if (res && res.status === 'connected') {
        console.log("(App) Recovered active connection from backend");
        setStatus('connected');
        useThingentStore.getState().fetchMetadata();
      }
    });

    // Return a cleanup function to prevent memory leaks and duplicate execution in React Strict Mode
    return () => {
      if (unsubscribeStatus) unsubscribeStatus();
      if (unsubscribeStream) unsubscribeStream();
    };
  }, [setStatus]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const isMac = navigator.userAgent.indexOf('Mac') > -1;
      const isCmdOrCtrl = isMac ? e.metaKey : e.ctrlKey;
      if (isCmdOrCtrl && e.key.toLowerCase() === 'r') {
        e.preventDefault();
        console.log("(App) Intercepted Reload shortcut. Fetching metadata...");
        useThingentStore.getState().fetchMetadata();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [setStatus]);

  const studioSettings = useThingentStore(state => state.studioSettings);
  
  const [isSystemDark, setIsSystemDark] = useState(
    window.matchMedia('(prefers-color-scheme: dark)').matches
  );

  useEffect(() => {
    if (studioSettings.theme !== 'device') return;
    
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = (e: MediaQueryListEvent) => {
       setIsSystemDark(e.matches);
    };
    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, [studioSettings.theme]);

  const isDark = studioSettings.theme === 'device' ? isSystemDark : studioSettings.theme === 'dark';

  useEffect(() => {
    const html = document.documentElement;
    // Remove old classes
    const classesToRemove = Array.from(html.classList).filter(c => 
      c.startsWith('size-') || c.startsWith('font-') || c === 'dark'
    );
    html.classList.remove(...classesToRemove);
    
    // Add new classes
    html.classList.add(`size-${studioSettings.fontSize}`);
    html.classList.add(`font-${studioSettings.fontWeight}`);
    if (isDark) html.classList.add('dark');

    // Notify main process
    // @ts-ignore
    if (window.thingentApi?.setTheme) {
      // @ts-ignore
      window.thingentApi.setTheme(isDark ? 'dark' : 'light');
    }
  }, [isDark, studioSettings.fontSize, studioSettings.fontWeight]);

  return (
    <ErrorBoundary>
      <div className="h-screen w-screen overflow-hidden bg-[var(--bg-app)] font-sans text-[var(--text-main)] flex flex-col antialiased relative transition-colors duration-200">
        <Layout />
        <ConfirmDialog />
        <NotificationDetailModal />
        {isConnectModalOpen && (
          <div className="absolute inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-[2px] animate-in fade-in duration-200">
            <ConnectModal />
          </div>
        )}
        {isStudioSettingsOpen && (
          <div className="absolute inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-[4px] animate-in fade-in duration-300">
            <StudioSettings />
          </div>
        )}
      </div>
    </ErrorBoundary>
  );
}

export default App;
