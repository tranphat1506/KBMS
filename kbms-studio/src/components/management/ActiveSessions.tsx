import { useKbmsStore } from '../../store/kbmsStore';
import { Users, XCircle, Globe, Database, Activity, RefreshCw } from 'lucide-react';

export default function ActiveSessions() {
  const { systemSessions, refreshSessions, killSession } = useKbmsStore();

  return (
    <div className="space-y-8 animate-in fade-in duration-300">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-xs font-bold text-[var(--text-muted)] uppercase tracking-[0.2em]">Real-time Connectivity</h3>
          <p className="text-[11px] text-[var(--text-sub)] font-thin mt-1 italic">Monitoring and management of active stateful client connections.</p>
        </div>
        <button 
          onClick={refreshSessions}
          className="flex items-center space-x-2 px-4 py-2 bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded text-xs font-thin text-[var(--text-sub)] hover:bg-[var(--bg-surface-alt)] hover:text-[var(--brand-primary)] hover:border-[var(--brand-primary)] transition-all active:bg-[var(--bg-app)] shadow-sm"
        >
          <RefreshCw className="w-4 h-4" />
          <span>Synchronize Registry</span>
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <SessionStatCard 
          label="Total Connections" 
          value={systemSessions.length} 
          icon={Globe} 
          color="text-[var(--brand-primary)]"
          bg="bg-[var(--brand-primary-light)]/50"
        />
        <SessionStatCard 
          label="Unique Subjects" 
          value={new Set(systemSessions.map(s => s.Username)).size} 
          icon={Users} 
          color="text-[var(--brand-primary)]"
          bg="bg-[var(--brand-primary-light)]/50"
        />
        <SessionStatCard 
          label="Deployment Health" 
          value={systemSessions.length > 50 ? 'CONGESTED' : 'NOMINAL'} 
          icon={Activity} 
          color={systemSessions.length > 50 ? 'text-amber-500' : 'text-[var(--brand-primary)]'} 
          bg={systemSessions.length > 50 ? 'bg-amber-500/10' : 'bg-[var(--brand-primary-light)]/40'}
        />
      </div>

      {/* Sessions Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6 pb-10">
        {systemSessions.length === 0 ? (
          <div className="col-span-full py-20 text-center bg-[var(--bg-surface)] rounded-lg border border-dashed border-[var(--border-muted)]">
            <Users className="w-12 h-12 text-[var(--text-muted)]/10 mx-auto mb-4" />
            <p className="text-[var(--text-muted)] font-thin text-sm italic">No active client sessions detected in registry.</p>
          </div>
        ) : (
          systemSessions.map((session, i) => (
            <div key={i} className="bg-[var(--bg-surface)] rounded-lg border border-[var(--border-subtle)] p-6 hover:border-[var(--brand-primary)]/30 transition-all group overflow-hidden relative shadow-sm">
              <div className="absolute top-0 right-0 p-3 opacity-0 group-hover:opacity-100 transition-opacity">
                 <button 
                    onClick={() => { if(confirm(`Terminate session for ${session.Username}?`)) killSession(session.SessionId); }}
                    className="p-2 text-rose-300 hover:text-rose-600 hover:bg-rose-500/10 rounded transition-colors border border-transparent hover:border-rose-100"
                    title="Kill Session"
                 >
                   <XCircle className="w-5 h-5" />
                 </button>
              </div>

              <div className="flex items-center space-x-4 mb-6">
                <div className="w-10 h-10 rounded bg-[var(--bg-app)] border border-[var(--border-subtle)] flex items-center justify-center text-[var(--text-muted)] group-hover:text-[var(--brand-primary)] transition-colors shadow-sm">
                  <span className="font-bold text-sm tracking-tighter">{(session.Username || 'A')[0].toUpperCase()}</span>
                </div>
                <div className="min-w-0 flex-1">
                  <h4 className="font-bold text-[var(--text-main)] truncate tracking-tight">{session.Username || 'Anonymous'}</h4>
                  <p className="text-[10px] text-[var(--text-muted)] font-thin font-mono truncate tracking-widest mt-0.5">{session.IpAddress}</p>
                </div>
              </div>

              <div className="space-y-3 text-xs">
                <div className="flex items-center justify-between py-2 border-b border-[var(--border-muted)]">
                  <div className="flex items-center text-[var(--text-sub)] font-thin">
                    <Database className="w-3.5 h-3.5 mr-2 text-[var(--text-muted)]" />
                    <span>Context Cluster</span>
                  </div>
                  <span className="font-thin text-[var(--brand-primary)] uppercase tracking-widest">{session.CurrentKb || 'GLOBAL'}</span>
                </div>
                <div className="flex items-center justify-between py-1">
                  <div className="flex items-center text-[var(--text-sub)] font-thin">
                    <Activity className="w-3.5 h-3.5 mr-2 text-[var(--text-muted)]" />
                    <span>Handle</span>
                  </div>
                  <span className="font-mono text-[var(--text-muted)] truncate ml-2 max-w-[120px] font-thin">{session.SessionId.split('-')[0]}...</span>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

function SessionStatCard({ label, value, icon: Icon, color, bg }: any) {
  return (
    <div className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] flex items-center space-x-5 hover:border-[var(--brand-primary)]/20 transition-all shadow-sm group">
      <div className={`p-3 rounded transition-colors ${bg} ${color} shadow-sm`}>
        <Icon className="w-5 h-5" />
      </div>
      <div>
        <p className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] mb-1">{label}</p>
        <p className="text-2xl font-thin text-[var(--text-main)] leading-none tracking-tight">{value}</p>
      </div>
    </div>
  );
}
