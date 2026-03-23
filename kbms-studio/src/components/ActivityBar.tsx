import { Database, Search, Settings, FileCode2, Server } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function ActivityBar() {
  const { setConnectModalOpen, activeSidebarView, setActiveSidebarView } = useKbmsStore();
  const isMac = navigator.userAgent.indexOf('Mac') > -1;
  const cmd = isMac ? '⌘' : 'Ctrl';

  return (
    <div className="w-[42px] flex-shrink-0 border-r border-[#e2e8f0] bg-slate-50 flex flex-col items-center py-3 space-y-4 z-20 font-sans">
      <button 
        onClick={() => setConnectModalOpen(true)}
        className="text-emerald-700 hover:text-emerald-600 transition-colors p-1.5 rounded bg-emerald-100/60 shadow-sm border border-emerald-200/50 relative group cursor-pointer"
        title="Connect to Server Manager"
      >
        <Database className="w-4 h-4 stroke-[2]" />
      </button>

      <div className="w-6 h-[1px] bg-slate-200 my-1" />

      <button 
        onClick={() => setActiveSidebarView('explorer')}
        className={`transition-colors p-1.5 rounded relative group cursor-pointer ${activeSidebarView === 'explorer' ? 'text-emerald-700 bg-emerald-50' : 'text-slate-500 hover:text-slate-800 hover:bg-slate-200/50'}`} 
        title={`Object Explorer (${cmd} + Shift + E)`}
      >
        <FileCode2 className="w-4 h-4 stroke-[1.5]" />
        {activeSidebarView === 'explorer' && (
          <div className="absolute left-0 top-1/2 -translate-y-1/2 w-0.5 h-4 bg-emerald-500 rounded-r-full -ml-[8px]" />
        )}
      </button>

      <button 
        onClick={() => setActiveSidebarView('system')}
        className={`transition-colors p-1.5 rounded relative group cursor-pointer ${activeSidebarView === 'system' ? 'text-emerald-700 bg-emerald-50' : 'text-slate-500 hover:text-slate-800 hover:bg-slate-200/50'}`} 
        title="System Management (Server Logs & Status)"
      >
        <Server className="w-4 h-4 stroke-[1.5]" />
        {activeSidebarView === 'system' && (
          <div className="absolute left-0 top-1/2 -translate-y-1/2 w-0.5 h-4 bg-emerald-500 rounded-r-full -ml-[8px]" />
        )}
      </button>

      <button className="text-slate-300 transition-colors p-1.5 rounded cursor-not-allowed" title={`Global Search (${cmd} + Shift + F) - Coming Soon`}>
        <Search className="w-4 h-4 stroke-[1.5]" />
      </button>
      
      <div className="flex-1" />
      
      <button className="text-slate-400 hover:text-slate-700 transition-colors p-1.5 rounded hover:bg-slate-200/50 mb-1 cursor-pointer" title={`Settings (${cmd} + ,)`}>
        <Settings className="w-4 h-4 stroke-[1.5]" />
      </button>
    </div>
  );
}
