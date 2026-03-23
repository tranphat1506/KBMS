import React, { useEffect, useState } from 'react';
import { useKbmsStore } from '../../store/kbmsStore';
import { Users, UserPlus, Trash2, Shield, Database, X, Activity, ArrowLeft } from 'lucide-react';

export default function UserManagement() {
  const { systemUsers, systemSessions, fetchSystemUsers, refreshSessions, upsertUser, deleteUser, grantPermission, revokePermission, metadata } = useKbmsStore();
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [selectedUsername, setSelectedUsername] = useState<string | null>(null);
  
  const [newUser, setNewUser] = useState({ username: '', password: '', role: 'USER' });

  useEffect(() => {
    fetchSystemUsers();
    refreshSessions();
  }, [fetchSystemUsers, refreshSessions]);

  const handleAddUser = async (e: React.FormEvent) => {
    e.preventDefault();
    await upsertUser(newUser);
    setIsAddModalOpen(false);
    setNewUser({ username: '', password: '', role: 'USER' });
  };

  const selectedUser = systemUsers.find(u => u.Username === selectedUsername);
  const userSessions = systemSessions.filter(s => s.Username === selectedUsername);

  // Detail View Component (Internal)
  // Detail View Component (Internal)
  if (selectedUser) {
    const isRoot = selectedUser.Role === 'ROOT' || selectedUser.Role === 0;

    return (
      <div className="flex flex-col h-full space-y-8 animate-in slide-in-from-right-4 duration-300">
        <div className="flex items-center justify-between shrink-0">
          <div className="flex items-center space-x-6">
            <button 
              onClick={() => setSelectedUsername(null)}
              className="p-2.5 hover:bg-[var(--bg-surface-alt)] text-[var(--text-sub)] rounded-lg transition-all border border-[var(--border-subtle)]"
            >
              <ArrowLeft className="w-5 h-5" />
            </button>
            <div>
              <h2 className="text-2xl font-bold text-[var(--text-main)] tracking-tight">{selectedUser.Username}</h2>
              <div className="flex items-center space-x-3 mt-1">
                <span className="text-[10px] font-thin text-[var(--text-muted)] tracking-widest uppercase">UUID: {selectedUser.Id}</span>
                <span className="w-1 h-1 rounded-full bg-[var(--border-subtle)]" />
                <span className="text-[10px] text-[var(--brand-primary)] font-bold uppercase tracking-[0.2em]">{isRoot ? 'ROOT ACCOUNT' : 'STANDARD USER'}</span>
              </div>
            </div>
          </div>
          <div className="flex items-center space-x-3">
             {!isRoot && (
               <button 
                 onClick={() => { if(confirm(`Delete user ${selectedUser.Username}?`)) { deleteUser(selectedUser.Username); setSelectedUsername(null); } }}
                 className="flex items-center space-x-2 px-4 py-2 text-rose-500 hover:bg-rose-500/10 rounded text-xs font-thin border border-rose-500/20 transition-all"
               >
                 <Trash2 className="w-4 h-4" />
                 <span>Delete User</span>
               </button>
             )}
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 flex-1 min-h-0 overflow-y-auto pr-1 custom-scrollbar">
          {/* Sidebar stats & Role editing */}
          <div className="lg:col-span-1 space-y-8">
            <div className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] space-y-4">
              <label className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] block">Security Role</label>
              <select 
                disabled={isRoot}
                className={`w-full px-4 py-2.5 border rounded-lg text-sm font-thin outline-none transition-all ${
                   isRoot 
                   ? 'bg-[var(--bg-app)] border-[var(--border-muted)] text-[var(--text-muted)] cursor-not-allowed'
                   : 'bg-[var(--bg-app)] border-[var(--border-subtle)] text-[var(--text-main)] focus:border-[var(--brand-primary)]'
                }`}
                value={selectedUser.Role === 0 ? 'ROOT' : 'USER'}
                onChange={(e) => upsertUser({ username: selectedUser.Username, role: e.target.value })}
              >
                <option value="USER">USER</option>
                <option value="ROOT">ROOT</option>
              </select>
              {isRoot ? (
                <p className="text-[10px] text-[var(--brand-primary)] leading-relaxed font-thin bg-[var(--brand-primary-light)]/50 p-3 rounded border border-[var(--brand-primary)]/20">
                  Primary ROOT account is protected and cannot be downgraded via management suite.
                </p>
              ) : (
                <p className="text-[10px] text-[var(--text-sub)] leading-relaxed font-thin px-1">Standard users require explicit Knowledge Base permissions for data access.</p>
              )}
            </div>

            <div className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] space-y-4">
              <label className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] block">Operational Stats</label>
              <div className="space-y-4">
                <div className="flex items-center justify-between pb-2 border-b border-[var(--border-muted)]">
                  <span className="text-xs text-[var(--text-sub)] font-thin">Active Sessions</span>
                  <span className="text-xs font-thin text-[var(--brand-primary)]">{userSessions.length}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-xs text-[var(--text-sub)] font-thin">Managed Resources</span>
                  <span className="text-xs font-thin text-[var(--brand-primary)]">{isRoot ? 'SYSTEM (*)' : Object.keys(selectedUser.KbPrivileges || {}).length}</span>
                </div>
              </div>
            </div>

            <div className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] space-y-4">
               <label className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] block">Authentication</label>
               <button className="w-full px-4 py-2.5 border border-[var(--border-subtle)] text-[var(--text-sub)] rounded-lg text-xs font-thin hover:bg-[var(--bg-app)] transition-colors">
                  Reset Password Policy
               </button>
            </div>
          </div>

          {/* Permissions & Session details */}
          <div className="lg:col-span-2 space-y-8">
            <div className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)] relative overflow-hidden">
              <div className="flex items-center space-x-2 mb-6">
                <Shield className="w-4 h-4 text-[var(--brand-primary)]" />
                <h4 className="text-sm font-bold text-[var(--text-main)] uppercase tracking-widest">Resource Access Control</h4>
              </div>
              
              {isRoot ? (
                <div className="bg-[var(--brand-primary-light)]/30 border border-[var(--brand-primary)]/20 rounded-lg p-10 text-center">
                   <h5 className="text-3xl font-thin text-[var(--brand-primary)] mb-2">*</h5>
                   <p className="text-xs font-thin text-[var(--brand-primary-text)] tracking-wide uppercase">Unrestricted System Access</p>
                   <p className="text-[10px] text-[var(--brand-primary-text)]/70 mt-3 font-thin max-w-sm mx-auto">ROOT role bypasses permission checks and is granted full ADMIN privileges to all database clusters.</p>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {metadata.databases.map(db => {
                    const currentPriv = selectedUser.KbPrivileges?.[db];
                    return (
                      <div key={db} className="bg-[var(--bg-app)]/50 p-4 rounded-lg border border-[var(--border-subtle)] flex items-center justify-between hover:border-[var(--brand-primary)]/30 transition-colors">
                        <div className="flex items-center min-w-0">
                          <Database className="w-3.5 h-3.5 text-[var(--text-muted)] mr-3 shrink-0" />
                          <span className="text-xs font-thin text-[var(--text-sub)] truncate">{db}</span>
                        </div>
                        <div className="flex items-center space-x-1.5 shrink-0">
                          {['READ', 'WRITE', 'ADMIN'].map(p => (
                            <button 
                              key={p}
                              onClick={() => currentPriv === p ? revokePermission(selectedUser.Username, db) : grantPermission(selectedUser.Username, db, p as any)}
                              className={`text-[9px] px-2 py-1 rounded font-normal border transition-all ${
                                currentPriv === p 
                                  ? 'bg-[var(--brand-primary)] text-white border-[var(--brand-primary-hover)]' 
                                  : 'bg-[var(--bg-surface)] text-[var(--text-muted)] border-[var(--border-subtle)] hover:border-[var(--text-muted)]'
                              }`}
                            >
                              {p}
                            </button>
                          ))}
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>

            <div className="bg-[var(--bg-surface)] p-6 rounded-lg border border-[var(--border-subtle)]">
              <div className="flex items-center space-x-2 mb-6">
                <Activity className="w-4 h-4 text-[var(--brand-primary)]" />
                <h4 className="text-sm font-bold text-[var(--text-main)] uppercase tracking-widest">Active Connectivity</h4>
              </div>
              {userSessions.length === 0 ? (
                <div className="py-12 text-center bg-[var(--bg-app)] rounded-lg border border-dashed border-[var(--border-muted)]">
                  <p className="text-xs text-[var(--text-muted)] font-thin italic">User agent is currently disconnected</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {userSessions.map((session, i) => (
                      <div key={i} className="flex items-center justify-between p-4 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded-lg hover:border-[var(--brand-primary)]/20 transition-colors">
                        <div className="flex flex-col">
                          <span className="text-xs font-thin text-[var(--text-sub)]">{session.IpAddress}</span>
                          <span className="text-[9px] text-[var(--text-muted)] font-thin font-mono mt-1">{session.SessionId}</span>
                        </div>
                        <div className="flex items-center space-x-6">
                          <div className="text-right">
                            <span className="text-[9px] text-[var(--text-muted)] font-thin block uppercase tracking-[0.2em] mb-1">Target KB</span>
                            <span className="text-xs font-thin text-[var(--brand-primary)]">{session.CurrentKb || 'GLOBAL'}</span>
                          </div>
                          <button 
                            onClick={() => { if(confirm('Terminate this session?')) useKbmsStore.getState().killSession(session.SessionId); }}
                            className="p-2 text-[var(--text-muted)] hover:text-rose-600 hover:bg-rose-500/10 rounded transition-all border border-transparent hover:border-rose-100"
                            title="Kill Session"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 animate-in slide-in-from-left-4 duration-300">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-xs font-bold text-[var(--text-muted)] uppercase tracking-[0.2em]">Deployment Overview</h3>
          <p className="text-[11px] text-[var(--text-sub)] font-thin mt-1 italic">Administrative interface for server access control and privilege policy management.</p>
        </div>
        <button 
          onClick={() => setIsAddModalOpen(true)}
          className="flex items-center space-x-2 px-4 py-2 bg-[var(--brand-primary)] text-white rounded hover:bg-[var(--brand-primary-hover)] transition-colors text-xs font-thin border border-[var(--brand-primary-hover)] shadow-sm"
        >
          <UserPlus className="w-4 h-4" />
          <span>Provision User</span>
        </button>
      </div>

      <div className="bg-[var(--bg-surface)] rounded-lg border border-[var(--border-subtle)] overflow-hidden mb-8 shadow-sm">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-[var(--bg-surface-alt)]/50 border-b border-[var(--border-muted)]">
              <th className="px-6 py-4 text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.3em]">Identity</th>
              <th className="px-6 py-4 text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.3em]">Access Role</th>
              <th className="px-6 py-4 text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.3em]">Database Privileges</th>
              <th className="px-6 py-4 text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.3em] text-right">Operations</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--border-muted)]">
            {systemUsers.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-6 py-16 text-center text-[var(--text-muted)] italic font-thin text-xs">
                   Registry is empty / Pending synchronization...
                </td>
              </tr>
            ) : systemUsers.map((user, i) => {
              const isRoot = user.Role === 'ROOT' || user.Role === 0;
              return (
                <tr key={i} className="hover:bg-[var(--bg-surface-alt)]/30 transition-colors group">
                  <td className="px-6 py-4">
                    <div className="flex items-center space-x-4">
                      <div className="w-9 h-9 rounded bg-[var(--bg-surface-alt)] border border-[var(--border-subtle)] flex items-center justify-center text-[var(--text-muted)] group-hover:text-[var(--brand-primary)] transition-colors">
                        <Users className="w-4 h-4" />
                      </div>
                      <div>
                        <span className="text-xs font-bold text-[var(--text-main)] block tracking-tight">{user.Username}</span>
                        <span className="text-[9px] text-[var(--text-muted)] font-thin font-mono uppercase">ID: {user.Id?.substring(0,8)}</span>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`text-[9px] font-thin px-2 py-1 rounded border tracking-widest ${
                      isRoot ? 'text-[var(--brand-primary)] border-[var(--brand-primary)]/20 bg-[var(--brand-primary-light)]/40' : 'text-[var(--text-sub)] border-[var(--border-subtle)] bg-[var(--bg-surface-alt)]/50'
                    }`}>
                      {user.Role === 0 ? 'ROOT' : 'USER'}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-wrap gap-1.5">
                      {isRoot ? (
                        <span className="text-[9px] text-[var(--brand-primary)] font-thin uppercase tracking-widest px-2 py-0.5 border border-[var(--brand-primary)]/20 rounded bg-[var(--brand-primary-light)]/20">* System-Wide</span>
                      ) : (
                        Object.entries(user.KbPrivileges || {}).map(([kb, priv]: any) => (
                          <span key={kb} className="text-[9px] text-[var(--text-sub)] font-thin px-2 py-0.5 border border-[var(--border-subtle)] rounded hover:border-[var(--brand-primary)]/30 transition-colors cursor-default">
                            {kb}:{priv[0]}
                          </span>
                        ))
                      )}
                      {!isRoot && Object.keys(user.KbPrivileges || {}).length === 0 && (
                        <span className="text-[9px] text-[var(--text-muted)] italic font-thin">Zero assignments</span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex items-center justify-end space-x-3">
                      <button 
                        onClick={() => setSelectedUsername(user.Username)}
                        className="px-4 py-1.5 text-[10px] font-thin text-[var(--text-sub)] hover:text-[var(--brand-primary)] hover:bg-[var(--bg-surface-alt)] rounded border border-[var(--border-subtle)] transition-all font-mono"
                      >
                        [ VIEW ]
                      </button>
                      {!isRoot && (
                        <button 
                          onClick={() => { if(confirm(`Delete user ${user.username}?`)) deleteUser(user.username); }}
                          className="p-2 text-[var(--text-muted)] hover:text-rose-500 transition-all opacity-0 group-hover:opacity-100"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Add User Modal */}
      {isAddModalOpen && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-[1px] z-50 flex items-center justify-center p-4 transition-all animate-in fade-in duration-200">
          <div className="bg-[var(--bg-surface)] rounded-lg w-full max-w-sm overflow-hidden animate-in zoom-in-95 duration-200 border border-[var(--border-subtle)] shadow-2xl">
            <div className="p-8">
              <div className="flex items-center justify-between mb-8">
                <h3 className="text-sm font-bold text-[var(--text-main)] uppercase tracking-[0.2em]">Register Subject</h3>
                <button onClick={() => setIsAddModalOpen(false)} className="text-[var(--text-muted)] hover:text-[var(--text-sub)] transition-colors"><X className="w-5 h-5" /></button>
              </div>
              <form onSubmit={handleAddUser} className="space-y-6">
                <div>
                  <label className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] block mb-2">Subject Handle</label>
                  <input 
                    type="text" required
                    className="w-full px-4 py-2.5 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded text-sm focus:outline-none focus:border-[var(--brand-primary)] font-thin text-[var(--text-main)] transition-all"
                    placeholder="Username..."
                    value={newUser.username} onChange={e => setNewUser({...newUser, username: e.target.value})}
                  />
                </div>
                <div>
                  <label className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] block mb-2">Primary Key</label>
                  <input 
                    type="password" required
                    className="w-full px-4 py-2.5 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded text-sm focus:outline-none focus:border-[var(--brand-primary)] font-thin text-[var(--text-main)] transition-all"
                    placeholder="Enter passphrase..."
                    value={newUser.password} onChange={e => setNewUser({...newUser, password: e.target.value})}
                  />
                </div>
                <div>
                  <label className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em] block mb-2">Entitlement Profile</label>
                  <select 
                    className="w-full px-4 py-2.5 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded text-sm focus:outline-none focus:border-[var(--brand-primary)] font-thin text-[var(--text-main)] appearance-none transition-all"
                    value={newUser.role} onChange={e => setNewUser({...newUser, role: e.target.value})}
                  >
                    <option value="USER">Standard Subject</option>
                    <option value="ROOT">Root Authority</option>
                  </select>
                </div>
                <div className="pt-4 flex space-x-4">
                  <button type="button" onClick={() => setIsAddModalOpen(false)} className="flex-1 px-4 py-2.5 border border-[var(--border-subtle)] text-[var(--text-sub)] rounded text-xs font-thin hover:bg-[var(--bg-surface-alt)] transition-colors">Abort</button>
                  <button type="submit" className="flex-1 px-4 py-2.5 bg-[var(--brand-primary)] text-white rounded text-xs font-thin hover:bg-[var(--brand-primary-hover)] transition-colors border border-[var(--brand-primary-hover)] shadow-sm">Commit Subject</button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
