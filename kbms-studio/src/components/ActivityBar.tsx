import { Database, Search, Settings, FileCode2, Server } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function ActivityBar() {
  const { setConnectModalOpen, activeSidebarView, setActiveSidebarView, setStudioSettingsOpen, isStudioSettingsOpen } = useKbmsStore();
  const isMac = navigator.userAgent.indexOf('Mac') > -1;
  const cmd = isMac ? '⌘' : 'Ctrl';

  return (
    <div className="w-[42px] flex-shrink-0 border-r border-[var(--border-subtle)] bg-[var(--bg-sidebar)] flex flex-col items-center py-3 space-y-4 z-20 font-sans transition-colors duration-200">
      <button 
        onClick={() => setConnectModalOpen(true)}
        className="text-[var(--brand-primary-text)] hover:text-[var(--brand-primary)] transition-colors p-1.5 rounded bg-[var(--brand-primary-light)] shadow-sm border border-[var(--brand-primary)]/20 relative group cursor-pointer"
        title="Connect to Server Manager"
      >
        <Database className="w-4 h-4 stroke-[2]" />
      </button>

      <div className="w-6 h-[1px] bg-[var(--border-muted)] my-1" />

      <button 
        onClick={() => setActiveSidebarView('explorer')}
        className={`transition-colors p-1.5 rounded relative group cursor-pointer ${activeSidebarView === 'explorer' ? 'text-[var(--brand-primary-text)] bg-[var(--brand-primary-light)]' : 'text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]'}`} 
        title={`Object Explorer (${cmd} + Shift + E)`}
      >
        <FileCode2 className="w-4 h-4 stroke-[1.5]" />
        {activeSidebarView === 'explorer' && (
          <div className="absolute left-0 top-1/2 -translate-y-1/2 w-0.5 h-4 bg-[var(--brand-primary)] rounded-r-full -ml-[8px]" />
        )}
      </button>

      <button 
        onClick={() => setActiveSidebarView('system')}
        className={`transition-colors p-1.5 rounded relative group cursor-pointer ${activeSidebarView === 'system' ? 'text-[var(--brand-primary-text)] bg-[var(--brand-primary-light)]' : 'text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]'}`} 
        title="System Management (Server Logs & Status)"
      >
        <Server className="w-4 h-4 stroke-[1.5]" />
        {activeSidebarView === 'system' && (
          <div className="absolute left-0 top-1/2 -translate-y-1/2 w-0.5 h-4 bg-[var(--brand-primary)] rounded-r-full -ml-[8px]" />
        )}
      </button>

      <button className="text-[var(--text-muted)] opacity-50 transition-colors p-1.5 rounded cursor-not-allowed" title={`Global Search (${cmd} + Shift + F) - Coming Soon`}>
        <Search className="w-4 h-4 stroke-[1.5]" />
      </button>
      
      <div className="flex-1" />
      
      <button 
        onClick={() => setStudioSettingsOpen(true)}
        className={`transition-colors p-1.5 rounded mb-1 cursor-pointer ${isStudioSettingsOpen ? 'text-[var(--brand-primary-text)] bg-[var(--brand-primary-light)]' : 'text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]'}`} 
        title={`Studio Settings (${cmd} + ,)`}
      >
        <Settings className="w-4 h-4 stroke-[1.5]" />
      </button>
    </div>
  );
}
