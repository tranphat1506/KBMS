import { create } from 'zustand';

export interface QueryTab {
  id: string;
  name: string;
  query: string;
  isSaved: boolean;
  filePath?: string;
}

export interface ServerProfile {
  id: string;
  name: string;
  host: string;
  port: number;
  user: string;
  pass?: string;
}

export interface KbmsState {
  status: 'disconnected' | 'connecting' | 'connected' | 'error';
  connectionDetails: { host: string; port: number; name?: string } | null;
  lastCredentials: { host: string; port: number; user: string; pass: string; name?: string } | null;
  tabs: QueryTab[];
  activeTabId: string;
  isExecuting: boolean;
  result: any[] | null;
  activeTab: 'results' | 'messages';
  metadata: {
    concepts: any[];
    databases: string[];
    users: any[];
    hierarchies: any[];
    relations: any[];
    rules: any[];
    functions: any[];
    operators: any[];
  };
  profiles: ServerProfile[];
  isConnectModalOpen: boolean;
  selectedKb: string;
  lastError: string | null;
  lastDescribeResult: any | null;
  metadataDetails: Record<string, any>;
  editorMarkers: any[];
  currentRequestId: string | null;
  setSelectedKb: (kb: string) => void;
  setConnectModalOpen: (v: boolean) => void;
  setQuery: (query: string) => void;
  setActiveTab: (t: 'results' | 'messages') => void;
  addTab: () => void;
  removeTab: (id: string) => void;
  setActiveTabId: (id: string) => void;
  saveTab: (id: string) => Promise<void>;
  openTab: () => Promise<void>;
  execute: (query?: string, options?: { isDescribe?: boolean, targetName?: string, isBackground?: boolean }) => Promise<any>;
  stopExecution: () => void;
  fetchMetadata: () => Promise<void>;
  changeKnowledgeBase: (kb: string) => Promise<void>;
  connect: (host: string, port: number, user: string, pass: string, name?: string) => Promise<{ success: boolean, error?: string }>;
  disconnect: () => Promise<void>;
  handleResultFragment: (fragment: any) => void;
  saveProfile: (p: ServerProfile) => void;
  deleteProfile: (id: string) => void;
  setStatus: (status: 'disconnected' | 'connecting' | 'connected' | 'error') => void;
  clearEditorMarkers: () => void;
  confirmDialog: {
    isOpen: boolean;
    title: string;
    message: string;
    onConfirm?: () => void;
    onCancel?: () => void;
  };
  showConfirm: (title: string, message: string, onConfirm: () => void, onCancel?: () => void) => void;
  closeConfirm: () => void;
}

const loadProfiles = (): ServerProfile[] => {
  try {
    const stored = localStorage.getItem('kbms_server_profiles');
    if (stored) return JSON.parse(stored);
  } catch (e) { }
  return [];
};

const loadTabs = (): QueryTab[] => {
  try {
    const stored = localStorage.getItem('kbms_editor_tabs');
    if (stored) return JSON.parse(stored);
  } catch (e) { }
  return [{ id: '1', name: 'Query1.kbql', query: '', isSaved: true }];
};

const loadActiveTabId = (): string => {
  return localStorage.getItem('kbms_active_tab_id') || '1';
};

const loadConnectionDetails = () => {
  try {
    const stored = localStorage.getItem('kbms_connection_details');
    if (stored) return JSON.parse(stored);
  } catch (e) { }
  return null;
};

const loadLastCredentials = () => {
  try {
    const stored = localStorage.getItem('kbms_last_credentials');
    if (stored) return JSON.parse(stored);
  } catch (e) { }
  return null;
};

