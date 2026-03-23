import { useEffect } from 'react';
import { useKbmsStore } from '../store/kbmsStore';
import { 
  Activity, ShieldAlert, FileText, CheckCircle2, AlertCircle, Clock, 
  Terminal, Server, Database
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
    systemActiveTab
  } = useKbmsStore();

  const activeTab = systemActiveTab;

  useEffect(() => {
    if (status === 'connected') {
      // Start real-time log stream
      subscribeLogs();
      
      // Initial loads
      if (activeTab === 'overview') {
        fetchSystemLogs();
        fetchAuditLogs();
        refreshStats();
      } else if (activeTab === 'sessions') {
        refreshSessions();
      } else if (activeTab === 'users') {
        fetchSystemUsers();
      } else if (activeTab === 'logs') {
        // Log analyzer handles its own initial fetch
      } else if (activeTab === 'settings') {
        fetchSettings();
      }
      
      // Periodic stats refresh only on overview
      if (activeTab === 'overview' || activeTab === 'sessions') {
        const interval = setInterval(() => {
            if (activeTab === 'overview') refreshStats();
            refreshSessions();
        }, 5000);
        return () => clearInterval(interval);
      }
    }
  }, [status, activeTab, fetchSystemLogs, fetchAuditLogs, refreshStats, refreshSessions, fetchSystemUsers, fetchSettings, subscribeLogs]);

  const statsList = [
    { label: 'Uptime', value: systemStats?.Uptime || '...', icon: Clock, color: 'text-[var(--brand-primary)]', bg: 'bg-[var(--brand-primary-light)]/20' },
    { label: 'Engine', value: systemStats?.EngineVersion || '3.1.0', icon: Server, color: 'text-[var(--brand-primary)]', bg: 'bg-[var(--brand-primary-light)]/10' },
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
    settings: 'Server Settings'
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[var(--bg-app)] overflow-hidden select-none transition-colors duration-200">
      <div className="flex-1 overflow-y-auto p-10 custom-scrollbar bg-[var(--bg-surface-alt)]/50">
        <div className="mb-10">
           <h1 className="text-2xl font-bold text-[var(--text-main)] tracking-tight uppercase">{tabTitles[activeTab] || 'System Management'}</h1>
           <div className="h-0.5 w-8 bg-[var(--brand-primary)] mt-1" />
        </div>

        {activeTab === 'overview' && (
          <div className="space-y-8 animate-in fade-in duration-300">
            {/* Quick Stats Grid */}
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

            {/* Dashboard Content */}
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-8 pb-10">
              <OverviewLogSection title="Recent Server Logs" icon={FileText} logs={systemLogs} type="system" />
              <OverviewLogSection title="Audit Trails" icon={ShieldAlert} logs={auditLogs} type="audit" />
            </div>
          </div>
        )}

        {activeTab === 'sessions' && <div className="animate-in slide-in-from-bottom-2 duration-300"><ActiveSessions /></div>}
        {activeTab === 'users' && <div className="animate-in slide-in-from-bottom-2 duration-300"><UserManagement /></div>}
        {activeTab === 'logs' && <div className="h-full animate-in slide-in-from-bottom-2 duration-300"><LogAnalyzer /></div>}
        {activeTab === 'settings' && <div className="animate-in slide-in-from-bottom-2 duration-300"><ServerSettings /></div>}
      </div>
    </div>
  );
}

function OverviewLogSection({ title, icon: Icon, logs, type }: any) {
  return (
    <section className="flex flex-col h-[450px] bg-[var(--bg-surface)] rounded-lg border border-[var(--border-subtle)] overflow-hidden hover:border-[var(--brand-primary)]/30 transition-colors shadow-sm">
      <div className="p-4 border-b border-[var(--border-muted)] flex items-center justify-between shrink-0 bg-[var(--bg-surface-alt)]/50">
        <div className="flex items-center">
          <Icon className="w-3.5 h-3.5 text-[var(--brand-primary)] mr-2" />
          <h3 className="text-xs font-bold text-[var(--text-main)] uppercase tracking-widest">{title}</h3>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto p-4 bg-[var(--bg-surface)] space-y-2 custom-scrollbar">
        {logs.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-[var(--text-muted)] italic text-[11px] font-thin">No logs available.</div>
        ) : (
          logs.slice(0, 30).map((log: any, i: number) => {
            const level = ((log.level || log.Level || 'INFO').toUpperCase());
            const msg = log.message || log.Message || log.command || 'Audit Event';
            return (
              <div key={i} className="flex items-start p-2 rounded hover:bg-[var(--bg-surface-alt)]/50 border border-transparent transition-all group">
                <div className="mt-1">
                  {type === 'system' ? (
                     level === 'ERROR' ? <AlertCircle className="w-3.5 h-3.5 text-rose-500" /> : <CheckCircle2 className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
                  ) : <Terminal className="w-3.5 h-3.5 text-[var(--text-muted)]" />}
                </div>
                <div className="ml-3 flex-1 min-w-0">
                  <div className="flex items-center space-x-2">
                    <span className="text-[9px] font-thin text-[var(--text-muted)]">{new Date(log.timestamp).toLocaleTimeString()}</span>
                    {type === 'system' && <span className={`text-[8px] font-bold px-1.5 py-0.5 rounded border uppercase tracking-tighter ${level === 'ERROR' ? 'text-rose-500 border-rose-500/20 bg-rose-500/5' : 'text-[var(--brand-primary)] border-[var(--brand-primary)]/20 bg-[var(--brand-primary-light)]/20 shadow-sm'}`}>{level}</span>}
                  </div>
                  <p className="text-xs text-[var(--text-sub)] font-thin break-words mt-1">{msg}</p>
                </div>
              </div>
            );
          })
        )}
      </div>
    </section>
  );
}
