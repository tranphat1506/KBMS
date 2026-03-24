import { useEffect, useState } from 'react';
import { useKbmsStore } from '../store/kbmsStore';
import { 
  Activity, ShieldAlert, FileText, CheckCircle2, AlertCircle, Clock, 
  Terminal, Server, Database, AlertTriangle, ExternalLink, X,
  Wrench, Bug, Info, XCircle
} from 'lucide-react';
import UserManagement from './management/UserManagement';
import LogAnalyzer from './management/LogAnalyzer';
import ServerSettings from './management/ServerSettings';
import ActiveSessions from './management/ActiveSessions';

export default function SystemManagement() {
  const { 
    systemLogs, 
    auditLogs, 
    systemStats,
    status, 
    subscribeLogs,
    refreshStats,
    refreshSessions,
    fetchSystemLogs, 
    fetchAuditLogs,
    fetchSystemUsers,
    fetchSettings,
    systemActiveTab,
    logTest
  } = useKbmsStore();

  const [selectedLog, setSelectedLog] = useState<any>(null);
  const [overviewLogLevel, setOverviewLogLevel] = useState<string>('');

  const activeTab = systemActiveTab;

  useEffect(() => {
    if (status === 'connected') {
      subscribeLogs();
      
      if (activeTab === 'overview') {
        fetchSystemLogs({ limit: 10, logLevel: overviewLogLevel });
        fetchAuditLogs({ limit: 10 });
        refreshStats();
      } else if (activeTab === 'sessions') {
        refreshSessions();
      } else if (activeTab === 'users') {
        fetchSystemUsers();
      } else if (activeTab === 'settings') {
        fetchSettings();
      }
      
      if (activeTab === 'overview' || activeTab === 'sessions') {
        const interval = setInterval(() => {
            if (activeTab === 'overview') refreshStats();
            refreshSessions();
        }, 5000);
        return () => clearInterval(interval);
      }
    }
  }, [status, activeTab, fetchSystemLogs, fetchAuditLogs, refreshStats, refreshSessions, fetchSystemUsers, fetchSettings, subscribeLogs, overviewLogLevel]);

  const statsList = [
    { label: 'Uptime', value: systemStats?.Uptime || '...', icon: Clock, color: 'text-[var(--brand-primary)]', bg: 'bg-[var(--brand-primary-light)]/20' },
    { label: 'Engine', value: systemStats?.EngineVersion || '3.1.0-beta', icon: Server, color: 'text-[var(--brand-primary)]', bg: 'bg-[var(--brand-primary-light)]/10' },
    { label: 'Memory', value: systemStats ? `${systemStats.MemoryMb} MB` : '...', icon: Database, color: 'text-[var(--brand-primary)]', bg: 'bg-[var(--brand-primary-light)]/20' },
    { label: 'Sessions', value: systemStats?.ActiveSessions ?? '...', icon: Activity, color: 'text-amber-500', bg: 'bg-amber-500/10' },
  ];

  if (status !== 'connected') {
    return (
      <div className="flex flex-col items-center justify-center h-full bg-[var(--bg-app)] text-[var(--text-muted)] animate-in fade-in transition-colors duration-200">
        <Server className="w-12 h-12 mb-4 opacity-20" />
        <p className="text-sm font-medium">Connect to a server to see system metrics.</p>
      </div>
    );
  }

  const tabTitles: Record<string, string> = {
    overview: 'System Overview',
    sessions: 'Active Sessions',
    users: 'User Management',
    logs: 'Log Analyzer',
    settings: 'Server Settings',
    debug: 'DEBUG Tool'
  };


  return (
    <div className="flex-1 flex flex-col h-full bg-[var(--bg-app)] overflow-hidden select-none transition-colors duration-200 relative">
      <div className="flex-1 overflow-y-auto p-10 custom-scrollbar bg-[var(--bg-surface-alt)]/50">
        <div className="mb-10">
           <h1 className="text-2xl font-bold text-[var(--text-main)] tracking-tight uppercase">{tabTitles[activeTab] || 'System Management'}</h1>
           <div className="h-0.5 w-8 bg-[var(--brand-primary)] mt-1" />
        </div>

        {activeTab === 'overview' && (
          <div className="space-y-8 animate-in fade-in duration-300">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {statsList.map((stat, i) => (
                <div key={i} className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] hover:border-[var(--brand-primary)]/50 transition-all group min-h-[110px] flex flex-col shadow-sm">
                  <div className="flex items-start justify-between gap-x-3">
                    <div className="min-w-0 flex-1">
                      <p className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] mb-2">{stat.label}</p>
                      <p className="text-2xl font-thin text-[var(--text-main)] break-words leading-tight tracking-tight">{stat.value}</p>
                    </div>
                    <div className={`p-2 rounded transition-all flex-shrink-0 ${stat.bg} ${stat.color} shadow-sm`}>
                      <stat.icon className="w-4 h-4" />
                    </div>
                  </div>
                </div>
              ))}
            </div>

            <div className="grid grid-cols-1 xl:grid-cols-2 gap-8 pb-10">
              <OverviewLogSection 
                title="Recent Server Logs" 
                icon={FileText} 
                logs={systemLogs} 
                type="system" 
                onSelect={setSelectedLog} 
                filterValue={overviewLogLevel}
                onFilterChange={setOverviewLogLevel}
              />
              <OverviewLogSection 
                title="Audit Trails" 
                icon={ShieldAlert} 
                logs={auditLogs} 
                type="audit" 
                onSelect={setSelectedLog} 
              />
            </div>
          </div>
        )}

        {activeTab === 'sessions' && <div className="animate-in slide-in-from-bottom-2 duration-300"><ActiveSessions /></div>}
        {activeTab === 'users' && <div className="animate-in slide-in-from-bottom-2 duration-300"><UserManagement /></div>}
        {activeTab === 'logs' && <div className="h-full animate-in slide-in-from-bottom-2 duration-300"><LogAnalyzer onSelect={setSelectedLog} /></div>}
        {activeTab === 'settings' && <div className="animate-in slide-in-from-bottom-2 duration-300"><ServerSettings /></div>}
        
        {activeTab === 'debug' && (
          <div className="space-y-10 animate-in fade-in duration-300 max-w-4xl pb-10">
            <section className="bg-[var(--bg-surface)] p-8 rounded-lg border border-[var(--border-subtle)] shadow-sm">
              <div className="flex items-center mb-6">
                <Wrench className="w-5 h-5 text-[var(--brand-primary)] mr-3" />
                <h2 className="text-base font-bold text-[var(--text-main)] uppercase tracking-widest">Telemetry Test Suite</h2>
              </div>
              <p className="text-xs text-[var(--text-muted)] font-thin mb-8 leading-relaxed max-w-2xl">
                Execute manual diagnostic log events to verify real-time monitoring delivery, enriched metadata capture, and UI severity color orchestration across the administrator dashboard.
              </p>
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                <DiagButton icon={Bug} color="text-sky-500" bg="bg-sky-500/10" label="Test Debug" onClick={() => logTest('DEBUG', 'DIAGNOSTIC: Kernel thread pool reallocation check successful.')} />
                <DiagButton icon={Info} color="text-[var(--brand-primary)]" bg="bg-[var(--brand-primary-light)]/20" label="Test Info" onClick={() => logTest('INFO', 'DIAGNOSTIC: System telemetry heartbeat verified.')} />
                <DiagButton icon={AlertTriangle} color="text-amber-500" bg="bg-amber-500/10" label="Test Warning" onClick={() => logTest('WARN', 'DIAGNOSTIC: Buffer pool high-watermark approach simulated.')} />
                <DiagButton icon={XCircle} color="text-rose-500" bg="bg-rose-500/10" label="Test Error" onClick={() => logTest('ERROR', 'DIAGNOSTIC: Simulated I/O timeout exception for diagnostic verification.')} />
              </div>
            </section>
          </div>
        )}
      </div>

      {selectedLog && <LogDetailModal log={selectedLog} onClose={() => setSelectedLog(null)} />}
    </div>
  );
}


