import { useEffect } from 'react';
import { useKbmsStore } from '../store/kbmsStore';
import { 
  Activity, ShieldAlert, FileText, CheckCircle2, AlertCircle, Clock, 
  Terminal, Server, RefreshCw, Database, Users
} from 'lucide-react';

export default function SystemDashboard() {
  const { 
    systemLogs, 
    auditLogs, 
    systemStats,
    systemSessions,
    status, 
    subscribeLogs,
    refreshStats,
    refreshSessions,
    killSession,
    fetchSystemLogs, 
    fetchAuditLogs 
  } = useKbmsStore();

  useEffect(() => {
    if (status === 'connected') {
      // Start real-time log stream
      subscribeLogs();
      
      // Fallback/Initial load from DB
      fetchSystemLogs();
      fetchAuditLogs();
      
      // Periodic stats refresh (every 5 seconds)
      const interval = setInterval(() => {
        refreshStats();
        refreshSessions();
      }, 5000);

      return () => clearInterval(interval);
    }
  }, [status, subscribeLogs, fetchSystemLogs, fetchAuditLogs, refreshStats, refreshSessions]);

  const statsList = [
    { label: 'Uptime', value: systemStats?.Uptime || '...', icon: Clock, color: 'text-emerald-600' },
    { label: 'Engine', value: systemStats?.EngineVersion || '3.1.0', icon: Server, color: 'text-sky-600' },
    { label: 'Memory', value: systemStats ? `${systemStats.MemoryMb} MB` : '...', icon: Database, color: 'text-indigo-600' },
    { label: 'Sessions', value: systemStats?.ActiveSessions ?? '...', icon: Activity, color: 'text-amber-600' },
  ];

  if (status !== 'connected') {
    return (
      <div className="flex flex-col items-center justify-center h-full bg-slate-50 text-slate-400">
        <Server className="w-12 h-12 mb-4 opacity-20" />
        <p className="text-sm font-medium">Connect to a server to see system metrics.</p>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col h-full bg-[#f8fafc] overflow-hidden select-none">
      {/* Header */}
      <div className="h-14 border-b border-slate-200 bg-white flex items-center px-6 shrink-0">
        <Activity className="w-5 h-5 text-emerald-600 mr-3" />
        <div>
          <h2 className="text-sm font-bold text-slate-800 leading-tight">System Dashboard</h2>
          <p className="text-[10px] text-slate-500 font-medium uppercase tracking-wider">Internal Monitoring & Server Logs</p>
        </div>
        <div className="ml-auto flex items-center space-x-2">
          <button 
            onClick={() => { fetchSystemLogs(); fetchAuditLogs(); refreshStats(); refreshSessions(); }}
            className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-50 text-emerald-700 rounded border border-emerald-200 hover:bg-emerald-100 transition-colors text-xs font-medium cursor-pointer"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            <span>Sync All</span>
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-6 space-y-6 custom-scrollbar">
        {/* Quick Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {statsList.map((stat, i) => (
            <div key={i} className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm hover:shadow-md transition-shadow group">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">{stat.label}</p>
                  <p className="text-lg font-bold text-slate-800">{stat.value}</p>
                </div>
                <div className={`p-2 rounded-lg bg-slate-50 group-hover:bg-slate-100 transition-colors ${stat.color}`}>
                  <stat.icon className="w-4 h-4" />
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Dashboard Content */}
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
          {/* Recent System Logs */}
          <section className="flex flex-col h-[400px] bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="p-4 border-b border-slate-100 flex items-center justify-between shrink-0">
              <div className="flex items-center">
                <FileText className="w-4 h-4 text-emerald-600 mr-2" />
                <h3 className="text-sm font-bold text-slate-800">Recent Server Logs</h3>
              </div>
              <span className="text-[10px] font-bold text-slate-400 bg-slate-50 px-2 py-0.5 rounded-full border border-slate-100">Live Mirroring</span>
            </div>
            <div className="flex-1 overflow-y-auto p-2 bg-slate-50/50 space-y-1 custom-scrollbar">
              {systemLogs.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-slate-400 italic text-[11px]">
                  No logs available.
                </div>
              ) : (
                  systemLogs.map((log, i) => {
                    const level = (log.level || log.Level || 'INFO').toUpperCase();
                    const timestamp = log.timestamp || log.Timestamp;
                    const message = log.message || log.Message;
                    
                    return (
                      <div key={i} className="flex items-start p-2 rounded hover:bg-white border border-transparent hover:border-slate-200 transition-all group">
                        <div className="mt-0.5">
                          {level === 'ERROR' ? <AlertCircle className="w-3 h-3 text-rose-500" /> : 
                           level === 'WARN' ? <AlertCircle className="w-3 h-3 text-amber-500" /> : 
                           <CheckCircle2 className="w-3 h-3 text-emerald-500" />}
                        </div>
                        <div className="ml-2 flex-1 min-w-0">
                          <div className="flex items-center space-x-2">
                            <span className="text-[10px] font-mono text-slate-400">{timestamp ? new Date(timestamp).toLocaleTimeString() : '--:--:--'}</span>
                            <span className={`text-[9px] font-bold px-1 rounded border ${
                              level === 'ERROR' ? 'text-rose-600 bg-rose-50 border-rose-100' : 
                              level === 'WARN' ? 'text-amber-600 bg-amber-50 border-amber-100' : 
                              'text-emerald-600 bg-emerald-50 border-emerald-100'
                            }`}>{level}</span>
                          </div>
                          <p className="text-[11px] text-slate-700 font-medium break-words mt-0.5">{message}</p>
                        </div>
                      </div>
                    );
                  })
              )}
            </div>
          </section>

          {/* Audit Trails */}
          <section className="flex flex-col h-[400px] bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="p-4 border-b border-slate-100 flex items-center justify-between shrink-0">
              <div className="flex items-center">
                <ShieldAlert className="w-4 h-4 text-emerald-600 mr-2" />
                <h3 className="text-sm font-bold text-slate-800">Security & Audit Trails</h3>
              </div>
              <span className="text-[10px] font-bold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-100">Persistence Enabled</span>
            </div>
            <div className="flex-1 overflow-y-auto p-2 bg-slate-50/50 space-y-1 custom-scrollbar">
              {auditLogs.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-slate-400 italic text-[11px]">
                  No audit entries found.
                </div>
              ) : (
                auditLogs.map((audit, i) => {
                  const operation = audit.command || audit.Operation || 'QUERY';
                  const user = audit.username || audit.User || 'anonymous';
                  const ip = audit.ip_address || audit.IpAddress || 'local';
                  const timestamp = audit.timestamp || audit.Timestamp;
                  const status = audit.status || audit.Status || 'UNKNOWN';

                  return (
                    <div key={i} className="flex flex-col p-2.5 rounded bg-white border border-slate-200/50 shadow-sm hover:border-emerald-200 transition-all">
                      <div className="flex items-center justify-between mb-1.5">
                        <div className="flex items-center space-x-2">
                          <Terminal className="w-3 h-3 text-slate-400" />
                          <span className="text-[10px] font-bold text-slate-700 uppercase tracking-tighter">{operation}</span>
                          <span className={`text-[8px] px-1 rounded-sm ${status === 'SUCCESS' ? 'bg-emerald-50 text-emerald-600' : 'bg-rose-50 text-rose-600'}`}>
                            {status}
                          </span>
                        </div>
                        <span className="text-[9px] text-slate-400 font-medium">{timestamp ? new Date(timestamp).toLocaleString() : ''}</span>
                      </div>
                      <div className="flex items-center space-x-1.5 text-[10px]">
                        <span className="text-slate-400">User:</span>
                        <span className="font-bold text-indigo-600">{user}</span>
                        <span className="text-slate-200 ml-1">|</span>
                        <span className="text-slate-400 ml-1">IP:</span>
                        <span className="text-slate-600 font-mono tracking-tighter">{ip}</span>
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </section>
        </div>

        {/* Active Sessions */}
        <section id="active-sessions-section" className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden mb-6">
          <div className="p-4 border-b border-slate-100 flex items-center justify-between shrink-0">
            <div className="flex items-center">
              <Users className="w-4 h-4 text-emerald-600 mr-2" />
              <h3 className="text-sm font-bold text-slate-800">Active Connections & Management</h3>
            </div>
            <div className="flex items-center space-x-2">
              <span className="text-[10px] font-bold text-slate-400 bg-slate-50 px-2 py-0.5 rounded-full border border-slate-100">
                {systemSessions.length} Online
              </span>
              <button 
                onClick={() => refreshSessions()}
                className="p-1 hover:bg-slate-100 rounded text-slate-500 transition-colors"
              >
                <RefreshCw className="w-3 h-3" />
              </button>
            </div>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-slate-50/50 border-b border-slate-100">
                  <th className="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">User</th>
                  <th className="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">IP Address</th>
                  <th className="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Connected</th>
                  <th className="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {systemSessions.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="px-5 py-8 text-center text-slate-400 text-xs italic">No active sessions found.</td>
                  </tr>
                ) : (
                  systemSessions.map((session, i) => (
                    <tr key={i} className="hover:bg-slate-50/30 transition-colors">
                      <td className="px-5 py-3">
                        <div className="flex items-center space-x-2">
                          <div className="w-2 h-2 rounded-full bg-emerald-500" />
                          <span className="text-[12px] font-semibold text-slate-700">{session.Username || 'Anonymous'}</span>
                        </div>
                      </td>
                      <td className="px-5 py-3 text-[11px] text-slate-500 font-mono">{session.IpAddress}</td>
                      <td className="px-5 py-3 text-[11px] text-slate-500">{new Date(session.ConnectedAt).toLocaleTimeString()}</td>
                      <td className="px-5 py-3 text-right">
                          <button 
                            onClick={() => {
                              if (confirm(`Are you sure you want to terminate session ${session.SessionId}?`)) {
                                  killSession(session.SessionId);
                              }
                            }}
                            className="px-3 py-1 text-[10px] font-bold text-rose-600 hover:bg-rose-50 rounded border border-rose-100 transition-colors cursor-pointer"
                          >
                            Kill
                          </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </div>
  );
}
