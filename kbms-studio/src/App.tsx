import Layout from './components/Layout';
import ConnectModal from './components/ConnectModal';
import { useKbmsStore } from './store/kbmsStore';
import StudioSettings from './components/management/StudioSettings';

import { useEffect } from 'react';
import { ErrorBoundary } from './components/ErrorBoundary';
import ConfirmDialog from './components/ConfirmDialog';

function App() {
  const isConnectModalOpen = useKbmsStore(state => state.isConnectModalOpen);
  const isStudioSettingsOpen = useKbmsStore(state => state.isStudioSettingsOpen);
  const setStatus = useKbmsStore(state => state.setStatus);
  const tabs = useKbmsStore(state => state.tabs);
  const showConfirm = useKbmsStore(state => state.showConfirm);

  useEffect(() => {
    // @ts-ignore
    if (window.kbmsApi?.onAppCloseRequested) {
      // @ts-ignore
      window.kbmsApi.onAppCloseRequested(() => {
        showConfirm(
          'Confirm Exit',
          'You have unsaved changes. Are you sure you want to quit? Any unsaved work will be lost.',
          () => {
            // @ts-ignore
            window.kbmsApi.forceQuit();
          }
        );
      });
    }
  }, [showConfirm]);

  useEffect(() => {
    const hasUnsaved = tabs.some(t => !t.isSaved);
    // @ts-ignore
    if (window.kbmsApi?.setUnsavedStatus) {
      // @ts-ignore
      window.kbmsApi.setUnsavedStatus(hasUnsaved);
    }
  }, [tabs]);

  useEffect(() => {
    // @ts-ignore
    const unsubscribeStatus = window.kbmsApi.onStatusChange((status: any) => {
      console.log("(App) Connection status changed:", status);
      setStatus(status);
    });

    // @ts-ignore
    const unsubscribeStream = window.kbmsApi.onDataStream((data: any) => {
      console.log("(App) Incoming data stream fragment:", data);
      useKbmsStore.getState().handleResultFragment(data);
    });

    // Recover status on startup
    // @ts-ignore
    window.kbmsApi.getStatus().then((res: any) => {
      if (res && res.status === 'connected') {
        console.log("(App) Recovered active connection from backend");
        setStatus('connected');
        useKbmsStore.getState().fetchMetadata();
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
        useKbmsStore.getState().fetchMetadata();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [setStatus]);

  const studioSettings = useKbmsStore(state => state.studioSettings);
  const fontSizeClass = `size-${studioSettings.fontSize}`;
  const fontWeightClass = `font-${studioSettings.fontWeight}`;
  const themeClass = studioSettings.theme === 'dark' ? 'dark' : '';

  return (
    <ErrorBoundary>
      <div className={`h-screen w-screen overflow-hidden bg-[var(--bg-app)] font-sans text-[var(--text-main)] flex flex-col antialiased relative ${fontSizeClass} ${fontWeightClass} ${themeClass} transition-colors duration-200`}>
        <Layout />
        <ConfirmDialog />
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
