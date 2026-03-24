import { X, Info, AlertTriangle, AlertCircle, CheckCircle, Settings2, BellOff, EyeOff } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';
import type { Notification } from '../store/kbmsStore';
import { useState, useEffect, useRef } from 'react';

export default function NotificationToasts() {
  const { activeToasts, dismissToast, dismissBatch, updateNotificationSettings, notificationSettings, setSelectedNotification, markAsRead } = useKbmsStore();
  const [isHovered, setIsHovered] = useState(false);
  const timerRef = useRef<any>(null);

  useEffect(() => {
    if (activeToasts.length === 0) return;

    if (timerRef.current) clearTimeout(timerRef.current);

    if (!isHovered) {
      timerRef.current = setTimeout(() => {
        dismissBatch();
      }, 5000); 
    }

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [activeToasts, isHovered, dismissBatch]);

  if (activeToasts.length === 0) return null;

  return (
    <div className="fixed bottom-6 right-6 z-[200] flex flex-col items-end pointer-events-none gap-3">
      {activeToasts.map((toast) => (
        <div 
          key={toast.id}
          className="pointer-events-auto cursor-pointer"
          onMouseEnter={() => setIsHovered(true)}
          onMouseLeave={() => setIsHovered(false)}
          onClick={() => {
            setSelectedNotification(toast);
            markAsRead(toast.id);
            dismissToast(toast.id);
          }}
        >
          <ToastItem 
            toast={toast} 
            onDismiss={() => dismissToast(toast.id)}
            onMuteType={() => {
              const muted = [...notificationSettings.mutedTypes, toast.type];
              updateNotificationSettings({ mutedTypes: muted });
              dismissToast(toast.id);
            }}
            onDisableCategory={() => {
              if (toast.type === 'query') updateNotificationSettings({ enableQuerySuccess: false });
              if (toast.type === 'log') updateNotificationSettings({ enableServerLogs: false });
              dismissToast(toast.id);
            }}
          />
        </div>
      ))}
    </div>
  );
}

function ToastItem({ toast, onDismiss, onMuteType, onDisableCategory }: { 
  toast: Notification, 
  onDismiss: () => void,
  onMuteType: () => void,
  onDisableCategory: () => void
}) {
  const [showOptions, setShowOptions] = useState(false);

  const icons = {
    success: <CheckCircle className="w-4 h-4 text-emerald-500" />,
    info: <Info className="w-4 h-4 text-sky-500" />,
    warn: <AlertTriangle className="w-4 h-4 text-amber-500" />,
    error: <AlertCircle className="w-4 h-4 text-rose-500" />
  };

  const bgColors = {
    success: 'border-emerald-500/20 bg-emerald-500/5',
    info: 'border-sky-500/20 bg-sky-500/5',
    warn: 'border-amber-500/20 bg-amber-500/5',
    error: 'border-rose-500/20 bg-rose-500/5'
  };

  return (
    <div className={`pointer-events-auto group w-[260px] bg-[var(--bg-surface)] border ${bgColors[toast.severity]} rounded-lg shadow-2xl p-3 animate-in slide-in-from-right-full duration-300 relative overflow-hidden flex flex-col`}>
      <div className="flex items-start">
        <div className="mr-2.5 mt-0.5 shrink-0">
          {icons[toast.severity]}
        </div>
        <div className="flex-1 min-w-0 pr-5">
          <h4 className="text-[10px] font-bold text-[var(--text-main)] uppercase tracking-wider mb-0.5 truncate" title={toast.title}>{toast.title}</h4>
          <p 
            className="text-[10px] text-[var(--text-main)]/90 font-normal leading-normal line-clamp-3 break-words"
            title={toast.message}
          >
            {toast.message}
          </p>
        </div>
        <button 
          onClick={(e) => {
            e.stopPropagation();
            onDismiss();
          }}
          className="absolute top-2.5 right-2.5 p-1 text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-app)] rounded transition-all opacity-0 group-hover:opacity-100"
        >
          <X className="w-3 h-3" />
        </button>
      </div>

      <div className="mt-2.5 flex items-center justify-between pt-2.5 border-t border-[var(--border-subtle)]/30">
        <span className="text-[8px] text-[var(--text-muted)] font-mono opacity-60">
          {new Date(toast.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </span>
        <button 
          onClick={(e) => {
            e.stopPropagation();
            setShowOptions(!showOptions);
          }}
          className="flex items-center space-x-1.5 px-2 py-1 text-[9px] font-bold uppercase tracking-widest text-[var(--text-muted)] hover:text-[var(--brand-primary)] hover:bg-[var(--brand-primary-light)]/10 rounded transition-all"
        >
          <Settings2 className="w-3 h-3" />
          <span>Options</span>
        </button>
      </div>

      {showOptions && (
        <div className="mt-2 space-y-1 animate-in fade-in slide-in-from-bottom-1 duration-200">
          <button 
            onClick={(e) => {
              e.stopPropagation();
              onMuteType();
            }}
            className="w-full text-left px-2 py-1.5 flex items-center space-x-2 text-[10px] text-[var(--text-sub)] hover:bg-[var(--bg-app)] rounded transition-all group/opt"
          >
            <EyeOff className="w-3 h-3 text-[var(--text-muted)] group-hover/opt:text-amber-500" />
            <span>Mute toasts for this type</span>
          </button>
          <button 
            onClick={(e) => {
              e.stopPropagation();
              onDisableCategory();
            }}
            className="w-full text-left px-2 py-1.5 flex items-center space-x-2 text-[10px] text-rose-500/80 hover:bg-rose-500/5 rounded transition-all"
          >
            <BellOff className="w-3 h-3" />
            <span>Disable these notifications</span>
          </button>
        </div>
      )}
    </div>
  );
}
