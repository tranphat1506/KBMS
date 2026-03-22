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
  result: any | null;
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
  setSelectedKb: (kb: string) => void;
  setConnectModalOpen: (v: boolean) => void;
  setQuery: (query: string) => void;
  setActiveTab: (t: 'results' | 'messages') => void;
  addTab: () => void;
  removeTab: (id: string) => void;
  setActiveTabId: (id: string) => void;
  saveTab: (id: string) => Promise<void>;
  openTab: () => Promise<void>;
  execute: (query?: string, isDescribe?: boolean, targetName?: string) => Promise<void>;
  stopExecution: () => void;
  fetchMetadata: () => Promise<void>;
  changeKnowledgeBase: (kb: string) => Promise<void>;
  connect: (host: string, port: number, user: string, pass: string, name?: string) => Promise<{ success: boolean, error?: string }>;
  disconnect: () => Promise<void>;
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
          result: { messages: [{ type: 'error', text: `Connection lost. Auto-reconnect failed: ${res.error}` }] },
          activeTab: 'messages'
        });
        return;
      }
    }

    set({ isExecuting: true, selectedKb: '' }); // Reset selection during change
    // @ts-ignore
    const res = await window.kbmsApi.execute(`USE ${kb};`);
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
        result: res || { messages: [{ type: 'error', text: `Failed to use Knowledge Base: ${kb}` }] },
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
      const resKBs = await window.kbmsApi.execute("SHOW KNOWLEDGE BASES;");
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
          const res = await window.kbmsApi.execute(item.q);
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

  execute: async (query, isDescribe = false, targetName) => {
    const { status } = get();

    if (status !== 'connected') {
      set({
        isExecuting: false,
        result: {
          headers: ["Connection Required"],
          rows: [],
          messages: [{ type: 'error', text: "Not Connected: Please connect to a KBMS server before executing queries." }]
        },
        activeTab: 'messages',
        isConnectModalOpen: true
      });
      return;
    }

    const tab = get().tabs.find(t => t.id === get().activeTabId);
    const finalQuery = query || tab?.query || '';
    if (!finalQuery.trim()) return;

    set({ isExecuting: true, activeTab: 'results' });

    try {
      // @ts-ignore
      const resData = await window.kbmsApi.execute(finalQuery);

      const res = resData || { headers: [], rows: [], messages: [{ type: 'info', text: 'No response from server' }] };

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
        newState.result = res;
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
    } catch (e: any) {
      set({
        isExecuting: false,
        result: { messages: [{ type: 'error', text: e.message || 'Error occurred' }], rows: [], headers: [] }
      });
    }
  },
}));
