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

export interface StudioSettings {
  theme: 'dark' | 'light';
  primaryColor: string;
  fontSize: 'small' | 'medium' | 'big';
  fontWeight: 'thin' | 'regular' | 'medium';
}

export interface NotificationSetting {
  enableQuerySuccess: boolean;
  enableServerLogs: boolean;
  logLevels: string[]; // ['ERROR', 'WARN', 'INFO']
  mutedTypes: string[]; // Types to hide from toasts
}

export interface Notification {
  id: string;
  type: 'query' | 'log';
  severity: 'info' | 'warn' | 'error' | 'success';
  title: string;
  message: string;
  timestamp: number;
  read: boolean;
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
  activeSidebarView: 'explorer' | 'system';
  systemLogs: any[];
  auditLogs: any[];
  systemUsers: any[];
  systemSettings: any[];
  systemStats: any | null;
  systemSessions: any[];
  systemActiveTab: 'overview' | 'users' | 'logs' | 'settings' | 'sessions' | 'debug';
  studioSettings: StudioSettings;
  isStudioSettingsOpen: boolean;
  selectedKb: string;
  lastError: string | null;
  lastDescribeResult: any | null;
  metadataDetails: Record<string, any>;
  editorMarkers: any[];
  currentRequestId: string | null;
  notifications: Notification[];
  notificationSettings: NotificationSetting;
  toastQueue: Notification[]; // Pending toasts
  activeToasts: Notification[]; // Currently visible toasts (batch of up to 3)
  selectedNotification: Notification | null;
  setSelectedNotification: (n: Notification | null) => void;
  markAsRead: (id: string) => void;
  setSystemActiveTab: (tab: 'overview' | 'users' | 'logs' | 'settings' | 'sessions' | 'debug') => void;
  setSelectedKb: (kb: string) => void;
  setActiveSidebarView: (v: 'explorer' | 'system') => void;
  fetchSystemLogs: (filter?: any, append?: boolean) => Promise<number>;
  fetchAuditLogs: (filter?: any, append?: boolean) => Promise<number>;
  fetchSystemUsers: () => Promise<void>;
  upsertUser: (userData: any) => Promise<void>;
  deleteUser: (username: string) => Promise<void>;
  grantPermission: (username: string, kb: string, privilege: string) => Promise<void>;
  revokePermission: (username: string, kb: string) => Promise<void>;
  fetchSettings: () => Promise<void>;
  updateSetting: (name: string, value: string) => Promise<void>;
  refreshStats: () => Promise<void>;
  refreshSessions: () => Promise<void>;
  killSession: (sessionId: string) => Promise<void>;
  logTest: (level: string, message: string) => Promise<void>;
  dismissBatch: () => void;
  addNotification: (n: Omit<Notification, 'id' | 'timestamp' | 'read'>) => void;
  dismissToast: (id: string) => void;
  processNextToast: () => void;
  removeNotification: (id: string) => void;
  clearNotifications: () => void;
  updateNotificationSettings: (settings: Partial<NotificationSetting>) => void;
  subscribeLogs: () => void;
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

const loadStudioSettings = (): StudioSettings => {
  try {
    const stored = localStorage.getItem('kbms_studio_settings');
    if (stored) return JSON.parse(stored);
  } catch (e) { }
  return {
    theme: 'light',
    primaryColor: 'emerald',
    fontSize: 'medium',
    fontWeight: 'regular'
  };
};

const loadNotificationSettings = (): NotificationSetting => {
  try {
    const stored = localStorage.getItem('kbms_notification_settings');
    if (stored) return JSON.parse(stored);
  } catch (e) { }
  return {
    enableQuerySuccess: true,
    enableServerLogs: true,
    logLevels: ['ERROR', 'WARN', 'INFO'],
    mutedTypes: []
  };
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
  activeSidebarView: 'explorer',
  systemLogs: [],
  auditLogs: [],
  systemUsers: [],
  systemSettings: [],
  systemStats: null,
  systemSessions: [],
  systemActiveTab: 'overview',
  studioSettings: loadStudioSettings(),
  isStudioSettingsOpen: false,
  isConnectModalOpen: false,
  selectedKb: '',
  lastError: null,
  lastDescribeResult: null,
  editorMarkers: [],
  currentRequestId: null,
  metadataDetails: {},
  notifications: [],
  notificationSettings: loadNotificationSettings(),
  toastQueue: [],
  activeToasts: [],
  selectedNotification: null,
  setSelectedNotification: (n) => set({ selectedNotification: n }),
  markAsRead: (id) => set(state => ({
    notifications: state.notifications.map(n => n.id === id ? { ...n, read: true } : n)
  })),
  confirmDialog: {
    isOpen: false,
    title: '',
    message: ''
  },

  setSystemActiveTab: (tab) => set({ systemActiveTab: tab }),
  updateStudioSetting: (key: keyof StudioSettings, value: any) => set((state) => {
    const newSettings = { ...state.studioSettings, [key]: value };
    localStorage.setItem('kbms_studio_settings', JSON.stringify(newSettings));
    return { studioSettings: newSettings };
  }),
  setStudioSettingsOpen: (v: boolean) => set({ isStudioSettingsOpen: v }),
  setSelectedKb: (kb) => set({ selectedKb: kb }),
  setActiveSidebarView: (v) => set({ activeSidebarView: v }),
  setConnectModalOpen: (v: boolean) => set({ isConnectModalOpen: v }),
  stopExecution: () => set({ isExecuting: false }),
  clearEditorMarkers: () => set({ editorMarkers: [] }),
  showConfirm: (title, message, onConfirm, onCancel) => set({ 
    confirmDialog: { isOpen: true, title, message, onConfirm, onCancel } 
  }),
  closeConfirm: () => set((state) => ({ 
    confirmDialog: { ...state.confirmDialog, isOpen: false } 
  })),

  updateNotificationSettings: (settings) => set(state => {
    const newSettings = { ...state.notificationSettings, ...settings };
    localStorage.setItem('kbms_notification_settings', JSON.stringify(newSettings));
    return { notificationSettings: newSettings };
  }),

  addNotification: (n) => {
    const { notificationSettings } = get();
    
    // Check if notifications are disabled for this type
    if (n.type === 'query' && !notificationSettings.enableQuerySuccess) return;
    if (n.type === 'log' && !notificationSettings.enableServerLogs) return;
    if (n.type === 'log') {
       const severity = n.severity.toUpperCase();
       const allowedLevels = notificationSettings.logLevels;
       if (!allowedLevels.includes(severity) && !(severity === 'INFO' && allowedLevels.includes('Information'))) return;
    }

    const id = Date.now().toString();
    const newNotification: Notification = { ...n, id, timestamp: Date.now(), read: false };
    
    // Add to queue if not muted
    set(state => ({
      notifications: [newNotification, ...state.notifications].slice(0, 100),
      toastQueue: state.notificationSettings.mutedTypes.includes(n.type) 
        ? state.toastQueue 
        : [...state.toastQueue, newNotification]
    }));

    // Start processing queue if nothing is active
    if (get().activeToasts.length === 0) {
       get().processNextToast();
    }
  },

  processNextToast: () => {
    const { toastQueue, activeToasts } = get();
    // Only process if no toasts are currently visible and there are items in queue
    if (activeToasts.length > 0 || toastQueue.length === 0) return;

    // Take up to 3 from the queue
    const batch = toastQueue.slice(0, 3);
    
    // Play sound ONLY once for the whole batch if any are not muted
    const hasUnmuted = batch.some(n => !get().notificationSettings.mutedTypes.includes(n.type));
    if (hasUnmuted) {
      const audio = new Audio('/notification-sound.wav');
      audio.volume = 0.8;
      audio.play().catch(() => {});
    }

    set(state => ({
      activeToasts: batch,
      toastQueue: state.toastQueue.slice(batch.length)
    }));
  },

  dismissBatch: () => {
    set({ activeToasts: [] });
    setTimeout(() => {
      get().processNextToast();
    }, 1000);
  },

  dismissToast: (id) => {
    set(state => {
      const newActive = state.activeToasts.filter(t => t.id !== id);
      
      // If manually dismissing the last one, trigger next batch
      if (newActive.length === 0) {
        setTimeout(() => {
          get().processNextToast();
        }, 1000);
      }
      
      return { activeToasts: newActive };
    });
  },

  removeNotification: (id) => set(state => ({
    notifications: state.notifications.filter(n => n.id !== id)
  })),

  clearNotifications: () => set({ notifications: [] }),

  fetchSystemLogs: async (filter: any = {}, append: boolean = false) => {
    if (get().status !== 'connected') return 0;
    try {
      // @ts-ignore
      const res = await window.kbmsApi.mgmtAction('SEARCH_LOGS', { logType: 'system', ...filter });
      if (Array.isArray(res)) {
        const logs = res.map((obj: any) => obj.Values || obj.values || obj);
        set((state) => ({ 
          systemLogs: append ? [...state.systemLogs, ...logs] : logs 
        }));
        return logs.length;
      }
      return 0;
    } catch (err) {
      console.error("Failed to fetch system logs:", err);
      return 0;
    }
  },

  fetchAuditLogs: async (filter: any = {}, append: boolean = false) => {
    if (get().status !== 'connected') return 0;
    try {
      // @ts-ignore
      const res = await window.kbmsApi.mgmtAction('SEARCH_LOGS', { logType: 'audit', ...filter });
      if (Array.isArray(res)) {
        const logs = res.map((obj: any) => obj.Values || obj.values || obj);
        set((state) => ({ 
          auditLogs: append ? [...state.auditLogs, ...logs] : logs 
        }));
        return logs.length;
      }
      return 0;
    } catch (err) {
      console.error("Failed to fetch audit logs:", err);
      return 0;
    }
  },

  fetchSystemUsers: async () => {
    if (get().status !== 'connected') return;
    try {
      // @ts-ignore
      const res = await window.kbmsApi.mgmtAction('LIST_USERS');
      if (Array.isArray(res)) {
        set({ systemUsers: res });
      }
    } catch (err) {
      console.error("Failed to fetch system users:", err);
    }
  },

  upsertUser: async (userData) => {
    try {
      // @ts-ignore
      await window.kbmsApi.mgmtAction('UPSERT_USER', userData);
      get().fetchSystemUsers();
    } catch (err) {
      console.error("Failed to upsert user:", err);
    }
  },

  deleteUser: async (username) => {
    try {
      // @ts-ignore
      await window.kbmsApi.mgmtAction('DELETE_USER', { username });
      get().fetchSystemUsers();
    } catch (err) {
      console.error("Failed to delete user:", err);
    }
  },

  grantPermission: async (username, kb, privilege) => {
    try {
      // @ts-ignore
      await window.kbmsApi.mgmtAction('GRANT', { username, kb, privilege });
      get().fetchSystemUsers();
    } catch (err) {
      console.error("Failed to grant permission:", err);
    }
  },

  revokePermission: async (username, kb) => {
    try {
      // @ts-ignore
      await window.kbmsApi.mgmtAction('REVOKE', { username, kb });
      get().fetchSystemUsers();
    } catch (err) {
      console.error("Failed to revoke permission:", err);
    }
  },

  fetchSettings: async () => {
    if (get().status !== 'connected') return;
    try {
      // @ts-ignore
      const res = await window.kbmsApi.mgmtAction('GET_SETTINGS');
      if (Array.isArray(res)) {
        set({ systemSettings: res.map((obj: any) => obj.Values || obj.values || obj) });
      }
    } catch (err) {
      console.error("Failed to fetch settings:", err);
    }
  },

  updateSetting: async (name, value) => {
    try {
      // @ts-ignore
      await window.kbmsApi.mgmtAction('UPDATE_SETTING', { settingName: name, settingValue: value });
      get().fetchSettings();
    } catch (err) {
      console.error("Failed to update setting:", err);
    }
  },

  refreshStats: async () => {
    if (get().status !== 'connected') return;
    try {
      // @ts-ignore
      const stats = await window.kbmsApi.getStats();
      if (stats && stats.messages) {
          // If stats came back as a RESULT message in the queue
          const msg = stats.messages.find((m: any) => m.type === 'info');
          if (msg) {
              try { set({ systemStats: JSON.parse(msg.text) }); } catch { }
          }
      } else if (stats) {
          set({ systemStats: stats });
      }
    } catch (err) {
      console.error("Failed to refresh stats:", err);
    }
  },

  refreshSessions: async () => {
    if (get().status !== 'connected') return;
    try {
      // @ts-ignore
      const res = await window.kbmsApi.getSessions();
      if (res && res.messages) {
          const msg = res.messages.find((m: any) => m.type === 'info');
          if (msg) {
              try { set({ systemSessions: JSON.parse(msg.text) }); } catch { }
          }
      } else if (Array.isArray(res)) {
          set({ systemSessions: res });
      }
    } catch (err) {
      console.error("Failed to refresh sessions:", err);
    }
  },

  killSession: async (sessionId: string) => {
    try {
      // @ts-ignore
      const res = await window.kbmsApi.mgmtAction('KILL_SESSION', { sessionId });
      if (res && (res.success || res.success === undefined)) {
        set(state => ({
          systemSessions: state.systemSessions.filter(s => s.SessionId !== sessionId)
        }));
      }
    } catch (e) {
      console.error("(Store) Failed to kill session:", e);
    }
  },

  logTest: async (level, message) => {
    try {
      // @ts-ignore
      await window.kbmsApi.mgmtAction('LOG_TEST', { logLevel: level, password: message });
    } catch (e) {
      console.error("(Store) Failed to trigger log test:", e);
    }
  },

  subscribeLogs: () => {
    if (get().status !== 'connected') return;
    // @ts-ignore
    window.kbmsApi.subscribeLogs();
  },

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
      const resData = await window.kbmsApi.execute(finalQuery, { isBackground, requestId });

      const res = resData || { headers: [], rows: [], messages: [{ type: 'info', text: 'No response from server' }] };

      // Update current query ID if it was returned or generated? 
      // Actually, the electron client generates it and we'll get it via fragments.
      // We can just keep it null here, or set it if resData has it.
      
      const newState: Partial<KbmsState> = {
        isExecuting: false,
        lastError: null
      };

      // Cache metadata details even for background queries (used by Sidebar)
      if (isDescribe) {
        newState.lastDescribeResult = res;
        const targetKey = (targetName || res.ConceptName?.replace('Describe_', ''))?.toLowerCase();
        if (targetKey) {
          newState.metadataDetails = { ...get().metadataDetails, [targetKey]: res };
        }
      }

      if (!isBackground) {
        if (!isDescribe) {
          newState.editorMarkers = res?.editorMarkers || [];
          const hasError = res && res.messages && res.messages.some((m: any) => 
              typeof m === 'string' ? m.toLowerCase().includes('error') : (m.type === 'error' || m.type === 'Error')
          );
          const hasResultTable = res && (res.ConceptName || (res.rows && res.rows.length > 0) || (res.headers && res.headers.length > 0 && res.headers[0] !== 'Result'));
          newState.activeTab = (hasResultTable && !hasError) ? 'results' : 'messages';
        }
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
      
      // Notify on success if we are NOT in explorer view (meaning background success)
      if (!isBackground && !isDescribe && (!res || !res.error)) {
        // We only notify on success if it's NOT a background query
        // and NOT a metadata-only query (describe).
        // However, the user now says "query nào background thì không hiện", 
        // and previously said "nếu không phải ở trang đó thì mới thông báo".
        // I will remove the background notification trigger as requested.
      }
      
      return resData;
    } catch (e: any) {
      console.error(`(Store) Execution error [Background: ${isBackground}]:`, e);
      if (!isBackground) {
        const errRes = { messages: [{ type: 'error', text: e.message || 'Error occurred' }], rows: [], headers: [] };
        set({
          isExecuting: false,
          result: [errRes],
          activeTab: 'messages'
        });
      } else {
        set({ isExecuting: false });
      }
      return { success: false, error: e.message };
    }
  },

