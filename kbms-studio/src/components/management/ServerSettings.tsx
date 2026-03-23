import { useEffect, useState } from 'react';
import { useKbmsStore } from '../../store/kbmsStore';
import { Settings, Save, RefreshCw, Network, Layers } from 'lucide-react';

export default function ServerSettings() {
  const { systemSettings, fetchSettings, updateSetting } = useKbmsStore();
  const [editing, setEditing] = useState<Record<string, string>>({});

  useEffect(() => {
    fetchSettings();
  }, [fetchSettings]);

  const handleSave = async (name: string) => {
    if (editing[name] !== undefined) {
      await updateSetting(name, editing[name]);
      const newEditing = { ...editing };
      delete newEditing[name];
      setEditing(newEditing);
    }
  };

  return (
    <div className="space-y-8 animate-in fade-in duration-300">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-xs font-bold text-[var(--text-muted)] uppercase tracking-[0.2em]">Global Configuration</h3>
          <p className="text-[11px] text-[var(--text-sub)] font-thin mt-1 italic">Tuning parameters for the core database engine and network abstraction layers.</p>
        </div>
        <button 
          onClick={() => fetchSettings()}
          className="p-2.5 bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded text-[var(--text-muted)] hover:text-[var(--brand-primary)] hover:border-[var(--brand-primary)] transition-all active:bg-[var(--bg-app)] shadow-sm"
        >
          <RefreshCw className="w-4 h-4" />
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {systemSettings.map((s: any, i: number) => {
          const name = s.variable_name || s.Values?.variable_name;
          const value = s.variable_value || s.Values?.variable_value;
          const isDirty = editing[name] !== undefined;

          return (
            <div key={i} className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] flex flex-col justify-between group hover:border-[var(--brand-primary)]/20 transition-all shadow-sm">
              <div className="mb-6">
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center space-x-3">
                    <div className="p-2 bg-[var(--bg-app)] rounded border border-[var(--border-muted)] text-[var(--text-muted)] group-hover:text-[var(--brand-primary)] transition-colors shadow-sm">
                        {name.includes('conn') ? <Network className="w-4 h-4" /> : 
                         name.includes('page') ? <Layers className="w-4 h-4" /> : 
                         <Settings className="w-4 h-4" />}
                    </div>
                    <span className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em]">{name}</span>
                  </div>
                  {isDirty && <span className="text-[9px] font-thin text-amber-600 bg-amber-500/10 px-2 py-0.5 rounded border border-amber-500/20 uppercase tracking-widest shadow-sm">Pending</span>}
                </div>
                <input 
                  type="text"
                  className="w-full bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded px-4 py-2.5 text-xs font-mono font-thin text-[var(--text-main)] outline-none focus:bg-[var(--bg-surface)] focus:border-[var(--brand-primary)] transition-all placeholder-[var(--text-muted)]/50"
                  value={editing[name] ?? value}
                  onChange={e => setEditing({...editing, [name]: e.target.value})}
                />
              </div>
              <div className="flex items-center justify-between">
                <span className="text-[10px] text-[var(--text-muted)] font-thin italic">Managed internal metadata</span>
                <button 
                  disabled={!isDirty}
                  onClick={() => handleSave(name)}
                  className={`flex items-center space-x-2 px-5 py-2 rounded text-[10px] font-thin transition-all border shadow-sm ${
                    isDirty 
                      ? 'bg-[var(--brand-primary)] text-white border-[var(--brand-primary-hover)] hover:bg-[var(--brand-primary-hover)]' 
                      : 'bg-[var(--bg-app)] text-[var(--text-muted)] border-[var(--border-muted)] cursor-not-allowed'
                  }`}
                >
                  <Save className="w-3.5 h-3.5" />
                  <span className="uppercase tracking-widest">Commit Change</span>
                </button>
              </div>
            </div>
          );
        })}
      </div>
      
      {/* Informational Alerts */}
      <div className="bg-[var(--brand-primary-light)]/30 border border-[var(--brand-primary)]/20 p-6 rounded-lg flex items-start space-x-4 mb-10 shadow-sm">
        <ServerIcon className="w-6 h-6 text-[var(--brand-primary)] shrink-0 mt-0.5" />
        <div>
          <h4 className="text-xs font-bold text-[var(--brand-primary-text)] uppercase tracking-widest">Operational Directive</h4>
          <p className="text-[10px] text-[var(--brand-primary-text)]/80 leading-relaxed font-thin mt-1.5">Certain infrastructure variables require a service restart for orchestration. High-risk parameters should only be modified during maintenance windows.</p>
        </div>
      </div>
    </div>
  );
}

function ServerIcon({ className }: { className?: string }) {
    return (
        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
            <rect width="20" height="8" x="2" y="2" rx="2" ry="2"/>
            <rect width="20" height="8" x="2" y="14" rx="2" ry="2"/>
            <line x1="6" x2="6.01" y1="6" y2="6"/>
            <line x1="6" x2="6.01" y1="18" y2="18"/>
        </svg>
    );
}
