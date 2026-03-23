import { useEffect, useState, useRef } from 'react';
import {
  Database, ChevronDown, ChevronRight, Folder, Table, GitBranch, Link, Settings2,
  TerminalSquare, Copy, RefreshCw, AlignLeft, Unplug, Search, Activity, ShieldAlert
} from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function Sidebar() {
  const {
    status,
    metadata,
    fetchMetadata,
    activeSidebarView,
    selectedKb,
    changeKnowledgeBase,
    connectionDetails,
    lastCredentials,
    setQuery,
    execute,
    setConnectModalOpen,
    connect,
    disconnect,
    metadataDetails
  } = useKbmsStore();

  const [expanded, setExpanded] = useState<Record<string, boolean>>({ 'server': true, 'databases': true, 'system': true });
  const [contextMenu, setContextMenu] = useState<{ x: number, y: number, concept: any } | null>(null);
  const [serverContextMenu, setServerContextMenu] = useState<{ x: number, y: number } | null>(null);

  const menuRef = useRef<HTMLDivElement>(null);

  const toggle = (key: string) => {
    setExpanded(prev => ({ ...prev, [key]: !prev[key] }));
  };

  useEffect(() => {
    setExpanded(prev => ({ ...prev, 'server': true, 'databases': true, 'system': true }));
  }, [selectedKb]);

  useEffect(() => {
    if (status === 'connected') {
      fetchMetadata();
    }
  }, [status, fetchMetadata]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setContextMenu(null);
        setServerContextMenu(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleContextMenu = (e: React.MouseEvent, concept: any) => {
    e.preventDefault();
    setServerContextMenu(null);
    setContextMenu({ x: e.pageX, y: e.pageY, concept });
  };

  const handleServerContextMenu = (e: React.MouseEvent) => {
    e.preventDefault();
    setContextMenu(null);
    setServerContextMenu({ x: e.pageX, y: e.pageY });
  };

  const handleAction = (action: 'select' | 'drop' | 'insert') => {
    if (!contextMenu?.concept) return;
    const cName = contextMenu.concept.Name;
    let q = '';

    if (action === 'select') q = `SELECT * FROM ${cName};\n`;
    if (action === 'drop') q = `DROP CONCEPT ${cName};\n`;
    if (action === 'insert') {
      const vars = String(contextMenu.concept.Variables).split(',').map(v => v.trim());
      q = `INSERT INTO ${cName} (${vars.join(', ')})\nVALUES (${vars.map(() => 'NULL').join(', ')});\n`;
    }

    setQuery(q);
    setContextMenu(null);
  };

  const isMac = typeof navigator !== 'undefined' && navigator.userAgent.indexOf('Mac') > -1;
  const cmd = isMac ? '⌘' : 'Ctrl';

  return (
    <div className="h-full flex flex-col pt-3 text-slate-800 relative font-sans">
      <div className="px-3 pb-2.5 flex items-center justify-between">
        <span className="text-[10px] font-semibold text-slate-700 uppercase tracking-wide opacity-90">
          {activeSidebarView === 'explorer' ? 'Object Explorer' : 'System Management'}
        </span>
        <div className="flex items-center space-x-1">
          <button className="text-slate-500 hover:text-emerald-700 p-0.5 rounded hover:bg-emerald-100 transition-colors tooltip cursor-pointer" title="Filter (Coming Soon)">
            <AlignLeft className="w-3.5 h-3.5" />
          </button>
          <button
            onClick={() => {
              if (activeSidebarView === 'explorer') fetchMetadata();
              else {
                useKbmsStore.getState().fetchSystemLogs();
                useKbmsStore.getState().fetchAuditLogs();
              }
            }}
            disabled={status !== 'connected'}
            className="text-slate-500 hover:text-emerald-700 p-0.5 rounded hover:bg-emerald-100 transition-colors disabled:opacity-50 cursor-pointer disabled:cursor-not-allowed"
            title={`Reload ${activeSidebarView === 'explorer' ? 'Metadata' : 'Logs'} (${cmd} + R)`}
          >
            <RefreshCw className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      <div className="py-2 px-3 border-b border-slate-200/60 mb-1.5 relative">
        <div className="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none">
          <Search className="w-3.5 h-3.5 text-slate-400" />
        </div>
        <input
          type="text"
          placeholder={`Search ${activeSidebarView === 'explorer' ? 'objects' : 'logs'}...`}
          disabled={status !== 'connected'}
          className="w-full text-[11px] font-normal box-border pl-7 pr-3 py-1 bg-white border border-slate-300/80 rounded focus:outline-none focus:border-emerald-500 transition-all placeholder:text-slate-400 shadow-inner"
        />
      </div>

      <div className="flex-1 overflow-y-auto px-1.5 py-1 custom-scrollbar">
        {status !== 'connected' ? (
          <div className="flex flex-col items-center justify-center h-full text-slate-400 space-y-3 px-3 text-center mt-[-30px]">
            <Unplug className="w-10 h-10 opacity-30" />
            <p className="text-[11px] font-normal">Not connected to server.</p>
            <button
              onClick={() => setConnectModalOpen(true)}
              className="px-4 py-1.5 bg-white text-emerald-700 font-medium text-[11px] rounded border border-emerald-300 hover:bg-emerald-50 transition-colors shadow-sm cursor-pointer"
            >
              Connect Now
            </button>
          </div>
        ) : activeSidebarView === 'system' ? (
          <div className="space-y-0.5 mt-1 px-1">
            <div className="px-2.5 pb-1.5 pt-1">
               <span className="text-[9px] font-bold text-slate-400 uppercase tracking-widest">Real-Time Monitor</span>
            </div>
            <button className="w-full flex items-center space-x-2 px-2.5 py-2.5 rounded hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 transition-all group cursor-pointer text-[12px] border border-transparent hover:border-emerald-100/50">
              <div className="flex items-center justify-center text-slate-400 group-hover:text-emerald-500 transition-colors">
                <RefreshCw className="w-3.5 h-3.5" />
              </div>
              <span className="flex-1 text-left font-medium">Server Logs</span>
            </button>
            <button className="w-full flex items-center space-x-2 px-2.5 py-2.5 rounded hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 transition-all group cursor-pointer text-[12px] border border-transparent hover:border-emerald-100/50">
              <div className="flex items-center justify-center text-slate-400 group-hover:text-emerald-500 transition-colors">
                <ShieldAlert className="w-3.5 h-3.5" />
              </div>
              <span className="flex-1 text-left font-medium">Audit Trails</span>
            </button>
            <button className="w-full flex items-center space-x-2 px-2.5 py-2.5 rounded hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 transition-all group cursor-pointer text-[12px] border border-transparent hover:border-emerald-100/50">
              <div className="flex items-center justify-center text-slate-400 group-hover:text-emerald-500 transition-colors">
                <Activity className="w-3.5 h-3.5" />
              </div>
              <span className="flex-1 text-left font-medium">Server Health</span>
            </button>
            
            <div className="pt-5 px-2.5 pb-1.5">
               <span className="text-[9px] font-bold text-slate-400 uppercase tracking-widest">Administrative</span>
            </div>
            <button className="w-full flex items-center space-x-2 px-2.5 py-2.5 rounded hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 transition-all group cursor-pointer text-[12px]" onClick={() => {
                // Scroll to active sessions in dashboard or switch view
                const el = document.getElementById('active-sessions-section');
                if (el) el.scrollIntoView({ behavior: 'smooth' });
            }}>
              <div className="flex items-center justify-center text-slate-400 group-hover:text-emerald-500 transition-colors">
                <GitBranch className="w-3.5 h-3.5" />
              </div>
              <span className="flex-1 text-left font-medium">Active Sessions</span>
            </button>
            <button className="w-full flex items-center space-x-2 px-2.5 py-2.5 rounded hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 transition-all group cursor-pointer text-[12px]">
              <div className="flex items-center justify-center text-slate-400 group-hover:text-emerald-500 transition-colors">
                <Settings2 className="w-3.5 h-3.5" />
              </div>
              <span className="flex-1 text-left font-medium">System Config</span>
            </button>
          </div>
        ) : (
          <ul className="text-[12px] font-normal text-slate-700 select-none">
            {/* Server Node */}
            <li>
              <div
                onClick={() => toggle('server')}
                onContextMenu={handleServerContextMenu}
                className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['server'] ? 'bg-emerald-50/50' : 'hover:bg-slate-200/60'}`}
              >
                {expanded['server'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                <Database className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                <div className="flex flex-col min-w-0 pr-2">
                  <div className="flex items-center space-x-1.5">
                    <span className={`truncate group-hover:text-emerald-700 ${expanded['server'] ? 'text-emerald-800 font-medium' : ''}`}>
                      {connectionDetails?.name || (connectionDetails ? `${connectionDetails.host}:${connectionDetails.port}` : (lastCredentials?.name || (lastCredentials ? `${lastCredentials.host}:${lastCredentials.port}` : 'Connected Server')))}
                    </span>
                    {status !== 'connected' && lastCredentials && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          connect(lastCredentials.host, lastCredentials.port, lastCredentials.user, lastCredentials.pass);
                        }}
                        className="p-0.5 hover:bg-emerald-100 rounded text-emerald-600 transition-colors"
                      >
                        <RefreshCw className="w-3 h-3" />
                      </button>
                    )}
                  </div>
                  <div className="flex items-center space-x-1">
                    <div className={`w-1.5 h-1.5 rounded-full ${status === 'connected' ? 'bg-emerald-500 animate-pulse' : (status === 'connecting' ? 'bg-amber-400 animate-bounce' : 'bg-rose-500')}`} />
                    <span className={`text-[9px] font-bold uppercase tracking-tighter ${status === 'connected' ? 'text-emerald-600' : (status === 'connecting' ? 'text-amber-600' : 'text-rose-600')}`}>
                      {status === 'connected' ? 'Live' : (status === 'connecting' ? 'Connecting...' : 'Disconnected')}
                    </span>
                  </div>
                </div>
              </div>

              <div className={`grid transition-all duration-200 ${expanded['server'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                <div className="overflow-hidden">
                  <ul className="pl-5 mt-0.5 space-y-0.5">
                    {/* Databases Node */}
                    <li>
                      <div
                        onClick={() => toggle('databases')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['databases'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['databases'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <Folder className="w-3.5 h-3.5 text-amber-500 fill-amber-100 shrink-0" />
                        <span className="truncate">Knowledge Bases</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['databases'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.databases.map((db, i) => (
                              <li
                                key={i}
                                onClick={() => changeKnowledgeBase(db)}
                                className={`flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded cursor-pointer group relative transition-colors ${selectedKb === db ? 'bg-emerald-50 text-emerald-700 font-medium' : ''}`}
                              >
                                <div className="absolute -left-[1px] w-[6px] h-[1px] bg-slate-200/80 top-1/2" />
                                <Database className="w-3 h-3 text-emerald-500" />
                                <span className="truncate">{db}</span>
                              </li>
                            ))}
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Concepts Node */}
                    <li>
                      <div
                        onClick={() => toggle('system')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['system'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['system'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <Folder className="w-3.5 h-3.5 text-sky-500 fill-sky-200 shrink-0" />
                        <span className="truncate">Concepts</span>
                      </div>

                      <div className={`grid transition-all duration-200 ${expanded['system'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.concepts.length === 0 ? (
                              <li className="pl-3 py-1 text-slate-400 text-[11px] italic font-normal">Empty</li>
                            ) : (
                              metadata.concepts.map((concept, i) => (
                                <li key={i}>
                                  <div
                                    onClick={() => {
                                      toggle(`concept-${i}`);
                                      execute(`DESCRIBE (CONCEPT : ${concept.Name});`, { isDescribe: true, targetName: concept.Name, isBackground: true });
                                    }}
                                    onContextMenu={(e) => handleContextMenu(e, concept)}
                                    className="flex items-center space-x-1.5 p-1 pl-2.5 hover:bg-emerald-50 rounded cursor-pointer group relative transition-colors"
                                  >
                                    <div className="absolute -left-[1px] w-[6px] h-[1px] bg-slate-200/80 top-1/2" />
                                    <Table className="w-[12px] h-[12px] text-indigo-500 shrink-0 group-hover:text-emerald-600" />
                                    <span className="truncate group-hover:text-emerald-700 leading-tight">{concept?.Name || 'Unknown'}</span>
                                  </div>
                                  <div className={`grid transition-all duration-200 ${expanded[`concept-${i}`] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                                    <div className="overflow-hidden">
                                      {metadataDetails[concept.Name.toLowerCase()] ? (
                                        <ul className="pl-6 pb-1 pt-0.5 space-y-0.5 text-[10px] font-normal text-slate-500 border-l border-emerald-100/50 ml-3">
                                          {metadataDetails[concept.Name.toLowerCase()].headers?.map((h: string, hi: number) => {
                                            const val = metadataDetails[concept.Name.toLowerCase()].rows?.[0]?.[h];
                                            if (!val || val === 'None') return null;
                                            return (
                                              <li key={hi} className="flex flex-col space-y-0.5 py-0.5">
                                                <span className="font-bold text-[9px] text-slate-400 tracking-tighter uppercase">{h}</span>
                                                <span className="pl-1 text-slate-600 break-words">{String(val)}</span>
                                              </li>
                                            );
                                          })}
                                        </ul>
                                      ) : (
                                        <div className="pl-9 py-1 text-[10px] text-slate-400 italic animate-pulse">Loading...</div>
                                      )}
                                    </div>
                                  </div>
                                </li>
                              ))
                            )}
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Hierarchies Node */}
                    <li>
                      <div onClick={() => toggle('hierarchies')} className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['hierarchies'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}>
                        {expanded['hierarchies'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <GitBranch className="w-3.5 h-3.5 text-orange-500 shrink-0" />
                        <span className="truncate">Hierarchies</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['hierarchies'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.hierarchies.length === 0 ? <li className="pl-3 py-1 text-slate-400 text-[11px] italic">Empty</li> :
                              metadata.hierarchies.map((h, i) => (
                                <li key={i} className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded text-slate-600 transition-colors cursor-pointer relative">
                                  <div className="absolute -left-[1px] w-[6px] h-[1px] bg-slate-200/80 top-1/2" />
                                  <span className="truncate text-[11px]">{h.ParentConcept} → {h.ChildConcept}</span>
                                </li>
                              ))
                            }
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Relations Node */}
                    <li>
                      <div onClick={() => toggle('relations')} className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['relations'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}>
                        {expanded['relations'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <Link className="w-3.5 h-3.5 text-indigo-500 shrink-0" />
                        <span className="truncate">Relations</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['relations'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.relations.length === 0 ? <li className="pl-3 py-1 text-slate-400 text-[11px] italic">Empty</li> :
                              metadata.relations.map((r, i) => (
                                <li key={i} className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded text-slate-600 transition-colors cursor-pointer relative">
                                  <div className="absolute -left-[1px] w-[6px] h-[1px] bg-slate-200/80 top-1/2" />
                                  <span className="truncate text-[11px]">{r.Name}</span>
                                </li>
                              ))
                            }
                          </ul>
                        </div>
                      </div>
                    </li>
                  </ul>
                </div>
              </div>
            </li>
          </ul>
        )}
      </div>

      {/* Context Menus */}
      {serverContextMenu && (
        <div ref={menuRef} className="fixed z-50 bg-white border border-slate-200 rounded shadow-xl py-1 w-36 text-[11px]" style={{ top: serverContextMenu.y, left: serverContextMenu.x }}>
          <button onClick={() => { disconnect(); setServerContextMenu(null); }} className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-red-50 text-red-500">
            <Unplug className="w-3.5 h-3.5" />
            <span>Disconnect</span>
          </button>
        </div>
      )}

      {contextMenu && (
        <div ref={menuRef} className="fixed z-50 bg-white border border-slate-200 rounded shadow-xl py-1 w-44 text-[11px]" style={{ top: contextMenu.y, left: contextMenu.x }}>
          <div className="px-3 py-1.5 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-100 mb-1">
            {contextMenu.concept.Name}
          </div>
          <button onClick={() => handleAction('select')} className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-emerald-50">
            <TerminalSquare className="w-3.5 h-3.5" />
            <span>Select All</span>
          </button>
          <button onClick={() => handleAction('insert')} className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-emerald-50">
            <Copy className="w-3.5 h-3.5" />
            <span>Script INSERT</span>
          </button>
        </div>
      )}
    </div>
  );
}
