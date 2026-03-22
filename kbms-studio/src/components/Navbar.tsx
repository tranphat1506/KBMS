import { Play, Square, Save, FolderOpen, DatabaseZap } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function Navbar() {
  const { status, execute, stopExecution, isExecuting, openTab, saveTab, activeTabId, tabs, selectedKb } = useKbmsStore();
  const hasTabs = tabs.length > 0;
  
  const isMac = navigator.userAgent.indexOf('Mac') > -1;
  const cmd = isMac ? '⌘' : 'Ctrl';
  const alt = isMac ? '⌥' : 'Alt';

  return (
    <div className={`h-11 bg-white flex items-center px-4 justify-between border-b border-slate-200 shadow-[0_1px_3px_rgba(0,0,0,0.02)] z-20 font-sans select-none ${isMac ? 'pl-[90px]' : ''}`} style={{ WebkitAppRegion: 'drag' } as any}>
      <div className="flex items-center space-x-5" style={{ WebkitAppRegion: 'no-drag' } as any}>
        <div className="flex items-center space-x-1.5 text-emerald-600 select-none">
          <DatabaseZap className="w-4 h-4 fill-emerald-100" />
          <span className="font-medium text-[13px] tracking-tight text-slate-700 uppercase">KBMS Studio</span>
        </div>
        
        <div className="flex items-center space-x-0.5 bg-slate-100/80 p-0.5 rounded border border-slate-200 shadow-inner">
          <button 
            title={`Open File (${cmd} + O)`} 
            onClick={openTab} 
            className="flex items-center space-x-1 px-2.5 py-1 text-[11px] font-normal text-slate-600 hover:bg-white hover:text-emerald-700 hover:shadow-sm rounded transition-all cursor-pointer"
          >
            <FolderOpen className="w-3.5 h-3.5" />
            <span>Open File</span>
          </button>
          <button 
             title={`Save File (${cmd} + S)`}
             onClick={() => hasTabs && saveTab(activeTabId)} 
             disabled={!hasTabs}
             className="flex items-center space-x-1 px-2.5 py-1 text-[11px] font-normal text-slate-600 hover:bg-white hover:text-emerald-700 hover:shadow-sm rounded transition-all disabled:opacity-50 disabled:hover:bg-transparent disabled:cursor-not-allowed cursor-pointer"
          >
            <Save className="w-3.5 h-3.5" />
            <span>Save</span>
          </button>
        </div>
        
        <div className="w-px h-4 bg-slate-200" />
        
        <div className="flex items-center space-x-2">
           <button 
             title={`Execute Query (${alt} + Enter)`}
             onClick={() => execute()}
             disabled={isExecuting || status !== 'connected'}
             className={`flex items-center space-x-1.5 px-3 py-1 text-[11px] font-medium text-white rounded shadow-sm transition-all relative cursor-pointer ${
               isExecuting || status !== 'connected' 
                 ? 'bg-slate-300 cursor-not-allowed opacity-70 border border-slate-400/50' 
                 : 'bg-emerald-500 hover:bg-emerald-400 border border-emerald-600 shadow-[0_1px_2px_rgba(16,185,129,0.15)] active:scale-95'
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
                 ? 'bg-slate-50 text-slate-400 border-slate-200 cursor-not-allowed' 
                 : 'bg-white text-red-600 border-red-200 hover:bg-red-50 hover:border-red-300 shadow-[0_1px_2px_rgba(239,68,68,0.1)] active:scale-95'
             }`}
           >
             <Square className="w-3 h-3 fill-current" />
             <span>Stop</span>
           </button>
        </div>
      </div>
      
      <div className="flex items-center space-x-3" style={{ WebkitAppRegion: 'no-drag' } as any}>
        <select 
          value={selectedKb}
          onChange={(e) => useKbmsStore.getState().changeKnowledgeBase(e.target.value)}
          disabled={status !== 'connected' || isExecuting} 
          className={`text-[11px] outline-none font-normal border rounded px-2 py-0.5 cursor-pointer transition-colors shadow-sm min-w-[170px] disabled:opacity-50 ${selectedKb === '' ? 'text-slate-400 border-slate-200 bg-slate-100' : 'text-slate-700 border-emerald-300 bg-white'}`}
        >
          <option value="" disabled>Select Knowledge Base...</option>
          {useKbmsStore.getState().metadata.databases.map(db => (
             <option key={db} value={db}>{db}</option>
          ))}
        </select>
      </div>
    </div>
  );
}