  handleResultFragment: (f: any) => {
    if (f.type === 'log-signal') {
      const log = f.data;
      if (log.type === 'SYSTEM' || log.type === 'AUDIT') {
        const data = log.data;
        const logList = log.type === 'SYSTEM' ? get().systemLogs : get().auditLogs;
        const setKey = log.type === 'SYSTEM' ? 'systemLogs' : 'auditLogs';
        
        set({ [setKey]: [data, ...logList].slice(0, 100) });
        
        // Level Mapping
        const rawLevel = data.level || 'Information';
        const mappedLevel = rawLevel === 'Information' ? 'INFO' : rawLevel === 'Warning' ? 'WARN' : rawLevel === 'Error' ? 'ERROR' : rawLevel;
        
        if (get().notificationSettings.enableServerLogs && get().notificationSettings.logLevels.includes(mappedLevel)) {
          // The user specifically asked to EXCLUDE audit logs from notifications
          // but they SHOULD still triggers for SYSTEM logs.
          if (log.type === 'SYSTEM') {
            get().addNotification({
              type: 'log',
              severity: rawLevel === 'Error' ? 'error' : rawLevel === 'Warning' ? 'warn' : 'info',
              title: `System ${rawLevel}`,
              message: data.message || 'Log entry received'
            });
          }
        }
      }
      return; 
    }

    if (f.isBackground) return; 

    const state = get();
    if (state.currentRequestId && f.requestId !== state.currentRequestId) {
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
