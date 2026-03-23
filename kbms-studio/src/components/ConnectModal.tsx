import { useState } from 'react';
import { Database, Loader2, Wifi, ServerIcon, AlertCircle, Plus, Trash2, X, HardDrive } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';
import type { ServerProfile } from '../store/kbmsStore';

export default function ConnectModal() {
  const { connect, isExecuting, profiles, saveProfile, deleteProfile, setConnectModalOpen } = useKbmsStore();
  const [selectedId, setSelectedId] = useState<string>('new');
  
  // Form State
  const [name, setName] = useState('New Server');
  const [host, setHost] = useState('127.0.0.1');
  const [port, setPort] = useState('3307');
  const [user, setUser] = useState('admin');
  const [pass, setPass] = useState('admin');
  
  const [errorVisible, setErrorVisible] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const loadProfileIntoForm = (id: string) => {
     setSelectedId(id);
     setErrorVisible(false);
     if (id === 'new') {
         setName('New Server');
         setHost('127.0.0.1');
         setPort('3307');
         setUser('admin');
         setPass('admin');
     } else {
         const p = profiles.find(x => x.id === id);
         if (p) {
            setName(p.name);
            setHost(p.host);
            setPort(p.port.toString());
            setUser(p.user);
            setPass(p.pass || '');
         }
     }
  };

  const handleSave = () => {
      const p: ServerProfile = {
          id: selectedId === 'new' ? Date.now().toString() : selectedId,
          name: name.trim() || 'Unnamed',
          host,
          port: parseInt(port) || 3307,
          user,
          pass
      };
      saveProfile(p);
      if (selectedId === 'new') setSelectedId(p.id);
  };

  const handleConnect = async () => {
    if (!host || !port) return;
    handleSave(); // Auto save fields on connect
    setErrorVisible(false);

    const res = await connect(host, parseInt(port), user, pass, name);
    if (!res.success) {
       setErrorVisible(true);
       setErrorMsg(res.error || 'Connection failed. Please check credentials or network.');
    } else {
       setConnectModalOpen(false); // Close on success
    }
  };

  const handleDelete = (e: React.MouseEvent) => {
     e.stopPropagation();
     if (selectedId !== 'new') {
        deleteProfile(selectedId);
        loadProfileIntoForm('new');
     }
  };

  return (
    <div className="w-[640px] h-[440px] bg-[var(--bg-surface)] rounded-lg shadow-[0_20px_60px_-15px_rgba(0,0,0,0.5)] border border-[var(--border-subtle)] overflow-hidden flex font-sans animate-in zoom-in-95 duration-200 relative transition-colors">
       {/* Left Pane - Profile List */}
       <div className="w-[200px] border-r border-[var(--border-muted)] bg-[var(--bg-app)] flex flex-col shrink-0">
          <div className="px-3 py-3 border-b border-[var(--border-muted)] bg-[var(--bg-surface-alt)]/50 flex items-center justify-between">
             <span className="text-[10px] font-semibold text-[var(--text-muted)] uppercase tracking-widest">Saved Profiles</span>
             <button onClick={() => loadProfileIntoForm('new')} className="p-1 hover:bg-[var(--bg-surface-alt)] rounded text-[var(--text-muted)] transition-colors cursor-pointer">
                <Plus className="w-3.5 h-3.5" />
             </button>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-1 custom-scrollbar">
             {profiles.map(p => (
                <div 
                  key={p.id}
                  onClick={() => loadProfileIntoForm(p.id)}
                  className={`px-3 py-2 rounded flex items-center space-x-2 cursor-pointer border transition-all ${selectedId === p.id ? 'bg-[var(--bg-surface)] border-[var(--brand-primary)]/40 shadow-sm text-[var(--brand-primary-text)]' : 'border-transparent hover:bg-[var(--bg-surface-alt)] text-[var(--text-sub)]'}`}
                >
                   <Database className={`w-3.5 h-3.5 shrink-0 transition-colors ${selectedId === p.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />
                   <div className="flex flex-col overflow-hidden">
                     <span className={`text-[12px] truncate ${selectedId === p.id ? 'font-medium' : 'font-normal font-thin'}`}>{p.name}</span>
                     <span className="text-[10px] text-[var(--text-muted)] truncate -mt-0.5 opacity-70">{p.host}:{p.port}</span>
                   </div>
                </div>
             ))}
             
             {/* Virtual New Server Item */}
             {selectedId === 'new' && (
                <div className="px-3 py-2 rounded flex items-center space-x-2 cursor-pointer border bg-[var(--bg-surface)] border-[var(--brand-primary)]/40 shadow-sm text-[var(--brand-primary-text)]">
                   <ServerIcon className="w-3.5 h-3.5 shrink-0 text-[var(--brand-primary)]" />
                   <div className="flex flex-col overflow-hidden">
                     <span className="text-[12px] font-medium truncate italic text-[var(--text-muted)]">Unregistered...</span>
                   </div>
                </div>
             )}
          </div>
       </div>

        {/* Right Pane - Editing & Linking */}
        <div className="flex-1 flex flex-col bg-[var(--bg-surface)] transition-colors">
          <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--border-muted)] bg-[var(--bg-app)]/30">
              <div className="flex items-center space-x-4">
                 <div className="w-10 h-10 bg-[var(--brand-primary-light)]/20 rounded flex items-center justify-center border border-[var(--brand-primary)]/10 shadow-inner">
                    <HardDrive className="w-5 h-5 text-[var(--brand-primary)]" />
                 </div>
                 <div>
                    <h2 className="text-[15px] font-bold text-[var(--text-main)] tracking-tight uppercase">{selectedId === 'new' ? 'Initialize Connection' : 'Modify Configuration'}</h2>
                    <p className="text-[11px] font-thin text-[var(--text-muted)] mt-0.5 uppercase tracking-widest">Direct TCP Endpoint Protocol</p>
                 </div>
              </div>
              <button title="Close (Escape)" onClick={() => setConnectModalOpen(false)} className="text-[var(--text-muted)] hover:text-[var(--text-main)] p-1 hover:bg-[var(--bg-surface-alt)] rounded transition-all -mt-3 -mr-1 cursor-pointer">
                 <X className="w-4 h-4" />
              </button>
          </div>

          <div className="flex-1 px-6 py-6 overflow-y-auto space-y-5">
             {errorVisible && (
               <div className="p-3 bg-red-500/10 border border-red-500/20 rounded flex items-start space-x-3 animate-in fade-in transition-colors">
                  <AlertCircle className="w-4 h-4 text-red-500 mt-0.5 shrink-0" />
                  <span className="text-[11px] text-red-500 font-medium leading-relaxed font-thin">{errorMsg}</span>
               </div>
             )}

             <div className="space-y-4">
                <div className="space-y-1.5 focus-within:text-[var(--brand-primary)] transition-colors">
                   <label className="text-[10px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Cluster Name</label>
                   <input 
                     type="text" 
                     value={name}
                     onChange={e => setName(e.target.value)}
                     placeholder="e.g. Primary Production Node"
                     className="w-full text-[12px] font-thin tracking-wide px-3 py-2 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded-md focus:bg-[var(--bg-surface)] focus:border-[var(--brand-primary)] focus:ring-1 focus:ring-[var(--brand-primary)] transition-all outline-none placeholder-[var(--text-muted)]/30"
                   />
                </div>

                <div className="flex space-x-4">
                   <div className="flex-1 space-y-1.5 focus-within:text-[var(--brand-primary)] transition-colors">
                     <label className="text-[10px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Internal Host (IP)</label>
                     <input 
                       type="text" 
                       value={host}
                       onChange={e => setHost(e.target.value)}
                       className="w-full text-[12px] font-mono px-3 py-2 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded-md focus:bg-[var(--bg-surface)] focus:border-[var(--brand-primary)] transition-all outline-none"
                     />
                   </div>
                   <div className="w-24 space-y-1.5 focus-within:text-[var(--brand-primary)] transition-colors">
                     <label className="text-[10px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Port</label>
                     <input 
                       type="number" 
                       value={port}
                       onChange={e => setPort(e.target.value)}
                       className="w-full text-[12px] font-mono px-3 py-2 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded-md focus:bg-[var(--bg-surface)] focus:border-[var(--brand-primary)] transition-all outline-none"
                     />
                   </div>
                </div>

                <div className="flex space-x-4">
                   <div className="flex-1 space-y-1.5 focus-within:text-[var(--brand-primary)] transition-colors">
                     <label className="text-[10px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Auth Identity</label>
                     <input 
                       type="text" 
                       value={user}
                       onChange={e => setUser(e.target.value)}
                       placeholder="Username"
                       className="w-full text-[12px] font-thin px-3 py-2 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded-md focus:bg-[var(--bg-surface)] focus:border-[var(--brand-primary)] transition-all outline-none"
                     />
                   </div>
                   <div className="flex-1 space-y-1.5 focus-within:text-[var(--brand-primary)] transition-colors">
                     <label className="text-[10px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Secret Token</label>
                     <input 
                       type="password" 
                       value={pass}
                       onChange={e => setPass(e.target.value)}
                       placeholder="••••••••"
                       className="w-full text-[12px] font-thin px-3 py-2 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded-md focus:bg-[var(--bg-surface)] focus:border-[var(--brand-primary)] transition-all outline-none"
                     />
                   </div>
                </div>
             </div>
          </div>

          <div className="p-4 bg-[var(--bg-app)] border-t border-[var(--border-muted)] flex items-center justify-between shrink-0 transition-colors">
             <div>
                {selectedId !== 'new' && (
                   <button onClick={handleDelete} className="flex items-center space-x-1 px-4 py-1.5 text-[11px] font-medium text-rose-500 hover:bg-rose-500/10 rounded transition-all cursor-pointer font-thin uppercase tracking-widest">
                      <Trash2 className="w-3.5 h-3.5" />
                      <span>Purge Profile</span>
                   </button>
                )}
             </div>
             <div className="flex items-center space-x-3">
                <button 
                  onClick={handleSave} 
                  className="px-5 py-1.5 text-[11px] font-medium text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)] rounded transition-all cursor-pointer font-thin uppercase tracking-widest"
                >
                   Persist
                </button>
                <button 
                  onClick={handleConnect}
                  disabled={isExecuting}
                  className="flex items-center space-x-2 px-6 py-2 bg-[var(--brand-primary)] hover:bg-[var(--brand-primary-hover)] text-white text-[11px] font-bold rounded shadow-md hover:shadow-emerald-500/20 transition-all focus:ring-2 focus:ring-[var(--brand-primary)] focus:ring-offset-1 disabled:bg-[var(--bg-surface-alt)] disabled:text-[var(--text-muted)] disabled:shadow-none cursor-pointer disabled:cursor-not-allowed uppercase tracking-widest"
                >
                   {isExecuting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Wifi className="w-4 h-4" />}
                   <span>{isExecuting ? 'Handshaking...' : 'Establish Link'}</span>
                </button>
             </div>
          </div>
       </div>
    </div>
  );
}
