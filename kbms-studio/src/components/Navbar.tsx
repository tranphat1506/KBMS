import { Play, Square, Save, FolderOpen, DatabaseZap, User } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';
import NotificationBell from './NotificationBell';

export default function Navbar() {
  const { 
    status, execute, stopExecution, isExecuting, openTab, saveTab, 
    activeTabId, tabs, selectedKb, activeSidebarView, lastCredentials 
  } = useKbmsStore();
  const hasTabs = tabs.length > 0;
  const isSystemView = activeSidebarView === 'system';
  
  const isMac = navigator.userAgent.indexOf('Mac') > -1;
  const cmd = isMac ? '⌘' : 'Ctrl';
  const alt = isMac ? '⌥' : 'Alt';

  return (
    <div className={`h-11 bg-[var(--bg-surface)] flex items-center px-4 justify-between border-b border-[var(--border-subtle)] shadow-[0_1px_3px_rgba(0,0,0,0.02)] z-20 font-sans select-none transition-colors duration-200 ${isMac ? 'pl-[90px]' : ''}`} style={{ WebkitAppRegion: 'drag' } as any}>
      <div className="flex items-center space-x-5" style={{ WebkitAppRegion: 'no-drag' } as any}>
        <div className="flex items-center space-x-1.5 text-[var(--brand-primary)] select-none mr-2">
          <DatabaseZap className="w-4 h-4 fill-[var(--brand-primary-light)]/30" />
          <span className="font-medium text-[13px] tracking-tight text-[var(--text-main)] uppercase">KBMS Studio</span>
        </div>
        
        {!isSystemView && (
          <>
            <div className="flex items-center space-x-0.5 bg-[var(--bg-app)] p-0.5 rounded border border-[var(--border-subtle)] shadow-inner">
              <button 
                title={`Open File (${cmd} + O)`} 
                onClick={openTab} 
                className="flex items-center space-x-1 px-2.5 py-1 text-[11px] font-normal text-[var(--text-sub)] hover:bg-[var(--bg-surface)] hover:text-[var(--brand-primary)] hover:shadow-sm rounded transition-all cursor-pointer"
              >
                <FolderOpen className="w-3.5 h-3.5" />
                <span>Open File</span>
              </button>
              <button 
                 title={`Save File (${cmd} + S)`}
                 onClick={() => hasTabs && saveTab(activeTabId)} 
                 disabled={!hasTabs}
                 className="flex items-center space-x-1 px-2.5 py-1 text-[11px] font-normal text-[var(--text-sub)] hover:bg-[var(--bg-surface)] hover:text-[var(--brand-primary)] hover:shadow-sm rounded transition-all disabled:opacity-50 disabled:hover:bg-transparent disabled:cursor-not-allowed cursor-pointer"
              >
                <Save className="w-3.5 h-3.5" />
                <span>Save</span>
              </button>
            </div>
            
            <div className="w-px h-4 bg-[var(--border-muted)]" />
            
            <div className="flex items-center space-x-2">
               <button 
                 title={`Execute Query (${alt} + Enter)`}
                 onClick={() => execute()}
                 disabled={isExecuting || status !== 'connected'}
                 className={`flex items-center space-x-1.5 px-3 py-1 text-[11px] font-medium text-white rounded shadow-sm transition-all relative cursor-pointer ${
                   isExecuting || status !== 'connected' 
                     ? 'bg-[var(--bg-app)] cursor-not-allowed opacity-70 border border-[var(--border-subtle)] text-[var(--text-muted)]' 
                     : 'bg-[var(--brand-primary)] hover:bg-[var(--brand-primary-hover)] border border-[var(--brand-primary-hover)] shadow-[0_1px_2px_rgba(16,185,129,0.15)] active:scale-95'
                 }`}
               >
                 <Play className="w-3 h-3 fill-current" />
                 <span>Execute</span>
               </button>
               
               <button 
                 title={`Stop Execution (${alt} + Space)`}
                 onClick={stopExecution}
                 disabled={!isExecuting}
                 className={`flex items-center space-x-1.5 px-3 py-1 text-[11px] font-medium rounded shadow-sm border transition-all cursor-pointer ${
                     !isExecuting 
                     ? 'bg-[var(--bg-app)] text-[var(--text-muted)] border-[var(--border-subtle)] cursor-not-allowed' 
                     : 'bg-[var(--bg-surface)] text-rose-500 border-rose-500/20 hover:bg-rose-500/10 hover:border-rose-500/30 shadow-[0_1px_2px_rgba(239,68,68,0.1)] active:scale-95'
                 }`}
               >
                 <Square className="w-3 h-3 fill-current" />
                 <span>Stop</span>
               </button>
            </div>
          </>
        )}
      </div>
      
      <div className="flex items-center space-x-4 ml-4" style={{ WebkitAppRegion: 'no-drag' } as any}>
        {!isSystemView && (
          <select 
            value={selectedKb}
            onChange={(e) => useKbmsStore.getState().changeKnowledgeBase(e.target.value)}
            disabled={status !== 'connected' || isExecuting} 
            className={`text-[11px] outline-none font-normal border rounded px-2 py-1 cursor-pointer transition-colors shadow-sm min-w-[150px] disabled:opacity-50 ${selectedKb === '' ? 'text-[var(--text-muted)] border-[var(--border-subtle)] bg-[var(--bg-app)]' : 'text-[var(--text-main)] border-[var(--brand-primary)]/40 bg-[var(--bg-surface)]'}`}
          >
            <option value="" disabled>Active Knowledge Base...</option>
            {useKbmsStore.getState().metadata.databases.map(db => (
               <option key={db} value={db}>{db}</option>
            ))}
          </select>
        )}

        <div className="flex items-center space-x-3 pl-4 border-l border-[var(--border-subtle)]">
           <NotificationBell />
           
           <div className="flex items-center space-x-2.5 px-2 py-1 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded shadow-sm group hover:border-[var(--brand-primary)]/30 transition-all cursor-default">
              <div className="w-6 h-6 rounded-full bg-[var(--brand-primary-light)]/20 flex items-center justify-center text-[var(--brand-primary)] shadow-inner">
                <User className="w-3.5 h-3.5" />
              </div>
              <div className="flex flex-col">
                <span className="text-[10px] font-bold text-[var(--text-main)] leading-none uppercase tracking-tighter">{lastCredentials?.user || 'Anonymous'}</span>
                <span className={`text-[8px] font-thin uppercase tracking-widest ${status === 'connected' ? 'text-emerald-500' : 'text-rose-500'}`}>
                  {status === 'connected' ? 'Online' : 'Offline'}
                </span>
              </div>
           </div>
        </div>
      </div>
    </div>
  );
}
