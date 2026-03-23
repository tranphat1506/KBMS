import { useState, useEffect } from 'react';
import { useKbmsStore } from '../../store/kbmsStore';
import { Search, Filter, Calendar, User, Tag, AlertCircle, Info, Download } from 'lucide-react';

export default function LogAnalyzer() {
  const { systemLogs, auditLogs, fetchSystemLogs, fetchAuditLogs } = useKbmsStore();
  const [logType, setLogType] = useState<'system' | 'audit'>('system');
  const [filters, setFilters] = useState({
    userFilter: '',
    logLevel: '',
    startTime: '',
    endTime: ''
  });

  const handleSearch = () => {
    if (logType === 'system') {
      fetchSystemLogs(filters);
    } else {
      fetchAuditLogs(filters);
    }
  };

  useEffect(() => {
    handleSearch();
  }, [logType]);

  const currentLogs = logType === 'system' ? systemLogs : auditLogs;

  return (
    <div className="flex flex-col h-full space-y-6 overflow-hidden animate-in fade-in duration-300">
      {/* Search & Filter Bar */}
      <div className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] space-y-6 shrink-0 transition-colors shadow-sm">
        <div className="flex items-center space-x-6">
          <div className="flex bg-[var(--bg-app)] p-1 rounded border border-[var(--border-subtle)]">
            <button 
              onClick={() => setLogType('system')}
              className={`px-4 py-1.5 text-xs font-thin rounded transition-all ${logType === 'system' ? 'bg-[var(--bg-surface)] text-[var(--brand-primary)] border border-[var(--border-muted)] shadow-sm' : 'text-[var(--text-sub)] hover:text-[var(--text-main)]'}`}
            >
              System Events
            </button>
            <button 
              onClick={() => setLogType('audit')}
              className={`px-4 py-1.5 text-xs font-thin rounded transition-all ${logType === 'audit' ? 'bg-[var(--bg-surface)] text-[var(--brand-primary)] border border-[var(--border-muted)] shadow-sm' : 'text-[var(--text-sub)] hover:text-[var(--text-main)]'}`}
            >
              Audit Registry
            </button>
          </div>

          <div className="flex-1 flex items-center bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded px-4 py-2 focus-within:border-[var(--brand-primary)] transition-all">
            <Search className="w-4 h-4 text-[var(--text-muted)] mr-3" />
            <input 
              type="text" 
              placeholder={logType === 'system' ? "Query event messages..." : "Filter subject or operation..."}
              className="bg-transparent border-none outline-none text-xs w-full text-[var(--text-main)] font-thin placeholder-[var(--text-muted)]/50"
              value={filters.userFilter}
              onChange={e => setFilters({...filters, userFilter: e.target.value})}
              onKeyDown={e => e.key === 'Enter' && handleSearch()}
            />
          </div>

          <button 
            onClick={handleSearch}
            className="px-8 py-2 bg-[var(--brand-primary)] text-white rounded text-xs font-thin hover:bg-[var(--brand-primary-hover)] transition-colors border border-[var(--brand-primary-hover)] shadow-sm"
          >
            Execute Query
          </button>
        </div>

        <div className="flex flex-wrap items-center gap-6 pt-4 border-t border-[var(--border-muted)]">
          <div className="flex items-center space-x-3">
            <Calendar className="w-3.5 h-3.5 text-[var(--text-muted)]" />
            <input 
              type="datetime-local" 
              className="text-[10px] bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded px-2.5 py-1.5 outline-none focus:border-[var(--brand-primary)] font-thin text-[var(--text-sub)] transition-all"
              onChange={e => setFilters({...filters, startTime: e.target.value})}
            />
            <span className="text-[10px] text-[var(--text-muted)] font-thin">thru</span>
            <input 
              type="datetime-local" 
              className="text-[10px] bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded px-2.5 py-1.5 outline-none focus:border-[var(--brand-primary)] font-thin text-[var(--text-sub)] transition-all"
              onChange={e => setFilters({...filters, endTime: e.target.value})}
            />
          </div>

          {logType === 'system' && (
            <div className="flex items-center space-x-3">
              <Tag className="w-3.5 h-3.5 text-[var(--text-muted)]" />
              <select 
                className="text-[10px] bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded px-2.5 py-1.5 outline-none focus:border-[var(--brand-primary)] font-thin text-[var(--text-main)] appearance-none transition-all"
                value={filters.logLevel}
                onChange={e => setFilters({...filters, logLevel: e.target.value})}
              >
                <option value="">All Severity Levels</option>
                <option value="INFO">Information</option>
                <option value="WARN">Warning</option>
                <option value="ERROR">Critical Error</option>
              </select>
            </div>
          )}

          <div className="ml-auto">
             <button className="flex items-center space-x-2 px-4 py-1.5 text-[var(--text-sub)] hover:text-[var(--brand-primary)] text-[10px] font-thin border border-[var(--border-subtle)] rounded hover:bg-[var(--bg-surface-alt)] transition-all">
                <Download className="w-3.5 h-3.5" />
                <span>Export Registry</span>
             </button>
          </div>
        </div>
      </div>

      {/* Log List */}
      <div className="flex-1 bg-[var(--bg-surface)] rounded-lg border border-[var(--border-subtle)] overflow-hidden flex flex-col min-h-0 shadow-inner">
        <div className="flex-1 overflow-y-auto p-4 space-y-2 custom-scrollbar bg-[var(--bg-surface-alt)]/20">
          {currentLogs.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-[var(--text-muted)] italic text-xs py-20 font-thin">
              <Filter className="w-10 h-10 mb-4 opacity-10" />
              <p>Requested query returned zero results.</p>
            </div>
          ) : (
            currentLogs.map((log: any, i: number) => (
              <LogEntry key={`${i}-${log.timestamp}`} log={log} type={logType} />
            ))
          )}
        </div>
      </div>
    </div>
  );
}