export const useKbmsStore = create<KbmsState>((set, get) => ({
  status: 'disconnected',
  connectionDetails: loadConnectionDetails(),
  lastCredentials: loadLastCredentials(),
  tabs: loadTabs(),
  activeTabId: loadActiveTabId(),
  isExecuting: false,
  result: null,
  activeTab: 'results',
  metadata: {
    concepts: [],
    databases: [],
    users: [],
    hierarchies: [],
    relations: [],
    rules: [],
    functions: [],
    operators: []
  },
  profiles: loadProfiles(),
  isConnectModalOpen: false,
  selectedKb: '',
  lastError: null,
  lastDescribeResult: null,
  editorMarkers: [],
  currentRequestId: null,
  metadataDetails: {},
  confirmDialog: {
    isOpen: false,
    title: '',
    message: ''
  },

  setSelectedKb: (kb) => set({ selectedKb: kb }),
  setConnectModalOpen: (v: boolean) => set({ isConnectModalOpen: v }),
  stopExecution: () => set({ isExecuting: false }),
  clearEditorMarkers: () => set({ editorMarkers: [] }),
  showConfirm: (title, message, onConfirm, onCancel) => set({ 
    confirmDialog: { isOpen: true, title, message, onConfirm, onCancel } 
  }),
  closeConfirm: () => set((state) => ({ 
    confirmDialog: { ...state.confirmDialog, isOpen: false } 
  })),

  saveProfile: (p) => set((state) => {
    const exists = state.profiles.some(x => x.id === p.id);
    let newProfiles = [];
    if (exists) {
      newProfiles = state.profiles.map(x => x.id === p.id ? p : x);
    } else {
      newProfiles = [...state.profiles, p];
    }
    localStorage.setItem('kbms_server_profiles', JSON.stringify(newProfiles));
    return { profiles: newProfiles };
  }),

  deleteProfile: (id) => set((state) => {
    const newProfiles = state.profiles.filter(p => p.id !== id);
    localStorage.setItem('kbms_server_profiles', JSON.stringify(newProfiles));
    return { profiles: newProfiles };
  }),

  setQuery: (query) => set((state) => {
    const newTabs = state.tabs.map(t => t.id === state.activeTabId ? { ...t, query, isSaved: false } : t);
    localStorage.setItem('kbms_editor_tabs', JSON.stringify(newTabs));
    return { tabs: newTabs };
  }),

  setActiveTab: (t) => set({ activeTab: t }),

  addTab: () => set((state) => {
    const newId = Date.now().toString();
    const newName = `Query${state.tabs.length + 1}.kbql`;
    const newTabs = [...state.tabs, { id: newId, name: newName, query: '', isSaved: false }];
    localStorage.setItem('kbms_editor_tabs', JSON.stringify(newTabs));
    localStorage.setItem('kbms_active_tab_id', newId);
    return {
      tabs: newTabs,
      activeTabId: newId
    };
  }),

  removeTab: (id) => set((state) => {
    const newTabs = state.tabs.filter(t => t.id !== id);
    let newActiveId = state.activeTabId;
    if (state.activeTabId === id && newTabs.length > 0) {
      newActiveId = newTabs[newTabs.length - 1].id;
    } else if (newTabs.length === 0) {
      newActiveId = '';
    }
    localStorage.setItem('kbms_editor_tabs', JSON.stringify(newTabs));
    localStorage.setItem('kbms_active_tab_id', newActiveId);
    return { tabs: newTabs, activeTabId: newActiveId };
  }),

  setActiveTabId: (id) => {
    localStorage.setItem('kbms_active_tab_id', id);
    set({ activeTabId: id });
  },

  saveTab: async (id) => {
    const tab = get().tabs.find(t => t.id === id);
    if (!tab) return;
    const isNewFile = !tab.filePath;
    // @ts-ignore
    const res = await window.kbmsApi.saveFile(tab.query, tab.filePath || tab.name, isNewFile);
    if (res && res.success) {
      const newTabs = get().tabs.map(t => t.id === id ? {
        ...t,
        isSaved: true,
        filePath: res.filePath,
        name: res.filePath.split(/[/\\]/).pop() || t.name
      } : t);
      localStorage.setItem('kbms_editor_tabs', JSON.stringify(newTabs));
      set({ tabs: newTabs });
    }
  },

  openTab: async () => {
    // @ts-ignore
    const res = await window.kbmsApi.openFile();
    if (res && res.success) {
      set((state) => {
        const newId = Date.now().toString();
        const newName = res.filePath.split(/[/\\]/).pop() || 'Query.kbql';
        const newTabs = [...state.tabs, { id: newId, name: newName, query: res.content, isSaved: true, filePath: res.filePath }];
        localStorage.setItem('kbms_editor_tabs', JSON.stringify(newTabs));
        localStorage.setItem('kbms_active_tab_id', newId);
        return {
          tabs: newTabs,
          activeTabId: newId
        };
      });
    }
  },

  changeKnowledgeBase: async (kb: string) => {
    const state = get();
    if (state.status !== 'connected' && state.lastCredentials) {
      console.log("(Store) Attempting auto-reconnect before USE...");
      const res = await state.connect(state.lastCredentials.host, state.lastCredentials.port, state.lastCredentials.user, state.lastCredentials.pass);
      if (!res.success) {
        set({
          result: [{ messages: [{ type: 'error', text: `Connection lost. Auto-reconnect failed: ${res.error}` }] }],
          activeTab: 'messages'
        });
        return;
      }
    }

    // Use store execute for background USE query
    const res = await get().execute(`USE ${kb};`, { isBackground: true });
    set({ isExecuting: false });
    const hasError = res && (res.error || (res.messages && res.messages.some((m: any) => 
        typeof m === 'string' ? m.toLowerCase().includes('error') : (m.type === 'error' || m.type === 'Error')
    )));

    if (res && !hasError && (res.success !== false)) {
      set(s => {
        const dbs = s.metadata.databases.includes(kb) ? s.metadata.databases : [...s.metadata.databases, kb];
        return { selectedKb: kb, metadata: { ...s.metadata, databases: dbs } };
      });
      get().fetchMetadata();
    } else {
      // Show error in results/messages pane
      set({
        selectedKb: '', // Revert to empty selection on error
        result: Array.isArray(res) ? res : [res || { messages: [{ type: 'error', text: `Failed to use Knowledge Base: ${kb}` }] }],
        activeTab: 'messages'
      });
    }
  },

  fetchMetadata: async () => {
    if (get().status !== 'connected') return;
    set({ metadataDetails: {} }); // Reset details when reloading metadata

    // Fetch Knowledge Bases (Always available)
    try {
      // @ts-ignore
      const resKBs = await get().execute("SHOW KNOWLEDGE BASES;", { isBackground: true });
      if (resKBs && resKBs.rows) {
        const dbs = resKBs.rows.map((r: any) => r.Name || r.KnowledgeBase || r.Database).filter(Boolean);
        set((state) => ({ metadata: { ...state.metadata, databases: dbs } }));
      }
    } catch (err) {
      console.error("(Store) Failed to fetch databases:", err);
    }

    const selectedKb = get().selectedKb;
    if (selectedKb) {
      try {
        const queries = [
          { key: 'concepts', q: 'SHOW CONCEPTS;' },
          { key: 'hierarchies', q: 'SHOW HIERARCHIES;' },
          { key: 'relations', q: 'SHOW RELATIONS;' },
          { key: 'rules', q: 'SHOW RULES;' },
          { key: 'functions', q: 'SHOW FUNCTIONS;' },
          { key: 'operators', q: 'SHOW OPERATORS;' }
        ];

        for (const item of queries) {
          // @ts-ignore
          const res = await get().execute(item.q, { isBackground: true });
          if (res && res.rows) {
            set((state) => ({
              metadata: { ...state.metadata, [item.key]: res.rows }
            }));
          }
        }
      } catch (err) {
        console.error("(Store) Failed to fetch KB metadata:", err);
      }
    } else {
      set((state) => ({
        metadata: {
          ...state.metadata,
          concepts: [],
          hierarchies: [],
          relations: [],
          rules: [],
          functions: [],
          operators: []
        }
      }));
    }
  },

  connect: async (host, port, user, pass, name) => {
    set({ status: 'connecting', isExecuting: true });
    try {
      // @ts-ignore
      const res = await window.kbmsApi.connect(host, port, user, pass);
      set({ isExecuting: false });
      if (res.success) {
        const details = { host, port, name };
        const creds = { host, port, user, pass, name };
        localStorage.setItem('kbms_connection_details', JSON.stringify(details));
        localStorage.setItem('kbms_last_credentials', JSON.stringify(creds));
        set({
          status: 'connected',
          connectionDetails: details,
          lastCredentials: creds
        });
        get().fetchMetadata();
      } else {
        set({ status: 'error' });
      }
      return res;
    } catch (e: any) {
      set({ status: 'error', isExecuting: false });
      return { success: false, error: e.message };
    }
  },

  setStatus: (status) => {
    const currentStatus = get().status;
    if (currentStatus === status) return;

    set({ status });
    if (status === 'disconnected') {
      // Optionally clear metadata on unexpected disconnect? 
      // User might prefer keeping it visible but "grayed out"
    }
  },

  disconnect: async () => {
    // @ts-ignore
    await window.kbmsApi.disconnect();
    set({
      status: 'disconnected',
      connectionDetails: null,
      metadata: {
        concepts: [],
        databases: [],
        users: [],
        hierarchies: [],
        relations: [],
        rules: [],
        functions: [],
        operators: []
      },
      selectedKb: ''
    });
  },

  execute: async (query, options: { isDescribe?: boolean, targetName?: string, isBackground?: boolean } = {}) => {
    const { status } = get();
    const { isDescribe = false, targetName, isBackground = false } = options;

    if (status !== 'connected') {
      set({
        isExecuting: false,
        result: [{
          headers: ["Connection Required"],
          rows: [],
          messages: [{ type: 'error', text: "Not Connected: Please connect to a KBMS server before executing queries." }]
        }],
        activeTab: 'messages',
        isConnectModalOpen: true
      });
      return;
    }

    const tab = get().tabs.find(t => t.id === get().activeTabId);
    const finalQuery = query || tab?.query || '';
    if (!finalQuery.trim()) return;

    const requestId = isBackground ? null : `req_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;

    if (!isBackground) {
      set({ isExecuting: true, activeTab: 'results', result: [], currentRequestId: requestId }); 
    } else {
      // Do not set isExecuting for background queries to keep UI clean
    }

    try {
      // @ts-ignore
      const resData = await window.kbmsApi.execute(finalQuery, isBackground, requestId);

      const res = resData || { headers: [], rows: [], messages: [{ type: 'info', text: 'No response from server' }] };

      // Update current query ID if it was returned or generated? 
      // Actually, the electron client generates it and we'll get it via fragments.
      // We can just keep it null here, or set it if resData has it.
      
      const newState: Partial<KbmsState> = {
        isExecuting: false,
        lastError: null
      };

      if (isDescribe) {
        newState.lastDescribeResult = res;
        // Cache in metadataDetails using lowercase key to avoid case issues
        const targetKey = (targetName || res.ConceptName?.replace('Describe_', ''))?.toLowerCase();
        if (targetKey) {
          console.log(`[Store] Caching metadata for key: ${targetKey}`, res);
          newState.metadataDetails = { ...get().metadataDetails, [targetKey]: res };
        } else {
          console.warn(`[Store] Could not determine key for DESCRIBE result`, res);
        }
      } else {
        // Find existing result for this batch and update it? 
        // No, execute returns one final cumulative result, but we prefer the streaming ones.
        // For compatibility, we'll store the final one as the last item or similar.
        // But better is to just mark execution as finished.
        // We rely on streaming (handleResultFragment) to populate get().result.
        // We do not append resData here to avoid duplicates.
        newState.editorMarkers = res?.editorMarkers || [];
        const hasError = res && res.messages && res.messages.some((m: any) => 
            typeof m === 'string' ? m.toLowerCase().includes('error') : (m.type === 'error' || m.type === 'Error')
        );
        const hasResultTable = res && (res.ConceptName || (res.rows && res.rows.length > 0) || (res.headers && res.headers.length > 0 && res.headers[0] !== 'Result'));
        newState.activeTab = (hasResultTable && !hasError) ? 'results' : 'messages';
      }

      set(newState);
 
      // Auto-intercept USE <KnowledgeBase>
      const useMatch = finalQuery.match(/USE\s+([a-zA-Z0-9_]+)\s*;/i);
      if (useMatch && (!res || !res.error || res.success)) {
        const kb = useMatch[1];
        set(s => {
          const dbs = s.metadata.databases.includes(kb) ? s.metadata.databases : [...s.metadata.databases, kb];
          return { selectedKb: kb, metadata: { ...s.metadata, databases: dbs } };
        });
        get().fetchMetadata();
      }
      
      return resData; // Return the data for callers like fetchMetadata
    } catch (e: any) {
      const errRes = { messages: [{ type: 'error', text: e.message || 'Error occurred' }], rows: [], headers: [] };
      set({
        isExecuting: false,
        result: [errRes]
      });
      return errRes;
    }
  },

  handleResultFragment: (f: any) => {
    // Strictly hide ALL background queries from the result/message UI
    if (f.isBackground) return; 
    
    const state = get();
    
    // Strictly filter by currentRequestId and skip background messages in ResultsPane
    if (f.isBackground) {
      console.log(`[Store] Skipping background fragment: ${f.type}`);
      return;
    }

    if (state.currentRequestId && f.requestId !== state.currentRequestId) {
      console.log(`[Store] Ignoring fragment for stale/different RequestId. Current: ${state.currentRequestId}, Fragment: ${f.requestId}`);
      return;
    }

    let resultSets = [...(state.result || [])];

    if (f.type === 'metadata') {
      resultSets.push({
        requestId: f.requestId,
        ConceptName: f.metadata.ConceptName || f.metadata.conceptName || '',
        headers: f.metadata.Columns || [],
        rows: [],
        messages: []
      });
    } else {
      if (resultSets.length === 0) {
        resultSets.push({ requestId: f.requestId, headers: [], rows: [], messages: [] });
      }
      
      // Find the MOST RECENT result set with this requestId, because batch queries 
      // can generate multiple result sets for the same requestId!
      let current;
      for (let i = resultSets.length - 1; i >= 0; i--) {
        if (resultSets[i].requestId === f.requestId) {
           current = resultSets[i];
           break;
        }
      }
      
      if (!current) {
         current = resultSets[resultSets.length - 1];
      }

      if (f.type === 'row') {
        const rowData = f.row;
        if (Array.isArray(rowData)) {
           current.rows = [...(current.rows || []), ...rowData];
        } else {
           current.rows = [...(current.rows || []), rowData];
        }
      } else if (f.type === 'result' || f.type === 'error') {
        current.messages = [...(current.messages || []), { type: f.type === 'error' ? 'error' : 'info', text: f.text || f.message || (f.type === 'error' ? 'Unknown error' : '') }];
        if (f.markers) {
          set({ editorMarkers: [...state.editorMarkers, ...f.markers] });
        }
      }
    }

    const hasTabularData = resultSets.some(r => r.headers?.length > 0);
    const shouldSwitchToMessages = f.type === 'error' || resultSets.some(r => r.messages?.some((m: any) => m.type === 'error'));
    
    set({ 
      result: resultSets, 
      activeTab: (shouldSwitchToMessages) ? 'messages' : (hasTabularData ? 'results' : 'messages') 
    });
  },
}));
