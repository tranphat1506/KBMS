import { Bell, Trash2, CheckCircle2, Info, AlertTriangle, AlertCircle, X } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';
import type { Notification } from '../store/kbmsStore';
import { useState, useRef, useEffect } from 'react';

export default function NotificationBell() {
  const { notifications, clearNotifications, removeNotification, setSelectedNotification, markAsRead } = useKbmsStore();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

// ... existing unreadCount, useEffect ...
  const unreadCount = notifications.filter(n => !n.read).length;

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div className="relative" ref={dropdownRef}>
      <button 
        onClick={() => setIsOpen(!isOpen)}
        className={`p-1.5 rounded-full transition-all relative group ${isOpen ? 'bg-[var(--brand-primary-light)]/20 text-[var(--brand-primary)]' : 'hover:bg-[var(--bg-app)] text-[var(--text-muted)] hover:text-[var(--text-main)]'}`}
      >
        <Bell className={`w-4 h-4 ${unreadCount > 0 ? 'animate-tada' : ''}`} />
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 min-w-[15px] h-[15px] bg-rose-500 text-white text-[8px] flex items-center justify-center rounded-full border border-[var(--bg-surface)] font-bold px-0.5 shadow-sm">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded-lg shadow-2xl z-[300] overflow-hidden animate-in fade-in zoom-in-95 duration-200">
          <div className="p-3 border-b border-[var(--border-muted)] flex items-center justify-between bg-[var(--bg-surface-alt)]/50">
            <h3 className="text-[10px] font-bold text-[var(--text-main)] uppercase tracking-widest">Notification Center</h3>
            {notifications.length > 0 && (
              <button 
                onClick={clearNotifications}
                className="p-1 text-[var(--text-muted)] hover:text-rose-500 transition-all rounded"
                title="Clear all"
              >
                <Trash2 className="w-3.5 h-3.5" />
              </button>
            )}
          </div>

          <div className="max-h-[400px] overflow-y-auto custom-scrollbar bg-[var(--bg-surface)]">
            {notifications.length === 0 ? (
              <div className="p-10 text-center text-[var(--text-muted)] italic text-[11px] font-thin">
                <Bell className="w-8 h-8 mx-auto mb-3 opacity-10" />
                <p>No recent activity.</p>
              </div>
            ) : (
              <div className="divide-y divide-[var(--border-muted)]">
                {notifications.map((n) => (
                  <NotificationItem 
                    key={n.id} 
                    n={n} 
                    onRemove={() => removeNotification(n.id)} 
                    onClick={() => {
                        setSelectedNotification(n);
                        markAsRead(n.id);
                        setIsOpen(false);
                    }}
                  />
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function NotificationItem({ n, onRemove, onClick }: { n: Notification, onRemove: () => void, onClick: () => void }) {
  const icons = {
    success: <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />,
    info: <Info className="w-3.5 h-3.5 text-sky-500" />,
    warn: <AlertTriangle className="w-3.5 h-3.5 text-amber-500" />,
    error: <AlertCircle className="w-3.5 h-3.5 text-rose-500" />
  };

  return (
    <div 
      className={`p-4 hover:bg-[var(--bg-app)]/50 transition-all group relative flex items-start cursor-pointer border-l-2 ${n.read ? 'border-transparent' : 'border-[var(--brand-primary)] bg-[var(--brand-primary-light)]/5'}`}
      onClick={onClick}
    >
      <div className="mt-0.5 mr-3 shrink-0">
        {icons[n.type === 'query' ? 'success' : n.severity] || icons.info}
      </div>
      <div className="flex-1 min-w-0 pr-4">
        <div className="flex items-center justify-between mb-0.5">
          <span className={`text-[9px] uppercase tracking-wider truncate ${n.read ? 'font-medium text-[var(--text-sub)]' : 'font-bold text-[var(--text-main)]'}`}>{n.title}</span>
          <span className="text-[8px] text-[var(--text-muted)] font-thin shrink-0">{new Date(n.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
        </div>
        <p className={`text-[10px] leading-relaxed break-words line-clamp-2 ${n.read ? 'text-[var(--text-muted)] font-thin' : 'text-[var(--text-sub)] font-normal'}`}>{n.message}</p>
      </div>
      <button 
        onClick={(e) => {
            e.stopPropagation();
            onRemove();
        }}
        className="absolute top-2 right-2 p-1 text-[var(--text-muted)] opacity-0 group-hover:opacity-100 hover:text-rose-500 transition-all"
      >
        <X className="w-3 h-3" />
      </button>
    </div>
  );
}
