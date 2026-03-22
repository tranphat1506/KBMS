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
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-900/40 backdrop-blur-[3px] animate-in fade-in duration-200">
      <div className="w-[420px] bg-white rounded-xl shadow-[0_25px_70px_-15px_rgba(0,0,0,0.4)] border border-slate-200/80 overflow-hidden font-sans animate-in zoom-in-95 duration-200">
        <div className="flex items-center justify-between px-5 py-3 border-b border-slate-100 bg-slate-50/50">
          <div className="flex items-center space-x-2.5">
            <div className="w-7 h-7 bg-amber-100 rounded-lg flex items-center justify-center border border-amber-200/60 shadow-sm">
              <AlertTriangle className="w-4 h-4 text-amber-600" />
            </div>
            <h3 className="text-[14px] font-semibold text-slate-800 tracking-tight">{title || 'Confirm Action'}</h3>
          </div>
          <button onClick={handleCancel} className="text-slate-400 hover:text-slate-600 p-1 hover:bg-slate-100 rounded transition-colors cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="px-6 py-6 text-center">
          <p className="text-[13px] text-slate-600 leading-relaxed font-normal">
            {message || 'Are you sure you want to proceed?'}
          </p>
        </div>

        <div className="px-5 py-3.5 bg-slate-50 border-t border-slate-100 flex items-center justify-end space-x-2.5">
          <button 
            onClick={handleCancel}
            className="px-4 py-1.5 text-[12px] font-medium text-slate-600 hover:bg-slate-200/70 rounded-lg transition-colors cursor-pointer"
          >
            Cancel
          </button>
          <button 
            onClick={handleConfirm}
            className="px-5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white text-[12px] font-semibold rounded-lg shadow-sm hover:shadow-md transition-all focus:ring-2 focus:ring-emerald-500 focus:ring-offset-1 cursor-pointer active:scale-95"
          >
            Confirm
          </button>
        </div>
      </div>
    </div>
  );
}