function LogEntry({ log, type }: { log: any; type: 'system' | 'audit' }) {
  if (type === 'system') {
    const level = (log.level || 'INFO').toUpperCase();
    return (
      <div className="flex items-start p-4 bg-[var(--bg-surface)] rounded border border-[var(--border-muted)] group hover:border-[var(--brand-primary)]/20 transition-all shadow-sm">
        <div className="mt-1 shrink-0">
          {level === 'ERROR' ? <AlertCircle className="w-4 h-4 text-rose-500" /> : 
           level === 'WARN' ? <AlertCircle className="w-4 h-4 text-amber-500" /> : 
           <Info className="w-4 h-4 text-[var(--brand-primary)]" />}
        </div>
        <div className="ml-4 flex-1 min-w-0">
          <div className="flex items-center space-x-4 mb-1.5">
            <span className="text-[10px] font-thin font-mono text-[var(--text-muted)] tracking-tighter">{new Date(log.timestamp).toLocaleString()}</span>
            <span className={`text-[8px] font-thin px-1.5 py-0.5 rounded border uppercase tracking-[0.2em] ${
              level === 'ERROR' ? 'text-rose-600 border-rose-100 shadow-sm' : 
              level === 'WARN' ? 'text-amber-600 border-amber-100 shadow-sm' : 
              'text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20 shadow-sm'
            }`}>{level}</span>
          </div>
          <p className="text-[11px] text-[var(--text-sub)] leading-relaxed font-thin break-words">{log.message}</p>
        </div>
      </div>
    );
  }

  // Audit Entry
  const status = (log.status || 'UNKNOWN').toUpperCase();
  return (
    <div className="flex flex-col p-4 bg-[var(--bg-surface)] rounded border border-[var(--border-muted)] hover:border-[var(--brand-primary)]/20 transition-all shadow-sm">
      <div className="flex items-center justify-between mb-2.5">
        <div className="flex items-center space-x-3">
          <div className={`w-1.5 h-1.5 rounded-full ${status === 'SUCCESS' ? 'bg-[var(--brand-primary)] shadow-[0_0_8px_var(--brand-primary)]' : 'bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.4)]'}`} />
          <span className="text-[10px] font-thin text-[var(--text-main)] uppercase tracking-widest">{log.command}</span>
          <span className={`text-[8px] px-2 font-thin rounded border uppercase tracking-widest ${status === 'SUCCESS' ? 'text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20' : 'text-rose-600 border-rose-100'}`}>
            {status}
          </span>
        </div>
        <span className="text-[10px] font-thin font-mono text-[var(--text-muted)]">{new Date(log.timestamp).toLocaleString()}</span>
      </div>
      <div className="flex items-center text-[10px] text-[var(--text-muted)] font-thin italic">
        <User className="w-3 h-3 mr-2 text-[var(--text-muted)]/50" />
        <span className="text-[var(--brand-primary)] mr-4 font-normal">{log.username}</span>
        <span className="text-[var(--border-muted)] mr-4">/</span>
        <span className="text-[var(--text-muted)] mr-2">SOURCE:</span>
        <span className="text-[var(--text-main)] font-mono tracking-tighter">{log.ip_address}</span>
      </div>
    </div>
  );
}
