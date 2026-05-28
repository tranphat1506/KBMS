import { useEffect, useState, useRef } from 'react';
import {
  Database, ChevronDown, ChevronRight, Folder, Table, GitBranch, Link, Settings2,
  TerminalSquare, Copy, RefreshCw, AlignLeft, Unplug, Search, Activity, LayoutDashboard, FileText, Users,
  Wrench, Zap, Code, Calculator, Network, Download
} from 'lucide-react';
import { useThingentStore } from '../store/thingentStore';

export default function Sidebar() {
  const {
    openDetailTab,
    status,
    serverMetadata,
    kbMetadata,
    fetchMetadata,
    activeSidebarView,
    selectedKb,
    changeKnowledgeBase,
    connectionDetails,
    lastCredentials,
    setQuery,
    setConnectModalOpen,
    connect,
    disconnect,
    systemActiveTab,
    setSystemActiveTab
  } = useThingentStore();

  const [expanded, setExpanded] = useState<Record<string, boolean>>({ 'server': true, 'databases': true, 'system': true });
  const [contextMenu, setContextMenu] = useState<{ x: number, y: number, concept: any } | null>(null);
  const [serverContextMenu, setServerContextMenu] = useState<{ x: number, y: number } | null>(null);
  const [kbContextMenu, setKbContextMenu] = useState<{ x: number, y: number, kbName: string } | null>(null);

  const menuRef = useRef<HTMLDivElement>(null);

  const toggle = (key: string) => {
    setExpanded(prev => ({ ...prev, [key]: !prev[key] }));
  };

  useEffect(() => {
    // Collapse all object folders when switching KB, don't auto-fetch
    setExpanded(prev => ({ ...prev, 'system': false, 'hierarchies': false, 'relations': false, 'rules': false, 'functions': false, 'operators': false }));
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
        setKbContextMenu(null);
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
    setKbContextMenu(null);
    setServerContextMenu({ x: e.pageX, y: e.pageY });
  };

  const handleKbContextMenu = (e: React.MouseEvent, kbName: string) => {
    e.preventDefault();
    setContextMenu(null);
    setServerContextMenu(null);
    setKbContextMenu({ x: e.pageX, y: e.pageY, kbName });
  };

  const handleKbAction = async (action: 'export') => {
    if (!kbContextMenu?.kbName) return;
    const kbName = kbContextMenu.kbName;
    setKbContextMenu(null);

    if (action === 'export') {
      try {
        // First ask for save path via thingentApi.saveFile without content just to get the path
        // Wait, thingentApi.saveFile is not a "save dialog" alone, it writes.
        // We can create a new ipcMain handle in main.ts to show a save dialog.
        // For now, I'll just execute the query with a temp path, but that's backend.
        // We can execute a query to export to a default file, or use IPC to show dialog.
        
        // Actually, if we just run EXPORT KNOWLEDGE BASE {kbName} FORMAT: KBPKG;
        // The server will export it to the CWD by default if we don't specify FILE.
        // Wait! The Parser says FILE is REQUIRED! `if (Consume(TokenType.FILE) == null) throw Error("Expected 'FILE' in EXPORT parameters");`
        // So we MUST specify a file!
        // We can generate a dummy file path, but on the server?
        // Let's use `window.thingentApi.saveFile('', 'export.kbpkg', true)` to prompt user and get a path?
        // Let's do that!
        
        // @ts-ignore
        const res = await window.thingentApi.saveFile('', `${kbName}.kbpkg`, true);
        if (res && res.success && res.filePath) {
           const query = `EXPORT(KNOWLEDGE BASE: ${kbName}, FORMAT: KBPKG, FILE: '${res.filePath.replace(/\\/g, '/')}');`;
           setQuery(query);
           // Auto execute it
           // @ts-ignore
           window.thingentApi.execute(query);
           
           useThingentStore.getState().addNotification({
             type: 'log',
             severity: 'info',
             title: 'Export Started',
             message: `Exporting ${kbName} to ${res.filePath}`
           });
        }
      } catch (e) {
        console.error(e);
      }
    }
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

  const currentMetadata = kbMetadata[selectedKb] || {
    concepts: [],
    hierarchies: [],
    relations: [],
    rules: [],
    functions: [],
    operators: []
  };

  return (
    <div className="h-full flex flex-col pt-3 text-[var(--text-main)] relative font-sans transition-colors duration-200">
      <div className="px-3 pb-2.5 flex items-center justify-between">
        <span className="text-[10px] font-semibold text-[var(--text-main)] uppercase tracking-wide opacity-90">
          {activeSidebarView === 'explorer' ? 'Object Explorer' : 'System Management'}
        </span>
        <div className="flex items-center space-x-1">
          <button className="text-[var(--text-sub)] hover:text-[var(--brand-primary)] p-0.5 rounded hover:bg-[var(--brand-primary-light)] transition-colors tooltip cursor-pointer" title="Filter (Coming Soon)">
            <AlignLeft className="w-3.5 h-3.5" />
          </button>
          <button
            onClick={() => {
              if (activeSidebarView === 'explorer') fetchMetadata();
              else {
                useThingentStore.getState().fetchSystemLogs();
                useThingentStore.getState().fetchAuditLogs();
              }
            }}
            disabled={status !== 'connected'}
            className="text-[var(--text-sub)] hover:text-[var(--brand-primary)] p-0.5 rounded hover:bg-[var(--brand-primary-light)] transition-colors disabled:opacity-50 cursor-pointer disabled:cursor-not-allowed"
            title={`Reload ${activeSidebarView === 'explorer' ? 'Metadata' : 'Logs'} (${cmd} + R)`}
          >
            <RefreshCw className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      <div className="py-2 px-3 border-b border-[var(--border-subtle)]/60 mb-1.5 relative">
        <div className="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none">
          <Search className="w-3.5 h-3.5 text-[var(--text-muted)]" />
        </div>
        <input
          type="text"
          placeholder={`Search ${activeSidebarView === 'explorer' ? 'objects' : 'logs'}...`}
          disabled={status !== 'connected'}
          className="w-full text-xs font-normal box-border pl-7 pr-3 py-1 bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded text-[var(--text-main)] focus:outline-none focus:border-[var(--brand-primary)] transition-all placeholder:text-[var(--text-muted)] shadow-inner"
        />
      </div>

      <div className="flex-1 overflow-y-auto px-1.5 py-1 custom-scrollbar">
        {status !== 'connected' ? (
          <div className="flex flex-col items-center justify-center h-full text-[var(--text-muted)] space-y-3 px-3 text-center mt-[-30px]">
            <Unplug className="w-10 h-10 opacity-30" />
            <p className="text-xs font-normal">Not connected to server.</p>
            <button
              onClick={() => setConnectModalOpen(true)}
              className="px-4 py-1.5 bg-[var(--bg-surface)] text-[var(--brand-primary-text)] font-medium text-xs rounded border border-[var(--brand-primary)]/30 hover:bg-[var(--brand-primary-light)] transition-colors shadow-sm cursor-pointer"
            >
              Connect Now
            </button>
          </div>
        ) : activeSidebarView === 'system' ? (
          <div className="space-y-4 mt-2 px-1 pb-4">
            {/* Monitoring Group */}
            <div>
              <div className="px-2.5 pb-1.5 pt-1">
                <span className="text-[9px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Monitoring</span>
              </div>
              <div className="space-y-0.5">
                <button 
                  onClick={() => setSystemActiveTab('overview')}
                  className={`w-full flex items-center space-x-2 px-2.5 py-2 rounded hover:bg-[var(--brand-primary-light)] transition-all group cursor-pointer text-sm border border-transparent ${systemActiveTab === 'logs' ? 'bg-[var(--brand-primary-light)] text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20 font-medium' : 'text-[var(--text-sub)]'}`}
                >
                  <LayoutDashboard className={`w-3.5 h-3.5 ${systemActiveTab === 'overview' ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)] group-hover:text-[var(--brand-primary)]'}`} />
                  <span className="flex-1 text-left">Overview</span>
                </button>
                <button 
                  onClick={() => setSystemActiveTab('logs')}
                  className={`w-full flex items-center space-x-2 px-2.5 py-2 rounded hover:bg-[var(--brand-primary-light)] transition-all group cursor-pointer text-[12px] border border-transparent ${systemActiveTab === 'logs' ? 'bg-[var(--brand-primary-light)] text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20 font-medium' : 'text-[var(--text-sub)]'}`}
                >
                  <FileText className={`w-3.5 h-3.5 ${systemActiveTab === 'logs' ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)] group-hover:text-[var(--brand-primary)]'}`} />
                  <span className="flex-1 text-left">Log Analyzer</span>
                </button>
              </div>
            </div>

            {/* Security Group */}
            <div>
              <div className="px-2.5 pb-1.5 pt-1">
                <span className="text-[9px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Security & Access</span>
              </div>
              <div className="space-y-0.5">
                <button 
                  onClick={() => setSystemActiveTab('users')}
                  className={`w-full flex items-center space-x-2 px-2.5 py-2 rounded hover:bg-[var(--brand-primary-light)] transition-all group cursor-pointer text-[12px] border border-transparent ${systemActiveTab === 'users' ? 'bg-[var(--brand-primary-light)] text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20 font-medium' : 'text-[var(--text-sub)]'}`}
                >
                  <Users className={`w-3.5 h-3.5 ${systemActiveTab === 'users' ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)] group-hover:text-[var(--brand-primary)]'}`} />
                  <span className="flex-1 text-left">User Management</span>
                </button>
                <button 
                  onClick={() => setSystemActiveTab('sessions')}
                  className={`w-full flex items-center space-x-2 px-2.5 py-2 rounded hover:bg-[var(--brand-primary-light)] transition-all group cursor-pointer text-[12px] border border-transparent ${systemActiveTab === 'sessions' ? 'bg-[var(--brand-primary-light)] text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20 font-medium' : 'text-[var(--text-sub)]'}`}
                >
                  <Activity className={`w-3.5 h-3.5 ${systemActiveTab === 'sessions' ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)] group-hover:text-[var(--brand-primary)]'}`} />
                  <span className="flex-1 text-left">Active Sessions</span>
                </button>
              </div>
            </div>

            {/* Configuration Group */}
            <div>
              <div className="px-2.5 pb-1.5 pt-1">
                <span className="text-[9px] font-bold text-[var(--text-muted)] uppercase tracking-widest">Configuration</span>
              </div>
              <div className="space-y-0.5">
                <button 
                  onClick={() => setSystemActiveTab('settings')}
                  className={`w-full flex items-center space-x-2 px-2.5 py-2 rounded hover:bg-[var(--brand-primary-light)] transition-all group cursor-pointer text-[12px] border border-transparent ${systemActiveTab === 'settings' ? 'bg-[var(--brand-primary-light)] text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20 font-medium' : 'text-[var(--text-sub)]'}`}
                >
                  <Settings2 className={`w-3.5 h-3.5 ${systemActiveTab === 'settings' ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)] group-hover:text-[var(--brand-primary)]'}`} />
                  <span className="flex-1 text-left">Server Settings</span>
                </button>
                <button 
                  onClick={() => setSystemActiveTab('debug' as any)}
                  className={`w-full flex items-center space-x-2 px-2.5 py-2 rounded hover:bg-[var(--brand-primary-light)] transition-all group cursor-pointer text-[12px] border border-transparent ${systemActiveTab === ('debug' as any) ? 'bg-[var(--brand-primary-light)] text-[var(--brand-primary-text)] border-[var(--brand-primary)]/20 font-medium' : 'text-[var(--text-sub)]'}`}
                >
                  <Wrench className={`w-3.5 h-3.5 ${systemActiveTab === ('debug' as any) ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)] group-hover:text-[var(--brand-primary)]'}`} />
                  <span className="flex-1 text-left">DEBUG Tool</span>
                </button>
              </div>
            </div>
          </div>
        ) : (
          <ul className="text-sm font-normal text-[var(--text-main)] select-none">
            {/* Server Node */}
            <li>
              <div
                onClick={() => toggle('server')}
                onContextMenu={handleServerContextMenu}
                className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['server'] ? 'bg-[var(--brand-primary-light)]/50' : 'hover:bg-[var(--bg-surface-alt)]'}`}
              >
                {expanded['server'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                <Database className="w-3.5 h-3.5 text-[var(--brand-primary)] shrink-0" />
                <div className="flex flex-col min-w-0 pr-2">
                  <div className="flex items-center space-x-1.5">
                    <span className={`truncate group-hover:text-[var(--brand-primary-text)] ${expanded['server'] ? 'text-[var(--brand-primary-text)] font-medium' : ''}`}>
                      {connectionDetails?.name || (connectionDetails ? `${connectionDetails.host}:${connectionDetails.port}` : (lastCredentials?.name || (lastCredentials ? `${lastCredentials.host}:${lastCredentials.port}` : 'Connected Server')))}
                    </span>
                    {status !== 'connected' && lastCredentials && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          connect(lastCredentials.host, lastCredentials.port, lastCredentials.user, lastCredentials.pass);
                        }}
                        className="p-0.5 hover:bg-[var(--brand-primary-light)] rounded text-[var(--brand-primary)] transition-colors"
                      >
                        <RefreshCw className="w-3 h-3" />
                      </button>
                    )}
                  </div>
                  <div className="flex items-center space-x-1">
                    <div className={`w-1.5 h-1.5 rounded-full ${status === 'connected' ? 'bg-[var(--brand-primary)] animate-pulse' : (status === 'connecting' ? 'bg-amber-400 animate-bounce' : 'bg-rose-500')}`} />
                    <span className={`text-[10px] font-bold uppercase tracking-tighter ${status === 'connected' ? 'text-[var(--brand-primary)]' : (status === 'connecting' ? 'text-amber-600' : 'text-rose-600')}`}>
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
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['databases'] ? 'bg-[var(--bg-surface-alt)]/40' : 'hover:bg-[var(--bg-surface-alt)]/50'}`}
                      >
                        {expanded['databases'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                        <Folder className="w-3.5 h-3.5 text-amber-500 fill-amber-100 shrink-0" />
                        <span className="truncate">Knowledge Bases</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['databases'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-[var(--border-subtle)] ml-[6px] pb-1">
                            {serverMetadata.databases.map((db, i) => (
                              <li
                                key={i}
                                onClick={() => changeKnowledgeBase(db)}
                                onContextMenu={(e) => handleKbContextMenu(e, db)}
                                className={`flex items-center space-x-2 p-1 pl-2.5 hover:bg-[var(--brand-primary-light)] rounded cursor-pointer group relative transition-colors ${selectedKb === db ? 'bg-[var(--brand-primary-light)] text-[var(--brand-primary-text)] font-medium' : ''}`}
                              >
                                <div className="absolute -left-[1px] w-[6px] h-[1px] border-t border-[var(--border-subtle)] top-1/2" />
                                <Database className="w-3 h-3 text-[var(--brand-primary)] shrink-0" />
                                <span className="truncate flex-1">{db}</span>
                                <button 
                                  onClick={(e) => { 
                                    e.stopPropagation(); 
                                    if (selectedKb !== db) changeKnowledgeBase(db);
                                    useThingentStore.getState().openOntologyBuilderTab(db); 
                                  }}
                                  className="opacity-0 group-hover:opacity-100 p-0.5 text-[var(--text-muted)] hover:text-[var(--brand-primary)] hover:bg-[var(--bg-surface-alt)] rounded"
                                  title="Open Visual Builder"
                                >
                                  <Network className="w-3.5 h-3.5" />
                                </button>
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
                        className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['system'] ? 'bg-[var(--bg-surface-alt)]/40' : 'hover:bg-[var(--bg-surface-alt)]/50'}`}
                      >
                        {expanded['system'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                        <Folder className="w-3.5 h-3.5 text-sky-500 fill-sky-200 shrink-0" />
                        <span className="truncate">Concepts</span>
                      </div>

                      <div className={`grid transition-all duration-200 ${expanded['system'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-[var(--border-subtle)] ml-[6px] pb-1">
                            {currentMetadata.concepts.length === 0 ? (
                              <li className="pl-3 py-1 text-[var(--text-muted)] text-xs italic font-normal">Empty</li>
                            ) : (
                              currentMetadata.concepts.map((concept, i) => (
                                <li key={i}>
                                  <div
                                    onClick={() => openDetailTab('Concept', concept.Name)}
                                    onContextMenu={(e) => handleContextMenu(e, concept)}
                                    className="flex items-center space-x-1.5 p-1 pl-2.5 hover:bg-[var(--brand-primary-light)] rounded cursor-pointer group relative transition-colors"
                                  >
                                    <div className="absolute -left-[1px] w-[6px] h-[1px] border-t border-[var(--border-subtle)] top-1/2" />
                                    <Table className="w-[12px] h-[12px] text-indigo-500 shrink-0 group-hover:text-[var(--brand-primary)]" />
                                    <span className="truncate group-hover:text-[var(--brand-primary-text)] leading-tight">{concept?.Name || 'Unknown'}</span>
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
                      <div onClick={() => toggle('hierarchies')} className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['hierarchies'] ? 'bg-[var(--bg-surface-alt)]/40' : 'hover:bg-[var(--bg-surface-alt)]/50'}`}>
                        {expanded['hierarchies'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                        <GitBranch className="w-3.5 h-3.5 text-orange-500 shrink-0" />
                        <span className="truncate">Hierarchies</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['hierarchies'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-[var(--border-subtle)] ml-[6px] pb-1">
                            {currentMetadata.hierarchies.length === 0 ? <li className="pl-3 py-1 text-[var(--text-muted)] text-[11px] italic font-normal">Empty</li> :
                              currentMetadata.hierarchies.map((h, i) => (
                                <li key={i} className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-[var(--brand-primary-light)] rounded text-[var(--text-sub)] transition-colors cursor-pointer relative">
                                  <div className="absolute -left-[1px] w-[6px] h-[1px] border-t border-[var(--border-subtle)] top-1/2" />
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
                      <div onClick={() => toggle('relations')} className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['relations'] ? 'bg-[var(--bg-surface-alt)]/40' : 'hover:bg-[var(--bg-surface-alt)]/50'}`}>
                        {expanded['relations'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                        <Link className="w-3.5 h-3.5 text-indigo-500 shrink-0" />
                        <span className="truncate">Relations</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['relations'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-[var(--border-subtle)] ml-[6px] pb-1">
                            {!currentMetadata.relations || currentMetadata.relations.length === 0 ? <li className="pl-3 py-1 text-[var(--text-muted)] text-xs italic font-normal">Empty</li> :
                              currentMetadata.relations.map((r, i) => (
                                <li key={i} onClick={() => openDetailTab('Relation', r.Name || r)} className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-[var(--brand-primary-light)] rounded text-[var(--text-sub)] hover:text-[var(--text-main)] transition-colors cursor-pointer relative">
                                  <div className="absolute -left-[1px] w-[6px] h-[1px] border-t border-[var(--border-subtle)] top-1/2" />
                                  <span className="truncate text-xs">{r.Name || r}</span>
                                </li>
                              ))
                            }
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Rules Node */}
                    <li>
                      <div onClick={() => toggle('rules')} className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['rules'] ? 'bg-[var(--bg-surface-alt)]/40' : 'hover:bg-[var(--bg-surface-alt)]/50'}`}>
                        {expanded['rules'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                        <Zap className="w-3.5 h-3.5 text-yellow-500 shrink-0" />
                        <span className="truncate">Rules</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['rules'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-[var(--border-subtle)] ml-[6px] pb-1">
                            {!currentMetadata.rules || currentMetadata.rules.length === 0 ? <li className="pl-3 py-1 text-[var(--text-muted)] text-xs italic font-normal">Empty</li> :
                              currentMetadata.rules.map((r, i) => (
                                <li key={i} onClick={() => openDetailTab('Rule', r.Name || r.Id || r)} className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-[var(--brand-primary-light)] rounded text-[var(--text-sub)] hover:text-[var(--text-main)] transition-colors cursor-pointer relative">
                                  <div className="absolute -left-[1px] w-[6px] h-[1px] border-t border-[var(--border-subtle)] top-1/2" />
                                  <span className="truncate text-xs">{r.Name || r.Id || r}</span>
                                </li>
                              ))
                            }
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Functions Node */}
                    <li>
                      <div onClick={() => toggle('functions')} className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['functions'] ? 'bg-[var(--bg-surface-alt)]/40' : 'hover:bg-[var(--bg-surface-alt)]/50'}`}>
                        {expanded['functions'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                        <Code className="w-3.5 h-3.5 text-emerald-500 shrink-0" />
                        <span className="truncate">Functions</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['functions'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-[var(--border-subtle)] ml-[6px] pb-1">
                            {!currentMetadata.functions || currentMetadata.functions.length === 0 ? <li className="pl-3 py-1 text-[var(--text-muted)] text-xs italic font-normal">Empty</li> :
                              currentMetadata.functions.map((f, i) => (
                                <li key={i} onClick={() => openDetailTab('Function', f.Name || f)} className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-[var(--brand-primary-light)] rounded text-[var(--text-sub)] hover:text-[var(--text-main)] transition-colors cursor-pointer relative">
                                  <div className="absolute -left-[1px] w-[6px] h-[1px] border-t border-[var(--border-subtle)] top-1/2" />
                                  <span className="truncate text-xs">{f.Name || f}</span>
                                </li>
                              ))
                            }
                          </ul>
                        </div>
                      </div>
                    </li>

                    {/* Operators Node */}
                    <li>
                      <div onClick={() => toggle('operators')} className={`flex items-center space-x-1 p-1 rounded cursor-pointer group transition-colors ${expanded['operators'] ? 'bg-[var(--bg-surface-alt)]/40' : 'hover:bg-[var(--bg-surface-alt)]/50'}`}>
                        {expanded['operators'] ? <ChevronDown className="w-3 h-3 text-[var(--text-muted)]" /> : <ChevronRight className="w-3 h-3 text-[var(--text-muted)]" />}
                        <Calculator className="w-3.5 h-3.5 text-purple-500 shrink-0" />
                        <span className="truncate">Operators</span>
                      </div>
                      <div className={`grid transition-all duration-200 ${expanded['operators'] ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'}`}>
                        <div className="overflow-hidden">
                          <ul className="pl-4 mt-0.5 space-y-0.5 border-l border-[var(--border-subtle)] ml-[6px] pb-1">
                            {!currentMetadata.operators || currentMetadata.operators.length === 0 ? <li className="pl-3 py-1 text-[var(--text-muted)] text-xs italic font-normal">Empty</li> :
                              currentMetadata.operators.map((o, i) => (
                                <li key={i} onClick={() => openDetailTab('Operator', o.Symbol || o)} className="flex items-center space-x-2 p-1 pl-2.5 hover:bg-[var(--brand-primary-light)] rounded text-[var(--text-sub)] hover:text-[var(--text-main)] transition-colors cursor-pointer relative">
                                  <div className="absolute -left-[1px] w-[6px] h-[1px] border-t border-[var(--border-subtle)] top-1/2" />
                                  <span className="truncate text-xs">{o.Symbol || o}</span>
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
        <div ref={menuRef} className="fixed z-50 bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded shadow-xl py-1 w-36 text-xs" style={{ top: serverContextMenu.y, left: serverContextMenu.x }}>
          <button onClick={() => { disconnect(); setServerContextMenu(null); }} className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-rose-500/10 text-rose-500">
            <Unplug className="w-3.5 h-3.5" />
            <span>Disconnect</span>
          </button>
        </div>
      )}

      {kbContextMenu && (
        <div ref={menuRef} className="fixed z-50 bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded shadow-xl py-1 w-44 text-xs" style={{ top: kbContextMenu.y, left: kbContextMenu.x }}>
          <div className="px-3 py-1.5 text-[10px] font-bold text-[var(--text-muted)] uppercase border-b border-[var(--border-muted)] mb-1">
            KB: {kbContextMenu.kbName}
          </div>
          <button onClick={() => handleKbAction('export')} className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-[var(--brand-primary-light)] text-[var(--text-main)]">
            <Download className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
            <span>Export KB (.kbpkg)</span>
          </button>
        </div>
      )}

      {contextMenu && (
        <div ref={menuRef} className="fixed z-50 bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded shadow-xl py-1 w-44 text-xs" style={{ top: contextMenu.y, left: contextMenu.x }}>
          <div className="px-3 py-1.5 text-[10px] font-bold text-[var(--text-muted)] uppercase border-b border-[var(--border-muted)] mb-1">
            {contextMenu.concept.Name}
          </div>
          <button onClick={() => handleAction('select')} className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-[var(--brand-primary-light)] text-[var(--text-main)]">
            <TerminalSquare className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
            <span>Select All</span>
          </button>
          <button onClick={() => handleAction('insert')} className="w-full flex items-center space-x-2 px-3 py-1.5 hover:bg-[var(--brand-primary-light)] text-[var(--text-main)]">
            <Copy className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
            <span>Script INSERT</span>
          </button>
        </div>
      )}
    </div>
  );
}
