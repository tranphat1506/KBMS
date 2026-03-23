import { AlertTriangle, X } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function ConfirmDialog() {
  const { confirmDialog, closeConfirm } = useKbmsStore();
  const { isOpen, title, message, onConfirm, onCancel } = confirmDialog;

  if (!isOpen) return null;

  const handleConfirm = () => {
    if (onConfirm) onConfirm();
    closeConfirm();
  };

  const handleCancel = () => {
    if (onCancel) onCancel();
    closeConfirm();
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 backdrop-blur-[4px] animate-in fade-in duration-200 px-4">
      <div className="w-full max-w-[420px] bg-[var(--bg-surface)] rounded-xl shadow-[0_25px_70px_-15px_rgba(0,0,0,0.5)] border border-[var(--border-subtle)] overflow-hidden font-sans animate-in zoom-in-95 duration-200">
        <div className="flex items-center justify-between px-5 py-3 border-b border-[var(--border-muted)] bg-[var(--bg-app)]/50">
          <div className="flex items-center space-x-2.5">
            <div className="w-7 h-7 bg-amber-500/10 rounded-lg flex items-center justify-center border border-amber-500/20 shadow-sm">
              <AlertTriangle className="w-4 h-4 text-amber-500" />
            </div>
            <h3 className="text-[14px] font-semibold text-[var(--text-main)] tracking-tight">{title || 'Confirm Action'}</h3>
          </div>
          <button onClick={handleCancel} className="text-[var(--text-muted)] hover:text-[var(--text-main)] p-1 hover:bg-[var(--bg-surface-alt)] rounded transition-all cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="px-6 py-8 text-center bg-[var(--bg-surface)]">
          <p className="text-[13px] text-[var(--text-sub)] leading-relaxed font-thin px-4">
            {message || 'Are you sure you want to proceed?'}
          </p>
        </div>

        <div className="px-5 py-4 bg-[var(--bg-app)] border-t border-[var(--border-muted)] flex items-center justify-end space-x-3 transition-colors">
          <button 
            onClick={handleCancel}
            className="px-4 py-1.5 text-[12px] font-medium text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)] rounded-lg transition-all cursor-pointer font-thin"
          >
            Cancel
          </button>
          <button 
            onClick={handleConfirm}
            className="px-6 py-1.5 bg-[var(--brand-primary)] hover:bg-[var(--brand-primary-hover)] text-white text-[12px] font-semibold rounded-lg shadow-sm hover:shadow-emerald-500/20 transition-all focus:ring-2 focus:ring-[var(--brand-primary)] focus:ring-offset-1 cursor-pointer active:scale-95"
          >
            Confirm Action
          </button>
        </div>
      </div>
    </div>
  );
}
