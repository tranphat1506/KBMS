import { useKbmsStore } from '../store/kbmsStore';
import { Wifi, WifiOff, Clock, HardDrive, CheckCircle2, XCircle } from 'lucide-react';

export default function StatusBar() {
  const { status, isExecuting, result } = useKbmsStore();
  
  return (
    <div className="h-6 flex-shrink-0 bg-emerald-700 text-emerald-50 flex items-center justify-between px-3 text-[11px] font-medium tracking-wide z-30 shadow-[0_-1px_2px_rgba(0,0,0,0.1)] select-none">
      <div className="flex items-center space-x-4">
        <div className="flex items-center space-x-1.5 opacity-90 hover:opacity-100 cursor-default px-1 py-0.5 rounded hover:bg-emerald-600/50 transition-colors">
          {status === 'connected' ? <Wifi className="w-3.5 h-3.5 text-emerald-300" /> : <WifiOff className="w-3.5 h-3.5 text-red-300" />}
          <span className="uppercase">{status === 'connected' ? 'Connected' : 'Disconnected'}</span>
        </div>
        
        {status === 'connected' && (
           <div className="flex items-center space-x-1 opacity-90 bg-emerald-800/80 px-2 py-0.5 rounded border border-emerald-600/50 shadow-inner">
             <HardDrive className="w-3 h-3" />
             <span>TCP: 127.0.0.1:3307</span>
           </div>
        )}
      </div>
      
      <div className="flex items-center space-x-4 opacity-90">
        {isExecuting && (
           <div className="flex items-center space-x-1.5 animate-pulse bg-emerald-800/80 px-2 py-0.5 rounded-full border border-emerald-600/50">
             <div className="w-1.5 h-1.5 rounded-full bg-emerald-300" />
             <span>Running Query...</span>
           </div>
        )}
        {!isExecuting && result && result.length > 0 && result[result.length - 1].messages && (
           <div className="flex items-center space-x-1.5 px-2 py-0.5">
             {(result[result.length - 1].messages as any[]).some((m: any) => typeof m === 'string' ? m.includes('Error') : (m?.type === 'error' || m?.type === 'Error')) ? (
                 <XCircle className="w-3.5 h-3.5 text-red-300" />
             ) : (
                 <CheckCircle2 className="w-3.5 h-3.5 text-emerald-300" />
             )}
             <span>Query OK</span>
           </div>
        )}
        
        <div className="flex items-center space-x-1.5 px-2 hover:bg-emerald-600/50 cursor-default rounded transition-colors py-0.5">
          <Clock className="w-3 h-3" />
          <span>{result && result.length > 0 && (result[result.length - 1] as any).executionTimeMs !== undefined ? `${Number((result[result.length - 1] as any).executionTimeMs).toFixed(1)} ms` : '0.0 ms'}</span>
        </div>
        
        <div className="pl-3 border-l border-emerald-600/60 font-semibold text-emerald-200">
           KBMS Driver v1.1
        </div>
      </div>
    </div>
  );
}