function OverviewLogSection({ title, icon: Icon, logs, type, onSelect, filterValue, onFilterChange }: any) {
  // Local filtering for real-time logs that bypass server search
  const filteredLogs = type === 'system' && filterValue
    ? logs.filter((l: any) => {
        const lvl = (l.level || l.Level || '').toUpperCase();
        if (filterValue === 'INFO') return lvl.startsWith('INFO');
        if (filterValue === 'WARN') return lvl.startsWith('WARN');
        if (filterValue === 'ERROR') return lvl.startsWith('ERR');
        return true;
      })
    : logs;

  return (
    <section className="flex flex-col h-[450px] bg-[var(--bg-surface)] rounded-lg border border-[var(--border-subtle)] overflow-hidden hover:border-[var(--brand-primary)]/30 transition-colors shadow-sm">
      <div className="p-4 border-b border-[var(--border-muted)] flex items-center justify-between shrink-0 bg-[var(--bg-surface-alt)]/50">
        <div className="flex items-center">
          <Icon className="w-3.5 h-3.5 text-[var(--brand-primary)] mr-2" />
          <h3 className="text-xs font-bold text-[var(--text-main)] uppercase tracking-widest">{title}</h3>
        </div>
        {type === 'system' && onFilterChange && (
          <select 
            value={filterValue}
            onChange={(e) => onFilterChange(e.target.value)}
            className="text-[9px] bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded px-2 py-1 outline-none text-[var(--text-muted)] font-thin uppercase tracking-tighter cursor-pointer hover:border-[var(--brand-primary)]/50 transition-all"
          >
            <option value="">All Levels</option>
            <option value="INFO">Information</option>
            <option value="WARN">Warning</option>
            <option value="ERROR">Error</option>
          </select>
        )}
      </div>
      <div className="flex-1 overflow-y-auto p-4 bg-[var(--bg-surface)] space-y-2 custom-scrollbar">
        {filteredLogs.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-[var(--text-muted)] italic text-[11px] font-thin">No logs available.</div>
        ) : (
          filteredLogs.slice(0, 10).map((log: any, i: number) => {
            const level = ((log.level || log.Level || 'INFO').toUpperCase());
            const msg = log.message || log.Message || log.command || 'Audit Event';
            const user = log.username || log.Username;
            const ip = log.ip_address || log.IpAddress;
            const comp = log.component || log.Component;
            
            return (
              <button 
                key={i} 
                onClick={() => onSelect(log)}
                className="w-full text-left flex items-start p-2 rounded hover:bg-[var(--bg-surface-alt)]/50 border border-transparent transition-all group cursor-pointer"
              >
                <div className="mt-1">
                  {type === 'system' ? (
                     level === 'ERROR' ? <AlertCircle className="w-3.5 h-3.5 text-rose-500" /> : 
                     level === 'WARN' || level === 'WARNING' ? <AlertTriangle className="w-3.5 h-3.5 text-amber-500" /> :
                     <CheckCircle2 className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
                  ) : <Terminal className="w-3.5 h-3.5 text-[var(--text-muted)]" />}
                </div>
                <div className="ml-3 flex-1 min-w-0">
                  <div className="flex items-center space-x-2 text-[10px]">
                    <span className="font-thin text-[var(--text-muted)]">{new Date(log.timestamp).toLocaleTimeString()}</span>
                    {type === 'audit' && user && (
                      <span className="font-bold text-[var(--brand-primary-text)] bg-[var(--brand-primary-light)]/20 px-1.5 py-0.5 rounded border border-[var(--brand-primary)]/20 tracking-tighter shadow-sm">{user}</span>
                    )}
                    {type === 'audit' && ip && (
                      <span className="font-thin text-[var(--text-muted)] text-[9px] opacity-60">@{ip}</span>
                    )}
                    {type === 'system' && comp && (
                      <span className="font-bold text-[var(--text-muted)] uppercase tracking-tighter bg-[var(--bg-surface-alt)] px-1.5 py-0.5 rounded border border-[var(--border-subtle)] shadow-sm">{comp}</span>
                    )}
                  </div>
                  <p className="text-xs text-[var(--text-sub)] font-thin truncate mt-1.5 group-hover:text-[var(--text-main)] transition-colors">{msg}</p>
                </div>
                <ExternalLink className="w-3 h-3 text-[var(--text-muted)] opacity-0 group-hover:opacity-40 transition-opacity mt-1" />
              </button>
            );
          })
        )}
      </div>
    </section>
  );
}

