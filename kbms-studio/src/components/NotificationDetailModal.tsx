import { X, Info, AlertTriangle, AlertCircle, CheckCircle2, Clock, Terminal } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function NotificationDetailModal() {
  const { selectedNotification, setSelectedNotification, markAsRead } = useKbmsStore();

  if (!selectedNotification) return null;

  const icons = {
    success: <CheckCircle2 className="w-6 h-6 text-emerald-500" />,
    info: <Info className="w-6 h-6 text-sky-500" />,
    warn: <AlertTriangle className="w-6 h-6 text-amber-500" />,
    error: <AlertCircle className="w-6 h-6 text-rose-500" />
  };

  const bgColors = {
    success: 'bg-emerald-500/5 border-emerald-500/10',
    info: 'bg-sky-500/5 border-sky-500/10',
    warn: 'bg-amber-500/5 border-amber-500/10',
    error: 'bg-rose-500/5 border-rose-500/10'
  };

  const onClose = () => {
    markAsRead(selectedNotification.id);
    setSelectedNotification(null);
  };

  return (
    <div className="fixed inset-0 z-[400] flex items-center justify-center p-4 bg-black/40 backdrop-blur-sm animate-in fade-in duration-200">
      <div 
        className="w-full max-w-lg bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded-xl shadow-2xl overflow-hidden animate-in zoom-in-95 duration-200"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`px-6 py-5 border-b border-[var(--border-subtle)] flex items-center justify-between ${bgColors[selectedNotification.severity]}`}>
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded-lg bg-[var(--bg-surface)] border border-[var(--border-subtle)] shadow-sm">
              {icons[selectedNotification.severity]}
            </div>
            <div>
              <h2 className="text-[12px] font-bold text-[var(--text-main)] uppercase tracking-widest">{selectedNotification.title}</h2>
              <div className="flex items-center space-x-2 mt-1">
                <span className={`text-[9px] font-bold px-1.5 py-0.5 rounded uppercase tracking-tighter ${selectedNotification.severity === 'error' ? 'bg-rose-500/10 text-rose-500' : 'bg-[var(--bg-app)] text-[var(--text-muted)]'}`}>
                  {selectedNotification.type} alert
                </span>
                <span className="text-[10px] text-[var(--text-muted)] flex items-center space-x-1">
                  <Clock className="w-3 h-3" />
                  <span>{new Date(selectedNotification.timestamp).toLocaleString()}</span>
                </span>
              </div>
            </div>
          </div>
          <button 
            onClick={onClose}
            className="p-2 hover:bg-black/5 rounded-full transition-all text-[var(--text-muted)] hover:text-[var(--text-main)]"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6">
          <div className="space-y-6">
            <section>
              <h3 className="text-[10px] font-bold text-[var(--text-muted)] uppercase tracking-wider mb-2 flex items-center space-x-2">
                <Terminal className="w-3.5 h-3.5" />
                <span>Message Payload</span>
              </h3>
              <div className="p-4 bg-[var(--bg-app)] border border-[var(--border-subtle)] rounded-lg">
                <p className="text-[13px] text-[var(--text-main)] font-normal leading-relaxed whitespace-pre-wrap break-words">
                  {selectedNotification.message}
                </p>
              </div>
            </section>

            {selectedNotification.type === 'log' && (
                <div className="text-[10px] text-[var(--text-muted)] font-thin italic">
                    This notification was triggered by a real-time server event.
                </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 bg-[var(--bg-app)]/30 border-t border-[var(--border-subtle)] flex justify-end items-center space-x-3">
           <button 
            onClick={onClose}
            className="px-5 py-2 bg-[var(--brand-primary)] text-white text-[11px] font-bold uppercase tracking-widest rounded-lg shadow-lg shadow-[var(--brand-primary)]/20 hover:scale-[1.02] transition-all"
          >
            Acknowledge
          </button>
        </div>
      </div>
    </div>
  );
}
