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

    const res = await connect(host, parseInt(port), user, pass);
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
    <div className="w-[600px] h-[400px] bg-white rounded-lg shadow-[0_20px_60px_-15px_rgba(0,0,0,0.4)] border border-slate-200/80 overflow-hidden flex font-sans animate-in zoom-in-95 duration-200 relative">
       {/* Left Pane - Profile List */}
       <div className="w-[180px] border-r border-slate-200 bg-slate-50 flex flex-col shrink-0">
          <div className="px-3 py-2.5 border-b border-slate-200 bg-slate-100/50 flex items-center justify-between">
             <span className="text-[10px] font-semibold text-slate-500 uppercase tracking-widest">Saved Servers</span>
             <button onClick={() => loadProfileIntoForm('new')} className="p-1 hover:bg-slate-200/80 rounded text-slate-500 transition-colors cursor-pointer">
                <Plus className="w-3.5 h-3.5" />
             </button>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-1 custom-scrollbar">
             {profiles.map(p => (
                <div 
                  key={p.id}
                  onClick={() => loadProfileIntoForm(p.id)}
                  className={`px-3 py-1.5 rounded flex items-center space-x-2 cursor-pointer border transition-colors ${selectedId === p.id ? 'bg-white border-emerald-300 shadow-sm text-emerald-800' : 'border-transparent hover:bg-slate-200/50 text-slate-600'}`}
                >
                   <Database className={`w-3.5 h-3.5 shrink-0 ${selectedId === p.id ? 'text-emerald-500' : 'text-slate-400'}`} />
                   <div className="flex flex-col overflow-hidden">
                     <span className={`text-[12px] truncate ${selectedId === p.id ? 'font-medium' : 'font-normal'}`}>{p.name}</span>
                     <span className="text-[10px] text-slate-400 truncate -mt-0.5">{p.host}:{p.port}</span>
                   </div>
                </div>
             ))}
             
             {/* Virtual New Server Item */}
             {selectedId === 'new' && (
                <div className="px-3 py-1.5 rounded flex items-center space-x-2 cursor-pointer border bg-white border-emerald-300 shadow-sm text-emerald-800">
                   <ServerIcon className="w-3.5 h-3.5 shrink-0 text-emerald-500" />
                   <div className="flex flex-col overflow-hidden">
                     <span className="text-[12px] font-medium truncate italic text-slate-500">Unsaved Server</span>
                   </div>
                </div>
             )}
          </div>
       </div>

       {/* Right Pane - Editing & Linking */}
       <div className="flex-1 flex flex-col bg-white">
          <div className="flex items-center justify-between px-5 py-3 border-b border-slate-100">
              <div className="flex items-center space-x-3">
                 <div className="w-8 h-8 bg-gradient-to-br from-emerald-100 to-emerald-50 rounded flex items-center justify-center border border-emerald-200/60 shadow-inner">
                    <HardDrive className="w-4 h-4 text-emerald-600" />
                 </div>
                 <div>
                    <h2 className="text-[14px] font-semibold text-slate-800 tracking-tight">{selectedId === 'new' ? 'New Connection' : 'Edit Connection'}</h2>
                    <p className="text-[11px] font-normal text-slate-400 mt-0.5">KBMS Direct TCP Endpoint</p>
                 </div>
              </div>
              <button title="Close (Escape)" onClick={() => setConnectModalOpen(false)} className="text-slate-400 hover:text-slate-600 p-1 hover:bg-slate-100 rounded transition-colors -mt-2 -mr-1 cursor-pointer">
                 <X className="w-4 h-4" />
              </button>
          </div>

          <div className="flex-1 px-5 py-4 overflow-y-auto">
             {errorVisible && (
               <div className="mb-4 p-2.5 bg-red-50 border border-red-200 rounded flex items-start space-x-2 animate-in fade-in">
                  <AlertCircle className="w-3.5 h-3.5 text-red-500 mt-0.5 shrink-0" />
                  <span className="text-[11px] text-red-700 font-medium leading-relaxed">{errorMsg}</span>
               </div>
             )}

             <div className="space-y-3">
                <div className="space-y-1">
                   <label className="text-[11px] font-medium text-slate-500">Profile Name</label>
                   <input 
                     type="text" 
                     value={name}
                     onChange={e => setName(e.target.value)}
                     placeholder="e.g. My Production KBMS"
                     className="w-full text-[12px] font-normal px-2.5 py-1.5 bg-slate-50 border border-slate-300/80 rounded focus:bg-white focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 transition-all outline-none"
                   />
                </div>

                <div className="flex space-x-3 pt-1">
                   <div className="flex-1 space-y-1">
                     <label className="text-[11px] font-medium text-slate-500">Host (IP Address)</label>
                     <input 
                       type="text" 
                       value={host}
                       onChange={e => setHost(e.target.value)}
                       className="w-full text-[12px] font-normal px-2.5 py-1.5 bg-slate-50 border border-slate-300/80 rounded focus:bg-white focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 transition-all outline-none"
                     />
                   </div>
                   <div className="w-20 space-y-1">
                     <label className="text-[11px] font-medium text-slate-500">Port</label>
                     <input 
                       type="number" 
                       value={port}
                       onChange={e => setPort(e.target.value)}
                       className="w-full text-[12px] font-normal px-2.5 py-1.5 bg-slate-50 border border-slate-300/80 rounded focus:bg-white focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 transition-all outline-none"
                     />
                   </div>
                </div>

                <div className="flex space-x-3 pt-1">
                   <div className="flex-1 space-y-1">
                     <label className="text-[11px] font-medium text-slate-500">Username</label>
                     <input 
                       type="text" 
                       value={user}
                       onChange={e => setUser(e.target.value)}
                       className="w-full text-[12px] font-normal px-2.5 py-1.5 bg-slate-50 border border-slate-300/80 rounded focus:bg-white focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 transition-all outline-none"
                     />
                   </div>
                   <div className="flex-1 space-y-1">
                     <label className="text-[11px] font-medium text-slate-500">Password</label>
                     <input 
                       type="password" 
                       value={pass}
                       onChange={e => setPass(e.target.value)}
                       className="w-full text-[12px] font-normal px-2.5 py-1.5 bg-slate-50 border border-slate-300/80 rounded focus:bg-white focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 transition-all outline-none"
                     />
                   </div>
                </div>
             </div>
          </div>

          <div className="p-3 bg-slate-50 border-t border-slate-200 flex items-center justify-between shrink-0">
             <div>
                {selectedId !== 'new' && (
                   <button onClick={handleDelete} className="flex items-center space-x-1 px-3 py-1 text-[11px] font-medium text-red-600 hover:bg-red-50 rounded transition-colors cursor-pointer">
                      <Trash2 className="w-3.5 h-3.5" />
                      <span>Delete</span>
                   </button>
                )}
             </div>
             <div className="flex items-center space-x-2">
                <button 
                  onClick={handleSave} 
                  className="px-4 py-1.5 text-[11px] font-medium text-slate-600 hover:bg-slate-200 rounded transition-colors cursor-pointer"
                >
                   Save
                </button>
                <button 
                  onClick={handleConnect}
                  disabled={isExecuting}
                  className="flex items-center space-x-1.5 px-5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white text-[11px] font-semibold rounded shadow-sm transition-all focus:ring-2 focus:ring-emerald-500 focus:ring-offset-1 disabled:bg-slate-400 disabled:shadow-none cursor-pointer disabled:cursor-not-allowed"
                >
                   {isExecuting ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Wifi className="w-3.5 h-3.5" />}
                   <span>{isExecuting ? 'Connecting...' : 'Connect'}</span>
                </button>
             </div>
          </div>
       </div>
    </div>
  );
}