function LogDetailModal({ log, onClose }: { log: any, onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-6 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-[var(--bg-surface)] w-full max-w-2xl max-h-[80vh] rounded-lg border border-[var(--border-subtle)] shadow-2xl flex flex-col overflow-hidden animate-in zoom-in-95 duration-200">
        <div className="p-4 border-b border-[var(--border-muted)] flex items-center justify-between bg-[var(--bg-surface-alt)]/50">
          <div className="flex items-center">
            <FileText className="w-4 h-4 text-[var(--brand-primary)] mr-2" />
            <h3 className="text-xs font-bold text-[var(--text-main)] uppercase tracking-widest">Entry Inspection</h3>
          </div>
          <button onClick={onClose} className="p-1 hover:bg-rose-500/10 text-[var(--text-muted)] hover:text-rose-500 rounded transition-all">
            <X className="w-4 h-4" />
          </button>
        </div>
        <div className="flex-1 overflow-y-auto p-6 bg-[var(--bg-surface)]">
          <div className="space-y-6">
            <div className="grid grid-cols-2 gap-4">
              <DetailField label="Timestamp" value={log.timestamp} />
              <DetailField label="Level/Status" value={log.level || log.status || 'INFO'} highlight={log.level === 'Error' || log.status === 'FAIL'} />
              {log.username && <DetailField label="User" value={log.username} />}
              {log.ip_address && <DetailField label="Source IP" value={log.ip_address} />}
              {log.role && <DetailField label="Role" value={log.role} />}
              {log.duration_ms !== undefined && <DetailField label="Execution" value={`${log.duration_ms.toFixed(2)}ms`} />}
            </div>
            
            <div className="space-y-2">
              <p className="text-[9px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Full Payload</p>
              <div className="bg-[var(--bg-surface-alt)] p-4 rounded border border-[var(--border-subtle)] overflow-x-auto shadow-inner">
                <pre className="text-[11px] font-mono text-[var(--text-main)] leading-relaxed">{JSON.stringify(log, null, 2)}</pre>
              </div>
            </div>

            {log.stack_trace && (
              <div className="space-y-2">
                <p className="text-[9px] font-bold text-rose-500 uppercase tracking-widest">Stack Trace</p>
                <div className="bg-rose-500/5 p-4 rounded border border-rose-500/20 overflow-x-auto">
                  <pre className="text-[10px] font-mono text-rose-600 leading-relaxed whitespace-pre-wrap">{log.stack_trace}</pre>
                </div>
              </div>
            )}
          </div>
        </div>
        <div className="p-4 border-t border-[var(--border-muted)] bg-[var(--bg-surface-alt)]/30 flex justify-end">
          <button onClick={onClose} className="px-5 py-2 text-[10px] font-bold uppercase tracking-widest border border-[var(--border-subtle)] rounded hover:bg-[var(--bg-surface-alt)] text-[var(--text-sub)] transition-all">Close</button>
        </div>
      </div>
    </div>
  );
}

function DetailField({ label, value, highlight }: { label: string, value: string, highlight?: boolean }) {
  return (
    <div className="space-y-1">
      <p className="text-[9px] font-bold text-[var(--text-muted)] uppercase tracking-[0.15em]">{label}</p>
      <p className={`text-xs font-thin break-words ${highlight ? 'text-rose-500 font-bold' : 'text-[var(--text-main)]'}`}>{value}</p>
    </div>
  );
}

function DiagButton({ icon: Icon, color, bg, label, onClick }: any) {
  return (
    <button 
      onClick={onClick}
      className={`flex flex-col items-center justify-center p-5 rounded-lg border border-transparent hover:border-current transition-all group ${bg} ${color} cursor-pointer active:scale-95`}
    >
      <Icon className="w-6 h-6 mb-3 group-hover:scale-110 transition-transform" />
      <span className="text-[10px] font-bold uppercase tracking-widest">{label}</span>
    </button>
  );
}
