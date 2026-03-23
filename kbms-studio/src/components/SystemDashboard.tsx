import { useEffect } from 'react';
import { useKbmsStore } from '../store/kbmsStore';
import { 
  Activity, ShieldAlert, FileText, CheckCircle2, AlertCircle, Clock, 
  Terminal, Server, RefreshCw, Cpu, HardDrive, Database
} from 'lucide-react';

export default function SystemDashboard() {
  const { 
    systemLogs, 
    auditLogs, 
    status, 
    fetchSystemLogs, 
    fetchAuditLogs 
  } = useKbmsStore();

  useEffect(() => {
    if (status === 'connected') {
      fetchSystemLogs();
      fetchAuditLogs();
    }
  }, [status, fetchSystemLogs, fetchAuditLogs]);

  const stats = [
    { label: 'Uptime', value: '2d 14h 22m', icon: Clock, color: 'text-emerald-600' },
    { label: 'CPU Usage', value: '4.2%', icon: Cpu, color: 'text-sky-600' },
    { label: 'Memory', value: '128MB / 1GB', icon: Database, color: 'text-indigo-600' },
    { label: 'Storage', value: '1.2GB Free', icon: HardDrive, color: 'text-amber-600' },
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
            onClick={() => { fetchSystemLogs(); fetchAuditLogs(); }}
            className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-50 text-emerald-700 rounded border border-emerald-200 hover:bg-emerald-100 transition-colors text-xs font-medium cursor-pointer"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            <span>Refresh All</span>
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-6 space-y-6 custom-scrollbar">
        {/* Quick Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {stats.map((stat, i) => (
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
                systemLogs.map((log, i) => (
                  <div key={i} className="flex items-start p-2 rounded hover:bg-white border border-transparent hover:border-slate-200 transition-all group">
                    <div className="mt-0.5">
                      {log.Level === 'ERROR' ? <AlertCircle className="w-3 h-3 text-rose-500" /> : 
                       log.Level === 'WARN' ? <AlertCircle className="w-3 h-3 text-amber-500" /> : 
                       <CheckCircle2 className="w-3 h-3 text-emerald-500" />}
                    </div>
                    <div className="ml-2 flex-1 min-w-0">
                      <div className="flex items-center space-x-2">
                        <span className="text-[10px] font-mono text-slate-400">{log.Timestamp ? new Date(log.Timestamp).toLocaleTimeString() : '--:--:--'}</span>
                        <span className={`text-[9px] font-bold px-1 rounded border ${
                          log.Level === 'ERROR' ? 'text-rose-600 bg-rose-50 border-rose-100' : 
                          log.Level === 'WARN' ? 'text-amber-600 bg-amber-50 border-amber-100' : 
                          'text-emerald-600 bg-emerald-50 border-emerald-100'
                        }`}>{log.Level}</span>
                      </div>
                      <p className="text-[11px] text-slate-700 font-medium break-words mt-0.5">{log.Message}</p>
                    </div>
                  </div>
                )).reverse()
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
                auditLogs.map((audit, i) => (
                  <div key={i} className="flex flex-col p-2.5 rounded bg-white border border-slate-200/50 shadow-sm hover:border-emerald-200 transition-all">
                    <div className="flex items-center justify-between mb-1.5">
                      <div className="flex items-center space-x-2">
                        <Terminal className="w-3 h-3 text-slate-400" />
                        <span className="text-[10px] font-bold text-slate-700 uppercase tracking-tighter">{audit.Operation}</span>
                      </div>
                      <span className="text-[9px] text-slate-400 font-medium">{audit.Timestamp ? new Date(audit.Timestamp).toLocaleString() : ''}</span>
                    </div>
                    <div className="flex items-center space-x-1.5 text-[10px]">
                      <span className="text-slate-400">User:</span>
                      <span className="font-bold text-indigo-600">{audit.User || 'anonymous'}</span>
                      <span className="text-slate-200 ml-1">|</span>
                      <span className="text-slate-400 ml-1">IP:</span>
                      <span className="text-slate-600 font-mono tracking-tighter">{audit.IpAddress || 'local'}</span>
                    </div>
                    {audit.Details && (
                      <div className="mt-2 p-1.5 bg-slate-100/50 rounded text-[9px] font-mono text-slate-500 truncate max-w-full">
                        {audit.Details}
                      </div>
                    )}
                  </div>
                )).reverse()
              )}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
