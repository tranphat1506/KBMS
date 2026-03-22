import { useEffect, useState, useRef } from 'react';
import {
  Database, ChevronDown, ChevronRight, Folder, Table, Hexagon, GitBranch, Link, Settings2, Variable, PlusSquare,
  TerminalSquare, Copy, Trash2, RefreshCw, AlignLeft, Unplug
} from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function Sidebar() {
  const {
    metadata, fetchMetadata, status, connectionDetails,
    lastCredentials, setQuery, execute, setConnectModalOpen,
    connect, disconnect, changeKnowledgeBase, metadataDetails, selectedKb
  } = useKbmsStore();
  const [expanded, setExpanded] = useState<Record<string, boolean>>({ 'server': true, 'databases': true, 'system': true });
  const [contextMenu, setContextMenu] = useState<{ x: number, y: number, concept: any } | null>(null);
  const [serverContextMenu, setServerContextMenu] = useState<{ x: number, y: number } | null>(null);

  const menuRef = useRef<HTMLDivElement>(null);

  const toggle = (key: string) => {
    setExpanded(prev => ({ ...prev, [key]: !prev[key] }));
  };

  useEffect(() => {
    // Reset expansion state (except top level) when KB changes
    setExpanded({ 'server': true, 'databases': true, 'system': true });
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
    document.addEventListener('click', handleClickOutside);
    document.addEventListener('scroll', () => {
      setContextMenu(null);
      setServerContextMenu(null);
    });
    return () => {
      document.removeEventListener('click', handleClickOutside);
      document.removeEventListener('scroll', () => {
        setContextMenu(null);
        setServerContextMenu(null);
      });
    };
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
        <span className="text-[10px] font-semibold text-slate-700 uppercase tracking-wide opacity-90">Object Explorer</span>
        <div className="flex items-center space-x-1">
          <button className="text-slate-500 hover:text-emerald-700 p-0.5 rounded hover:bg-emerald-100 transition-colors tooltip cursor-pointer" title="Filter Objects (Coming Soon)">
            <AlignLeft className="w-3.5 h-3.5" />
          </button>
          <button onClick={fetchMetadata} disabled={status !== 'connected'} className="text-slate-500 hover:text-emerald-700 p-0.5 rounded hover:bg-emerald-100 transition-colors disabled:opacity-50 cursor-pointer disabled:cursor-not-allowed" title={`Reload Metadata (${cmd} + R)`}>
            <RefreshCw className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      <div className="py-2 px-3 border-b border-slate-200/60 mb-1.5 relative">
        <div className="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none">
          <SearchIcon />
        </div>
        <input
          type="text"
          placeholder="Search objects..."
          disabled={status !== 'connected'}
          title="Search within Explorer"
          className="w-full text-[11px] font-normal box-border pl-7 pr-3 py-1 bg-white border border-slate-300/80 rounded focus:outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500/20 transition-all placeholder:text-slate-400 shadow-inner disabled:bg-slate-50 disabled:opacity-70"
        />
      </div>

      <div className="flex-1 overflow-y-auto px-1.5 py-1 custom-scrollbar">
        {status !== 'connected' ? (
          <div className="flex flex-col items-center justify-center h-full text-slate-400 space-y-3 px-3 text-center mt-[-30px]">
            <Unplug className="w-10 h-10 opacity-30" />
            <p className="text-[12px] font-normal leading-relaxed max-w-[180px] text-slate-500">Not connected to any KBMS Server.</p>
            <button
              onClick={() => setConnectModalOpen(true)}
              title="Open Server Manager"
              className="px-4 py-1.5 bg-white text-emerald-700 font-medium text-[11px] rounded border border-emerald-300 hover:bg-emerald-50 transition-colors shadow-sm"
            >
              Connect Now
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
                {expanded['server'] ? <ChevronDown className="w-3 h-3 text-slate-400 group-hover:text-emerald-600 transition-transform" /> : <ChevronRight className="w-3 h-3 text-slate-400 group-hover:text-emerald-600 transition-transform" />}
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
                        title="Reconnect"
                      >
                        <RefreshCw className="w-3 h-3" />
                      </button>
                    )}
                  </div>
                  <div className="flex items-center space-x-1">
                    <div className={`w-1.5 h-1.5 rounded-full ${status === 'connected' ? 'bg-emerald-500 animate-pulse shadow-[0_0_4px_rgba(16,185,129,0.4)]' : (status === 'connecting' ? 'bg-amber-400 animate-bounce' : 'bg-rose-500')}`} />
                    <span className={`text-[9px] font-bold uppercase tracking-tighter ${status === 'connected' ? 'text-emerald-600' : (status === 'connecting' ? 'text-amber-600' : 'text-rose-600')}`}>
                      {status === 'connected' ? 'Live' : (status === 'connecting' ? 'Connecting...' : 'Disconnected')}
                    </span>
                  </div>
                </div>
              </div>

              <div className={`grid transition-all duration-200 ease-in-out ${expanded['server'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
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
                      <div className={`grid transition-all duration-200 ease-in-out ${expanded['databases'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.databases.map((db, i) => (
                              <li
                                key={i}
                                onClick={() => changeKnowledgeBase(db)}
                                className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded cursor-pointer group relative transition-colors"
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

                    {/* System Concepts Node */}
                    <li>
                      <div
                        onClick={() => toggle('system')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['system'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['system'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <Folder className="w-3.5 h-3.5 text-sky-500 fill-sky-200 shrink-0" />
                        <span className="truncate">Concepts</span>
                      </div>

                      <div className={`grid transition-all duration-200 ease-in-out ${expanded['system'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.concepts.length === 0 ? (
                              <li className="pl-3 py-1 text-slate-400 text-[11px] italic font-normal">
                                Empty
                              </li>
                            ) : (
                              metadata.concepts.map((concept, i) => (
                                <li key={i}>
                                  <div
                                    onClick={() => {
                                      toggle(`concept-${i}`);
                                      execute(`DESCRIBE (CONCEPT : ${concept.Name});`, true, concept.Name);
                                    }}
                                    onContextMenu={(e) => handleContextMenu(e, concept)}
                                    className="flex items-center space-x-1.5 p-1 pl-2.5 hover:bg-emerald-50 rounded cursor-pointer group relative transition-colors"
                                  >
                                    <div className="absolute -left-[1px] w-[6px] h-[1px] bg-slate-200/80 top-1/2" />
                                    <Table className="w-[12px] h-[12px] text-indigo-500 shrink-0 group-hover:text-emerald-600 transition-colors" />
                                    <span className="truncate group-hover:text-emerald-700 leading-tight">{concept?.Name || 'Unknown'}</span>
                                  </div>
                                  <div className={`grid transition-all duration-200 ease-in-out ${expanded[`concept-${i}`] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                                    <div className="overflow-hidden">
                                      {metadataDetails[concept.Name.toLowerCase()] ? (
                                        <ul className="pl-6 pb-1 pt-0.5 space-y-0.5 text-[10px] font-normal text-slate-500 border-l border-emerald-100/50 ml-3">
                                          {metadataDetails[concept.Name.toLowerCase()].headers?.map((h: string, hi: number) => {
                                            const val = metadataDetails[concept.Name.toLowerCase()].rows?.[0]?.[h];
                                            if (!val || val === 'None') return null;

                                            if (h === 'Variables') {
                                              return (
                                                <li key={hi} className="flex flex-col space-y-0.5">
                                                  <span className="font-bold text-[9px] text-emerald-600/70 uppercase tracking-tighter">Variables</span>
                                                  <ul className="space-y-0.5 pl-1">
                                                    {String(val).split('\n').map((v, vi) => {
                                                      const parts = v.trim().match(/^(.+?)\s*\((.+?)\)$/);
                                                      return (
                                                        <li key={vi} className="flex items-center space-x-1.5 hover:text-slate-800 transition-colors">
                                                          <Hexagon className="w-2.5 h-2.5 text-emerald-400/60" />
                                                          <span className="truncate">{parts ? parts[1] : v.trim()}</span>
                                                          {parts && <span className="text-[9px] text-indigo-400 font-mono italic">({parts[2]})</span>}
                                                        </li>
                                                      );
                                                    })}
                                                  </ul>
                                                </li>
                                              );
                                            }

                                            return (
                                              <li key={hi} className="flex flex-col space-y-0.5 py-0.5">
                                                <span className="font-bold text-[9px] text-slate-400 uppercase tracking-tighter">{h}</span>
                                                <span className="pl-1 text-slate-600 break-words">{String(val)}</span>
                                              </li>
                                            );
                                          })}
                                        </ul>
                                      ) : (
                                        <div className="pl-9 py-1 text-[10px] text-slate-400 italic animate-pulse">Loading details...</div>
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
                      <div
                        onClick={() => toggle('hierarchies')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['hierarchies'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['hierarchies'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <GitBranch className="w-3.5 h-3.5 text-orange-500 shrink-0" />
                        <span className="truncate">Hierarchies</span>
                      </div>
                      <div className={`grid transition-all duration-200 ease-in-out ${expanded['hierarchies'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.hierarchies.length === 0 ? <li className="pl-3 py-1 text-slate-400 text-[11px] italic">Empty</li> :
                              metadata.hierarchies.map((h, i) => {
                                const key = `${h.Parent}:${h.Child}`;
                                return (
                                  <li key={i}>
                                    <div
                                      onClick={() => {
                                        toggle(`hierarchy-${i}`);
                                        if (h?.Parent && h?.Child) execute(`DESCRIBE (HIERARCHY : ${key});`, true, key);
                                      }}
                                      className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded text-slate-600 transition-colors cursor-pointer"
                                    >
                                      <span className="truncate text-[11px]">{h?.Parent || '?'} → {h?.Child || '?'}</span>
                                    </div>
                                    <div className={`grid transition-all duration-200 ease-in-out ${expanded[`hierarchy-${i}`] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                                      <div className="overflow-hidden">
                                        {metadataDetails[key.toLowerCase()] ? (
                                          <ul className="pl-6 pb-1 pt-0.5 space-y-0.5 text-[10px] font-normal text-slate-500 border-l border-orange-100/50 ml-3">
                                            {metadataDetails[key.toLowerCase()].headers?.map((header: string, hi: number) => {
                                              const val = metadataDetails[key.toLowerCase()].rows?.[0]?.[header];
                                              if (!val || val === 'None' || header === 'Parent' || header === 'Child') return null;
                                              return (
                                                <li key={hi} className="flex flex-col space-y-0.5 py-0.5">
                                                  <span className="font-bold text-[9px] text-slate-400 uppercase tracking-tighter">{header}</span>
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
                                );
                              })}
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Relations Node */}
                    <li>
                      <div
                        onClick={() => toggle('relations')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['relations'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['relations'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <Link className="w-3.5 h-3.5 text-indigo-500 shrink-0" />
                        <span className="truncate">Relations</span>
                      </div>
                      <div className={`grid transition-all duration-200 ease-in-out ${expanded['relations'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.relations.length === 0 ? <li className="pl-3 py-1 text-slate-400 text-[11px] italic">Empty</li> :
                              metadata.relations.map((r, i) => (
                                <li key={i}>
                                  <div
                                    onClick={() => {
                                      toggle(`relation-${i}`);
                                      execute(`DESCRIBE (RELATION : ${r.Name});`, true, r.Name);
                                    }}
                                    className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded text-slate-600 transition-colors cursor-pointer"
                                  >
                                    <span className="truncate text-[11px]">{r.Name} ({r.Domain} : {r.Range})</span>
                                  </div>
                                  <div className={`grid transition-all duration-200 ease-in-out ${expanded[`relation-${i}`] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                                    <div className="overflow-hidden">
                                      {metadataDetails[r.Name.toLowerCase()] ? (
                                        <ul className="pl-6 pb-1 pt-0.5 space-y-0.5 text-[10px] font-normal text-slate-500 border-l border-indigo-100/50 ml-3">
                                          {metadataDetails[r.Name.toLowerCase()].headers?.map((header: string, hi: number) => {
                                            const val = metadataDetails[r.Name.toLowerCase()].rows?.[0]?.[header];
                                            if (!val || val === 'None' || header === 'Relation') return null;
                                            return (
                                              <li key={hi} className="flex flex-col space-y-0.5 py-0.5">
                                                <span className="font-bold text-[9px] text-slate-400 uppercase tracking-tighter">{header}</span>
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
                            }
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Rules Node */}
                    <li>
                      <div
                        onClick={() => toggle('rules')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['rules'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['rules'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <Settings2 className="w-3.5 h-3.5 text-purple-500 shrink-0" />
                        <span className="truncate">Knowledge Rules</span>
                      </div>
                      <div className={`grid transition-all duration-200 ease-in-out ${expanded['rules'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.rules.length === 0 ? <li className="pl-3 py-1 text-slate-400 text-[11px] italic">Empty</li> :
                              metadata.rules.map((rule, i) => {
                                const name = rule.Name || rule.Id;
                                return (
                                  <li key={i}>
                                    <div
                                      onClick={() => {
                                        toggle(`rule-${i}`);
                                        execute(`DESCRIBE (RULE : ${name});`, true, name);
                                      }}
                                      className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded text-slate-600 transition-colors cursor-pointer"
                                    >
                                      <span className="truncate text-[11px]">{rule.Name || rule.Id}</span>
                                    </div>
                                    <div className={`grid transition-all duration-200 ease-in-out ${expanded[`rule-${i}`] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                                      <div className="overflow-hidden">
                                        {metadataDetails[name.toLowerCase()] ? (
                                          <ul className="pl-6 pb-1 pt-0.5 space-y-0.5 text-[10px] font-normal text-slate-500 border-l border-purple-100/50 ml-3">
                                            {metadataDetails[name.toLowerCase()].headers?.map((header: string, hi: number) => {
                                              const val = metadataDetails[name.toLowerCase()].rows?.[0]?.[header];
                                              if (!val || val === 'None' || header === 'Rule' || header === 'Id') return null;
                                              return (
                                                <li key={hi} className="flex flex-col space-y-0.5 py-0.5">
                                                  <span className="font-bold text-[9px] text-slate-400 uppercase tracking-tighter">{header}</span>
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
                                );
                              })}
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Functions Node */}
                    <li>
                      <div
                        onClick={() => toggle('functions')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['functions'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['functions'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <Variable className="w-3.5 h-3.5 text-pink-500 shrink-0" />
                        <span className="truncate">Functions</span>
                      </div>
                      <div className={`grid transition-all duration-200 ease-in-out ${expanded['functions'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.functions.length === 0 ? <li className="pl-3 py-1 text-slate-400 text-[11px] italic">Empty</li> :
                              metadata.functions.map((f, i) => (
                                <li key={i}>
                                  <div
                                    onClick={() => {
                                      toggle(`function-${i}`);
                                      execute(`DESCRIBE (FUNCTION : ${f.Name});`, true, f.Name);
                                    }}
                                    className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded text-slate-600 transition-colors cursor-pointer"
                                  >
                                    <span className="truncate text-[11px]">{f.Name}</span>
                                  </div>
                                  <div className={`grid transition-all duration-200 ease-in-out ${expanded[`function-${i}`] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                                    <div className="overflow-hidden">
                                      {metadataDetails[f.Name.toLowerCase()] ? (
                                        <ul className="pl-6 pb-1 pt-0.5 space-y-0.5 text-[10px] font-normal text-slate-500 border-l border-pink-100/50 ml-3">
                                          {metadataDetails[f.Name.toLowerCase()].headers?.map((header: string, hi: number) => {
                                            const val = metadataDetails[f.Name.toLowerCase()].rows?.[0]?.[header];
                                            if (!val || val === 'None' || header === 'Function') return null;
                                            return (
                                              <li key={hi} className="flex flex-col space-y-0.5 py-0.5">
                                                <span className="font-bold text-[9px] text-slate-400 uppercase tracking-tighter">{header}</span>
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
                            }
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Operators Node */}
                    <li>
                      <div
                        onClick={() => toggle('operators')}
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['operators'] ? 'bg-slate-200/40' : 'hover:bg-slate-200/50'}`}
                      >
                        {expanded['operators'] ? <ChevronDown className="w-3 h-3 text-slate-400" /> : <ChevronRight className="w-3 h-3 text-slate-400" />}
                        <PlusSquare className="w-3.5 h-3.5 text-emerald-500 shrink-0" />
                        <span className="truncate">Custom Operators</span>
                      </div>
                      <div className={`grid transition-all duration-200 ease-in-out ${expanded['operators'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-slate-200/80 ml-[6px] pb-1">
                            {metadata.operators.length === 0 ? <li className="pl-3 py-1 text-slate-400 text-[11px] italic">Empty</li> :
                              metadata.operators.map((op, i) => (
                                <li key={i}>
                                  <div
                                    onClick={() => {
                                      toggle(`operator-${i}`);
                                      execute(`DESCRIBE (OPERATOR : ${op.Symbol});`, true, op.Symbol);
                                    }}
                                    className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-emerald-50 rounded text-slate-600 transition-colors cursor-pointer"
                                  >
                                    <span className="truncate text-[11px] font-mono">{op.Symbol}</span>
                                  </div>
                                  <div className={`grid transition-all duration-200 ease-in-out ${expanded[`operator-${i}`] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                                    <div className="overflow-hidden">
                                      {metadataDetails[op.Symbol.toLowerCase()] ? (
                                        <ul className="pl-6 pb-1 pt-0.5 space-y-0.5 text-[10px] font-normal text-slate-500 border-l border-emerald-100/50 ml-3">
                                          {metadataDetails[op.Symbol.toLowerCase()].headers?.map((header: string, hi: number) => {
                                            const val = metadataDetails[op.Symbol.toLowerCase()].rows?.[0]?.[header];
                                            if (!val || val === 'None' || header === 'Symbol') return null;
                                            return (
                                              <li key={hi} className="flex flex-col space-y-0.5 py-0.5">
                                                <span className="font-bold text-[9px] text-slate-400 uppercase tracking-tighter">{header}</span>
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

      {/* Server Context Menu */}
      {serverContextMenu && (
        <div
          ref={menuRef}
          className="fixed z-50 bg-white border border-slate-200 rounded shadow-xl py-1 w-36 text-[11px] font-medium text-slate-700 animate-in fade-in zoom-in-95 duration-100"
          style={{ top: serverContextMenu.y, left: serverContextMenu.x }}
        >
          <button 
            onClick={() => { disconnect(); setServerContextMenu(null); }} 
            className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-red-50 hover:text-red-600 text-left text-red-500 transition-colors"
          >
            <Unplug className="w-3.5 h-3.5 shrink-0" />
            <span>Disconnect (Thoát)</span>
          </button>
        </div>
      )}

      {/* Concept Context Menu */}
      {contextMenu && (
        <div
          ref={menuRef}
          className="fixed z-50 bg-white border border-slate-200 rounded shadow-xl py-1 w-44 text-[11px] font-medium text-slate-700 animate-in fade-in zoom-in-95 duration-100"
          style={{ top: contextMenu.y, left: contextMenu.x }}
        >
          <div className="px-3 py-1.5 text-[10px] font-semibold text-slate-400 uppercase tracking-wider border-b border-slate-100 mb-0.5">
            Table: {contextMenu.concept.Name}
          </div>
          <button onClick={() => handleAction('select')} className="w-full flex items-center space-x-2 px-3 py-1 hover:bg-emerald-50 hover:text-emerald-700 text-left">
            <TerminalSquare className="w-3.5 h-3.5 shrink-0" />
            <span>Select All Rows</span>
          </button>
          <button onClick={() => handleAction('insert')} className="w-full flex items-center space-x-2 px-3 py-1 hover:bg-emerald-50 hover:text-emerald-700 text-left">
            <Copy className="w-3.5 h-3.5 shrink-0" />
            <span>Script as INSERT</span>
          </button>
          <div className="my-1 border-t border-slate-100" />
          <button onClick={() => handleAction('drop')} className="w-full flex items-center space-x-2 px-3 py-1 hover:bg-red-50 hover:text-red-600 text-left text-red-500 border-none outline-none">
            <Trash2 className="w-3.5 h-3.5 shrink-0" />
            <span>Script as DROP</span>
          </button>
        </div>
      )}
    </div>
  );
}

function SearchIcon() {
  return (
    <svg className="w-3.5 h-3.5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
  );
}
